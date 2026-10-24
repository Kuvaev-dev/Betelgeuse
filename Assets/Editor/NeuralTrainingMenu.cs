using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Меню Editor: офлайн-навчання MLP еволюційною стратегією ES(1+λ) тим самим кодом, що й headless-стенд
/// (Tools/HeadlessSim). Результат — BestWeights_Neural.json / BestWeights_Hybrid.json + *_training_log.csv
/// у корені проєкту.
/// </summary>
public static class NeuralTrainingMenu
{
    static string ProjectDir => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

    [MenuItem("Betelgeuse/Train MLP (ES 1+λ)/Neural (C)")]
    static void TrainNeural() => Train(ControlMode.Neural);

    [MenuItem("Betelgeuse/Train MLP (ES 1+λ)/Hybrid (D)")]
    static void TrainHybrid() => Train(ControlMode.Hybrid);

    [MenuItem("Betelgeuse/Train MLP (ES 1+λ)/Both")]
    static void TrainBoth()
    {
        if (Train(ControlMode.Neural)) Train(ControlMode.Hybrid);
    }

    static bool Train(ControlMode mode)
    {
        var o = EsTrainer.Options.Default(mode);
        var cts = new System.Threading.CancellationTokenSource();
        var trainer = new EsTrainer();
        MlpPolicy p;
        try
        {
            p = trainer.TrainAndSave(new ControllerSet(), o, ProjectDir, g =>
            {
                string info = $"{mode}: gen {g.Generation}/{o.Generations} · σ={g.Sigma:F3} · cost={g.ParentCost:F3}";
                if (EditorUtility.DisplayCancelableProgressBar("ES(1+λ) training", info, (float)g.Generation / o.Generations))
                    cts.Cancel();
            }, cts.Token);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
        if (p == null)
        {
            Debug.Log($"[ES] {mode}: навчання скасовано, ваги не змінено.");
            return false;
        }
        Debug.Log($"[ES] {mode}: val cost {p.baselineValidationCost:F3} → {p.validationCost:F3}, " +
                  $"val success {p.baselineValidationSuccess:P1} → {p.validationSuccess:P1}. Збережено в {ProjectDir}");
        foreach (var rp in Object.FindObjectsByType<RocketPhysics>(FindObjectsSortMode.None))
            rp.ReloadWeights();
        return true;
    }
}
