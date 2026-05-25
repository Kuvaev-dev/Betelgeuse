[System.Serializable]
public class PIDController
{
    public float Kp = 0.5f;
    public float Ki = 0.1f;
    public float Kd = 0.3f;

    private float integral;
    private float previousError;

    public float Calculate(float setpoint, float currentValue, float dt)
    {
        float error = setpoint - currentValue;
        integral += error * dt;
        float derivative = (error - previousError) / dt;

        previousError = error;

        return Kp * error + Ki * integral + Kd * derivative;
    }

    public void Reset()
    {
        integral = 0f;
        previousError = 0f;
    }
}