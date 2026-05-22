using UnityEngine;

public class RocketController : MonoBehaviour
{
    public float thrustPower = 10f;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Debug.Log("RocketController запущено!");
    }

    void Update()
    {
        // Підйом при натисканні Space
        if (Input.GetKey(KeyCode.Space))
        {
            rb.AddForce(Vector3.up * thrustPower);
        }

        // Поворот стрілками
        float rotateSpeed = 50f;
        if (Input.GetKey(KeyCode.LeftArrow))
            transform.Rotate(Vector3.forward * rotateSpeed * Time.deltaTime);
        if (Input.GetKey(KeyCode.RightArrow))
            transform.Rotate(Vector3.back * rotateSpeed * Time.deltaTime);
    }
}