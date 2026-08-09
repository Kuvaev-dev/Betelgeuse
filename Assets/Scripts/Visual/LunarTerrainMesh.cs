using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Процедурний диск Місяця (R≈2000 м) для демо посадки.
/// Декартова heightmap + crater field (simple/complex bowl, ejecta, mare).
/// C2-профілі кратерів, soft-min накладання, нейтральний сірий albedo.
/// </summary>
public static class LunarTerrainMesh
{
    /// <summary>Рівна зона під палубою (berm ~R64); кратери одразу за краєм.</summary>
    public const float PadClearRadius = 68f;

    /// <summary>Радіус cratered-диска (HorizonDisk ≤ цього).</summary>
    public const float TerrainRadius = 2000f;

    public static Mesh Build(out Texture2D albedoTex, int resolution = 256, float radius = -1f, int seed = 42)
    {
        if (radius < 1f) radius = TerrainRadius;
        // Вища сітка → менше «піксельних» кратерів
        resolution = Mathf.Clamp(resolution, 96, 448);
        var rng = new System.Random(seed);
        var craters = BuildCraterField(rng, radius);

        int n = resolution + 1;
        float half = radius;
        float step = (half * 2f) / resolution;
        var height = new float[n, n];
        var shade = new float[n, n];

        for (int iz = 0; iz < n; iz++)
        {
            for (int ix = 0; ix < n; ix++)
            {
                float x = -half + ix * step;
                float z = -half + iz * step;
                height[ix, iz] = SampleHeight(x, z, craters, radius, out float sh);
                shade[ix, iz] = sh;
            }
        }

        // 3×3 згладжування — природні схили без «сходинок»
        SmoothHeightField(height, n, 3);
        SmoothHeightField(shade, n, 2);

        int texSize = Mathf.ClosestPowerOfTwo(Mathf.Clamp(resolution * 4, 1024, 2048));
        albedoTex = BuildAlbedo(shade, n, texSize, radius, craters, seed);

        var vertList = new List<Vector3>(n * n / 2);
        var uvList = new List<Vector2>(n * n / 2);
        var map = new int[n, n];
        for (int iz = 0; iz < n; iz++)
            for (int ix = 0; ix < n; ix++)
                map[ix, iz] = -1;

        float r2 = radius * radius * 1.002f;
        for (int iz = 0; iz < n; iz++)
        {
            for (int ix = 0; ix < n; ix++)
            {
                float x = -half + ix * step;
                float z = -half + iz * step;
                if (x * x + z * z > r2) continue;

                float dist = Mathf.Sqrt(x * x + z * z);
                float h = height[ix, iz];
                // М'який спад лише на 1.5% краю — без широкого «порожнього» обідка
                float edge = Mathf.Clamp01((radius - dist) / (radius * 0.015f));
                if (edge < 1f) h = Mathf.Lerp(h - 2.5f, h, edge);

                map[ix, iz] = vertList.Count;
                vertList.Add(new Vector3(x, h, z));
                uvList.Add(new Vector2(ix / (float)resolution, iz / (float)resolution));
            }
        }

        // Одна сторона (вгору) — без double-sided faceting
        var tris = new List<int>(resolution * resolution * 6);
        for (int iz = 0; iz < resolution; iz++)
        {
            for (int ix = 0; ix < resolution; ix++)
            {
                int i00 = map[ix, iz];
                int i10 = map[ix + 1, iz];
                int i01 = map[ix, iz + 1];
                int i11 = map[ix + 1, iz + 1];
                if (i00 < 0 || i10 < 0 || i01 < 0 || i11 < 0) continue;

                tris.Add(i00); tris.Add(i01); tris.Add(i10);
                tris.Add(i10); tris.Add(i01); tris.Add(i11);
            }
        }

        var verts = vertList.ToArray();
        var norms = new Vector3[verts.Length];
        for (int i = 0; i < norms.Length; i++) norms[i] = Vector3.zero;

        var triArr = tris.ToArray();
        for (int t = 0; t < triArr.Length; t += 3)
        {
            int i0 = triArr[t], i1 = triArr[t + 1], i2 = triArr[t + 2];
            Vector3 e1 = verts[i1] - verts[i0];
            Vector3 e2 = verts[i2] - verts[i0];
            Vector3 faceN = Vector3.Cross(e1, e2);
            // area-weighted
            norms[i0] += faceN;
            norms[i1] += faceN;
            norms[i2] += faceN;
        }
        for (int i = 0; i < norms.Length; i++)
        {
            if (norms[i].sqrMagnitude > 1e-12f) norms[i].Normalize();
            else norms[i] = Vector3.up;
        }

        var mesh = new Mesh
        {
            name = "LunarTerrainDisk",
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        };
        mesh.vertices = verts;
        mesh.normals = norms;
        mesh.uv = uvList.ToArray();
        mesh.triangles = triArr;
        mesh.RecalculateBounds();

        Debug.Log($"[LunarTerrain] verts={verts.Length} tris={triArr.Length / 3} craters={craters.Length}");
        return mesh;
    }

