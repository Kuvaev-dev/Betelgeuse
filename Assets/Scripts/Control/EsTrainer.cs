using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Офлайн-навчання ваг <see cref="MlpPolicy"/> еволюційною стратегією ES(1+λ) з правилом 1/5
/// для кроку мутації σ.
///
/// Покоління g:
///   1) вибірка міні-батчу з K навчальних сценаріїв (нові на кожному поколінні);
///   2) батько θ і λ нащадків θ + σ·N(0, I) оцінюються на ТОМУ САМОМУ батчі (спільні випадкові числа);
///   3) якщо найкращий нащадок кращий за батька — він стає батьком (елітарний відбір «1+λ»);
///   4) σ ← σ·exp(0.6·(p_s − 0.2)), де p_s — частка нащадків, кращих за батька;
///   5) кожні N поколінь батько оцінюється на фіксованій валідаційній вибірці; повертається
///      політика з найменшою валідаційною вартістю.
/// Навчальні, валідаційні та тестові (Monte-Carlo, seed 42) сценарії мають різні базові seed.
/// </summary>
public sealed class EsTrainer
{
    public struct Options
    {
        public ControlMode Mode;          // Neural (база — профіль) або Hybrid (база — нечіткий закон)
        public int Lambda;
        public int Generations;
        public int BatchSize;
        public float Sigma0;
        public float SigmaMin;
        public float SigmaMax;
        public int Seed;
        public int ValidationSize;
        public int ValidateEvery;
        public int MaxParallel;

        public static Options Default(ControlMode mode) => new Options
        {
            Mode = mode,
            Lambda = 12,
            Generations = 300,
            BatchSize = 40,
            Sigma0 = 0.2f,
            SigmaMin = 0.01f,
            SigmaMax = 0.8f,
            Seed = 20261024,
            ValidationSize = 200,
            ValidateEvery = 10,
            MaxParallel = -1
        };
    }

    public struct GenerationLog
    {
        public int Generation;
        public float Sigma;
        public float ParentCost;
        public float BestChildCost;
        public float SuccessFraction;
        public bool Improved;
        public bool Validated;
        public float ValidationCost;
        public float ValidationSuccess;
    }

