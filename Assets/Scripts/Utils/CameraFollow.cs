using UnityEngine;

/// <summary>
/// Плавне слідкування камери за ракетою.
/// Забезпечує зручний огляд польоту з фіксованої відносної позиції.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Ціль для слідкування")]
    public Transform target;

    [Header("Налаштування камери")]
    [Tooltip("Зміщення камери відносно цілі")]
    public Vector3 offset = new Vector3(0, 80f, -150f);

    [Tooltip("Швидкість плавного слідкування")]
    public float smoothSpeed = 2f;

    void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        transform.position = smoothedPosition;
        transform.LookAt(target);
    }
}