    static Texture2D BuildAlbedo(float[,] shade, int n, int texSize, float terrainRadius, Crater[] craters, int seed)
    {
        var tex = new Texture2D(texSize, texSize, TextureFormat.RGB24, true);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        var cols = new Color[texSize * texSize];
        var rng = new System.Random(seed + 91);

        // Mare patches (темніші «моря»)
        int mareCount = 5 + rng.Next(0, 3);
        var mareCx = new float[mareCount];
        var mareCz = new float[mareCount];
        var mareR = new float[mareCount];
        for (int i = 0; i < mareCount; i++)
        {
            float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
            float dist = 400f + (float)rng.NextDouble() * terrainRadius * 0.55f;
            mareCx[i] = Mathf.Cos(ang) * dist;
            mareCz[i] = Mathf.Sin(ang) * dist;
            mareR[i] = 280f + (float)rng.NextDouble() * 520f;
        }

        float half = terrainRadius;
        for (int y = 0; y < texSize; y++)
        {
            for (int x = 0; x < texSize; x++)
            {
                float u = x / (float)(texSize - 1);
                float v = y / (float)(texSize - 1);
                float wx = -half + u * half * 2f;
                float wz = -half + v * half * 2f;

                float fx = u * (n - 1);
                float fy = v * (n - 1);
                int x0 = Mathf.Clamp(Mathf.FloorToInt(fx), 0, n - 2);
                int y0 = Mathf.Clamp(Mathf.FloorToInt(fy), 0, n - 2);
                float tx = fx - x0;
                float ty = fy - y0;
                float s00 = shade[x0, y0];
                float s10 = shade[x0 + 1, y0];
                float s01 = shade[x0, y0 + 1];
                float s11 = shade[x0 + 1, y0 + 1];
                float s = Mathf.Lerp(Mathf.Lerp(s00, s10, tx), Mathf.Lerp(s01, s11, tx), ty);
                s = Mathf.Clamp01(s);

                // Плавна крива shade→albedo (без ступінчастих band'ів)
                float g = SmoothAlbedoCurve(s);

                // Mare darkening (нейтрально-сірі «моря»)
                float mare = 0f;
                for (int m = 0; m < mareCount; m++)
                {
                    float md = Mathf.Sqrt((wx - mareCx[m]) * (wx - mareCx[m]) + (wz - mareCz[m]) * (wz - mareCz[m]));
                    float mr = mareR[m];
                    if (md < mr)
                    {
                        float t = 1f - md / mr;
                        t = t * t * (3f - 2f * t);
                        mare = Mathf.Max(mare, t * 0.16f);
                    }
                }
                g *= 1f - mare;

                // Multi-scale regolith grain (continuous noise)
                float nse = Noise2(wx * 0.06f, wz * 0.06f) * 0.014f;
                nse += Noise2(wx * 0.18f + 4f, wz * 0.18f - 3f) * 0.008f;
                nse += Noise2(wx * 0.45f - 2f, wz * 0.45f + 7f) * 0.004f;
                float padDist = Mathf.Sqrt(wx * wx + wz * wz);
                if (padDist < PadClearRadius + 55f)
                {
                    float pt = Mathf.SmoothStep(0f, 1f, padDist / (PadClearRadius + 55f));
                    g *= Mathf.Lerp(0.80f, 1f, pt);
                }
                // Ледь помітні «промені» ejecta біля великих кратерів (м'яко)
                g = Mathf.Clamp01(g + nse);

                // Нейтральний холоднувато-сірий (місячний реголіт)
                float rC = Mathf.Clamp01(g * 0.99f);
                float gC = g;
                float bC = Mathf.Clamp01(g * 1.02f);
                cols[y * texSize + x] = new Color(rC, gC, bC, 1f);
            }
        }
        tex.SetPixels(cols);
        tex.Apply(true, true);
        return tex;
    }

