using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Експорт результатів: кожен запуск = окремий каталог у SimulationLogs/.
/// Landing_* — одна посадка; Comparison_* — Monte-Carlo.
/// </summary>
public static class ResearchExporter
{
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static string LogsDirectory
    {
        get
        {
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "SimulationLogs"));
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    /// <summary>Створити порожню теку запуску в SimulationLogs (унікальна мітка).</summary>
    public static string CreateRunDirectory(string kind, string label)
    {
        string stamp = Stamp();
        string safe = Sanitize(string.IsNullOrEmpty(label) ? kind : label);
        string name = $"{Sanitize(kind)}_{safe}_{stamp}";
        string dir = Path.Combine(LogsDirectory, name);
        Directory.CreateDirectory(dir);
        return dir;
    }

    public sealed class LandingExportData
    {
        public string algorithm;
        public string timestamp;
        public LandingMetrics metrics;
        public float maxTouchdownVelocity = LandingCriteria.DefaultMaxTouchdownVelocity;
        public float maxLandingAngle = LandingCriteria.DefaultMaxLandingAngle;
        public float maxHorizontalMiss = LandingCriteria.DefaultMaxHorizontalMiss;
        public float maxHorizontalSpeed = LandingCriteria.DefaultMaxHorizontalSpeed;
        public string trajectoryCsvPath;
        public List<string> trajectoryRows;
        /// <summary>Покрокові семпли для SVG-графіків і детального аналізу.</summary>
        public List<DataLogger.Sample> samples;
        public string thesisTopic =
            "Розроблення інтелектуальної системи автономної посадки першого ступеня ракети-носія " +
            "на основі нечіткої логіки та машинного навчання (Earth LZ, після відділення)";
        public string environment = "Earth LZ";
        public string vehicle = "first stage";
        public string phase = "landing after separation";
    }

    public sealed class ComparisonExportData
    {
        public string timestamp;
        public int testsPerAlgorithm;
        public bool enableNoise;
        public float windStrength;
        public float massVariationPercent;
        public float angleVariationDegrees;
        public float positionJitterMeters = 18f;
        public bool continuousWind = true;
        public int experimentSeed = 42;
        public int protocolVersion = 2;
        public bool pairedSeeds = true;
        public bool hybridResidual = true;
        public float startHeight = 1800f;
        public float startDescentSpeed = 72f;
        public float startTiltDeg = 3.5f;
        public string environment = "Earth LZ";
        public string vehicle = "first stage";
        public string phase = "landing after separation";
        public List<AlgoStats> algorithms = new();

        /// <summary>True, якщо активний будь-який канал збурень (для формулювань звіту).</summary>
        public bool HasDisturbances =>
            enableNoise || windStrength > 0.05f || positionJitterMeters > 0.1f;
    }

    public sealed class AlgoStats
    {
        public string name;
        public int tests;
        public float successRate;
        public float avgTouchdownVelocity;
        public float avgAngleError;
        public float avgHorizontalMiss;
        public float avgHorizontalSpeed;
        public float avgFuelRemaining;
        public float avgFlightTime;
        public float avgSuccessScore;
        public float stdSuccessScore;
        public float minTouchdownVelocity;
        public float maxTouchdownVelocity;
        public int successCount;
    }

    public static string Stamp() => DateTime.Now.ToString("yyyyMMdd_HHmmss", Inv);

    /// <summary>
    /// Один каталог на посадку — усі файли всередині, без розкиданих копій у корені.
    /// </summary>
    public static string ExportLanding(LandingExportData data)
    {
        if (data == null || data.metrics == null)
            throw new ArgumentNullException(nameof(data));

        string stamp = string.IsNullOrEmpty(data.timestamp) ? Stamp() : data.timestamp;
        data.timestamp = stamp;
        string safeAlgo = Sanitize(data.algorithm);
        string packName = $"Landing_{safeAlgo}_{stamp}";
        string dir = Path.Combine(LogsDirectory, packName);
        Directory.CreateDirectory(dir);
        string charts = Path.Combine(dir, "charts");
        Directory.CreateDirectory(charts);

        string readme = Path.Combine(dir, "00_README.md");
        string mdPath = Path.Combine(dir, "01_SUMMARY.md");
        string jsonPath = Path.Combine(dir, "02_metrics.json");
        string csvPath = Path.Combine(dir, "03_timeseries.csv");
        string calcMd = Path.Combine(dir, "04_analysis.md");

        // Часовий ряд
        if (data.trajectoryRows != null && data.trajectoryRows.Count > 0)
            File.WriteAllLines(csvPath, data.trajectoryRows, Encoding.UTF8);
        else if (!string.IsNullOrEmpty(data.trajectoryCsvPath) && File.Exists(data.trajectoryCsvPath))
            File.Copy(data.trajectoryCsvPath, csvPath, overwrite: true);
        else
            File.WriteAllText(csvPath,
                "step,time_s,posX_m,posY_m,posZ_m\n(no samples)\n", Encoding.UTF8);

        File.WriteAllText(jsonPath, BuildLandingJson(data), Encoding.UTF8);
        File.WriteAllText(mdPath, BuildLandingMarkdown(data, packName), new UTF8Encoding(true));
        File.WriteAllText(readme, BuildLandingReadme(data, packName), new UTF8Encoding(true));

        var samples = data.samples;
        if (samples != null && samples.Count >= 2)
        {
            File.WriteAllText(Path.Combine(charts, "altitude_vs_time.svg"),
                BuildSvgSeries(samples, s => s.time, s => s.posY,
                    "Altitude h(t)", "t, s", "h, m", "#2563eb", true), Encoding.UTF8);
            File.WriteAllText(Path.Combine(charts, "velocity_vs_time.svg"),
                BuildSvgSeries(samples, s => s.time, s => s.velY,
                    "Vertical velocity Vy(t)", "t, s", "Vy, m/s", "#ea580c", true), Encoding.UTF8);
            File.WriteAllText(Path.Combine(charts, "thrust_vs_time.svg"),
                BuildSvgSeries(samples, s => s.time, s => s.thrustKn,
                    "Thrust F(t)", "t, s", "F, kN", "#16a34a", true), Encoding.UTF8);
            File.WriteAllText(Path.Combine(charts, "track_XZ.svg"),
                BuildSvgSeries(samples, s => s.posX, s => s.posZ,
                    "Ground track XZ (pad at 0,0)", "X, m", "Z, m", "#7c3aed", false), Encoding.UTF8);
            File.WriteAllText(Path.Combine(charts, "side_Xh.svg"),
                BuildSvgSeries(samples, s => s.posX, s => s.posY,
                    "Side view X-h", "X, m", "h, m", "#0ea5e9", false), Encoding.UTF8);
            File.WriteAllText(calcMd, BuildStepAnalysisMarkdown(data), new UTF8Encoding(true));
        }
        else
        {
            File.WriteAllText(calcMd,
                "# Аналіз\n\nНедостатньо семплів для графіків (потрібно ≥ 2).\n",
                new UTF8Encoding(true));
        }

        Debug.Log($"[Export] Пакет посадки: {dir}");
        return dir;
    }

