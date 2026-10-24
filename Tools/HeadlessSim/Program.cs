// Headless-стенд Betelgeuse: ті самі файли ядра (Assets/Scripts/Core, Control, Domain),
// зібрані поза Unity. Команди:
//   train  --mode neural|hybrid [--gens N --lambda L --batch K --seed S] [--out DIR]
//   mc     [--mode all|pid|fuzzy|neural|hybrid] [--n 15 --seed 42 --wind 5 --mass 5 --angle 4 --jitter 10
//           --h 1600 --vy 60 --tilt 2 --nav 0.35 --residual on|off] [--weights DIR]
//   ideal  [--weights DIR]       — чисті ПУ (Ideal [I]) для A–D
//   trace  --mode X [...]         — траєкторія одного trial по секундах
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

static class Program
{
    static Dictionary<string, string> A = new Dictionary<string, string>();
    static string Get(string k, string d) => A.TryGetValue(k, out var v) ? v : d;
    static float F(string k, float d) => float.Parse(Get(k, d.ToString(CultureInfo.InvariantCulture)), CultureInfo.InvariantCulture);
    static int I(string k, int d) => int.Parse(Get(k, d.ToString()));

    static int Main(string[] args)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        Console.OutputEncoding = Encoding.UTF8;
        if (args.Length == 0) { Console.WriteLine("commands: train | mc | ideal | trace"); return 1; }
        for (int i = 1; i < args.Length; i++)
            if (args[i].StartsWith("--")) A[args[i].Substring(2)] = i + 1 < args.Length && !args[i + 1].StartsWith("--") ? args[++i] : "1";
        UnityEngine.Debug.Quiet = true;
        string weights = Get("weights", Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..")));
        switch (args[0])
        {
            case "train": return Train(weights);
            case "mc": return Mc(weights);
            case "ideal": return Ideal(weights);
            case "trace": return Trace(weights);
            case "sweep": return Sweep(weights);
            case "robustness": return Robustness(weights);
            case "selftest": return SelfTest();
        }
        Console.WriteLine("unknown command");
        return 1;
    }

    static ControllerSet MakeSet(string weightsDir)
    {
        var set = new ControllerSet();
        if (Get("noweights", "0") != "1")
            Console.WriteLine("weights: " + set.LoadWeights(weightsDir));
        set.hybrid.useNeuralResidual = Get("residual", "on") != "off";
        return set;
    }

    static ControlMode[] Modes()
    {
        string m = Get("mode", "all").ToLowerInvariant();
        if (m == "all") return new[] { ControlMode.PID, ControlMode.Fuzzy, ControlMode.Neural, ControlMode.Hybrid };
        return m.Split(',').Select(Parse).ToArray();
    }

    static ControlMode Parse(string s) => s.ToLowerInvariant() switch
    {
        "pid" => ControlMode.PID, "fuzzy" => ControlMode.Fuzzy, "neural" => ControlMode.Neural, "hybrid" => ControlMode.Hybrid,
        _ => throw new ArgumentException(s)
    };

    static DisturbanceSettings Dist() => new DisturbanceSettings
    {
        WindStrength = F("wind", 5f),
        ContinuousWind = true,
        MassVariationPercent = F("mass", 5f),
        AngleVariationDeg = F("angle", 4f),
        PositionJitterMeters = F("jitter", 10f),
        NavNoiseScale = F("nav", DisturbanceSettings.DefaultNavNoise)
    };

    static void PrintHeader() =>
        Console.WriteLine($"{"mode",-8} {"succ%",6} {"Vy",6} {"tilt",6} {"miss",6} {"Vh",6} {"fuel",7} {"t",6} {"TO",3} {"score",6} {"cost",6}  fails(V/A/M/H)");