    struct Crater
    {
        public float x, z, radius, depth, rimH, ejecta, floorFrac;
        public float aspect, rot, peakH, peakR;
        public bool complex;
        public float terrace;
    }

    static Crater[] BuildCraterField(System.Random rng, float terrainRadius)
    {
        // Мінімальний відступ від pad; кратери йдуть майже до краю диска
        const float clear = PadClearRadius + 2f;
        var list = new List<Crater>(800);

        // Біля pad — кільце дрібних кратерів (природний «пояс»)
        for (int i = 0; i < 40; i++)
        {
            float ang = i * (Mathf.PI * 2f / 40f) + 0.08f * (float)rng.NextDouble();
            float R = 5f + (float)rng.NextDouble() * 22f;
            float dist = clear + R * 1.05f + (float)rng.NextDouble() * 55f;
            float x = Mathf.Cos(ang) * dist;
            float z = Mathf.Sin(ang) * dist;
            if (Fits(x, z, R * 1.25f, clear, terrainRadius))
                list.Add(Make(x, z, R, R * 0.22f, true, false, rng));
        }

        // Великі complex-басейни
        float[] bigR = { 280f, 220f, 340f, 180f, 260f, 200f, 300f, 165f, 240f, 190f };
        for (int i = 0; i < bigR.Length; i++)
        {
            float R = bigR[i];
            float ang = i * (Mathf.PI * 2f / bigR.Length) + 0.3f;
            float dist = 200f + i * 38f + R * 0.22f;
            dist = Mathf.Min(dist, terrainRadius * 0.72f);
            float x = Mathf.Cos(ang) * dist;
            float z = Mathf.Sin(ang) * dist;
            if (Fits(x, z, R * 1.5f, clear, terrainRadius))
                list.Add(Make(x, z, R, R * 0.12f, false, true, rng));
        }

        for (int i = 0; i < 18; i++)
            TryAdd(list, rng, terrainRadius, clear, 110f, 240f, R => R * 0.14f, false, true, 55);
        for (int i = 0; i < 90; i++)
            TryAdd(list, rng, terrainRadius, clear, 32f, 105f, R => R * 0.20f, false, false, 40);
        for (int i = 0; i < 140; i++)
            TryAdd(list, rng, terrainRadius, clear, 9f, 36f, R => R * 0.24f, true, false, 28);
        for (int i = 0; i < 140; i++)
            TryAdd(list, rng, terrainRadius, clear, 3.5f, 11f, R => R * 0.28f, true, false, 20);

        // Зовнішнє кільце — заповнює край диска (прибирає «порожню» облямівку)
        for (int i = 0; i < 90; i++)
        {
            float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
            float R = 6f + (float)rng.NextDouble() * 40f;
            float dist = terrainRadius * (0.78f + (float)rng.NextDouble() * 0.17f);
            float x = Mathf.Cos(ang) * dist;
            float z = Mathf.Sin(ang) * dist;
            if (!Fits(x, z, R * 1.25f, clear, terrainRadius)) continue;
            list.Add(Make(x, z, R, R * 0.22f, R < 18f, false, rng));
        }
        // Мікрократери на самому обідку
        for (int i = 0; i < 120; i++)
        {
            float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
            float R = 3f + (float)rng.NextDouble() * 10f;
            float dist = terrainRadius * (0.88f + (float)rng.NextDouble() * 0.09f);
            float x = Mathf.Cos(ang) * dist;
            float z = Mathf.Sin(ang) * dist;
            if (!Fits(x, z, R * 1.15f, clear, terrainRadius)) continue;
            list.Add(Make(x, z, R, R * 0.26f, true, false, rng));
        }

        // Secondary cluster біля великих
        int n0 = list.Count;
        for (int i = 0; i < n0 && list.Count < 560; i++)
        {
            if (list[i].radius < 90f) continue;
            int ns = 4 + rng.Next(0, 7);
            for (int s = 0; s < ns; s++)
            {
                float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
                float d = list[i].radius * (1.2f + (float)rng.NextDouble() * 1.8f);
                float sx = list[i].x + Mathf.Cos(ang) * d;
                float sz = list[i].z + Mathf.Sin(ang) * d;
                float sR = 5f + (float)rng.NextDouble() * 26f;
                if (!Fits(sx, sz, sR * 1.4f, clear, terrainRadius)) continue;
                list.Add(Make(sx, sz, sR, sR * 0.22f, true, false, rng));
            }
        }

        return list.ToArray();
    }

