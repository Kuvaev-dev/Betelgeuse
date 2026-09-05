#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// After clone: detect Git LFS pointer stubs left in Resources (fonts/textures/FBX).
/// Those break runtime loads — show a clear fix instead of silent pink/missing UI.
/// </summary>
[InitializeOnLoad]
public static class RuntimeAssetIntegrity
{
    const string SessionKey = "Betelgeuse.RuntimeAssetIntegrity.Checked";

    static readonly string[] RequiredRelative =
    {
        "Resources/Fonts/LiberationSans.ttf",
        "Resources/Fonts/SegoeUI.ttf",
        "Resources/Textures/grass_diff.jpg",
        "Resources/Textures/mud_diff.jpg",
        "Resources/Textures/sky_cloud.jpg",
        "Resources/Nature/tree_default.fbx",
        "TextMesh Pro/Fonts/LiberationSans.ttf",
        "TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset",
    };

    static RuntimeAssetIntegrity()
    {
        EditorApplication.delayCall += RunOnce;
    }

    [MenuItem("Betelgeuse/Validate Runtime Assets")]
    public static void ValidateMenu() => Run(showDialogAlways: true);

    static void RunOnce()
    {
        if (SessionState.GetBool(SessionKey, false)) return;
        SessionState.SetBool(SessionKey, true);
        Run(showDialogAlways: false);
    }

    static void Run(bool showDialogAlways)
    {
        var bad = new List<string>();
        string data = Application.dataPath;

        foreach (string rel in RequiredRelative)
        {
            string path = Path.Combine(data, rel.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
            {
                bad.Add("MISSING  " + rel);
                continue;
            }
            if (LooksLikeLfsPointer(path))
                bad.Add("LFS PTR  " + rel + "  (" + new FileInfo(path).Length + " B)");
        }

        if (bad.Count == 0)
        {
            if (showDialogAlways)
                EditorUtility.DisplayDialog("Betelgeuse assets", "All required runtime assets look OK.", "OK");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("Required runtime assets are missing or still Git LFS pointers.");
        sb.AppendLine("Play Mode / builds will lack fonts, textures, or nature meshes.");
        sb.AppendLine();
        foreach (string line in bad)
            sb.AppendLine(" • " + line);
        sb.AppendLine();
        sb.AppendLine("Fix (pick one):");
        sb.AppendLine("  1) git lfs install && git lfs pull");
        sb.AppendLine("  2) Pull latest master (assets are plain git blobs now)");
        sb.AppendLine("  3) Re-clone after LFS is installed");

        string msg = sb.ToString();
        Debug.LogError("[Betelgeuse] " + msg.Replace("\n", " | "));
        EditorUtility.DisplayDialog("Betelgeuse — assets incomplete", msg, "OK");
    }

    static bool LooksLikeLfsPointer(string path)
    {
        try
        {
            var fi = new FileInfo(path);
            // Real textures/fonts are >> 200 B; LFS pointers are ~120–140 B text
            if (fi.Length > 512) return false;
            string head;
            using (var sr = new StreamReader(path, Encoding.UTF8, true))
                head = sr.ReadLine() ?? "";
            return head.StartsWith("version https://git-lfs.github.com/spec/v1");
        }
        catch
        {
            return false;
        }
    }
}
#endif
