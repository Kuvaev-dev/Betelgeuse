using UnityEngine;

public class RocketInitializer : MonoBehaviour
{
    [Header("Початкові параметри")]
    public float startHeight = 500f;
    public Vector3 startVelocity = new Vector3(0, -50f, 0); // падає вниз

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Встановлюємо початкові умови
        transform.position = new Vector3(0, startHeight, 0);
        rb.linearVelocity = startVelocity;

        Debug.Log("✅ Ракета готова до симуляції. Висота: " + startHeight + " м");
    }

    void Update()
    {
        // Проста телеметрія в Console
        if (Time.frameCount % 60 == 0) // кожну секунду
        {
            Debug.Log($"Висота: {transform.position.y:F1} м | Швидкість: {rb.linearVelocity.y:F1} м/с");
        }
    }
}