    /// <summary>Розподіл навчальних умов (ширший за протокол захисту — для узагальнення).</summary>
    public static void SampleTrainingCase(System.Random rng, out LandingSetup setup, out DisturbanceSettings dist)
    {
        float U(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        setup = LandingSetup.FromInitialConditions(U(1200f, 2200f), U(45f, 80f), U(0f, 4f));
        dist = new DisturbanceSettings
        {
            WindStrength = U(0f, 18f),
            ContinuousWind = true,
            MassVariationPercent = U(0f, 8f),
            AngleVariationDeg = U(0f, 7f),
            PositionJitterMeters = U(0f, 30f),
            NavNoiseScale = rng.NextDouble() < 0.5 ? 0f : DisturbanceSettings.DefaultNavNoise
        };
    }

    struct Case
    {
        public LandingSetup Setup;
        public LandingScenario Scenario;
    }

    static Case[] MakeCases(int baseSeed, int count)
    {
        var cases = new Case[count];
        for (int k = 0; k < count; k++)
        {
            int seed = SimRng.DeriveSeed(baseSeed, k);
            var rng = new System.Random(seed);
            SampleTrainingCase(rng, out var setup, out var dist);
            cases[k] = new Case { Setup = setup, Scenario = LandingScenario.Sample(dist, seed ^ 0x5bd1e995) };
        }
        return cases;
    }

    public List<GenerationLog> Log { get; } = new List<GenerationLog>();

    /// <summary>Журнал навчання у CSV (крива навчання для звіту).</summary>
    public string LogCsv()
    {
        var ci = System.Globalization.CultureInfo.InvariantCulture;
        var sb = new System.Text.StringBuilder(
            "generation,sigma,parent_cost,best_child_cost,success_fraction,improved,validation_cost,validation_success\n");
        foreach (var g in Log)
            sb.Append(g.Generation).Append(',')
              .Append(g.Sigma.ToString("F4", ci)).Append(',')
              .Append(g.ParentCost.ToString("F4", ci)).Append(',')
              .Append(g.BestChildCost.ToString("F4", ci)).Append(',')
              .Append(g.SuccessFraction.ToString("F3", ci)).Append(',')
              .Append(g.Improved ? 1 : 0).Append(',')
              .Append(g.Validated ? g.ValidationCost.ToString("F4", ci) : "").Append(',')
              .Append(g.Validated ? g.ValidationSuccess.ToString("F4", ci) : "").Append('\n');
        return sb.ToString();
    }

    /// <summary>Навчити і зберегти ваги + журнал у каталог (спільний шлях для тренера, редактора й UI).</summary>
    public MlpPolicy TrainAndSave(ControllerSet template, Options o, string directory,
        Action<GenerationLog> onGeneration = null, CancellationToken cancel = default)
    {
        var policy = Train(template, o, onGeneration, cancel);
        if (cancel.IsCancellationRequested) return null;
        string file = o.Mode == ControlMode.Neural ? NeuralController.WeightsFileName : HybridController.WeightsFileName;
        System.IO.Directory.CreateDirectory(directory);
        policy.Save(System.IO.Path.Combine(directory, file));
        System.IO.File.WriteAllText(
            System.IO.Path.Combine(directory, System.IO.Path.GetFileNameWithoutExtension(file) + "_training_log.csv"), LogCsv());
        return policy;
    }

    /// <summary>Навчити політику для режиму o.Mode. template — налаштування базових законів.</summary>
    public MlpPolicy Train(ControllerSet template, Options o, Action<GenerationLog> onGeneration = null,
        CancellationToken cancel = default)
    {
        if (o.Mode != ControlMode.Neural && o.Mode != ControlMode.Hybrid)
            throw new ArgumentException("ES trains only Neural or Hybrid residual policies");
        Log.Clear();
        int threads = o.MaxParallel > 0 ? o.MaxParallel : Environment.ProcessorCount;
        string baseLaw = o.Mode == ControlMode.Neural ? NeuralController.BaseLaw : HybridController.BaseLaw;

        var rng = new System.Random(o.Seed);
        float Gauss()
        {
            double u1 = 1.0 - rng.NextDouble(), u2 = rng.NextDouble();
            return (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2));
        }

        // θ₀: випадкові приховані ваги, нульовий вихідний шар ⇒ стартова політика = базовий закон
        var neutral = MlpPolicy.CreateNeutral(baseLaw);
        float[] parent = neutral.GetParameters();
        int nIH = MlpPolicy.HiddenSize * MlpPolicy.InputSize, nH = MlpPolicy.HiddenSize;
        for (int i = 0; i < nIH; i++) parent[i] = 0.5f * Gauss();
        for (int i = nIH; i < nIH + nH; i++) parent[i] = 0.2f * Gauss();

        var validation = MakeCases(o.Seed + 7919, o.ValidationSize);
        EvaluateSet(template, o.Mode, new[] { parent }, validation, threads, out var baseVal, out var baseValSucc, cancel);

        float[] best = (float[])parent.Clone();
        float bestVal = baseVal[0];
        float bestValSucc = baseValSucc[0];
        float sigma = o.Sigma0;
        float lastParentCost = float.NaN;

        for (int g = 1; g <= o.Generations && !cancel.IsCancellationRequested; g++)
        {
            var batch = MakeCases(SimRng.DeriveSeed(o.Seed, 100000 + g), o.BatchSize);
            var pop = new float[o.Lambda + 1][];
            pop[0] = parent;
            for (int j = 1; j <= o.Lambda; j++)
            {
                var c = new float[parent.Length];
                for (int i = 0; i < c.Length; i++) c[i] = parent[i] + sigma * Gauss();
                pop[j] = c;
            }

            EvaluateSet(template, o.Mode, pop, batch, threads, out var cost, out _, cancel);
            if (cancel.IsCancellationRequested) break;

            int bestJ = 1, better = 0;
            for (int j = 1; j <= o.Lambda; j++)
            {
                if (cost[j] < cost[bestJ]) bestJ = j;
                if (cost[j] < cost[0]) better++;
            }
            bool improved = cost[bestJ] < cost[0];
            if (improved) parent = pop[bestJ];
            float ps = (float)better / o.Lambda;
            sigma = Mathf.Clamp(sigma * Mathf.Exp(0.6f * (ps - 0.2f)), o.SigmaMin, o.SigmaMax);
            lastParentCost = improved ? cost[bestJ] : cost[0];

            var entry = new GenerationLog
            {
                Generation = g,
                Sigma = sigma,
                ParentCost = cost[0],
                BestChildCost = cost[bestJ],
                SuccessFraction = ps,
                Improved = improved
            };

            if (g % Math.Max(1, o.ValidateEvery) == 0 || g == o.Generations)
            {
                EvaluateSet(template, o.Mode, new[] { parent }, validation, threads, out var vc, out var vs, cancel);
                entry.Validated = true;
                entry.ValidationCost = vc[0];
                entry.ValidationSuccess = vs[0];
                if (vc[0] < bestVal)
                {
                    bestVal = vc[0];
                    bestValSucc = vs[0];
                    best = (float[])parent.Clone();
                }
            }
            Log.Add(entry);
            onGeneration?.Invoke(entry);
        }

        var result = MlpPolicy.CreateNeutral(baseLaw);
        result.SetParameters(best);
        result.trainedBy = $"ES(1+{o.Lambda})";
        result.generations = Log.Count;
        result.lambda = o.Lambda;
        result.batchSize = o.BatchSize;
        result.sigmaFinal = sigma;
        result.trainCost = float.IsNaN(lastParentCost) ? 0f : lastParentCost;
        result.validationCost = bestVal;
        result.validationSuccess = bestValSucc;
        result.baselineValidationCost = baseVal[0];
        result.baselineValidationSuccess = baseValSucc[0];
        result.trainedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'");
        return result;
    }