    /// <summary>Один каталог на Monte-Carlo порівняння.</summary>
    public static string ExportComparison(ComparisonExportData data)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));

        string stamp = string.IsNullOrEmpty(data.timestamp) ? Stamp() : data.timestamp;
        data.timestamp = stamp;
        string packName = $"Comparison_{stamp}";
        string dir = Path.Combine(LogsDirectory, packName);
        Directory.CreateDirectory(dir);

        File.WriteAllText(Path.Combine(dir, "00_README.md"),
            BuildComparisonReadme(data, packName), new UTF8Encoding(true));
        File.WriteAllText(Path.Combine(dir, "01_SUMMARY.md"),
            BuildComparisonMarkdown(data), new UTF8Encoding(true));
        File.WriteAllText(Path.Combine(dir, "02_results.csv"),
            BuildComparisonCsv(data), Encoding.UTF8);
        File.WriteAllText(Path.Combine(dir, "03_results.json"),
            BuildComparisonJson(data), Encoding.UTF8);

        Debug.Log($"[Export] Пакет порівняння: {dir}");
        return dir;
    }

    public static AlgoStats ComputeStats(string name, List<LandingMetrics> list)
    {
        var s = new AlgoStats { name = name, tests = list?.Count ?? 0 };
        if (list == null || list.Count == 0) return s;

        int ok = 0;
        float sumV = 0, sumA = 0, sumM = 0, sumH = 0, sumF = 0, sumT = 0, sumS = 0;
        float minV = float.MaxValue, maxV = float.MinValue;

        foreach (var m in list)
        {
            if (m.isSuccessfulLanding) ok++;
            sumV += m.touchdownVelocity;
            sumA += m.landingAngleError;
            sumM += m.horizontalMiss;
            sumH += m.horizontalSpeed;
            sumF += m.fuelRemaining;
            sumT += m.totalFlightTime;
            sumS += m.SuccessScore;
            if (m.touchdownVelocity < minV) minV = m.touchdownVelocity;
            if (m.touchdownVelocity > maxV) maxV = m.touchdownVelocity;
        }

        int n = list.Count;
        s.successCount = ok;
        s.successRate = ok * 100f / n;
        s.avgTouchdownVelocity = sumV / n;
        s.avgAngleError = sumA / n;
        s.avgHorizontalMiss = sumM / n;
        s.avgHorizontalSpeed = sumH / n;
        s.avgFuelRemaining = sumF / n;
        s.avgFlightTime = sumT / n;
        s.avgSuccessScore = sumS / n;
        s.minTouchdownVelocity = minV;
        s.maxTouchdownVelocity = maxV;

        // Вибіркова stdev SuccessScore (n>1); 0 при одиночному trial
        if (n > 1)
        {
            float mean = s.avgSuccessScore;
            double acc = 0;
            foreach (var m in list)
            {
                double d = m.SuccessScore - mean;
                acc += d * d;
            }
            s.stdSuccessScore = (float)System.Math.Sqrt(acc / (n - 1));
        }
        return s;
    }

    public static void OpenLogsFolder()
    {
        RevealPath(LogsDirectory);
    }

    /// <summary>
    /// Opens OS file manager for an export path.
    /// Prefer selecting a concrete file (/select on Windows); otherwise open the folder.
    /// </summary>
    public static void RevealPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        path = Path.GetFullPath(path);
        string target = path;
        bool selectFile = false;

        if (File.Exists(path))
        {
            target = path;
            selectFile = true;
        }
        else if (Directory.Exists(path))
        {
            // Prefer highlighting a known pack artifact inside the export folder.
            string[] prefer =
            {
                "00_README.md",
                "01_SUMMARY.md",
                "02_results.csv",
                "02_metrics.json",
                "03_timeseries.csv"
            };
            foreach (string name in prefer)
            {
                string candidate = Path.Combine(path, name);
                if (File.Exists(candidate))
                {
                    target = candidate;
                    selectFile = true;
                    break;
                }
            }
        }
        else
        {
            Debug.LogWarning($"[Export] RevealPath: path not found: {path}");
            return;
        }

        try
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            string args = selectFile
                ? "/select,\"" + target + "\""
                : "\"" + target + "\"";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = args,
                UseShellExecute = true
            });
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            if (selectFile)
                System.Diagnostics.Process.Start("open", "-R \"" + target + "\"");
            else
                System.Diagnostics.Process.Start("open", "\"" + target + "\"");
#else
            string folder = selectFile ? Path.GetDirectoryName(target) : target;
            Application.OpenURL("file://" + folder.Replace("\\", "/"));
