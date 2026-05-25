using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Ціль для слідкування")]
    public Transform target;

    [Header("Налаштування камери")]
    public Vector3 offset = new Vector3(0, 80f, -150f);
    public float smoothSpeed = 2f;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        transform.position = smoothedPosition;
        transform.LookAt(target);
    }
}