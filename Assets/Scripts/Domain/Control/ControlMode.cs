/// <summary>
/// Алгоритм керування посадкою 1-го ступеня (A–D у UI та звітах).
/// </summary>
public enum ControlMode
{
    /// <summary>A — класичний PID (еталон).</summary>
    PID,
    /// <summary>B — нечітка логіка Sugeno-0.</summary>
    Fuzzy,
    /// <summary>C — нейромережа MLP 5→8→2, ваги навчені ES(1+λ).</summary>
    Neural,
    /// <summary>D — гібрид Neuro-Fuzzy: закон B + навчений MLP-residual.</summary>
    Hybrid
}
