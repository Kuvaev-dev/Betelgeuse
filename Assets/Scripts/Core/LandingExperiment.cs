using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Пакетні прогони без сцени: paired Monte-Carlo порівняння A–D і оцінка політик під час навчання.
/// Потокобезпечно: кожен потік має власні <see cref="ControllerSet"/> і <see cref="LandingSimulation"/>.
/// </summary>
public static class LandingExperiment
{
    /// <summary>Seed trial i для пакета з базовим seed (однаковий для всіх алгоритмів — paired).</summary>
    public static int TrialSeed(int baseSeed, int index) => SimRng.DeriveSeed(baseSeed, index);

    /// <summary>Один прогін посадки до торкання / timeout.</summary>
    public static LandingMetrics RunTrial(in LandingSetup setup, ILandingController controller,
        in LandingScenario scenario, LandingSimulation sim = null)
    {
        sim ??= new LandingSimulation();
        sim.Controller = controller;
        sim.Reset(setup, scenario);
        return sim.RunToEnd();
    }

    /// <summary>
    /// Paired Monte-Carlo для одного алгоритму: trial i використовує ті самі збурення для A–D.
    /// </summary>
    public static LandingMetrics[] RunPaired(
        LandingSetup setup, ControllerSet template, ControlMode mode,
        DisturbanceSettings disturbances, int baseSeed, int count,
        int maxParallel = -1, Action<int> onTrialDone = null, CancellationToken cancel = default)
    {
        var results = new LandingMetrics[count];
        int done = 0;
        var po = new ParallelOptions
        {
            MaxDegreeOfParallelism = maxParallel > 0 ? maxParallel : Environment.ProcessorCount,
            CancellationToken = cancel
        };
        var local = new ThreadLocal<(ControllerSet set, LandingSimulation sim)>(
            () => (template.Clone(), new LandingSimulation()));
        try
        {
            Parallel.For(0, count, po, i =>
            {
                var (set, sim) = local.Value;
                var sc = LandingScenario.Sample(disturbances, TrialSeed(baseSeed, i));
                results[i] = RunTrial(setup, set.Get(mode), sc, sim);
                onTrialDone?.Invoke(Interlocked.Increment(ref done));
            });
        }
        catch (OperationCanceledException) { }
        finally { local.Dispose(); }
        return results;
    }

    /// <summary>
    /// Функція вартості епізоду для навчання (менше — краще):
    /// Σ (метрика/допуск)² по Vy, нахилу, промаху, Vh (кожен доданок ≤ 9) + 2 за невдачу
    /// + 0.3·частка витраченого палива; timeout = 20.
    /// </summary>
    public static float EpisodeCost(LandingMetrics m, in LandingCriteria.Limits lim, float initialFuel)
    {
        if (m == null || m.timedOut) return 20f;
        float Q(float v, float gate) => Mathf.Min(9f, (v / gate) * (v / gate));
        float c = Q(m.touchdownVelocity, lim.MaxV) + Q(m.landingAngleError, lim.MaxAngle)
                + Q(m.horizontalMiss, lim.MaxMiss) + Q(m.horizontalSpeed, lim.MaxHSpeed);
        if (!m.isSuccessfulLanding) c += 2f;
        c += 0.3f * Mathf.Clamp01(1f - m.fuelRemaining / Mathf.Max(1f, initialFuel));
        return c;
    }

    public static float SuccessRate(LandingMetrics[] list)
    {
        if (list == null || list.Length == 0) return 0f;
        int ok = 0;
        foreach (var m in list) if (m != null && m.isSuccessfulLanding) ok++;
        return (float)ok / list.Length;
    }
}
