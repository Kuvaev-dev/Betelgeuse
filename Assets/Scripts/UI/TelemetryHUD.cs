using UnityEngine;
using TMPro;

public class TelemetryHUD : MonoBehaviour
{
    public RocketPhysics rocketPhysics;

    public TMP_Text heightText;
    public TMP_Text velocityText;
    public TMP_Text thrustText;
    public TMP_Text angleText;
    public TMP_Text controlModeText;

    void Update()
    {
        if (rocketPhysics == null || rocketPhysics.state == null)
            return;

        var s = rocketPhysics.state;

        if (heightText)
            heightText.text =
                $"Висота: {s.position.y:F1} м";

        if (velocityText)
            velocityText.text =
                $"Швидкість: {s.velocity.y:F1} м/с";

        if (thrustText)
            thrustText.text =
                $"Тяга: {(s.currentThrust / 1000f):F0} кН";

        if (angleText)
            angleText.text =
                $"Нахил: {Vector3.Angle(s.rotation * Vector3.up, Vector3.up):F1}°";

        if (controlModeText)
            controlModeText.text =
                $"Режим: {rocketPhysics.controlMode}";
    }
}