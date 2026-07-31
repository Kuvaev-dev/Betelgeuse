using UnityEngine;

/// <summary>
/// Камера: Follow (ракета) або Overview (уся траєкторія від старту до pad).
/// </summary>
public class CameraFollow : MonoBehaviour
{
    public enum ViewMode { Follow, Overview }

    public Transform target;
    public RocketPhysics rocket;
    public TrajectoryVisualizer trajectory;
    public ViewMode mode = ViewMode.Follow;

    [Header("Follow")]
    public Vector3 viewOffset = new(42f, 18f, -78f);
    public float bodyLookHeight = 18f;
    public float fov = 46f;
    public float positionSharpness = 12f;
    public float rotationSharpness = 14f;
    public float snapDistance = 80f;
    public float velocityLookAhead = 0.35f;
    public float nearGroundScale = 0.72f;
    public float highAltScale = 1.15f;
    public float minDist = 60f;
    public float maxDist = 320f;
    public float minCameraHeight = 8f;

    [Header("Overview")]
    public float overviewFov = 55f;
    public float overviewPadding = 1.35f;

    Camera cam;
    bool snappedOnce;

    void Awake()
    {
        cam = GetComponent<Camera>();
        Resolve();
        ApplyCameraSettings();
    }

    void Start()
    {
        Resolve();
        SnapNow();
        snappedOnce = true;
    }

    void OnEnable() => snappedOnce = false;

    void LateUpdate()
    {
        Resolve();
        if (mode == ViewMode.Overview)
        {
            UpdateOverview();
            return;
        }

        if (target == null) return;

        Vector3 focus = GetFocusPoint();
        Vector3 desired = focus + ScaledOffset(focus);
        if (desired.y < minCameraHeight) desired.y = minCameraHeight;

        float lag = Vector3.Distance(transform.position, desired);
        bool shouldSnap = !snappedOnce || lag > snapDistance
                          || (rocket != null && !rocket.simulationArmed && lag > 15f);

        if (shouldSnap)
        {
            transform.position = desired;
            snappedOnce = true;
        }
        else
        {
            float k = 1f - Mathf.Exp(-positionSharpness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desired, k);
        }

        Vector3 lookPoint = focus;
        if (focus.y < 120f)
            lookPoint = Vector3.Lerp(focus, Vector3.zero, 0.12f * (1f - focus.y / 120f));

        Vector3 toLook = lookPoint - transform.position;
        if (toLook.sqrMagnitude < 0.01f) return;

        Quaternion want = Quaternion.LookRotation(toLook.normalized, Vector3.up);
        float rk = 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, want, rk);

        if (cam != null)
        {
            float h = Mathf.Max(0f, focus.y);
            float targetFov = Mathf.Lerp(50f, fov, Mathf.Clamp01(h / 400f));
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, 1f - Mathf.Exp(-4f * Time.deltaTime));
        }
    }

    void UpdateOverview()
    {
        if (trajectory == null) trajectory = FindFirstObjectByType<TrajectoryVisualizer>();
        Vector3 center;
        float radius;
        if (trajectory != null && trajectory.TryGetOverview(out center, out radius))
        {
            // nothing
        }
        else if (rocket != null)
        {
            center = rocket.state.position * 0.5f;
            radius = Mathf.Max(120f, rocket.state.position.y * 0.55f);
        }
        else return;

        radius *= overviewPadding;
        // Place camera on a diagonal so full path (high start → pad) is visible
        Vector3 dir = new Vector3(0.55f, 0.35f, -0.75f).normalized;
        Vector3 desired = center + dir * (radius * 1.6f);
        if (desired.y < 40f) desired.y = 40f;

        float k = 1f - Mathf.Exp(-6f * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desired, k);

        Quaternion want = Quaternion.LookRotation((center - transform.position).normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, want, k);

        if (cam != null)
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, overviewFov, k);
    }

    public void SetMode(ViewMode m)
    {
        mode = m;
        if (m == ViewMode.Follow) SnapNow();
        else
        {
            // immediate jump into overview
            UpdateOverview();
            transform.position = transform.position; // already set in UpdateOverview partially
            // force hard place
            if (trajectory != null && trajectory.TryGetOverview(out var c, out var r))
            {
                r *= overviewPadding;
                Vector3 dir = new Vector3(0.55f, 0.35f, -0.75f).normalized;
                transform.position = c + dir * (r * 1.6f);
                if (transform.position.y < 40f)
                {
                    var p = transform.position;
                    p.y = 40f;
                    transform.position = p;
                }
                transform.LookAt(c);
            }
        }
    }

    public void SnapNow()
    {
        Resolve();
        mode = ViewMode.Follow;
        if (target == null) return;
        Vector3 focus = GetFocusPoint();
        Vector3 desired = focus + ScaledOffset(focus);
        if (desired.y < minCameraHeight) desired.y = minCameraHeight;
        transform.position = desired;
        transform.rotation = Quaternion.LookRotation((focus - desired).normalized, Vector3.up);
        snappedOnce = true;
        if (cam != null) cam.fieldOfView = fov;
    }

    Vector3 GetFocusPoint()
    {
        Vector3 pos;
        Vector3 vel = Vector3.zero;
        if (rocket != null)
        {
            pos = rocket.state.position;
            vel = rocket.state.velocity;
        }
        else pos = target.position;

        Vector3 focus = pos + Vector3.up * bodyLookHeight;
        if (rocket != null && rocket.simulationArmed && !rocket.state.simulationFinished)
            focus += vel * velocityLookAhead;
        return focus;
    }

    Vector3 ScaledOffset(Vector3 focus)
    {
        float h = Mathf.Max(0f, focus.y - bodyLookHeight);
        float t = Mathf.Clamp01(h / 2500f);
        float scale = Mathf.Lerp(nearGroundScale, highAltScale, t);
        if (rocket != null)
        {
            float miss = new Vector2(rocket.state.position.x, rocket.state.position.z).magnitude;
            scale += Mathf.Clamp(miss / 400f, 0f, 0.35f);
        }
        Vector3 o = viewOffset * scale;
        float dist = o.magnitude;
        if (dist < minDist) o = o.normalized * minDist;
        if (dist > maxDist) o = o.normalized * maxDist;
        return o;
    }

    void Resolve()
    {
        if (rocket == null) rocket = FindFirstObjectByType<RocketPhysics>();
        if (rocket != null) target = rocket.transform;
        if (trajectory == null) trajectory = FindFirstObjectByType<TrajectoryVisualizer>();
        if (cam == null) cam = GetComponent<Camera>();
    }

    void ApplyCameraSettings()
    {
        if (cam == null) cam = GetComponent<Camera>();
        if (cam == null) return;
        cam.farClipPlane = 16000f;
        cam.nearClipPlane = 0.25f;
        cam.fieldOfView = fov;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.01f, 0.012f, 0.035f);
        cam.allowHDR = true;
        try { if (!CompareTag("MainCamera")) tag = "MainCamera"; } catch { /* ignore */ }
    }
}
