#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Menu + CLI: builds Windows x86_64 standalone release to Desktop (or -outputPath).
/// CLI: Unity -batchmode -quit -projectPath ... -executeMethod BuildRelease.BuildFromCommandLine
/// Optional: -outputPath "C:\Users\...\Desktop\Betelgeuse_v1.2.0"
/// </summary>
public static class BuildRelease
{
    const string DefaultProductFolder = "Betelgeuse_v1.2.0";
    const string ScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Betelgeuse/Build Release to Desktop")]
    public static void BuildToDesktopMenu()
    {
        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        string outDir = Path.Combine(desktop, DefaultProductFolder);
        BuildTo(outDir, openFolder: true);
    }

    /// <summary>Batchmode entry.</summary>
    public static void BuildFromCommandLine()
    {
        string outDir = null;
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], "-outputPath", StringComparison.OrdinalIgnoreCase))
            {
                outDir = args[i + 1];
                break;
            }
        }

        if (string.IsNullOrWhiteSpace(outDir))
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            outDir = Path.Combine(desktop, DefaultProductFolder);
        }

        int code = BuildTo(outDir, openFolder: false);
        EditorApplication.Exit(code);
    }

    public static int BuildTo(string outputDirectory, bool openFolder)
    {
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            Debug.LogError("[BuildRelease] output directory empty");
            return 1;
        }

        outputDirectory = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(outputDirectory);

        string exeName = "Betelgeuse.exe";
        string exePath = Path.Combine(outputDirectory, exeName);

        if (!File.Exists(ScenePath))
        {
            Debug.LogError("[BuildRelease] Missing scene: " + ScenePath);
            return 2;
        }

        // Ensure scene in build settings
        var scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        EditorBuildSettings.scenes = scenes;

        var opts = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = exePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.CompressWithLz4HC
        };

        Debug.Log($"[BuildRelease] Building → {exePath}");
        BuildReport report = BuildPipeline.BuildPlayer(opts);
        BuildSummary summary = report.summary;

        if (summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"[BuildRelease] FAILED: {summary.result} errors={summary.totalErrors}");
            return 3;
        }

        // Sidecar files next to exe (runtime paths use Application.dataPath/..)
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        CopyIfExists(Path.Combine(projectRoot, "BestWeights_Neural.json"),
            Path.Combine(outputDirectory, "BestWeights_Neural.json"));
        CopyIfExists(Path.Combine(projectRoot, "HOW_TO_RUN.md"),
            Path.Combine(outputDirectory, "HOW_TO_RUN.md"));
        CopyIfExists(Path.Combine(projectRoot, "README.md"),
            Path.Combine(outputDirectory, "README.md"));
        CopyIfExists(Path.Combine(projectRoot, "RELEASE.md"),
            Path.Combine(outputDirectory, "RELEASE.md"));

        WriteReadme(outputDirectory);

        Debug.Log($"[BuildRelease] OK in {summary.totalTime.TotalSeconds:F1}s → {outputDirectory}");
        Debug.Log($"[BuildRelease] size≈{summary.totalSize / (1024f * 1024f):F1} MB");

        if (openFolder)
            EditorUtility.RevealInFinder(exePath);

        return 0;
    }

    static void CopyIfExists(string src, string dst)
    {
        if (!File.Exists(src)) return;
        File.Copy(src, dst, overwrite: true);
    }

    static void WriteReadme(string dir)
    {
        string path = Path.Combine(dir, "00_START_HERE.txt");
        File.WriteAllText(path,
            "Betelgeuse v1.2.0 — Autonomous rocket landing GNC simulator\r\n" +
            "============================================================\r\n\r\n" +
            "Run:  Betelgeuse.exe\r\n\r\n" +
            "Quick defense demo:\r\n" +
            "  D  — Defense demo (Hybrid + Ideal + Start)\r\n" +
            "  P  — Compare algorithms (Monte-Carlo)\r\n" +
            "  E  — Export reports  →  SimulationLogs/ next to this exe\r\n" +
            "  F1 — Help\r\n" +
            "  1-4 — PID / Fuzzy / Neural / Hybrid\r\n" +
            "  Space — Start landing\r\n\r\n" +
            "See HOW_TO_RUN.md and RELEASE.md for full notes.\r\n");
    }
}
#endif
