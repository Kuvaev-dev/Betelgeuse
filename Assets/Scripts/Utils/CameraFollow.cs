using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Стабільна orbit-камера навколо ракети.
/// Єдина модель: focus + (yaw, pitch, distance).
/// Під час drag/клавіш — без Lerp (миттєво), інакше — м'яке згладжування.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    public enum ViewMode { Follow, Overview, Manual }

    public Transform target;
    public RocketPhysics rocket;
    public TrajectoryVisualizer trajectory;
    public ViewMode mode = ViewMode.Follow;

    [Header("Focus")]
    public float bodyLookHeight = 18f;
    public float focusSmooth = 14f;

    [Header("Orbit defaults")]
    public float defaultYaw = 28f;
    public float defaultPitch = 18f;
    public float defaultDistance = 100f;
    public float minDist = 12f;
    public float maxDist = 600f;
    /// <summary>Від'ємний pitch = погляд знизу / під носій.</summary>
    public float minPitch = -35f;
    public float maxPitch = 82f;
    public float minCameraHeight = 2f;

    [Header("Follow auto")]
    public float nearDistance = 75f;
    public float farDistance = 160f;
    public float autoReturnSpeed = 1.2f;

    [Header("Overview")]
    public float overviewPadding = 1.25f;
    public float overviewMaxDistance = 2400f;
    public float overviewMinHeight = 35f;

    [Header("Input")]
    public float orbitSensitivity = 0.22f;
    /// <summary>М'який зум: частка відстані за один крок колеса (~4–6%).</summary>
    public float zoomSensitivity = 0.045f;
    public float keyOrbitSpeed = 55f;
    public bool invertY;

    [Header("Bounds")]
    public float worldBoundRadius = 4500f;
    public float worldBoundMaxY = 4200f;
    public float worldBoundMinY = 4f;
    public Vector3 worldBoundCenter = new(0f, 800f, 0f);

    [Header("Lens")]
    public float fov = 46f;
    public float overviewFov = 50f;

    // Orbit state (єдине джерело правди)
    float yaw;
    float pitch;
    float distance;
    float ovYaw, ovPitch, ovDistMul = 1f;

    Vector3 smoothFocus;
    bool focusInited;
    bool orbitDragging;
    Vector3 lastMouse;
    /// <summary>After manual orbit, don't auto-return angle until cleared (F/R).</summary>
    public bool userOrbitLock;
    Camera cam;

    // Compat fields used elsewhere / inspector
    public float manualYaw { get => yaw; set => yaw = value; }
    public float manualPitch { get => pitch; set => pitch = value; }
    public float manualDistance { get => distance; set => distance = value; }
    public Vector3 viewOffset = new(42f, 18f, -78f);
    public float followDistanceMul = 1f;

    public bool IsManual => mode == ViewMode.Manual;
    public bool IsOverview => mode == ViewMode.Overview;

    public string ModeLabelKey => mode switch
    {
        ViewMode.Overview => "cam_overview",
        ViewMode.Manual => "cam_manual",
        _ => "cam_follow"
    };

    public string ModeLabel => UILocale.CamLabel(mode);

    void Awake()
    {
        cam = GetComponent<Camera>();
        Resolve();
        ApplyCameraSettings();
        ResetOrbitDefaults();
    }

    void Start()
    {
        Resolve();
        focusInited = false;
        SnapNow();
    }

    void OnEnable()
    {
        focusInited = false;
    }

    void Update() => HandleInput();

    void LateUpdate()
    {
        Resolve();
        Vector3 targetFocus = ComputeFocus();
        if (!focusInited)
        {
            smoothFocus = targetFocus;
            focusInited = true;
        }
        else
        {
            // Focus завжди гладко йде за ракетою — це прибирає дьоргання від look-ahead
            float fk = 1f - Mathf.Exp(-focusSmooth * Time.deltaTime);
            smoothFocus = Vector3.Lerp(smoothFocus, targetFocus, fk);
        }

        bool hard = orbitDragging || Input.anyKey; // під час керування — без лагу
        // anyKey too broad - only when orbit keys
        hard = orbitDragging || IsOrbitKeyHeld();

        if (mode == ViewMode.Overview)
            PlaceOverview(hard);
        else
            PlaceOrbit(smoothFocus, yaw, pitch, distance, hard);

        if (cam != null)
        {
            float wantFov = mode == ViewMode.Overview ? overviewFov : fov;
            cam.fieldOfView = hard ? wantFov : Mathf.Lerp(cam.fieldOfView, wantFov, 1f - Mathf.Exp(-6f * Time.deltaTime));
        }
    }

    bool IsOrbitKeyHeld()
    {
        return Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D)
            || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S)
            || Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.E)
            || Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow)
            || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow)
            || Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.Minus)
            || Input.GetKey(KeyCode.KeypadPlus) || Input.GetKey(KeyCode.KeypadMinus);
    }

    void HandleInput()
    {
        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        // Mode keys (F/T/C/R) are owned by MissionControlUI to avoid double-handling
        // that could trap the camera in Overview.

        // Zoom: працює завжди в центрі екрана; біля minDist — від'їзд працює
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f && (!overUI || Input.GetKey(KeyCode.LeftControl)))
            ApplyZoom(scroll);

        if (overUI)
        {
            if (!Input.GetMouseButton(0) && !Input.GetMouseButton(1))
                orbitDragging = false;
            return;
        }

        // LMB / RMB orbit
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            orbitDragging = true;
            lastMouse = Input.mousePosition;
            userOrbitLock = true;
        }
        if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1))
        {
            if (!Input.GetMouseButton(0) && !Input.GetMouseButton(1))
                orbitDragging = false;
        }

        if (orbitDragging && (Input.GetMouseButton(0) || Input.GetMouseButton(1)))
        {
            Vector3 delta = Input.mousePosition - lastMouse;
            lastMouse = Input.mousePosition;
            float dy = delta.y * orbitSensitivity * (invertY ? 1f : -1f);
            ApplyOrbit(delta.x * orbitSensitivity, dy);
        }

        // Keys
        float k = keyOrbitSpeed * Time.unscaledDeltaTime;
        float yD = 0f, pD = 0f, zD = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.Q)) yD -= k;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.E)) yD += k;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) pD += k * 0.7f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) pD -= k * 0.7f;
        if (Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.KeypadPlus)) zD -= 1f;
        if (Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus)) zD += 1f;

        if (Mathf.Abs(yD) + Mathf.Abs(pD) + Mathf.Abs(zD) > 0.0001f)
        {
            userOrbitLock = true;
            ApplyOrbit(yD, pD);
            if (Mathf.Abs(zD) > 0.01f)
                ApplyZoom(-zD * Time.deltaTime * 12f);
        }

        // Follow auto-distance when not user-locked
        if (mode == ViewMode.Follow && !userOrbitLock && !orbitDragging)
        {
            float h = 0f;
            if (rocket != null) h = Mathf.Max(0f, rocket.state.position.y);
            float wantDist = Mathf.Lerp(nearDistance, farDistance, Mathf.Clamp01(h / 2000f));
            distance = Mathf.Lerp(distance, wantDist, 1f - Mathf.Exp(-autoReturnSpeed * Time.deltaTime));
            // М'яко повертаємо кут до default
            yaw = Mathf.LerpAngle(yaw, defaultYaw, 1f - Mathf.Exp(-autoReturnSpeed * 0.35f * Time.deltaTime));
            pitch = Mathf.Lerp(pitch, defaultPitch, 1f - Mathf.Exp(-autoReturnSpeed * 0.35f * Time.deltaTime));
        }
    }

    void ApplyZoom(float scroll)
    {
        float steps = Mathf.Clamp(scroll, -4f, 4f);
        // Additive step ∝ current distance — біля minDist крок не нульовий
        if (mode == ViewMode.Overview)
        {
            float step = Mathf.Max(0.03f, ovDistMul * zoomSensitivity);
            ovDistMul = Mathf.Clamp(ovDistMul - steps * step, 0.55f, 2.4f);
        }
        else
        {
            float step = Mathf.Max(1.2f, distance * zoomSensitivity);
            distance = Mathf.Clamp(distance - steps * step, minDist, maxDist);
            userOrbitLock = true;
        }
    }

    void ApplyOrbit(float yawDelta, float pitchDelta)
    {
        if (mode == ViewMode.Overview)
        {
            ovYaw += yawDelta;
            ovPitch = Mathf.Clamp(ovPitch + pitchDelta, -20f, 75f);
        }
        else
        {
            yaw += yawDelta;
            pitch = Mathf.Clamp(pitch + pitchDelta, minPitch, maxPitch);
        }
    }

    void PlaceOrbit(Vector3 focus, float y, float p, float dist, bool hard)
    {
        Quaternion rot = Quaternion.Euler(p, y, 0f);
        Vector3 desired = focus + rot * (Vector3.back * dist);
        // Дозволяємо погляд знизу: камера може бути нижче focus, але не під землю
        float floor = Mathf.Max(minCameraHeight, 1.5f);
        if (desired.y < floor) desired.y = floor;
        desired = ClampPoint(desired);

        if (hard)
            transform.position = desired;
        else
        {
            float t = 1f - Mathf.Exp(-18f * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desired, t);
        }

        Vector3 lookDir = focus - transform.position;
        if (lookDir.sqrMagnitude > 0.0001f)
        {
            Quaternion want = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
            if (hard)
                transform.rotation = want;
            else
            {
                float rt = 1f - Mathf.Exp(-20f * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, want, rt);
            }
        }
    }

    void PlaceOverview(bool hard)
    {
        ComputeFraming(out Vector3 center, out float radius, out Vector3 lookAt);
        radius *= overviewPadding * ovDistMul;
        Quaternion orbit = Quaternion.Euler(ovPitch, ovYaw, 0f);
        Vector3 dir = orbit * Vector3.back;
        float dist = Mathf.Clamp(radius * 1.5f, 120f, overviewMaxDistance);
        Vector3 desired = center + dir * dist;
        if (desired.y < overviewMinHeight) desired.y = overviewMinHeight;
        desired = ClampPoint(desired);

        if (hard)
        {
            transform.position = desired;
            transform.rotation = Quaternion.LookRotation((lookAt - desired).normalized, Vector3.up);
        }
        else
        {
            float t = 1f - Mathf.Exp(-8f * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desired, t);
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation((lookAt - transform.position).normalized, Vector3.up), t);
        }
    }

    Vector3 ComputeFocus()
    {
        if (rocket != null)
            return rocket.state.position + Vector3.up * bodyLookHeight;
        if (target != null)
            return target.position + Vector3.up * bodyLookHeight;
        return Vector3.up * bodyLookHeight;
    }

    public void SnapToFullTrajectoryView()
    {
        mode = ViewMode.Overview;
        userOrbitLock = false;
        ovDistMul = 1f;
        ovYaw = 40f;
        ovPitch = 28f;
        focusInited = false;
        PlaceOverview(true);
    }

    void ComputeFraming(out Vector3 center, out float radius, out Vector3 lookAt)
    {
        Vector3 min = Vector3.zero, max = Vector3.zero;
        bool any = false;
        void Enc(Vector3 p)
        {
            if (!any) { min = max = p; any = true; }
            else { min = Vector3.Min(min, p); max = Vector3.Max(max, p); }
        }
        Enc(Vector3.zero);
        if (rocket != null)
        {
            Enc(rocket.state.position);
            if (rocket.parameters != null) Enc(rocket.parameters.startPosition);
        }
        if (trajectory == null) trajectory = FindAnyObjectByType<TrajectoryVisualizer>();
        if (trajectory != null)
            foreach (var p in trajectory.Points) Enc(p);

        if (!any)
        {
            center = new Vector3(0f, 400f, 0f);
            radius = 400f;
            lookAt = center;
            return;
        }
        center = (min + max) * 0.5f;
        lookAt = center + Vector3.up * Mathf.Clamp((max.y - min.y) * 0.05f, 0f, 50f);
        radius = Mathf.Max(90f, (max - min).magnitude * 0.48f);
        radius = Mathf.Max(radius, max.y * 0.45f + 50f);
        radius = Mathf.Min(1500f, radius);
    }

    Vector3 ClampPoint(Vector3 p)
    {
        p.y = Mathf.Clamp(p.y, worldBoundMinY, worldBoundMaxY);
        Vector3 from = p - worldBoundCenter;
        float r = from.magnitude;
        if (r > worldBoundRadius && r > 0.01f)
            p = worldBoundCenter + from * (worldBoundRadius / r);
        return p;
    }

    void ResetOrbitDefaults()
    {
        yaw = defaultYaw;
        pitch = defaultPitch;
        distance = defaultDistance;
        ovYaw = 40f;
        ovPitch = 28f;
        ovDistMul = 1f;
        userOrbitLock = false;
    }

    public void SetMode(ViewMode m)
    {
        mode = m;
        if (m == ViewMode.Follow)
        {
            if (!userOrbitLock) ResetOrbitDefaults();
            SnapNow();
        }
        else if (m == ViewMode.Overview)
            SnapToFullTrajectoryView();
        else
        {
            // Manual — keep current orbit angles
            userOrbitLock = true;
            mode = ViewMode.Manual;
            SnapNow();
        }
    }

    public void EnterManualFromCurrent() => SetMode(ViewMode.Manual);

    public void ResetManualOrbit()
    {
        userOrbitLock = false;
        ResetOrbitDefaults();
        if (mode == ViewMode.Overview) SnapToFullTrajectoryView();
        else SetMode(ViewMode.Follow);
    }

    public void SnapNow()
    {
        Resolve();
        focusInited = false;
        smoothFocus = ComputeFocus();
        focusInited = true;
        if (mode == ViewMode.Overview)
            PlaceOverview(true);
        else
            PlaceOrbit(smoothFocus, yaw, pitch, distance, true);
        if (cam != null) cam.fieldOfView = mode == ViewMode.Overview ? overviewFov : fov;
    }

    void Resolve()
    {
        if (rocket == null) rocket = FindAnyObjectByType<RocketPhysics>();
        if (rocket != null) target = rocket.transform;
        if (trajectory == null) trajectory = FindAnyObjectByType<TrajectoryVisualizer>();
        if (cam == null) cam = GetComponent<Camera>();
    }

    void ApplyCameraSettings()
    {
        if (cam == null) cam = GetComponent<Camera>();
        if (cam == null) return;
        cam.farClipPlane = 12000f;
        cam.nearClipPlane = 0.3f;
        cam.fieldOfView = fov;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.06f, 0.06f, 0.07f);
        cam.allowHDR = true;
        try { if (!CompareTag("MainCamera")) tag = "MainCamera"; } catch { /* ignore */ }
    }
}