    /// <summary>Середня вартість і частка успіхів кожного вектора параметрів на наборі сценаріїв.</summary>
    static void EvaluateSet(ControllerSet template, ControlMode mode, float[][] thetas, Case[] cases,
        int threads, out float[] meanCost, out float[] successRate, CancellationToken cancel)
    {
        int n = thetas.Length, k = cases.Length;
        var cost = new float[n * k];
        var ok = new int[n * k];
        var local = new ThreadLocal<(ControllerSet set, LandingSimulation sim)>(
            () => (template.Clone(), new LandingSimulation()));
        try
        {
            Parallel.For(0, n * k, new ParallelOptions { MaxDegreeOfParallelism = threads, CancellationToken = cancel }, idx =>
            {
                int t = idx / k, c = idx % k;
                var (set, sim) = local.Value;
                MlpPolicy policy = mode == ControlMode.Neural ? set.neural.policy : set.hybrid.policy;
                policy.SetParameters(thetas[t]);
                if (mode == ControlMode.Hybrid) set.hybrid.useNeuralResidual = true;
                var cs = cases[c];
                var m = LandingExperiment.RunTrial(cs.Setup, set.Get(mode), cs.Scenario, sim);
                cost[idx] = LandingExperiment.EpisodeCost(m, cs.Setup.Limits, sim.Setup.FuelMass * cs.Scenario.FuelScale);
                ok[idx] = m.isSuccessfulLanding ? 1 : 0;
            });
        }
        catch (OperationCanceledException) { }
        finally { local.Dispose(); }

        meanCost = new float[n];
        successRate = new float[n];
        for (int t = 0; t < n; t++)
        {
            double s = 0; int succ = 0;
            for (int c = 0; c < k; c++) { s += cost[t * k + c]; succ += ok[t * k + c]; }
            meanCost[t] = (float)(s / k);
            successRate[t] = (float)succ / k;
        }
    }
}
