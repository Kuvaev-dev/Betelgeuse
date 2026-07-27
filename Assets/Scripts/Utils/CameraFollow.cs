using UnityEngine;

/// <summary>
/// Камера chase: тримає ракету в кадрі на всьому діапазоні висот 0…3000 м.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 baseOffset = new(55f, 35f, -95f);
    public float smooth = 3.5f;
    public float lookHeight = 18f;
    public float minDistance = 40f;
    public float maxDistance = 420f;

    RocketPhysics rocket;

    void Start()
    {
        if (target == null)
        {
            rocket = FindFirstObjectByType<RocketPhysics>();
            if (rocket != null) target = rocket.transform;
        }
        else rocket = target.GetComponent<RocketPhysics>();

        // Tag as main
        try { if (!CompareTag("MainCamera")) tag = "MainCamera"; } catch { /* ignore */ }

        var cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.farClipPlane = 12000f;
            cam.fieldOfView = 48f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.42f, 0.6f, 0.82f);
        }

        // Snap once so first frame isn't empty
        if (target != null)
            transform.position = target.position + ScaledOffset();
    }

    void LateUpdate()
    {
        if (target == null)
        {
            if (rocket == null) rocket = FindFirstObjectByType<RocketPhysics>();
            if (rocket != null) target = rocket.transform;
            if (target == null) return;
        }

        Vector3 offset = ScaledOffset();
        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-smooth * Time.deltaTime));

        Vector3 look = target.position + Vector3.up * lookHeight;
        // Also peek toward landing pad when high
        if (target.position.y > 200f)
            look = Vector3.Lerp(look, new Vector3(0f, 0f, 0f), 0.12f);

        Quaternion rot = Quaternion.LookRotation(look - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, 1f - Mathf.Exp(-smooth * Time.deltaTime));
    }

    Vector3 ScaledOffset()
    {
        float h = target != null ? Mathf.Max(0f, target.position.y) : 100f;
        // Closer near ground, pull back at altitude
        float k = Mathf.Lerp(0.55f, 1.35f, Mathf.Clamp01(h / 2500f));
        // Extra pull-back based on speed
        float spd = 0f;
        if (rocket != null) spd = rocket.state.velocity.magnitude;
        k += Mathf.Clamp(spd / 200f, 0f, 0.4f);

        Vector3 o = baseOffset * k;
        float dist = o.magnitude;
        if (dist < minDistance) o = o.normalized * minDistance;
        if (dist > maxDistance) o = o.normalized * maxDistance;
        // Keep camera above ground
        if (target != null && (target.position.y + o.y) < 8f)
            o.y = 8f - target.position.y + 15f;
        return o;
    }
}