#endif
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Export] RevealPath failed: {ex.Message}");
        }
    }

    // ─── builders ───

    public static string BuildLandingJson(LandingExportData d)
    {
        var m = d.metrics;
        var sb = new StringBuilder(1024);
        sb.AppendLine("{");
        sb.AppendLine($"  \"project\": \"Betelgeuse\",");
        sb.AppendLine($"  \"type\": \"single_landing\",");
        sb.AppendLine($"  \"timestamp\": \"{Esc(d.timestamp ?? Stamp())}\",");
        sb.AppendLine($"  \"algorithm\": \"{Esc(d.algorithm)}\",");
        sb.AppendLine($"  \"environment\": \"{Esc(d.environment ?? "Earth LZ")}\",");
        sb.AppendLine($"  \"vehicle\": \"{Esc(d.vehicle ?? "first stage")}\",");
        sb.AppendLine($"  \"phase\": \"{Esc(d.phase ?? "landing after separation")}\",");
        sb.AppendLine($"  \"thesisTopic\": \"{Esc(d.thesisTopic)}\",");
        sb.AppendLine($"  \"sampleCount\": {(d.samples != null ? d.samples.Count : 0)},");
        sb.AppendLine("  \"criteria\": {");
        sb.AppendLine($"    \"maxTouchdownVelocity\": {F(d.maxTouchdownVelocity)},");
        sb.AppendLine($"    \"maxLandingAngle\": {F(d.maxLandingAngle)},");
        sb.AppendLine($"    \"maxHorizontalMiss\": {F(d.maxHorizontalMiss)},");
        sb.AppendLine($"    \"maxHorizontalSpeed\": {F(d.maxHorizontalSpeed)}");
        sb.AppendLine("  },");
        sb.AppendLine("  \"results\": {");
        sb.AppendLine($"    \"successful\": {(m.isSuccessfulLanding ? "true" : "false")},");
        sb.AppendLine($"    \"timedOut\": {(m.timedOut ? "true" : "false")},");
        sb.AppendLine($"    \"touchdownVelocity_mps\": {F(m.touchdownVelocity)},");
        sb.AppendLine($"    \"landingAngle_deg\": {F(m.landingAngleError)},");
        sb.AppendLine($"    \"horizontalMiss_m\": {F(m.horizontalMiss)},");
        sb.AppendLine($"    \"horizontalSpeed_mps\": {F(m.horizontalSpeed)},");
        sb.AppendLine($"    \"fuelRemaining_kg\": {F(m.fuelRemaining)},");
        sb.AppendLine($"    \"maxAltitude_m\": {F(m.maxAltitude)},");
        sb.AppendLine($"    \"flightTime_s\": {F(m.totalFlightTime)},");
        sb.AppendLine($"    \"successScore\": {F(m.SuccessScore)}");
        sb.AppendLine("  }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    public static string BuildLandingReadme(LandingExportData d, string packFolder)
    {
        var m = d.metrics;
        var sb = new StringBuilder(1500);
        bool uk = UILocale.IsUK;
        if (uk)
        {
            sb.AppendLine("# Як читати цей каталог");
            sb.AppendLine();
            sb.AppendLine("Це **один запуск посадки**. Усі файли цього запуску лежать тут — нічого не розкидано по `SimulationLogs/`.");
            sb.AppendLine();
            sb.AppendLine("| Файл / папка | Навіщо |");
            sb.AppendLine("|--------------|--------|");
            sb.AppendLine("| **`01_SUMMARY.md`** | Головний звіт: успіх/невдача, таблиця критеріїв, пояснення |");
            sb.AppendLine("| `02_metrics.json` | Ті самі метрики для Excel/скриптів |");
            sb.AppendLine("| `03_timeseries.csv` | Покроковий часовий ряд (стан, тяга, gimbal) |");
            sb.AppendLine("| `04_analysis.md` | Екстремуми польоту та формули моделі |");
            sb.AppendLine("| `charts/*.svg` | Графіки — відкривайте у браузері |");
            sb.AppendLine();
            sb.AppendLine("**З чого почати:** відкрийте `01_SUMMARY.md`.");
            sb.AppendLine();
            sb.AppendLine($"- Алгоритм: **{d.algorithm}**");
            sb.AppendLine($"- Результат: **{(m.isSuccessfulLanding ? "успіх" : "невдача")}** · Score **{m.SuccessScore:F0}/100**");
            sb.AppendLine($"- Папка: `SimulationLogs/{packFolder}/`");
        }
        else
        {
            sb.AppendLine("# How to read this folder");
            sb.AppendLine();
            sb.AppendLine("This is **one landing run**. All files for this run are here — nothing is scattered in `SimulationLogs/` root.");
            sb.AppendLine();
            sb.AppendLine("| File / folder | Purpose |");
            sb.AppendLine("|---------------|---------|");
            sb.AppendLine("| **`01_SUMMARY.md`** | Main report: pass/fail, criteria table, explanation |");
            sb.AppendLine("| `02_metrics.json` | Same metrics for Excel/scripts |");
            sb.AppendLine("| `03_timeseries.csv` | Step time series (state, thrust, gimbal) |");
            sb.AppendLine("| `04_analysis.md` | Flight extremes and model formulas |");
            sb.AppendLine("| `charts/*.svg` | Charts — open in a browser |");
            sb.AppendLine();
            sb.AppendLine("**Start here:** open `01_SUMMARY.md`.");
            sb.AppendLine();
            sb.AppendLine($"- Algorithm: **{d.algorithm}**");
            sb.AppendLine($"- Result: **{(m.isSuccessfulLanding ? "success" : "fail")}** · Score **{m.SuccessScore:F0}/100**");
            sb.AppendLine($"- Folder: `SimulationLogs/{packFolder}/`");
        }
        return sb.ToString();
    }

    public static string BuildLandingMarkdown(LandingExportData d, string packFolder = null)
    {
        var m = d.metrics;
        var sb = new StringBuilder(4096);
        bool ok = m.isSuccessfulLanding;
        sb.AppendLine("# Звіт посадки / Landing report");
        sb.AppendLine();
        sb.AppendLine(ok ? "## Результат: УСПІШНА ПОСАДКА" : "## Результат: НЕВДАЛА ПОСАДКА");
        sb.AppendLine();
        sb.AppendLine($"| | |");
        sb.AppendLine($"|--|--|");
        sb.AppendLine($"| **Алгоритм** | {d.algorithm} |");
        sb.AppendLine($"| **Середовище** | {d.environment ?? "Earth LZ"} |");
        sb.AppendLine($"| **Об'єкт** | {d.vehicle ?? "first stage"} |");
        sb.AppendLine($"| **Фаза** | {d.phase ?? "landing after separation"} |");
        sb.AppendLine($"| **Дата** | {DateTime.Now:yyyy-MM-dd HH:mm:ss} |");
        sb.AppendLine($"| **Оцінка (SuccessScore)** | **{m.SuccessScore:F1} / 100** |");
        sb.AppendLine($"| **Кроків у CSV** | {(d.samples != null ? d.samples.Count : 0)} |");
        if (!string.IsNullOrEmpty(packFolder))
            sb.AppendLine($"| **Каталог** | `SimulationLogs/{packFolder}/` |");
        sb.AppendLine();
        sb.AppendLine("### Тема роботи");
        sb.AppendLine();
        sb.AppendLine(d.thesisTopic);
        sb.AppendLine();
        sb.AppendLine("## 1. Критерії soft-landing");
        sb.AppendLine();
        sb.AppendLine("Посадка **успішна**, лише якщо виконані **всі** норми нижче (і немає timeout).");
        sb.AppendLine();
        sb.AppendLine("| Параметр | Значення | Норма | Статус |");
        sb.AppendLine("|----------|----------|-------|--------|");
        sb.AppendLine(Row("|Vy| приземлення", $"{m.touchdownVelocity:F2} м/с", $"< {d.maxTouchdownVelocity}", m.touchdownVelocity < d.maxTouchdownVelocity && !m.timedOut));
        sb.AppendLine(Row("Нахил корпусу", $"{m.landingAngleError:F2}°", $"< {d.maxLandingAngle}°", m.landingAngleError < d.maxLandingAngle && !m.timedOut));
        sb.AppendLine(Row("Промах від центру pad", $"{m.horizontalMiss:F2} м", $"< {d.maxHorizontalMiss} м", m.horizontalMiss < d.maxHorizontalMiss && !m.timedOut));
        sb.AppendLine(Row("|Vh| бічна швидкість", $"{m.horizontalSpeed:F2} м/с", $"< {d.maxHorizontalSpeed}", m.horizontalSpeed < d.maxHorizontalSpeed && !m.timedOut));
        if (m.timedOut)
            sb.AppendLine(Row("Timeout симуляції", "так", "ні", false));
        sb.AppendLine();
        sb.AppendLine("## 2. Додаткові метрики");
        sb.AppendLine();
        sb.AppendLine("| Параметр | Значення |");
        sb.AppendLine("|----------|----------|");
        sb.AppendLine($"| Залишок палива | {m.fuelRemaining:F1} кг |");
        sb.AppendLine($"| Час польоту | {m.totalFlightTime:F1} с |");
        sb.AppendLine($"| Макс. висота | {m.maxAltitude:F0} м |");
        sb.AppendLine();
        sb.AppendLine("**SuccessScore** (0…100): 35% швидкість + 25% кут + 15% паливо + 15% промах + 10% бічна V.");
        sb.AppendLine();
        sb.AppendLine("## 3. Пояснення");
        sb.AppendLine();
        sb.AppendLine("```");
        sb.AppendLine(m.BuildUserSummary(d.maxTouchdownVelocity, d.maxLandingAngle, d.maxHorizontalMiss, d.maxHorizontalSpeed));
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("## 4. Графіки");
        sb.AppendLine();
        sb.AppendLine("Відкрийте SVG у браузері (подвійний клік або перетягніть у Chrome/Edge).");
        sb.AppendLine();
        sb.AppendLine("| Файл | Що показує |");
        sb.AppendLine("|------|------------|");
        sb.AppendLine("| `charts/altitude_vs_time.svg` | Висота h(t) |");
        sb.AppendLine("| `charts/velocity_vs_time.svg` | Вертикальна швидкість Vy(t) |");
        sb.AppendLine("| `charts/thrust_vs_time.svg` | Тяга F(t) |");
        sb.AppendLine("| `charts/track_XZ.svg` | Слід на землі (pad = 0,0) |");
        sb.AppendLine("| `charts/side_Xh.svg` | Бічний профіль X–h |");
        sb.AppendLine();
        sb.AppendLine("![h(t)](charts/altitude_vs_time.svg)");
        sb.AppendLine();
        sb.AppendLine("![Vy(t)](charts/velocity_vs_time.svg)");
        sb.AppendLine();
        sb.AppendLine("![F(t)](charts/thrust_vs_time.svg)");
        sb.AppendLine();
        sb.AppendLine("![XZ](charts/track_XZ.svg)");
        sb.AppendLine();
        sb.AppendLine("![side](charts/side_Xh.svg)");
        sb.AppendLine();
        sb.AppendLine("## 5. Інші файли цього запуску");
        sb.AppendLine();
        sb.AppendLine("| Файл | Для кого |");
        sb.AppendLine("|------|----------|");
        sb.AppendLine("| `00_README.md` | Коротка навігація папкою |");
        sb.AppendLine("| `02_metrics.json` | Скрипти / Excel Power Query |");
        sb.AppendLine("| `03_timeseries.csv` | Excel: увесь політ по кроках |");
        sb.AppendLine("| `04_analysis.md` | Екстремуми + формули моделі |");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine("*Betelgeuse · МКР 2026 · один запуск = один каталог*");
        return sb.ToString();
    }

    public static string BuildComparisonReadme(ComparisonExportData d, string packFolder)
    {
        var sb = new StringBuilder(1200);
        if (UILocale.IsUK)
        {
            sb.AppendLine("# Як читати цей каталог (порівняння)");
            sb.AppendLine();
            sb.AppendLine("Це **один прогін Monte-Carlo** (усі алгоритми A–D). Усі файли — лише в цій папці.");
            sb.AppendLine();
            sb.AppendLine("| Файл | Зміст |");
            sb.AppendLine("|------|--------|");
            sb.AppendLine("| **`01_SUMMARY.md`** | Головний звіт + переможець |");
            sb.AppendLine("| `02_results.csv` | Таблиця для Excel |");
            sb.AppendLine("| `03_results.json` | Для скриптів |");
            sb.AppendLine();
            sb.AppendLine($"- Запусків на алгоритм: **{d.testsPerAlgorithm}**");
            sb.AppendLine($"- Збурення: **{(d.HasDisturbances ? "увімкнено" : "вимкнено")}** (вітер={d.windStrength:F1}, jitter={d.positionJitterMeters:F0} м)");
            sb.AppendLine($"- Paired seeds: **{(d.pairedSeeds ? "так" : "ні")}** · protocol v{d.protocolVersion}");
            sb.AppendLine($"- Папка: `SimulationLogs/{packFolder}/`");
            sb.AppendLine();
            sb.AppendLine("**З чого почати:** `01_SUMMARY.md`.");
        }
        else
        {
            sb.AppendLine("# How to read this folder (comparison)");
            sb.AppendLine();
            sb.AppendLine("This is **one Monte-Carlo run** (all algorithms). All files are only in this folder.");
            sb.AppendLine();
            sb.AppendLine("| File | Content |");
            sb.AppendLine("|------|---------|");
            sb.AppendLine("| **`01_SUMMARY.md`** | Main report + winner |");
            sb.AppendLine("| `02_results.csv` | Excel table |");
            sb.AppendLine("| `03_results.json` | For scripts |");
            sb.AppendLine();
            sb.AppendLine($"- Runs per algorithm: **{d.testsPerAlgorithm}**");
            sb.AppendLine($"- Disturbances: **{(d.HasDisturbances ? "on" : "off")}** (wind={d.windStrength:F1}, jitter={d.positionJitterMeters:F0} m)");
            sb.AppendLine($"- Paired seeds: **{(d.pairedSeeds ? "yes" : "no")}** · protocol v{d.protocolVersion}");
            sb.AppendLine($"- Folder: `SimulationLogs/{packFolder}/`");
            sb.AppendLine();
            sb.AppendLine("**Start here:** `01_SUMMARY.md`.");
        }
        return sb.ToString();
    }


    /// <summary>SVG line chart from samples (xSel, ySel) with titled axes, grid, and tick labels.</summary>
    public static string BuildSvgSeries(
        List<DataLogger.Sample> samples,
        System.Func<DataLogger.Sample, float> xSel,
        System.Func<DataLogger.Sample, float> ySel,
        string title, string xLabel, string yLabel, string stroke, bool markZero)
    {
        const int W = 960, H = 500;
        const float padL = 78f, padR = 36f, padT = 56f, padB = 72f;
        float plotW = W - padL - padR;
        float plotH = H - padT - padB;

        if (samples == null || samples.Count == 0)
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
                   $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{W}\" height=\"{H}\" viewBox=\"0 0 {W} {H}\">" +
                   "<rect width=\"100%\" height=\"100%\" fill=\"#f7f8fa\"/>" +
                   $"<text x=\"{W / 2}\" y=\"{H / 2}\" text-anchor=\"middle\" font-family=\"Segoe UI,Arial,sans-serif\" font-size=\"14\" fill=\"#888\">No samples</text></svg>";
        }

        float xMin = float.MaxValue, xMax = float.MinValue;
        float yMin = float.MaxValue, yMax = float.MinValue;
        foreach (var s in samples)
        {
            float x = xSel(s), y = ySel(s);
            if (x < xMin) xMin = x; if (x > xMax) xMax = x;
            if (y < yMin) yMin = y; if (y > yMax) yMax = y;
        }
        if (Mathf.Approximately(xMin, xMax)) { xMin -= 1f; xMax += 1f; }
        if (Mathf.Approximately(yMin, yMax)) { yMin -= 1f; yMax += 1f; }

        float xPad = (xMax - xMin) * 0.04f;
        float yPad = (yMax - yMin) * 0.08f;
        xMin -= xPad; xMax += xPad;
        yMin -= yPad; yMax += yPad;
        if (markZero)
        {
            if (yMin > 0f) yMin = 0f;
            if (yMax < 0f) yMax = 0f;
        }

        float X(float v) => padL + (v - xMin) / (xMax - xMin) * plotW;
        float Y(float v) => padT + (1f - (v - yMin) / (yMax - yMin)) * plotH;

        GetNiceTicks(xMin, xMax, 6, out float[] xTicks, out float xStep);
        GetNiceTicks(yMin, yMax, 6, out float[] yTicks, out float yStep);

        string strokeSafe = string.IsNullOrEmpty(stroke) ? "#2563eb" : stroke;
        var sb = new StringBuilder(samples.Count * 28 + 2400);
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{W}\" height=\"{H}\" viewBox=\"0 0 {W} {H}\" role=\"img\" aria-label=\"{EscXml(title)}\">");
        sb.AppendLine("<defs>");
        sb.AppendLine("<linearGradient id=\"plotBg\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\"><stop offset=\"0%\" stop-color=\"#ffffff\"/><stop offset=\"100%\" stop-color=\"#f3f6fb\"/></linearGradient>");
        sb.AppendLine("<filter id=\"softShadow\" x=\"-2%\" y=\"-2%\" width=\"104%\" height=\"104%\"><feDropShadow dx=\"0\" dy=\"1\" stdDeviation=\"1.2\" flood-color=\"#0f172a\" flood-opacity=\"0.08\"/></filter>");
        sb.AppendLine("</defs>");
        sb.AppendLine("<rect width=\"100%\" height=\"100%\" fill=\"#f7f8fa\"/>");
        sb.AppendLine($"<rect x=\"{padL}\" y=\"{padT}\" width=\"{plotW}\" height=\"{plotH}\" fill=\"url(#plotBg)\" stroke=\"#cbd5e1\" stroke-width=\"1.25\" rx=\"4\" ry=\"4\" filter=\"url(#softShadow)\"/>");

        sb.AppendLine($"<text x=\"{W / 2}\" y=\"30\" text-anchor=\"middle\" font-family=\"Segoe UI,Arial,sans-serif\" font-size=\"18\" font-weight=\"600\" fill=\"#0f172a\">{EscXml(title)}</text>");

        foreach (float yv in yTicks)
        {
            if (yv < yMin - 1e-6f || yv > yMax + 1e-6f) continue;
            float yy = Y(yv);
            string yys = yy.ToString("0.##", Inv);
            sb.AppendLine($"<line x1=\"{padL}\" y1=\"{yys}\" x2=\"{(padL + plotW).ToString("0.##", Inv)}\" y2=\"{yys}\" stroke=\"#e2e8f0\" stroke-width=\"1\"/>");
            sb.AppendLine($"<line x1=\"{(padL - 5).ToString("0.##", Inv)}\" y1=\"{yys}\" x2=\"{padL.ToString("0.##", Inv)}\" y2=\"{yys}\" stroke=\"#64748b\" stroke-width=\"1.25\"/>");
            sb.AppendLine($"<text x=\"{(padL - 8).ToString("0.##", Inv)}\" y=\"{(yy + 4).ToString("0.##", Inv)}\" text-anchor=\"end\" font-family=\"Segoe UI,Consolas,monospace\" font-size=\"11\" fill=\"#475569\">{FormatTick(yv, yStep)}</text>");
        }

        foreach (float xv in xTicks)
        {
            if (xv < xMin - 1e-6f || xv > xMax + 1e-6f) continue;
            float xx = X(xv);
            string xxs = xx.ToString("0.##", Inv);
            sb.AppendLine($"<line x1=\"{xxs}\" y1=\"{padT.ToString("0.##", Inv)}\" x2=\"{xxs}\" y2=\"{(padT + plotH).ToString("0.##", Inv)}\" stroke=\"#e2e8f0\" stroke-width=\"1\"/>");
            sb.AppendLine($"<line x1=\"{xxs}\" y1=\"{(padT + plotH).ToString("0.##", Inv)}\" x2=\"{xxs}\" y2=\"{(padT + plotH + 5).ToString("0.##", Inv)}\" stroke=\"#64748b\" stroke-width=\"1.25\"/>");
            sb.AppendLine($"<text x=\"{xxs}\" y=\"{(padT + plotH + 20).ToString("0.##", Inv)}\" text-anchor=\"middle\" font-family=\"Segoe UI,Consolas,monospace\" font-size=\"11\" fill=\"#475569\">{FormatTick(xv, xStep)}</text>");
        }

        if (yMin < 0f && yMax > 0f)
        {
            float zy = Y(0f);
            sb.AppendLine($"<line x1=\"{padL}\" y1=\"{zy.ToString("0.##", Inv)}\" x2=\"{(padL + plotW).ToString("0.##", Inv)}\" y2=\"{zy.ToString("0.##", Inv)}\" stroke=\"#94a3b8\" stroke-width=\"1.25\" stroke-dasharray=\"5 4\"/>");
        }
        if (xMin < 0f && xMax > 0f)
        {
            float zx = X(0f);
            sb.AppendLine($"<line x1=\"{zx.ToString("0.##", Inv)}\" y1=\"{padT}\" x2=\"{zx.ToString("0.##", Inv)}\" y2=\"{(padT + plotH).ToString("0.##", Inv)}\" stroke=\"#94a3b8\" stroke-width=\"1.25\" stroke-dasharray=\"5 4\"/>");
        }

        sb.AppendLine($"<line x1=\"{padL}\" y1=\"{padT}\" x2=\"{padL}\" y2=\"{(padT + plotH).ToString("0.##", Inv)}\" stroke=\"#334155\" stroke-width=\"1.5\"/>");
        sb.AppendLine($"<line x1=\"{padL}\" y1=\"{(padT + plotH).ToString("0.##", Inv)}\" x2=\"{(padL + plotW).ToString("0.##", Inv)}\" y2=\"{(padT + plotH).ToString("0.##", Inv)}\" stroke=\"#334155\" stroke-width=\"1.5\"/>");

        sb.Append($"<polyline fill=\"none\" stroke=\"{EscXml(strokeSafe)}\" stroke-width=\"2.4\" stroke-linejoin=\"round\" stroke-linecap=\"round\" points=\"");
        int stepPts = Mathf.Max(1, samples.Count / 1200);
        for (int i = 0; i < samples.Count; i += stepPts)
        {
            var s = samples[i];
            sb.Append(X(xSel(s)).ToString("0.##", Inv)).Append(',')
              .Append(Y(ySel(s)).ToString("0.##", Inv)).Append(' ');
        }
        var last = samples[samples.Count - 1];
        sb.Append(X(xSel(last)).ToString("0.##", Inv)).Append(',')
          .Append(Y(ySel(last)).ToString("0.##", Inv));
        sb.AppendLine("\"/>");

        var first = samples[0];
        float fx = X(xSel(first)), fy = Y(ySel(first));
        float lx = X(xSel(last)), ly = Y(ySel(last));
        sb.AppendLine($"<circle cx=\"{fx.ToString("0.##", Inv)}\" cy=\"{fy.ToString("0.##", Inv)}\" r=\"4.5\" fill=\"#16a34a\" stroke=\"#fff\" stroke-width=\"1.5\"/>");
        sb.AppendLine($"<circle cx=\"{lx.ToString("0.##", Inv)}\" cy=\"{ly.ToString("0.##", Inv)}\" r=\"4.5\" fill=\"#dc2626\" stroke=\"#fff\" stroke-width=\"1.5\"/>");

        sb.AppendLine($"<text x=\"{(padL + plotW * 0.5f).ToString("0.##", Inv)}\" y=\"{(H - 14).ToString("0.##", Inv)}\" text-anchor=\"middle\" font-family=\"Segoe UI,Arial,sans-serif\" font-size=\"13\" font-weight=\"600\" fill=\"#334155\">{EscXml(xLabel)}</text>");
        float yTitleX = 18f;
        float yTitleY = padT + plotH * 0.5f;
        sb.AppendLine($"<text x=\"{yTitleX.ToString("0.##", Inv)}\" y=\"{yTitleY.ToString("0.##", Inv)}\" text-anchor=\"middle\" font-family=\"Segoe UI,Arial,sans-serif\" font-size=\"13\" font-weight=\"600\" fill=\"#334155\" transform=\"rotate(-90 {yTitleX.ToString("0.##", Inv)} {yTitleY.ToString("0.##", Inv)})\">{EscXml(yLabel)}</text>");

        float legendX = padL + plotW - 132f;
        float legendY = padT + 14f;
        sb.AppendLine($"<rect x=\"{legendX.ToString("0.##", Inv)}\" y=\"{legendY.ToString("0.##", Inv)}\" width=\"120\" height=\"40\" rx=\"4\" ry=\"4\" fill=\"#ffffff\" fill-opacity=\"0.92\" stroke=\"#e2e8f0\"/>");
        sb.AppendLine($"<circle cx=\"{(legendX + 14).ToString("0.##", Inv)}\" cy=\"{(legendY + 14).ToString("0.##", Inv)}\" r=\"4\" fill=\"#16a34a\"/>");
        sb.AppendLine($"<text x=\"{(legendX + 24).ToString("0.##", Inv)}\" y=\"{(legendY + 18).ToString("0.##", Inv)}\" font-family=\"Segoe UI,Arial,sans-serif\" font-size=\"11\" fill=\"#334155\">start</text>");
        sb.AppendLine($"<circle cx=\"{(legendX + 14).ToString("0.##", Inv)}\" cy=\"{(legendY + 30).ToString("0.##", Inv)}\" r=\"4\" fill=\"#dc2626\"/>");
        sb.AppendLine($"<text x=\"{(legendX + 24).ToString("0.##", Inv)}\" y=\"{(legendY + 34).ToString("0.##", Inv)}\" font-family=\"Segoe UI,Arial,sans-serif\" font-size=\"11\" fill=\"#334155\">end</text>");

        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    static void GetNiceTicks(float min, float max, int targetCount, out float[] ticks, out float step)
    {
        float range = Mathf.Max(max - min, 1e-12f);
        float rough = range / Mathf.Max(1, targetCount);
        float mag = Mathf.Pow(10f, Mathf.Floor(Mathf.Log10(rough)));
        float norm = rough / mag;
        float nice;
        if (norm <= 1.5f) nice = 1f;
        else if (norm <= 3f) nice = 2f;
        else if (norm <= 7f) nice = 5f;
        else nice = 10f;
        step = nice * mag;

        float first = Mathf.Ceil(min / step) * step;
        if (Mathf.Abs(first) < step * 1e-6f) first = 0f;
        var list = new List<float>(targetCount + 3);
        for (float v = first; v <= max + step * 0.5f; v += step)
        {
            float t = Mathf.Abs(v) < step * 1e-6f ? 0f : v;
            if (t >= min - step * 1e-6f && t <= max + step * 1e-6f)
                list.Add(t);
            if (list.Count > 24) break;
        }
        if (list.Count == 0)
        {
            list.Add(min);
            list.Add(max);
            step = range;
        }
        ticks = list.ToArray();
    }

    static string FormatTick(float value, float step)
    {
        float a = Mathf.Abs(value);
        float s = Mathf.Abs(step);
        if (a == 0f) return "0";
        if (a >= 1e5f || (a > 0f && a < 1e-3f && s < 1e-3f))
            return value.ToString("0.##e0", Inv);
        if (s >= 1f) return value.ToString("0.##", Inv);
        if (s >= 0.1f) return value.ToString("0.##", Inv);
        if (s >= 0.01f) return value.ToString("0.###", Inv);
        return value.ToString("0.####", Inv);
    }
    public static string BuildStepAnalysisMarkdown(LandingExportData d)
    {
        var sb = new StringBuilder(2048);
        sb.AppendLine("# Покроковий аналіз симуляції");
        sb.AppendLine();
        sb.AppendLine($"Алгоритм: **{d.algorithm}**");
        sb.AppendLine();
        if (d.samples == null || d.samples.Count == 0)
        {
            sb.AppendLine("_Немає семплів._");
            return sb.ToString();
        }

        var s0 = d.samples[0];
        var sn = d.samples[d.samples.Count - 1];
        float maxH = float.MinValue, minVy = float.MaxValue, maxTilt = 0f, maxTwr = 0f, maxMiss = 0f;
        int iMaxH = 0, iMinVy = 0, iMaxTilt = 0;
        for (int i = 0; i < d.samples.Count; i++)
        {
            var s = d.samples[i];
            if (s.posY > maxH) { maxH = s.posY; iMaxH = i; }
            if (s.velY < minVy) { minVy = s.velY; iMinVy = i; }
            if (s.tiltDeg > maxTilt) { maxTilt = s.tiltDeg; iMaxTilt = i; }
            if (s.twr > maxTwr) maxTwr = s.twr;
            if (s.miss > maxMiss) maxMiss = s.miss;
        }

        sb.AppendLine("## Екстремуми");
        sb.AppendLine();
        sb.AppendLine("| Величина | Значення | t, с |");
        sb.AppendLine("|----------|----------|------|");
        sb.AppendLine($"| Макс. висота | {maxH:F1} м | {d.samples[iMaxH].time:F2} |");
        sb.AppendLine($"| Мін. Vy (найшвидший спуск) | {minVy:F2} м/с | {d.samples[iMinVy].time:F2} |");
        sb.AppendLine($"| Макс. нахил | {maxTilt:F2}° | {d.samples[iMaxTilt].time:F2} |");
        sb.AppendLine($"| Макс. T/W | {maxTwr:F2} | — |");
        sb.AppendLine($"| Макс. промах | {maxMiss:F1} м | — |");
        sb.AppendLine();
        sb.AppendLine("## Старт → фініш");
        sb.AppendLine();
        sb.AppendLine($"- t₀={s0.time:F2} с · h={s0.posY:F1} м · Vy={s0.velY:F2} · F={s0.thrustKn:F1} кН · mode={s0.controlMode}");
        sb.AppendLine($"- t_f={sn.time:F2} с · h={sn.posY:F1} м · Vy={sn.velY:F2} · miss={sn.miss:F2} м · tilt={sn.tiltDeg:F2}°");
        sb.AppendLine();
        sb.AppendLine("## Формули (модель симулятора)");
        sb.AppendLine();
        sb.AppendLine("- Трансляція: **RK4**, a = g(h)·(−ŷ) + T/m − drag/m");
        sb.AppendLine("- g(h) = g₀ · (R/(R+h))²");
        sb.AppendLine("- ρ(h) = 1.225 · exp(−h · 1.184·10⁻⁴)");
        sb.AppendLine("- ṁ = F / (Isp · g₀)");
        sb.AppendLine("- T/W = F / (m · g(h))");
        sb.AppendLine("- SuccessScore = 0.35·vel + 0.25·angle + 0.15·fuel + 0.15·miss + 0.10·Vh");
        sb.AppendLine();
        sb.AppendLine($"Повний часовий ряд: **{d.samples.Count}** кроків у `03_timeseries.csv`.");
        return sb.ToString();
    }

    static string EscXml(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
    }

    public static string BuildComparisonCsv(ComparisonExportData d)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Algorithm,Tests,SuccessCount,SuccessRate(%),AvgTouchdownVelocity,MinTouchdownVelocity,MaxTouchdownVelocity,AvgAngleError,AvgHorizontalMiss,AvgHorizontalSpeed,AvgFuelRemaining,AvgFlightTime,AvgSuccessScore,StdSuccessScore,Seed,HybridResidual,PositionJitter_m,Wind,PairedSeeds,ProtocolVersion");
        foreach (var a in d.algorithms)
        {
            sb.Append(EscCsv(a.name)).Append(',')
              .Append(a.tests).Append(',')
              .Append(a.successCount).Append(',')
              .Append(F(a.successRate)).Append(',')
              .Append(F(a.avgTouchdownVelocity)).Append(',')
              .Append(F(a.minTouchdownVelocity)).Append(',')
              .Append(F(a.maxTouchdownVelocity)).Append(',')
              .Append(F(a.avgAngleError)).Append(',')
              .Append(F(a.avgHorizontalMiss)).Append(',')
              .Append(F(a.avgHorizontalSpeed)).Append(',')
              .Append(F(a.avgFuelRemaining)).Append(',')
              .Append(F(a.avgFlightTime)).Append(',')
              .Append(F(a.avgSuccessScore)).Append(',')
              .Append(F(a.stdSuccessScore)).Append(',')
              .Append(d.experimentSeed).Append(',')
              .Append(d.hybridResidual ? "1" : "0").Append(',')
              .Append(F(d.positionJitterMeters)).Append(',')
              .Append(F(d.windStrength)).Append(',')
              .Append(d.pairedSeeds ? "1" : "0").Append(',')
              .Append(d.protocolVersion)
              .AppendLine();
        }
        return sb.ToString();
    }

    public static string BuildComparisonJson(ComparisonExportData d)
    {
        var sb = new StringBuilder(2048);
        sb.AppendLine("{");
        sb.AppendLine("  \"project\": \"Betelgeuse\",");
        sb.AppendLine("  \"type\": \"monte_carlo_comparison\",");
        sb.AppendLine($"  \"timestamp\": \"{Esc(d.timestamp ?? Stamp())}\",");
        sb.AppendLine($"  \"environment\": \"{Esc(d.environment ?? "Earth LZ")}\",");
        sb.AppendLine($"  \"vehicle\": \"{Esc(d.vehicle ?? "first stage")}\",");
        sb.AppendLine($"  \"phase\": \"{Esc(d.phase ?? "landing after separation")}\",");
        sb.AppendLine("  \"experiment\": {");
        sb.AppendLine($"    \"testsPerAlgorithm\": {d.testsPerAlgorithm},");
        sb.AppendLine($"    \"enableNoise\": {(d.enableNoise ? "true" : "false")},");
        sb.AppendLine($"    \"windStrength\": {F(d.windStrength)},");
        sb.AppendLine($"    \"massVariationPercent\": {F(d.massVariationPercent)},");
        sb.AppendLine($"    \"angleVariationDegrees\": {F(d.angleVariationDegrees)},");
        sb.AppendLine($"    \"positionJitterMeters\": {F(d.positionJitterMeters)},");
        sb.AppendLine($"    \"continuousWind\": {(d.continuousWind ? "true" : "false")},");
        sb.AppendLine($"    \"experimentSeed\": {d.experimentSeed},");
        sb.AppendLine($"    \"protocolVersion\": {d.protocolVersion},");
        sb.AppendLine($"    \"pairedSeeds\": {(d.pairedSeeds ? "true" : "false")},");
        sb.AppendLine($"    \"hybridResidual\": {(d.hybridResidual ? "true" : "false")},");
        sb.AppendLine($"    \"startHeight_m\": {F(d.startHeight)},");
        sb.AppendLine($"    \"startDescentSpeed_mps\": {F(d.startDescentSpeed)},");
        sb.AppendLine($"    \"startTilt_deg\": {F(d.startTiltDeg)}");
        sb.AppendLine("  },");
        sb.AppendLine("  \"algorithms\": [");
        for (int i = 0; i < d.algorithms.Count; i++)
        {
            var a = d.algorithms[i];
            sb.AppendLine("    {");
            sb.AppendLine($"      \"name\": \"{Esc(a.name)}\",");
            sb.AppendLine($"      \"tests\": {a.tests},");
            sb.AppendLine($"      \"successCount\": {a.successCount},");
            sb.AppendLine($"      \"successRate_pct\": {F(a.successRate)},");
            sb.AppendLine($"      \"avgTouchdownVelocity_mps\": {F(a.avgTouchdownVelocity)},");
            sb.AppendLine($"      \"minTouchdownVelocity_mps\": {F(a.minTouchdownVelocity)},");
            sb.AppendLine($"      \"maxTouchdownVelocity_mps\": {F(a.maxTouchdownVelocity)},");
            sb.AppendLine($"      \"avgAngleError_deg\": {F(a.avgAngleError)},");
            sb.AppendLine($"      \"avgHorizontalMiss_m\": {F(a.avgHorizontalMiss)},");
            sb.AppendLine($"      \"avgHorizontalSpeed_mps\": {F(a.avgHorizontalSpeed)},");
            sb.AppendLine($"      \"avgFuelRemaining_kg\": {F(a.avgFuelRemaining)},");
            sb.AppendLine($"      \"avgFlightTime_s\": {F(a.avgFlightTime)},");
            sb.AppendLine($"      \"avgSuccessScore\": {F(a.avgSuccessScore)},");
            sb.AppendLine($"      \"stdSuccessScore\": {F(a.stdSuccessScore)}");
            sb.Append("    }").AppendLine(i < d.algorithms.Count - 1 ? "," : "");
        }
        sb.AppendLine("  ]");
        sb.AppendLine("}");
        return sb.ToString();
    }

    public static string BuildComparisonMarkdown(ComparisonExportData d)
    {
        var sb = new StringBuilder(4096);
        sb.AppendLine("# Порівняння алгоритмів GNC (Monte-Carlo)");
        sb.AppendLine();
        sb.AppendLine("**Середовище:** Earth LZ · **Об'єкт:** first stage · **Фаза:** landing after separation.");
        sb.AppendLine();
        sb.AppendLine("Один експеримент: кожен алгоритм (PID / Fuzzy / Neural / Hybrid) на **ділянці посадки 1-го ступеня** (N разів).");
        sb.AppendLine("**Paired seeds** — trial `i` має однакові збурення для всіх алгоритмів (чесне порівняння).");
        sb.AppendLine("Вітер/шум застосовуються після відділення (MC стартує вже у фазі Stage1).");
        sb.AppendLine();
        sb.AppendLine($"| | |");
        sb.AppendLine($"|--|--|");
        sb.AppendLine($"| **Дата** | {DateTime.Now:yyyy-MM-dd HH:mm:ss} |");
        sb.AppendLine($"| **Середовище** | {d.environment ?? "Earth LZ"} |");
        sb.AppendLine($"| **Об'єкт** | {d.vehicle ?? "first stage"} |");
        sb.AppendLine($"| **Фаза** | {d.phase ?? "landing after separation"} |");
        sb.AppendLine($"| **Запусків на алгоритм (N)** | {d.testsPerAlgorithm} |");
        sb.AppendLine($"| **Seed** | `{d.experimentSeed}` |");
        sb.AppendLine($"| **Protocol** | v{d.protocolVersion} · paired={(d.pairedSeeds ? "yes" : "no")} |");
        sb.AppendLine($"| **Hybrid residual** | {(d.hybridResidual ? "ON (Neuro-Fuzzy)" : "OFF = Fuzzy-only ablation")} |");
        sb.AppendLine($"| **h₀** | {d.startHeight:F0} м |");
        sb.AppendLine($"| **|Vy|₀** | {d.startDescentSpeed:F0} м/с |");
        sb.AppendLine($"| **нахил₀** | {d.startTiltDeg:F1}° |");
        sb.AppendLine($"| **Збурення** | {(d.HasDisturbances ? "увімкнено" : "вимкнено")} |");
        sb.AppendLine($"| Вітер | {d.windStrength:F1} · continuous={(d.continuousWind ? "on" : "off")} |");
        sb.AppendLine($"| ±маса | {d.massVariationPercent:F1}% |");
        sb.AppendLine($"| ±кут | {d.angleVariationDegrees:F1}° |");
        sb.AppendLine($"| Position jitter | ±{d.positionJitterMeters:F0} м |");
        sb.AppendLine($"| enableNoise (mass/angle/jitter) | {(d.enableNoise ? "on" : "off")} |");
        sb.AppendLine();
        sb.AppendLine("## Зведена таблиця");
        sb.AppendLine();
        sb.AppendLine("| Алгоритм | N | Успіх % | V̄_touch | ∠̄ | Промах | Score ±σ |");
        sb.AppendLine("|----------|---|---------|---------|-----|--------|----------|");
        foreach (var a in d.algorithms)
        {
            sb.AppendLine($"| {a.name} | {a.tests} | **{a.successRate:F1}%** | {a.avgTouchdownVelocity:F2} м/с | {a.avgAngleError:F2}° | {a.avgHorizontalMiss:F1} м | {a.avgSuccessScore:F1} ± {a.stdSuccessScore:F1} |");
        }
        sb.AppendLine();

        string winner = "—";
        float bestRate = -1f;
        float bestScore = -1f;
        foreach (var a in d.algorithms)
        {
            if (a.successRate > bestRate + 1e-4f
                || (Mathf.Abs(a.successRate - bestRate) <= 1e-4f && a.avgSuccessScore > bestScore))
            {
                bestRate = a.successRate;
                bestScore = a.avgSuccessScore;
                winner = a.name;
            }
        }
        sb.AppendLine($"## Переможець: **{winner}** ({bestRate:F1}% успішних)");
        sb.AppendLine();
        sb.AppendLine("Критерій перемоги: вищий **% успіху**; при рівності — вищий середній **SuccessScore**.");
        sb.AppendLine();
        sb.AppendLine("## Деталі по алгоритмах");
        sb.AppendLine();
        foreach (var a in d.algorithms)
        {
            sb.AppendLine($"### {a.name}");
            sb.AppendLine();
            sb.AppendLine($"- Успішних: **{a.successCount} / {a.tests}** ({a.successRate:F1}%)");
            sb.AppendLine($"- V_touch: сер. {a.avgTouchdownVelocity:F2} · мін {a.minTouchdownVelocity:F2} · макс {a.maxTouchdownVelocity:F2} м/с");
            sb.AppendLine($"- Кут: {a.avgAngleError:F2}° · промах: {a.avgHorizontalMiss:F2} м · Vh: {a.avgHorizontalSpeed:F2} м/с");
            sb.AppendLine($"- Паливо: {a.avgFuelRemaining:F0} кг · час: {a.avgFlightTime:F1} с · Score: {a.avgSuccessScore:F1}/100");
            sb.AppendLine();
        }
        sb.AppendLine("## Критерії успішної посадки (кожен запуск)");
        sb.AppendLine();
        sb.AppendLine(
            $"|Vy| &lt; {LandingCriteria.DefaultMaxTouchdownVelocity:0.#} м/с · " +
            $"нахил &lt; {LandingCriteria.DefaultMaxLandingAngle:0.#}° · " +
            $"промах &lt; {LandingCriteria.DefaultMaxHorizontalMiss:0.#} м · " +
            $"|Vh| &lt; {LandingCriteria.DefaultMaxHorizontalSpeed:0.#} м/с · без timeout");
        sb.AppendLine();
        sb.AppendLine("## Файли цього експерименту");
        sb.AppendLine();
        sb.AppendLine("| Файл | Призначення |");
        sb.AppendLine("|------|-------------|");
        sb.AppendLine("| `00_README.md` | Навігація папкою |");
        sb.AppendLine("| `01_SUMMARY.md` | Цей звіт |");
        sb.AppendLine("| `02_results.csv` | Excel |");
        sb.AppendLine("| `03_results.json` | Скрипти |");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine("*Betelgeuse · Monte-Carlo · один експеримент = один каталог*");
        return sb.ToString();
    }

    static string Row(string name, string val, string norm, bool ok)
        => $"| {name} | {val} | {norm} | {(ok ? "✅" : "❌")} |";

    static string F(float v) => v.ToString("0.###", Inv);
    static string Esc(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
    static string EscCsv(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        if (s.Contains(',') || s.Contains('"') || s.Contains('\n'))
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        return s;
    }

    static string Sanitize(string s)
    {
        if (string.IsNullOrEmpty(s)) return "Unknown";
        var sb = new StringBuilder(s.Length);
        foreach (char c in s)
        {
            if (char.IsLetterOrDigit(c) || c == '_' || c == '-') sb.Append(c);
            else if (c == ' ' || c == '/') sb.Append('_');
        }
        return sb.Length > 0 ? sb.ToString() : "Unknown";
    }
}