    static void PrintStats(string name, LandingMetrics[] r, in LandingSetup setup)
    {
        float Avg(Func<LandingMetrics, float> f) => r.Average(f);
        int to = r.Count(m => m.timedOut);
        var lim = setup.Limits;
        int fv = r.Count(m => m.touchdownVelocity >= lim.MaxV), fa = r.Count(m => m.landingAngleError >= lim.MaxAngle);
        int fm = r.Count(m => m.horizontalMiss >= lim.MaxMiss), fh = r.Count(m => m.horizontalSpeed >= lim.MaxHSpeed);
        float fuel0 = setup.FuelMass;
        float cost = r.Average(m => LandingExperiment.EpisodeCost(m, lim, fuel0));
        Console.WriteLine($"{name,-8} {LandingExperiment.SuccessRate(r) * 100f,6:F1} {Avg(m => m.touchdownVelocity),6:F2} {Avg(m => m.landingAngleError),6:F2} " +
                          $"{Avg(m => m.horizontalMiss),6:F1} {Avg(m => m.horizontalSpeed),6:F2} {Avg(m => m.fuelRemaining),7:F0} {Avg(m => m.totalFlightTime),6:F1} {to,3} " +
                          $"{Avg(m => m.SuccessScore),6:F1} {cost,6:F2}  {fv}/{fa}/{fm}/{fh}");
    }

    static int Mc(string weights)
    {
        var set = MakeSet(weights);
        var setup = LandingSetup.FromInitialConditions(F("h", 1600f), F("vy", 60f), F("tilt", 2f));
        var d = Dist();
        int n = I("n", 15), seed = I("seed", 42);
        Console.WriteLine($"MC paired: n={n} seed={seed} wind={d.WindStrength} mass={d.MassVariationPercent}% angle={d.AngleVariationDeg} jitter={d.PositionJitterMeters} nav={d.NavNoiseScale} h0={setup.StartPosition.y} vy0={setup.StartVelocity.y} residual={set.hybrid.useNeuralResidual}");
        PrintHeader();
        var sw = Stopwatch.StartNew();
        foreach (var mode in Modes())
        {
            var r = LandingExperiment.RunPaired(setup, set, mode, d, seed, n);
            PrintStats(mode.ToString(), r, setup);
            if (Get("fails", "0") == "1")
                for (int i = 0; i < r.Length; i++)
                    if (!r[i].isSuccessfulLanding)
                        Console.WriteLine($"   #{i}: Vy={r[i].touchdownVelocity:F2} tilt={r[i].landingAngleError:F2} miss={r[i].horizontalMiss:F1} Vh={r[i].horizontalSpeed:F2} TO={r[i].timedOut} fuel={r[i].fuelRemaining:F0}");
        }
        Console.WriteLine($"elapsed {sw.Elapsed.TotalSeconds:F1}s");
        return 0;
    }

    static int Ideal(string weights)
    {
        var set = MakeSet(weights);
        var setup = LandingSetup.FromInitialConditions(F("h", 1400f), F("vy", 48f), F("tilt", 0f));
        PrintHeader();
        foreach (var mode in Modes())
        {
            var m = LandingExperiment.RunTrial(setup, set.Get(mode), LandingScenario.Nominal(1));
            PrintStats(mode.ToString(), new[] { m }, setup);
        }
        return 0;
    }

    static int Trace(string weights)
    {
        var set = MakeSet(weights);
        var setup = LandingSetup.FromInitialConditions(F("h", 1600f), F("vy", 60f), F("tilt", 2f));
        var mode = Parse(Get("mode", "pid"));
        var sc = LandingScenario.Sample(Dist(), LandingExperiment.TrialSeed(I("seed", 42), I("trial", 0)));
        var sim = new LandingSimulation { Controller = set.Get(mode) };
        sim.Reset(setup, sc);
        float next = 0f, every = F("every", 2f);
        Console.WriteLine($"wind={sc.Wind} off=({sc.OffsetX:F1},{sc.OffsetZ:F1}) tilt0=({sc.TiltXDeg:F1},{sc.TiltZDeg:F1})");
        Console.WriteLine("    t      h      vy     x      z     vx     vz   tilt  dTilt   thrust/mg  gx    gz   lat  fuel");
        while (!sim.Finished)
        {
            var s = sim.State;
            if (s.time >= next || s.position.y < 3f && s.time >= next - every + 0.25f)
            {
                next = s.time + every;
                float tilt = UnityEngine.Vector3.Angle(s.rotation * UnityEngine.Vector3.up, UnityEngine.Vector3.up);
                float dTilt = UnityEngine.Vector3.Angle(sim.LastDesiredUp, UnityEngine.Vector3.up);
                float g = AtmosphereModel.GetGravity(s.position.y);
                var c = sim.LastCommand;
                Console.WriteLine($"{s.time,6:F1} {s.position.y,7:F1} {s.velocity.y,6:F1} {s.position.x,6:F1} {s.position.z,6:F1} {s.velocity.x,6:F2} {s.velocity.z,6:F2} {tilt,5:F1} {dTilt,5:F1}   {s.currentThrust / (s.TotalMass * g),6:F2}   {c.GimbalEuler.x,5:F1} {c.GimbalEuler.z,5:F1} {c.LateralScale,4:F2} {s.currentFuelMass,6:F0}");
            }
            sim.Step();
        }
        var m = sim.Metrics;
        Console.WriteLine($"END Vy={m.touchdownVelocity:F2} tilt={m.landingAngleError:F2} miss={m.horizontalMiss:F1} Vh={m.horizontalSpeed:F2} TO={m.timedOut} ok={m.isSuccessfulLanding} t={m.totalFlightTime:F1}");
        return 0;
    }