    static void TryAdd(List<Crater> list, System.Random rng, float terrainRadius,
        float clearZone, float rMin, float rMax, System.Func<float, float> depthFn,
        bool small, bool complex, int attempts)
    {
        for (int attempt = 0; attempt < attempts; attempt++)
        {
            float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
            float R = rMin + (float)rng.NextDouble() * (rMax - rMin);
            float ejecta = 1.35f + (float)rng.NextDouble() * 0.35f;
            float minD = clearZone + R * ejecta + 1f;
            // Кратери майже до краю (раніше 0.86 лишало порожнє кільце)
            float maxD = terrainRadius * 0.96f - R * 0.5f;
            if (minD >= maxD) return;
            float dist = minD + (float)rng.NextDouble() * (maxD - minD);
            float x = Mathf.Cos(ang) * dist;
            float z = Mathf.Sin(ang) * dist;
            if (!Fits(x, z, R * ejecta, clearZone, terrainRadius)) continue;
            list.Add(Make(x, z, R, depthFn(R), small, complex, rng));
            return;
        }
    }

    static bool Fits(float x, float z, float inflR, float clear, float terrainR)
    {
        float d = Mathf.Sqrt(x * x + z * z);
        // Дозволяємо кратери до ~97% радіуса (раніше 88% → порожній обідок)
        if (d + inflR * 0.35f > terrainR * 0.985f) return false;
        if (d > terrainR * 0.97f) return false;
        return d - inflR * 0.85f >= clear;
    }

    static Crater Make(float x, float z, float R, float depth, bool small, bool complex, System.Random rng)
    {
        // Simple lunar: depth/D ≈ 0.15–0.25; complex: shallower + central peak
        float dRatio = complex ? (0.08f + (float)rng.NextDouble() * 0.06f)
                               : (0.16f + (float)rng.NextDouble() * 0.10f);
        float d = Mathf.Max(depth, R * 2f * dRatio * 0.55f);
        d = Mathf.Min(d, R * 0.55f);

        var c = new Crater
        {
            x = x, z = z, radius = R,
            depth = d,
            ejecta = 1.4f + (float)rng.NextDouble() * 0.35f,
            floorFrac = complex
                ? 0.38f + (float)rng.NextDouble() * 0.12f
                : 0.22f + (float)rng.NextDouble() * 0.12f,
            aspect = 0.90f + (float)rng.NextDouble() * 0.18f,
            rot = (float)rng.NextDouble() * Mathf.PI,
            peakH = 0f, peakR = 0f,
            complex = complex,
            terrace = 0f
        };

        c.rimH = c.depth * (0.22f + (float)rng.NextDouble() * 0.16f);
        if (small)
        {
            c.rimH *= 0.8f;
            c.ejecta *= 0.85f;
        }

        if (complex && R > 90f)
        {
            c.peakH = c.depth * (0.22f + (float)rng.NextDouble() * 0.28f);
            c.peakR = R * (0.08f + (float)rng.NextDouble() * 0.08f);
            c.terrace = 0.55f + (float)rng.NextDouble() * 0.25f;
        }

        return c;
    }

