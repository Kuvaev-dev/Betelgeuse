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

    /// <summary>Create empty run folder under SimulationLogs (unique stamp).</summary>
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
        public float maxTouchdownVelocity = 3.5f;
        public float maxLandingAngle = 7f;
        public float maxHorizontalMiss = 25f;
        public float maxHorizontalSpeed = 5f;
        public string trajectoryCsvPath;
        public List<string> trajectoryRows;
        /// <summary>Покрокові семпли для SVG-графіків і детального аналізу.</summary>
        public List<DataLogger.Sample> samples;
        public string thesisTopic =
            "Розроблення інтелектуальної системи автономної посадки ракетоносія " +
            "на основі нечіткої логіки та машинного навчання";
    }

    public sealed class ComparisonExportData
    {
        public string timestamp;
        public int testsPerAlgorithm;
        public bool enableNoise;
        public float windStrength;
        public float massVariationPercent;
        public float angleVariationDegrees;
        public List<AlgoStats> algorithms = new();
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

        // Timeseries
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
                    "Altitude h(t)", "t, s", "h, m", "#222", true), Encoding.UTF8);
            File.WriteAllText(Path.Combine(charts, "velocity_vs_time.svg"),
                BuildSvgSeries(samples, s => s.time, s => s.velY,
                    "Vertical velocity Vy(t)", "t, s", "Vy, m/s", "#333", true), Encoding.UTF8);
            File.WriteAllText(Path.Combine(charts, "thrust_vs_time.svg"),
                BuildSvgSeries(samples, s => s.time, s => s.thrustKn,
                    "Thrust F(t)", "t, s", "F, kN", "#444", true), Encoding.UTF8);
            File.WriteAllText(Path.Combine(charts, "track_XZ.svg"),
                BuildSvgSeries(samples, s => s.posX, s => s.posZ,
                    "Ground track XZ (pad at 0,0)", "X, m", "Z, m", "#222", false), Encoding.UTF8);
            File.WriteAllText(Path.Combine(charts, "side_Xh.svg"),
                BuildSvgSeries(samples, s => s.posX, s => s.posY,
                    "Side view X–h", "X, m", "h, m", "#222", false), Encoding.UTF8);
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
        return s;
    }

    public static void OpenLogsFolder()
    {
        string dir = LogsDirectory;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"\"{dir}\"",
            UseShellExecute = true
        });
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        System.Diagnostics.Process.Start("open", dir);
#else
        Application.OpenURL("file://" + dir.Replace("\\", "/"));