    // Швидка перевірка ключових інваріантів (дзеркало EditMode-тестів) без Unity.
    static int SelfTest()
    {
        int fails = 0;
        void Check(bool ok, string name) { Console.WriteLine((ok ? "PASS " : "FAIL ") + name); if (!ok) fails++; }
        var nominal = LandingSetup.FromInitialConditions(1400f, 48f, 0f);
        foreach (var mode in new[] { ControlMode.PID, ControlMode.Fuzzy, ControlMode.Neural, ControlMode.Hybrid })
        {
            var m = LandingExperiment.RunTrial(nominal, new ControllerSet().Get(mode), LandingScenario.Nominal());
            Check(m.isSuccessfulLanding && !m.timedOut, $"nominal {mode}");
            var setup = LandingSetup.FromInitialConditions(1500f, 50f, 0f);
            var sc = LandingScenario.Nominal(3); sc.TiltXDeg = 8f; sc.TiltZDeg = -6f;
            var sim = new LandingSimulation { Controller = new ControllerSet().Get(mode) };
            sim.Reset(setup, sc);
            float t0 = UnityEngine.Vector3.Angle(sim.State.rotation * UnityEngine.Vector3.up, UnityEngine.Vector3.up);
            for (int i = 0; i < 1600; i++) sim.Step();
            float t1 = UnityEngine.Vector3.Angle(sim.State.rotation * UnityEngine.Vector3.up, UnityEngine.Vector3.up);
            Check(t1 < t0 * 0.6f, $"restore tilt {mode}: {t0:F1} -> {t1:F1}");
        }
        var s2 = nominal; s2.MaxTime = 2f;
        var mt = LandingExperiment.RunTrial(s2, new ControllerSet().Get(ControlMode.Fuzzy), LandingScenario.Nominal());
        Check(mt.timedOut && !mt.isSuccessfulLanding && mt.SuccessScore == 0f, "timeout");
        var set = new ControllerSet(); set.hybrid.useNeuralResidual = false;
        var d = new DisturbanceSettings { WindStrength = 6f, ContinuousWind = true, MassVariationPercent = 5f, AngleVariationDeg = 4f, PositionJitterMeters = 10f, NavNoiseScale = 0.35f };
        var sc7 = LandingScenario.Sample(d, 7);
        var mf = LandingExperiment.RunTrial(LandingSetup.FromInitialConditions(1600, 60, 2), set.Get(ControlMode.Fuzzy), sc7);
        var mh = LandingExperiment.RunTrial(LandingSetup.FromInitialConditions(1600, 60, 2), set.Get(ControlMode.Hybrid), sc7);
        Check(mf.horizontalMiss == mh.horizontalMiss && mf.totalFlightTime == mh.totalFlightTime, "hybrid residual OFF == fuzzy");
        var r1 = LandingExperiment.RunPaired(LandingSetup.Default, new ControllerSet(), ControlMode.Hybrid, d, 42, 6, 1);
        var r2 = LandingExperiment.RunPaired(LandingSetup.Default, new ControllerSet(), ControlMode.Hybrid, d, 42, 6, 4);
        Check(r1.Zip(r2, (a, b) => a.horizontalMiss == b.horizontalMiss).All(x => x), "paired determinism");
        var sim2 = new LandingSimulation { Controller = new ControllerSet().Get(ControlMode.PID) };
        var sc20 = LandingScenario.Nominal(); sc20.TiltXDeg = 20f;
        sim2.Reset(nominal, sc20);
        var prev = sim2.State.thrustDirection; bool rl = true;
        for (int i = 0; i < 200; i++) { sim2.Step(); rl &= UnityEngine.Vector3.Angle(prev, sim2.State.thrustDirection) <= 12f * 0.005f + 0.05f; prev = sim2.State.thrustDirection; }
        Check(rl, "TVC rate limit");
        var o = EsTrainer.Options.Default(ControlMode.Hybrid); o.Generations = 3; o.Lambda = 3; o.BatchSize = 3; o.ValidationSize = 4; o.ValidateEvery = 1; o.Seed = 11;
        var pa = new EsTrainer().Train(new ControllerSet(), o); var pb = new EsTrainer().Train(new ControllerSet(), o);
        Check(pa.validationCost <= pa.baselineValidationCost + 1e-6f && pa.GetParameters().SequenceEqual(pb.GetParameters()), "ES tiny determinism / no worse");
        var js = pa.ToJson(); Check(MlpPolicy.TryFromJson(js, out var q) && q.GetParameters().SequenceEqual(pa.GetParameters()), "json roundtrip");
        foreach (ControlMode mode in Enum.GetValues(typeof(ControlMode)))
        {
            var dp = new DisturbanceSettings { WindStrength = 5, ContinuousWind = true, MassVariationPercent = 5, AngleVariationDeg = 4, PositionJitterMeters = 10, NavNoiseScale = 0.35f };
            var r = LandingExperiment.RunPaired(LandingSetup.FromInitialConditions(1600, 60, 2), new ControllerSet(), mode, dp, 42, 10);
            Check(LandingExperiment.SuccessRate(r) >= 0.8f, $"protocol >=80% {mode}");
        }
        Console.WriteLine(fails == 0 ? "ALL PASS" : $"{fails} FAILED");
        return fails;
    }