    static float SampleHeight(float x, float z, Crater[] craters, float terrainRadius, out float shade)
    {
        float dist = Mathf.Sqrt(x * x + z * z);
        shade = 0.54f;

        if (dist <= PadClearRadius)
        {
            // Ідеально рівна зона під падом + ледь помітний мікровисочинний шум
            float micro = Noise2(x * 0.35f, z * 0.35f) * 0.012f;
            // Легке «випалення» ближче до центру (темніший shade)
            float padT = dist / Mathf.Max(1f, PadClearRadius);
            shade = Mathf.Lerp(0.42f, 0.56f, padT * padT);
            return micro;
        }

        // Multi-octave highland undulation (smooth hills, no grid feel)
        float h = 0f;
        h += Noise2(x * 0.0022f, z * 0.0022f) * 4.0f;
        h += Noise2(x * 0.0065f + 11f, z * 0.0065f - 7f) * 1.8f;
        h += Noise2(x * 0.018f, z * 0.018f) * 0.7f;
        h += Noise2(x * 0.055f + 3f, z * 0.055f) * 0.28f;
        h += Noise2(x * 0.14f - 5f, z * 0.14f + 2f) * 0.1f;
        h += Noise2(x * 0.35f + 1f, z * 0.35f - 2f) * 0.035f;
        float edgeN = dist / Mathf.Max(1f, terrainRadius);
        h += edgeN * edgeN * 1.8f;

        // Плавний перехід pad → рельєф
        float blend = Mathf.SmoothStep(0f, 1f, (dist - PadClearRadius) / 22f);
        h *= blend;

        float floorH = 0f;
        float rimH = 0f;
        float minShade = shade;
        bool anyCrater = false;

        for (int i = 0; i < craters.Length; i++)
        {
            Crater c = craters[i];
            float dx = x - c.x;
            float dz = z - c.z;
            float ca = Mathf.Cos(c.rot), sa = Mathf.Sin(c.rot);
            float lx = (dx * ca + dz * sa) / c.aspect;
            float lz = -dx * sa + dz * ca;
            float d = Mathf.Sqrt(lx * lx + lz * lz);
            float outerR = c.radius * c.ejecta;
            if (d > outerR) continue;

            float p = CraterProfile(d, c, out float localShade) * blend;
            anyCrater = true;

            // Soft-min / soft-max — природне накладання кратерів
            if (p < 0f)
                floorH = SoftMin(floorH, p, 1.2f);
            else
                rimH = SoftMax(rimH, p, 0.9f);

            if (localShade < minShade)
                minShade = Mathf.Lerp(minShade, localShade, 0.85f);
        }

        float craterH = floorH + rimH;
        if (!anyCrater) craterH = 0f;

        // М'яка корекція shade від глибини (без жорстких порогів)
        shade = minShade;
        if (craterH < -0.2f)
        {
            float depthT = Mathf.Clamp01((-craterH) / 8f);
            shade = Mathf.Min(shade, Mathf.Lerp(0.18f, 0.03f, Quintic01(depthT)));
        }
        else if (rimH > 0.25f)
        {
            float rimT = Mathf.Clamp01(rimH / 4f);
            shade = Mathf.Clamp(Mathf.Max(shade, Mathf.Lerp(0.54f, 0.60f, rimT)), 0f, 0.62f);
        }

        return h + craterH;
    }

    /// <summary>
    /// Природний lunar bowl: C2-гладкі переходи floor→wall→rim→ejecta.
    /// Без гострих зламів і «сходинок».
    /// </summary>
    static float CraterProfile(float d, Crater c, out float shade)
    {
        float R = Mathf.Max(1f, c.radius);
        float t = d / R;
        float floorT = Mathf.Clamp(c.floorFrac, 0.12f, 0.55f);
        shade = 0.54f;

        // Central peak (complex) — gaussian, dark
        if (c.peakH > 0.05f && t < floorT * 1.15f)
        {
            float pt = d / Mathf.Max(0.35f, c.peakR);
            float peak = c.peakH * Mathf.Exp(-pt * pt * 1.55f);
            float floorBase = -c.depth + 0.04f * c.depth * (t / Mathf.Max(0.05f, floorT));
            shade = Mathf.Lerp(0.06f, 0.12f, Mathf.Clamp01(pt * 0.5f));
            return floorBase + peak;
        }

        if (t <= floorT)
        {
            // Майже плоске дно + ледь піднятий край дна
            float ft = t / Mathf.Max(0.05f, floorT);
            float bowl = Quintic01(ft) * 0.05f * c.depth;
            shade = Mathf.Lerp(0.03f, 0.08f, ft);
            return -c.depth + bowl;
        }

        if (t <= 1f)
        {
            float u = (t - floorT) / Mathf.Max(0.08f, 1f - floorT);
            u = Mathf.Clamp01(u);
            // Quintic smoothstep — C2, без кутів
            float wall = Quintic01(u);

            // М'яка тераса (complex) — локальний bump, не злам
            if (c.complex && c.terrace > 0.15f && c.terrace < 0.9f)
            {
                float tw = 0.14f;
                float td = (u - c.terrace) / tw;
                float k = Mathf.Exp(-td * td * 2.2f);
                wall = Mathf.Clamp01(wall + k * 0.045f);
            }

            // Стінка: темне дно → сірий вал
            shade = Mathf.Lerp(0.06f, 0.58f, wall);
            float h = Mathf.Lerp(-c.depth, c.rimH, wall);

            // М'який crest на валу (parabola bump)
            float crestW = 0.18f;
            float crestT = (u - (1f - crestW)) / crestW;
            if (crestT > 0f && crestT < 1f)
            {
                float bump = crestT * (1f - crestT) * 4f; // 0..1..0
                h += c.rimH * 0.1f * bump;
                shade = Mathf.Min(0.62f, shade + 0.04f * bump);
            }
            return h;
        }

        // Ejecta blanket — плавний спад
        float te = (t - 1f) / Mathf.Max(0.12f, c.ejecta - 1f);
        if (te >= 1f) { shade = 0.54f; return 0f; }
        te = Mathf.Clamp01(te);
        float fall = 1f - Quintic01(te);
        shade = Mathf.Lerp(0.58f, 0.54f, te);
        return c.rimH * fall * 0.55f;
    }

