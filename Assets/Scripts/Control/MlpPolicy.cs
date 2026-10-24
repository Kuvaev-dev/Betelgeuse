using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Багатошаровий перцептрон 5→8→2 (tanh у прихованому шарі) — навчувана частина режимів C і D.
///
/// Входи (нормовані): h/2000, −Vy/60, (m−33 т)/8 т, нахил/10°, Vh/10.
/// Виходи (через tanh, тобто обмежені):
///   y₀ → поправка тяги ΔT = ResidualFrac·tanh(y₀)·m·g (residual над базовим законом);
///   y₁ → масштаб бічного наведення k = exp(LateralRange·tanh(y₁)) ∈ [e^−r; e^r].
/// Ваги навчаються офлайн еволюційною стратегією ES(1+λ) (<see cref="EsTrainer"/>)
/// і зберігаються у BestWeights_*.json; під час польоту та Monte-Carlo ваги заморожені.
/// </summary>
[Serializable]
public sealed class MlpPolicy
{
    public const int InputSize = 5;
    public const int HiddenSize = 8;
    public const int OutputSize = 2;
    public const int ParameterCount = HiddenSize * InputSize + HiddenSize + OutputSize * HiddenSize + OutputSize;

    /// <summary>Максимальна поправка тяги, частка m·g.</summary>
    public const float ResidualFrac = 0.3f;
    /// <summary>Діапазон ln-масштабу бічного наведення.</summary>
    public const float LateralRange = 0.5f;

    public float[] wIH = new float[HiddenSize * InputSize]; // [h * InputSize + i]
    public float[] bH = new float[HiddenSize];
    public float[] wHO = new float[OutputSize * HiddenSize]; // [o * HiddenSize + h]
    public float[] bO = new float[OutputSize];

    [Header("Метадані навчання")]
    public string baseLaw = "profile";
    public string trainedBy = "untrained";
    public int generations;
    public int lambda;
    public int batchSize;
    public float sigmaFinal;
    public float trainCost;
    public float validationCost;
    public float validationSuccess;
    public float baselineValidationCost;
    public float baselineValidationSuccess;
    public string trainedAt = "";

    [NonSerialized] float[] xBuf;
    [NonSerialized] float[] hBuf;

    public bool IsTrained => trainedBy != "untrained";

    /// <summary>Нейтральна мережа: усі ваги 0 ⇒ ΔT = 0, k = 1 (рівно базовий закон).</summary>
    public static MlpPolicy CreateNeutral(string baseLaw)
        => new MlpPolicy { baseLaw = baseLaw };

    /// <summary>Нормовані входи мережі.</summary>
    public static void Features(in ControlContext ctx, float[] x)
    {
        x[0] = Mathf.Clamp(ctx.Height / 2000f, 0f, 1.5f);
        x[1] = Mathf.Clamp(-ctx.VerticalVelocity / 60f, -0.5f, 2f);
        x[2] = Mathf.Clamp((ctx.Mass - 33000f) / 8000f, -1f, 1f);
        x[3] = Mathf.Clamp(ctx.TiltDeg / 10f, 0f, 3f);
        x[4] = Mathf.Clamp(ctx.HorizSpeed / 10f, 0f, 3f);
    }

    /// <summary>Пряме поширення. Повертає (thrustFrac ∈ [−R;R], lateralScale &gt; 0).</summary>
    public void Infer(in ControlContext ctx, out float thrustFrac, out float lateralScale)
    {
        if (xBuf == null) { xBuf = new float[InputSize]; hBuf = new float[HiddenSize]; }
        Features(in ctx, xBuf);
        for (int h = 0; h < HiddenSize; h++)
        {
            float s = bH[h];
            int b = h * InputSize;
            for (int i = 0; i < InputSize; i++) s += wIH[b + i] * xBuf[i];
            hBuf[h] = (float)Math.Tanh(s);
        }
        float y0 = bO[0], y1 = bO[1];
        for (int h = 0; h < HiddenSize; h++)
        {
            y0 += wHO[h] * hBuf[h];
            y1 += wHO[HiddenSize + h] * hBuf[h];
        }
        thrustFrac = ResidualFrac * (float)Math.Tanh(y0);
        lateralScale = (float)Math.Exp(LateralRange * Math.Tanh(y1));
    }

