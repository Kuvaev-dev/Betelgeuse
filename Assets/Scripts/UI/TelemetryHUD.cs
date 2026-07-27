using UnityEngine;
using TMPro;

/// <summary>
/// Mission-Control телеметрія в реальному часі.
/// </summary>
public class TelemetryHUD : MonoBehaviour
{
    public RocketPhysics rocketPhysics;

    public TMP_Text heightText;
    public TMP_Text velocityText;
    public TMP_Text thrustText;
    public TMP_Text angleText;
    public TMP_Text controlModeText;

    [Header("Optional extended")]
    public TMP_Text fuelText;
    public TMP_Text missText;
    public TMP_Text statusText;

    void Update()
    {
        if (rocketPhysics == null || rocketPhysics.state == null) return;

        var s = rocketPhysics.state;
        float tilt = Vector3.Angle(s.rotation * Vector3.up, Vector3.up);
        float miss = new Vector2(s.position.x, s.position.z).magnitude;
        float thrustPct = s.maxThrust > 0f ? s.currentThrust / s.maxThrust * 100f : 0f;

        if (heightText)
        {
            heightText.text = $"ALT  {s.position.y,8:F1} m";
            heightText.color = s.position.y < 50f ? MissionControlTheme.Amber : MissionControlTheme.Text;
        }

        if (velocityText)
        {
            velocityText.text = $"VEL  {s.velocity.y,8:F1} m/s";
            float av = Mathf.Abs(s.velocity.y);
            velocityText.color = av > 20f ? MissionControlTheme.Alert
                : av > 5f ? MissionControlTheme.Amber
                : MissionControlTheme.Ok;
        }

        if (thrustText)
            thrustText.text = $"THR  {s.currentThrust / 1000f,7:F0} kN  ({thrustPct:F0}%)";

        if (angleText)
        {
            angleText.text = $"TILT {tilt,7:F1}°";
            angleText.color = tilt > 7f ? MissionControlTheme.Alert
                : tilt > 3f ? MissionControlTheme.Amber
                : MissionControlTheme.Text;
        }

        if (controlModeText)
        {
            controlModeText.text = $"MODE {rocketPhysics.GetModeDisplayName()}";
            controlModeText.color = MissionControlTheme.Cyan;
        }

        if (fuelText)
            fuelText.text = $"FUEL {s.currentFuelMass,8:F0} kg";

        if (missText)
            missText.text = $"MISS {miss,8:F1} m";

        if (statusText)
        {
            if (s.simulationFinished)
            {
                bool ok = rocketPhysics.metrics != null && rocketPhysics.metrics.isSuccessfulLanding;
                statusText.text = ok ? "STATUS  TOUCHDOWN OK" : "STATUS  LANDING FAIL";
                statusText.color = ok ? MissionControlTheme.Ok : MissionControlTheme.Alert;
            }
            else
            {
                statusText.text = "STATUS  DESCENT";
                statusText.color = MissionControlTheme.Cyan;
            }
        }
    }
}