    // Крива робастності: успішність A–D залежно від вітру (paired, однакові інші збурення). CSV для звіту.
    static int Robustness(string weights)
    {
        var set = MakeSet(weights);
        var setup = LandingSetup.FromInitialConditions(F("h", 1600f), F("vy", 60f), F("tilt", 2f));
        int n = I("n", 200), seed = I("seed", 42);
        var winds = Get("winds", "0,4,8,10,12,14,16,18").Split(',').Select(s => float.Parse(s, CultureInfo.InvariantCulture)).ToArray();
        var modes = Modes();
        var csv = new StringBuilder("wind_mps," + string.Join(",", modes.Select(m => m + "_success," + m + "_miss," + m + "_vh")) + "\n");
        Console.WriteLine($"robustness n={n} seed={seed} mass={F("mass", 5f)}% angle={F("angle", 4f)} jitter={F("jitter", 10f)} nav={F("nav", 0.35f)} residual={set.hybrid.useNeuralResidual}");
        Console.WriteLine("wind  " + string.Join("  ", modes.Select(m => $"{m,-14}")));
        foreach (var w in winds)
        {
            var d = Dist();
            d.WindStrength = w;
            var line = new StringBuilder($"{w,4:F0}  ");
            csv.Append(w.ToString(CultureInfo.InvariantCulture));
            foreach (var mode in modes)
            {
                var r = LandingExperiment.RunPaired(setup, set, mode, d, seed, n);
                float s = LandingExperiment.SuccessRate(r) * 100f;
                line.Append($"{s,5:F1}% {r.Average(m => m.horizontalMiss),5:F1}m  ");
                csv.Append($",{s:F1},{r.Average(m => m.horizontalMiss):F2},{r.Average(m => m.horizontalSpeed):F2}");
            }
            csv.Append('\n');
            Console.WriteLine(line);
        }
        if (A.TryGetValue("csv", out var path)) { File.WriteAllText(path, csv.ToString()); Console.WriteLine("csv: " + path); }
        return 0;
    }