    /// <summary>
    /// Застосувати MLP-residual до базової команди: T = T_base + ΔT (ΔT згасає нижче 15 м → 0 на 3 м,
    /// щоб торкання завжди виконував інтерпретований базовий закон), бічний масштаб = k.
    /// </summary>
    public ControlCommand ApplyResidual(in ControlContext ctx, in ControlCommand baseCmd)
    {
        Infer(in ctx, out float frac, out float lat);
        float fade = Mathf.Clamp01((ctx.Height - 3f) / 12f);
        float hover = ctx.Mass * ctx.Gravity;
        float thrust = Mathf.Max(0f, baseCmd.Thrust + frac * hover * fade);
        return new ControlCommand(thrust, baseCmd.GimbalEuler, lat);
    }

    // ---------- вектор параметрів (для ES) ----------

    public float[] GetParameters()
    {
        var p = new float[ParameterCount];
        int k = 0;
        Array.Copy(wIH, 0, p, k, wIH.Length); k += wIH.Length;
        Array.Copy(bH, 0, p, k, bH.Length); k += bH.Length;
        Array.Copy(wHO, 0, p, k, wHO.Length); k += wHO.Length;
        Array.Copy(bO, 0, p, k, bO.Length);
        return p;
    }

    public void SetParameters(float[] p)
    {
        if (p == null || p.Length != ParameterCount)
            throw new ArgumentException($"Expected {ParameterCount} parameters");
        int k = 0;
        Array.Copy(p, k, wIH, 0, wIH.Length); k += wIH.Length;
        Array.Copy(p, k, bH, 0, bH.Length); k += bH.Length;
        Array.Copy(p, k, wHO, 0, wHO.Length); k += wHO.Length;
        Array.Copy(p, k, bO, 0, bO.Length);
    }

    public MlpPolicy Clone()
    {
        var c = (MlpPolicy)MemberwiseClone();
        c.wIH = (float[])wIH.Clone();
        c.bH = (float[])bH.Clone();
        c.wHO = (float[])wHO.Clone();
        c.bO = (float[])bO.Clone();
        c.xBuf = null;
        c.hBuf = null;
        return c;
    }

    // ---------- JSON (без залежності від JsonUtility — працює і в headless-тренері) ----------

    public string ToJson()
    {
        var ci = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        sb.Append("{\n");
        sb.Append($"  \"format\": \"betelgeuse-mlp-v2\",\n");
        sb.Append($"  \"inputSize\": {InputSize},\n  \"hiddenSize\": {HiddenSize},\n  \"outputSize\": {OutputSize},\n");
        sb.Append($"  \"inputs\": \"h/2000, -Vy/60, (m-33000)/8000, tilt/10deg, Vh/10\",\n");
        sb.Append($"  \"outputs\": \"dT = {ResidualFrac.ToString(ci)}*tanh(y0)*m*g ; k_lat = exp({LateralRange.ToString(ci)}*tanh(y1))\",\n");
        sb.Append($"  \"baseLaw\": \"{baseLaw}\",\n");
        sb.Append($"  \"trainedBy\": \"{trainedBy}\",\n");
        sb.Append($"  \"generations\": {generations},\n  \"lambda\": {lambda},\n  \"batchSize\": {batchSize},\n");
        sb.Append($"  \"sigmaFinal\": {F(sigmaFinal)},\n  \"trainCost\": {F(trainCost)},\n");
        sb.Append($"  \"validationCost\": {F(validationCost)},\n  \"validationSuccess\": {F(validationSuccess)},\n");
        sb.Append($"  \"baselineValidationCost\": {F(baselineValidationCost)},\n  \"baselineValidationSuccess\": {F(baselineValidationSuccess)},\n");
        sb.Append($"  \"trainedAt\": \"{trainedAt}\",\n");
        sb.Append($"  \"wIH\": {Arr(wIH)},\n  \"bH\": {Arr(bH)},\n  \"wHO\": {Arr(wHO)},\n  \"bO\": {Arr(bO)}\n");
        sb.Append("}\n");
        return sb.ToString();
    }

