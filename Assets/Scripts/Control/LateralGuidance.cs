using UnityEngine;

/// <summary>
/// Спільне бічне наведення до центру pad (однакове для A–D).
/// Зовнішній контур каскаду: PID по горизонтальній позиції/швидкості дає бажане горизонтальне
/// прискорення, яке перетворюється на бажану вісь корпусу (нахил ≤ MaxTiltDeg).
/// Внутрішній контур (орієнтація TVC) — у стратегії керування.
/// Інтеграл компенсує сталий знос вітром; у термінальній зоні позиційний канал вимикається,
/// лишається гасіння бічної швидкості; нижче UprightHeight корпус вертикальний.
/// </summary>
[System.Serializable]
public sealed class LateralGuidance
{
    public float Kp = 0.05f;
    public float Ki = 0.002f;
    public float Kv = 0.7f;
    public float MaxTiltDeg = 12f;
    public float TerminalHeight = 40f;
    public float TerminalMaxTiltDeg = 3f;
    public float UprightHeight = 6f;
    public float IntegralLimit = 600f;

    Vector2 integral;

    public void Reset() => integral = Vector2.zero;

    /// <param name="scale">Масштаб команди (1 — спільний закон; MLP у режимах C/D адаптує його).</param>
    /// <param name="accelUp">Поточне питоме прискорення від тяги вздовж вертикалі, м/с².</param>
    public Vector3 DesiredUp(float height, Vector3 position, Vector3 velocity,
        float scale, float accelUp, float dt)
    {
        if (height < UprightHeight) return Vector3.up;

        // 1 вище термінальної зони, 0 нижче половини її висоти
        float posW = Mathf.Clamp01((height - TerminalHeight * 0.5f) / (TerminalHeight * 0.5f));
        Vector2 p = new Vector2(position.x, position.z);
        Vector2 v = new Vector2(velocity.x, velocity.z);
        if (posW > 0f)
            integral = Vector2.ClampMagnitude(integral + p * (dt * posW), IntegralLimit);

        Vector2 a = -(p * (Kp * posW) + integral * (Ki * posW) + v * Kv) * Mathf.Max(0f, scale);

        float tiltLim = Mathf.Lerp(TerminalMaxTiltDeg, MaxTiltDeg, posW);
        float up = Mathf.Max(accelUp, 1f);
        float aMax = up * Mathf.Tan(tiltLim * Mathf.Deg2Rad);
        a = Vector2.ClampMagnitude(a, aMax);
        return new Vector3(a.x, up, a.y).normalized;
    }
}