    static float Quintic01(float t)
    {
        t = Mathf.Clamp01(t);
        // 6t^5 - 15t^4 + 10t^3
        return t * t * t * (t * (t * 6f - 15f) + 10f);
    }

    static float SoftMin(float a, float b, float k)
    {
        // polynomial soft-min (k > 0)
        float h = Mathf.Clamp01(0.5f + 0.5f * (b - a) / k);
        return Mathf.Lerp(b, a, h) - k * h * (1f - h);
    }

    static float SoftMax(float a, float b, float k)
    {
        float h = Mathf.Clamp01(0.5f + 0.5f * (a - b) / k);
        return Mathf.Lerp(b, a, h) + k * h * (1f - h);
    }

    static float SmoothAlbedoCurve(float s)
    {
        s = Mathf.Clamp01(s);
        // М'яка сіра палітра: дно темно-сіре → рівнина → ледь світліший вал
        if (s < 0.12f)
            return Mathf.Lerp(0.04f, 0.10f, Quintic01(s / 0.12f));
        if (s < 0.32f)
            return Mathf.Lerp(0.10f, 0.32f, Quintic01((s - 0.12f) / 0.20f));
        if (s < 0.52f)
            return Mathf.Lerp(0.32f, 0.48f, Quintic01((s - 0.32f) / 0.20f));
        if (s < 0.72f)
            return Mathf.Lerp(0.48f, 0.54f, Quintic01((s - 0.52f) / 0.20f));
        return Mathf.Lerp(0.54f, 0.60f, Quintic01((s - 0.72f) / 0.28f));
    }

    static void SmoothHeightField(float[,] f, int n, int passes)
    {
        if (f == null || n < 3 || passes <= 0) return;
        var tmp = new float[n, n];
        for (int p = 0; p < passes; p++)
        {
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float sum = 0f;
                float w = 0f;
                for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int xx = x + dx, yy = y + dy;
                    if (xx < 0 || yy < 0 || xx >= n || yy >= n) continue;
                    float ww = (dx == 0 && dy == 0) ? 4f : ((dx == 0 || dy == 0) ? 2f : 1f);
                    sum += f[xx, yy] * ww;
                    w += ww;
                }
                tmp[x, y] = sum / Mathf.Max(1e-6f, w);
            }
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
                f[x, y] = tmp[x, y];
        }
    }

    static float Noise2(float x, float y)
    {
        int x0 = Mathf.FloorToInt(x);
        int y0 = Mathf.FloorToInt(y);
        float fx = x - x0;
        float fy = y - y0;
        fx = fx * fx * (3f - 2f * fx);
        fy = fy * fy * (3f - 2f * fy);
        float v00 = Hash(x0, y0);
        float v10 = Hash(x0 + 1, y0);
        float v01 = Hash(x0, y0 + 1);
        float v11 = Hash(x0 + 1, y0 + 1);
        return Mathf.Lerp(Mathf.Lerp(v00, v10, fx), Mathf.Lerp(v01, v11, fx), fy) * 2f - 1f;
    }

    static float Hash(int x, int y)
    {
        unchecked
        {
            int h = x * 374761393 + y * 668265263;
            h = (h ^ (h >> 13)) * 1274126177;
            h ^= h >> 16;
            return (h & 0x7fffffff) / (float)0x7fffffff;
        }
    }

    public static GameObject Create(Transform parent, Material baseMat,
        int resolution = 256, float radius = -1f)
    {
        if (radius < 1f) radius = TerrainRadius;
        var go = new GameObject("LunarTerrain");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;

        var mesh = Build(out Texture2D albedo, Mathf.Max(resolution, 400), radius, 42);
        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        var mat = new Material(baseMat != null ? baseMat.shader : VisualMaterials.LitShader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.04f);
        if (mat.HasProperty("_BaseMap"))
        {
            mat.SetTexture("_BaseMap", albedo);
            mat.EnableKeyword("_BASEMAP");
        }
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", albedo);
        mat.mainTexture = albedo;

        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        mr.receiveShadows = true;
        // Back-face cull — чистіші нормалі/тіні на рельєфі
        if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 2f);
        mat.doubleSidedGI = false;

        return go;
    }
}