    static string F(float v) => v.ToString("0.######", CultureInfo.InvariantCulture);

    static string Arr(float[] a)
    {
        var sb = new StringBuilder("[");
        for (int i = 0; i < a.Length; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(a[i].ToString("R", CultureInfo.InvariantCulture));
        }
        return sb.Append(']').ToString();
    }

    /// <summary>Розбір файлу формату v2. Повертає false для старого формату/невідповідної топології.</summary>
    public static bool TryFromJson(string json, out MlpPolicy policy)
    {
        policy = null;
        if (string.IsNullOrEmpty(json) || !json.Contains("betelgeuse-mlp-v2")) return false;
        try
        {
            var p = new MlpPolicy
            {
                baseLaw = Str(json, "baseLaw") ?? "profile",
                trainedBy = Str(json, "trainedBy") ?? "untrained",
                generations = (int)Num(json, "generations"),
                lambda = (int)Num(json, "lambda"),
                batchSize = (int)Num(json, "batchSize"),
                sigmaFinal = Num(json, "sigmaFinal"),
                trainCost = Num(json, "trainCost"),
                validationCost = Num(json, "validationCost"),
                validationSuccess = Num(json, "validationSuccess"),
                baselineValidationCost = Num(json, "baselineValidationCost"),
                baselineValidationSuccess = Num(json, "baselineValidationSuccess"),
                trainedAt = Str(json, "trainedAt") ?? ""
            };
            if (!ReadArr(json, "wIH", p.wIH) || !ReadArr(json, "bH", p.bH)
                || !ReadArr(json, "wHO", p.wHO) || !ReadArr(json, "bO", p.bO))
                return false;
            policy = p;
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public void Save(string path) => File.WriteAllText(path, ToJson());

    public static MlpPolicy LoadOrNull(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
        return TryFromJson(File.ReadAllText(path), out var p) ? p : null;
    }

    static int ValueStart(string json, string key)
    {
        int k = json.IndexOf("\"" + key + "\"", StringComparison.Ordinal);
        if (k < 0) return -1;
        int c = json.IndexOf(':', k);
        return c < 0 ? -1 : c + 1;
    }

    static string Str(string json, string key)
    {
        int s = ValueStart(json, key);
        if (s < 0) return null;
        int q1 = json.IndexOf('"', s);
        int q2 = q1 < 0 ? -1 : json.IndexOf('"', q1 + 1);
        return q2 < 0 ? null : json.Substring(q1 + 1, q2 - q1 - 1);
    }

    static float Num(string json, string key)
    {
        int s = ValueStart(json, key);
        if (s < 0) return 0f;
        int e = s;
        while (e < json.Length && ",}\n\r".IndexOf(json[e]) < 0) e++;
        return float.Parse(json.Substring(s, e - s).Trim(), CultureInfo.InvariantCulture);
    }

    static bool ReadArr(string json, string key, float[] dst)
    {
        int s = ValueStart(json, key);
        if (s < 0) return false;
        int a = json.IndexOf('[', s), b = json.IndexOf(']', a + 1);
        if (a < 0 || b < 0) return false;
        var parts = json.Substring(a + 1, b - a - 1).Split(',');
        if (parts.Length != dst.Length) return false;
        for (int i = 0; i < parts.Length; i++)
            dst[i] = float.Parse(parts[i].Trim(), CultureInfo.InvariantCulture);
        return true;
    }
}