#endif
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
            sb.AppendLine($"- Збурення: **{(d.enableNoise ? "увімкнено" : "вимкнено")}**");
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
            sb.AppendLine($"- Disturbances: **{(d.enableNoise ? "on" : "off")}**");
            sb.AppendLine($"- Folder: `SimulationLogs/{packFolder}/`");
            sb.AppendLine();
            sb.AppendLine("**Start here:** `01_SUMMARY.md`.");
        }
        return sb.ToString();
    }

    /// <summary>SVG line chart from samples (xSel, ySel).</summary>
    public static string BuildSvgSeries(
        List<DataLogger.Sample> samples,
        System.Func<DataLogger.Sample, float> xSel,
        System.Func<DataLogger.Sample, float> ySel,
        string title, string xLabel, string yLabel, string stroke, bool markZero)
    {
        const int W = 900, H = 420;
        const float padL = 64f, padR = 24f, padT = 40f, padB = 48f;
        float plotW = W - padL - padR;
        float plotH = H - padT - padB;

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
        float yPad = (yMax - yMin) * 0.08f;
        yMin -= yPad; yMax += yPad;
        if (markZero) { if (yMin > 0) yMin = 0; if (yMax < 0) yMax = 0; }

        float X(float v) => padL + (v - xMin) / (xMax - xMin) * plotW;
        float Y(float v) => padT + (1f - (v - yMin) / (yMax - yMin)) * plotH;

        var sb = new StringBuilder(samples.Count * 24 + 800);
        sb.AppendLine($"<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{W}\" height=\"{H}\" viewBox=\"0 0 {W} {H}\">");
        sb.AppendLine($"<rect width=\"100%\" height=\"100%\" fill=\"#fafafa\"/>");
        sb.AppendLine($"<rect x=\"{padL}\" y=\"{padT}\" width=\"{plotW}\" height=\"{plotH}\" fill=\"#fff\" stroke=\"#ccc\"/>");
        sb.AppendLine($"<text x=\"{W / 2}\" y=\"24\" text-anchor=\"middle\" font-family=\"Segoe UI,Arial\" font-size=\"16\" fill=\"#222\">{EscXml(title)}</text>");
        sb.AppendLine($"<text x=\"{W / 2}\" y=\"{H - 12}\" text-anchor=\"middle\" font-family=\"Segoe UI,Arial\" font-size=\"12\" fill=\"#555\">{EscXml(xLabel)}</text>");
        sb.AppendLine($"<text x=\"16\" y=\"{H / 2}\" text-anchor=\"middle\" font-family=\"Segoe UI,Arial\" font-size=\"12\" fill=\"#555\" transform=\"rotate(-90 16 {H / 2})\">{EscXml(yLabel)}</text>");

        // grid
        for (int i = 0; i <= 5; i++)
        {
            float yy = padT + plotH * i / 5f;
            sb.AppendLine($"<line x1=\"{padL}\" y1=\"{yy}\" x2=\"{padL + plotW}\" y2=\"{yy}\" stroke=\"#eee\"/>");
            float yv = yMax - (yMax - yMin) * i / 5f;
            sb.AppendLine($"<text x=\"{padL - 6}\" y=\"{yy + 4}\" text-anchor=\"end\" font-size=\"10\" fill=\"#666\" font-family=\"Consolas,monospace\">{yv.ToString("0.##", Inv)}</text>");
        }

        if (markZero && yMin < 0f && yMax > 0f)
        {
            float zy = Y(0f);
            sb.AppendLine($"<line x1=\"{padL}\" y1=\"{zy}\" x2=\"{padL + plotW}\" y2=\"{zy}\" stroke=\"#999\" stroke-dasharray=\"4 3\"/>");
        }

        sb.Append($"<polyline fill=\"none\" stroke=\"{stroke}\" stroke-width=\"2\" points=\"");
        int step = Mathf.Max(1, samples.Count / 800);
        for (int i = 0; i < samples.Count; i += step)
        {
            var s = samples[i];
            sb.Append(X(xSel(s)).ToString("0.##", Inv)).Append(',')
              .Append(Y(ySel(s)).ToString("0.##", Inv)).Append(' ');
        }
        // last point
        var last = samples[samples.Count - 1];
        sb.Append(X(xSel(last)).ToString("0.##", Inv)).Append(',')
          .Append(Y(ySel(last)).ToString("0.##", Inv));
        sb.AppendLine("\"/>");

        // start/end markers
        var first = samples[0];
        sb.AppendLine($"<circle cx=\"{X(xSel(first)):0.##}\" cy=\"{Y(ySel(first)):0.##}\" r=\"4\" fill=\"#888\"/>");
        sb.AppendLine($"<circle cx=\"{X(xSel(last)):0.##}\" cy=\"{Y(ySel(last)):0.##}\" r=\"4\" fill=\"#222\"/>");
        sb.AppendLine("</svg>");
        return sb.ToString();
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
        sb.AppendLine("Algorithm,Tests,SuccessCount,SuccessRate(%),AvgTouchdownVelocity,MinTouchdownVelocity,MaxTouchdownVelocity,AvgAngleError,AvgHorizontalMiss,AvgHorizontalSpeed,AvgFuelRemaining,AvgFlightTime,AvgSuccessScore");
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
              .Append(F(a.avgSuccessScore))
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
        sb.AppendLine("  \"experiment\": {");
        sb.AppendLine($"    \"testsPerAlgorithm\": {d.testsPerAlgorithm},");
        sb.AppendLine($"    \"enableNoise\": {(d.enableNoise ? "true" : "false")},");
        sb.AppendLine($"    \"windStrength\": {F(d.windStrength)},");
        sb.AppendLine($"    \"massVariationPercent\": {F(d.massVariationPercent)},");
        sb.AppendLine($"    \"angleVariationDegrees\": {F(d.angleVariationDegrees)}");
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
            sb.AppendLine($"      \"avgSuccessScore\": {F(a.avgSuccessScore)}");
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
        sb.AppendLine("Один експеримент: кожен алгоритм (PID / Fuzzy / Neural / Hybrid) запускається N разів із випадковими збуреннями.");
        sb.AppendLine();
        sb.AppendLine($"| | |");
        sb.AppendLine($"|--|--|");
        sb.AppendLine($"| **Дата** | {DateTime.Now:yyyy-MM-dd HH:mm:ss} |");
        sb.AppendLine($"| **Запусків на алгоритм (N)** | {d.testsPerAlgorithm} |");
        sb.AppendLine($"| **Збурення** | {(d.enableNoise ? "увімкнено" : "вимкнено")} |");
        if (d.enableNoise)
        {
            sb.AppendLine($"| Вітер | {d.windStrength:F1} |");
            sb.AppendLine($"| ±маса | {d.massVariationPercent:F1}% |");
            sb.AppendLine($"| ±кут | {d.angleVariationDegrees:F1}° |");
        }
        sb.AppendLine();
        sb.AppendLine("## Зведена таблиця");
        sb.AppendLine();
        sb.AppendLine("| Алгоритм | N | Успіх % | V̄_touch | ∠̄ | Промах | Score |");
        sb.AppendLine("|----------|---|---------|---------|-----|--------|-------|");
        foreach (var a in d.algorithms)
        {
            sb.AppendLine($"| {a.name} | {a.tests} | **{a.successRate:F1}%** | {a.avgTouchdownVelocity:F2} м/с | {a.avgAngleError:F2}° | {a.avgHorizontalMiss:F1} м | {a.avgSuccessScore:F1} |");
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
        sb.AppendLine("|Vy| &lt; 3.5 м/с · нахил &lt; 7° · промах &lt; 25 м · |Vh| &lt; 5 м/с · без timeout");
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
