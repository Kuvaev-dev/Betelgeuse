using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Фонове навчання MLP (Neural, потім Hybrid) з UI застосунку: ES(1+λ) у пулі потоків,
/// результат — BestWeights_*.json у каталозі проєкту. Поки триває навчання, політ і Monte-Carlo
/// використовують попередні (заморожені) ваги.
/// </summary>
public sealed class NeuralTrainingJob
{
    public bool IsRunning { get; private set; }
    public bool Finished { get; private set; }
    public bool Cancelled { get; private set; }
    public float Progress01 => totalGens > 0 ? (float)Volatile.Read(ref doneGens) / totalGens : 0f;
    public string Status { get; private set; } = "";
    public string Error { get; private set; }

    CancellationTokenSource cancel;
    int doneGens, totalGens;

    public void Start(ControllerSet template, string weightsDirectory, int generationsPerMode)
    {
        if (IsRunning) return;
        IsRunning = true;
        Finished = false;
        Cancelled = false;
        Error = null;
        doneGens = 0;
        totalGens = generationsPerMode * 2;
        cancel = new CancellationTokenSource();
        var token = cancel.Token;
        var set = template.Clone();

        Task.Run(() =>
        {
            try
            {
                foreach (var mode in new[] { ControlMode.Neural, ControlMode.Hybrid })
                {
                    var o = EsTrainer.Options.Default(mode);
                    o.Generations = generationsPerMode;
                    var p = new EsTrainer().TrainAndSave(set, o, weightsDirectory, g =>
                    {
                        Interlocked.Increment(ref doneGens);
                        if (g.Validated)
                            Status = $"{mode}: gen {g.Generation}/{o.Generations} · val cost {g.ValidationCost:F3} · σ={g.Sigma:F3}";
                        else if (string.IsNullOrEmpty(Status) || g.Generation == 1)
                            Status = $"{mode}: gen {g.Generation}/{o.Generations}";
                    }, token);
                    if (p == null) break;
                    Status = $"{mode}: val cost {p.baselineValidationCost:F3} → {p.validationCost:F3}";
                }
            }
            catch (System.Exception e)
            {
                Error = e.Message;
            }
            finally
            {
                Cancelled = token.IsCancellationRequested;
                IsRunning = false;
                Finished = true;
            }
        });
    }

    public void Cancel() => cancel?.Cancel();

    /// <summary>Позначити результат як оброблений (UI вже перезавантажив ваги).</summary>
    public void Acknowledge() => Finished = false;
}