    // Підбір коефіцієнтів спільного бічного наведення (лише для налаштування, не для звітів).
    static int Sweep(string weights)
    {
        var set = MakeSet(weights);
        int n = I("n", 60), seed = I("seed", 42);
        float[] Ls(string k, string d) => Get(k, d).Split(',').Select(s => float.Parse(s, CultureInfo.InvariantCulture)).ToArray();
        var cases = new List<(LandingSetup, DisturbanceSettings)>
        {
            (LandingSetup.FromInitialConditions(1600, 60, 2), new DisturbanceSettings { WindStrength = 5, ContinuousWind = true, MassVariationPercent = 5, AngleVariationDeg = 4, PositionJitterMeters = 10, NavNoiseScale = 0.35f }),
            (LandingSetup.FromInitialConditions(1800, 72, 3.5f), new DisturbanceSettings { WindStrength = 10, ContinuousWind = true, MassVariationPercent = 8, AngleVariationDeg = 8, PositionJitterMeters = 22, NavNoiseScale = 0.35f }),
            (LandingSetup.FromInitialConditions(1600, 60, 2), new DisturbanceSettings { WindStrength = 15, ContinuousWind = true, MassVariationPercent = 8, AngleVariationDeg = 8, PositionJitterMeters = 30, NavNoiseScale = 0.35f }),
        };
        var modes = Modes();
        foreach (var kp in Ls("kp", "0.05")) foreach (var ki in Ls("ki", "0.003")) foreach (var kv in Ls("kv", "0.45"))
        foreach (var tl in Ls("tilt", "10")) foreach (var th in Ls("term", "40")) foreach (var tt in Ls("ttilt", "3"))
        {
            var sb = new StringBuilder($"kp={kp} ki={ki} kv={kv} tilt={tl} term={th} ttilt={tt} |");
            foreach (var (setup, d) in cases)
            foreach (var mode in modes)
            {
                var res = new LandingMetrics[n];
                System.Threading.Tasks.Parallel.For(0, n, i =>
                {
                    var s2 = set.Clone();
                    var sim = new LandingSimulation();
                    sim.Guidance.Kp = kp; sim.Guidance.Ki = ki; sim.Guidance.Kv = kv; sim.Guidance.MaxTiltDeg = tl;
                    sim.Guidance.TerminalHeight = th; sim.Guidance.TerminalMaxTiltDeg = tt;
                    res[i] = LandingExperiment.RunTrial(setup, s2.Get(mode), LandingScenario.Sample(d, LandingExperiment.TrialSeed(seed, i)), sim);
                });
                sb.Append($" {mode.ToString()[0]}{LandingExperiment.SuccessRate(res) * 100,4:F0}/{res.Average(m => m.horizontalMiss),4:F0}m");
            }
            Console.WriteLine(sb);
        }
        return 0;
    }

    static int Train(string weights)
    {
        var mode = Parse(Get("mode", "neural"));
        var set = new ControllerSet();
        var o = EsTrainer.Options.Default(mode);
        o.Generations = I("gens", o.Generations);
        o.Lambda = I("lambda", o.Lambda);
        o.BatchSize = I("batch", o.BatchSize);
        o.Seed = I("seed", o.Seed);
        o.ValidationSize = I("val", o.ValidationSize);
        o.Sigma0 = F("sigma", o.Sigma0);
        string outDir = Get("out", weights);
        Directory.CreateDirectory(outDir);
        var trainer = new EsTrainer();
        var sw = Stopwatch.StartNew();
        Console.WriteLine($"ES(1+{o.Lambda}) {mode}: gens={o.Generations} batch={o.BatchSize} val={o.ValidationSize} seed={o.Seed} threads={Environment.ProcessorCount}");
        var policy = trainer.TrainAndSave(set, o, outDir, g =>
        {
            if (g.Validated || g.Generation % 5 == 0)
                Console.WriteLine($"gen {g.Generation,4} σ={g.Sigma:F3} parent={g.ParentCost:F3} best={g.BestChildCost:F3} ps={g.SuccessFraction:F2}" +
                                  (g.Validated ? $" | VAL cost={g.ValidationCost:F3} succ={g.ValidationSuccess:P1}" : "") + $"  [{sw.Elapsed.TotalSeconds:F0}s]");
        });
        string file = mode == ControlMode.Neural ? NeuralController.WeightsFileName : HybridController.WeightsFileName;
        Console.WriteLine($"baseline VAL cost={policy.baselineValidationCost:F3} succ={policy.baselineValidationSuccess:P1} -> trained VAL cost={policy.validationCost:F3} succ={policy.validationSuccess:P1}");
        Console.WriteLine($"saved {Path.Combine(outDir, file)} (+ training log CSV) in {sw.Elapsed.TotalSeconds:F0}s");
        return 0;
    }
}
