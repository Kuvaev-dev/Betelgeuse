using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Круглий диск Місяця: декартова сітка, обрізана по колу.
/// Кратери simple/complex по всьому диску (включно з краєм — без «порожнього кільця»).
/// Albedo: чорне дно, сіра рівнина, стриманий вал, mare patches.
/// </summary>
public static class LunarTerrainMesh
{
    /// <summary>Рівна зона під палубою (~pad deck R≈51); кратери одразу за краєм.</summary>
    public const float PadClearRadius = 52f;

    /// <summary>Радіус cratered-диска (HorizonDisk ≤ цього).</summary>
    public const float TerrainRadius = 2000f;

    public static Mesh Build(out Texture2D albedoTex, int resolution = 256, float radius = -1f, int seed = 42)
    {
        if (radius < 1f) radius = TerrainRadius;
        resolution = Mathf.Clamp(resolution, 64, 384);
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

        int texSize = Mathf.ClosestPowerOfTwo(Mathf.Clamp(resolution * 2, 256, 1024));
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

        var tris = new List<int>(resolution * resolution * 12);
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
                tris.Add(i00); tris.Add(i10); tris.Add(i01);
                tris.Add(i10); tris.Add(i11); tris.Add(i01);
            }
        }

        var verts = vertList.ToArray();
        var norms = new Vector3[verts.Length];
        for (int i = 0; i < norms.Length; i++) norms[i] = Vector3.zero;

        var triArr = tris.ToArray();
        for (int t = 0; t < triArr.Length; t += 6)
        {
            for (int k = 0; k < 6; k += 3)
            {
                int i0 = triArr[t + k], i1 = triArr[t + k + 1], i2 = triArr[t + k + 2];
                Vector3 faceN = Vector3.Cross(verts[i1] - verts[i0], verts[i2] - verts[i0]);
                if (faceN.y < 0f) continue;
                norms[i0] += faceN;
                norms[i1] += faceN;
                norms[i2] += faceN;
            }
        }
        for (int i = 0; i < norms.Length; i++)
        {
            if (norms[i].sqrMagnitude > 1e-10f) norms[i].Normalize();
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

                // Дно = майже чорне; рівнина сіра; вал трохи світліший (без білих променів)
                float g;
                if (s < 0.12f)
                    g = Mathf.Lerp(0.01f, 0.04f, s / 0.12f);           // дно — чорне
                else if (s < 0.28f)
                    g = Mathf.Lerp(0.04f, 0.18f, (s - 0.12f) / 0.16f); // нижня стінка
                else if (s < 0.50f)
                    g = Mathf.Lerp(0.18f, 0.42f, (s - 0.28f) / 0.22f); // стінка
                else if (s < 0.72f)
                    g = Mathf.Lerp(0.42f, 0.52f, (s - 0.50f) / 0.22f); // рівнина
                else
                    g = Mathf.Lerp(0.52f, 0.62f, (s - 0.72f) / 0.28f); // вал (стриманий)

                // Mare darkening
                float mare = 0f;
                for (int m = 0; m < mareCount; m++)
                {
                    float md = Mathf.Sqrt((wx - mareCx[m]) * (wx - mareCx[m]) + (wz - mareCz[m]) * (wz - mareCz[m]));
                    float mr = mareR[m];
                    if (md < mr)
                    {
                        float t = 1f - md / mr;
                        t = t * t * (3f - 2f * t);
                        mare = Mathf.Max(mare, t * 0.18f);
                    }
                }
                g *= 1f - mare;

                // Дрібний шум реголіту (без радіальних «променів»)
                float nse = (Hash(x * 17, y * 23) - 0.5f) * 0.018f;
                nse += (Hash(x * 41 + 3, y * 37 - 5) - 0.5f) * 0.008f;
                g = Mathf.Clamp01(g + nse);

                float rC = g * 0.98f;
                float gC = g * 0.99f;
                float bC = Mathf.Clamp01(g * 1.01f);
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

        // Біля pad — одразу після clear-зони
        for (int i = 0; i < 32; i++)
        {
            float ang = i * (Mathf.PI * 2f / 32f) + 0.1f;
            float R = 7f + (float)rng.NextDouble() * 26f;
            float dist = clear + R * 1.1f + (float)rng.NextDouble() * 65f;
            float x = Mathf.Cos(ang) * dist;
            float z = Mathf.Sin(ang) * dist;
            if (Fits(x, z, R * 1.3f, clear, terrainRadius))
                list.Add(Make(x, z, R, R * 0.24f, true, false, rng));
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
            shade = 0.55f;
            return 0f;
        }

        // Multi-octave highland undulation (щільніше біля краю)
        float h = 0f;
        h += Noise2(x * 0.0028f, z * 0.0028f) * 3.4f;
        h += Noise2(x * 0.009f + 11f, z * 0.009f - 7f) * 1.25f;
        h += Noise2(x * 0.028f, z * 0.028f) * 0.42f;
        h += Noise2(x * 0.09f + 3f, z * 0.09f) * 0.18f;
        h += Noise2(x * 0.22f - 5f, z * 0.22f + 2f) * 0.07f;
        float edgeN = dist / Mathf.Max(1f, terrainRadius);
        h += edgeN * edgeN * 2.2f;

        // Швидкий перехід pad → рельєф (майже без «порожнього кільця»)
        float blend = Mathf.Clamp01((dist - PadClearRadius) / 8f);
        h *= blend * blend;

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

            if (p < 0f)
                floorH = Mathf.Min(floorH, p);
            else
                rimH = Mathf.Max(rimH, p);

            if (localShade < minShade)
                minShade = localShade;
        }

        float craterH = floorH + rimH;
        if (!anyCrater) craterH = 0f;

        shade = minShade;
        // Жорстко чорне дно
        if (craterH < -3f) shade = 0.02f;
        else if (craterH < -1.2f) shade = Mathf.Min(shade, 0.05f);
        else if (craterH < -0.35f) shade = Mathf.Min(shade, 0.12f);
        // Вал — без «білого» (макс ~0.65)
        else if (rimH > 0.5f) shade = Mathf.Min(Mathf.Max(shade, 0.58f), 0.65f);

        return h + craterH;
    }

    /// <summary>
    /// Lunar-like bowl: flat/raised floor → steep wall → sharp rim → ejecta blanket.
    /// Complex: central peak + mild terrace kink on wall.
    /// </summary>
    static float CraterProfile(float d, Crater c, out float shade)
    {
        float R = Mathf.Max(1f, c.radius);
        float t = d / R;
        float floorT = c.floorFrac;
        shade = 0.56f;

        // Central peak (complex) — теж темний, не світлий
        if (c.peakH > 0.05f && d < c.peakR * 2.8f && t < floorT * 1.2f)
        {
            float pt = d / Mathf.Max(0.25f, c.peakR);
            float peak = c.peakH * Mathf.Exp(-pt * pt * 1.35f);
            shade = 0.08f;
            return -c.depth + peak;
        }

        if (t <= floorT)
        {
            float ft = t / Mathf.Max(0.05f, floorT);
            float bowl = ft * ft * 0.06f * c.depth;
            shade = 0.015f; // чорне дно
            return -c.depth + bowl;
        }

        if (t <= 1f)
        {
            float u = (t - floorT) / Mathf.Max(0.06f, 1f - floorT);
            u = Mathf.Clamp01(u);

            float uPow = Mathf.Pow(u, 1.35f);
            float uSmooth = u * u * u * (u * (u * 6f - 15f) + 10f);
            float wall = Mathf.Lerp(uPow, uSmooth, 0.45f);

            if (c.complex && c.terrace > 0.1f)
            {
                float tw = 0.12f;
                float td = Mathf.Abs(u - c.terrace);
                if (td < tw)
                {
                    float k = 1f - td / tw;
                    wall += k * k * 0.06f;
                }
            }

            // Стінка: від чорного дна до сірого валу (без білого)
            shade = Mathf.Lerp(0.02f, 0.62f, wall);
            float h = Mathf.Lerp(-c.depth, c.rimH, wall);
            if (u > 0.88f)
            {
                float crest = (u - 0.88f) / 0.12f;
                h += c.rimH * 0.12f * crest * (1f - crest) * 4f;
                shade = Mathf.Min(0.64f, Mathf.Lerp(shade, 0.64f, crest * 0.35f));
            }
            return h;
        }

        // Ejecta — м’який сірий спад, без яскравих «променів»
        float te = (t - 1f) / Mathf.Max(0.08f, c.ejecta - 1f);
        if (te >= 1f) { shade = 0.54f; return 0f; }
        te = Mathf.Clamp01(te);
        float fall = 1f - te;
        fall = fall * fall * (0.55f + 0.45f * fall);
        shade = Mathf.Lerp(0.60f, 0.54f, Mathf.Sqrt(te));
        return c.rimH * fall * 0.65f;
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

        var mesh = Build(out Texture2D albedo, resolution, radius, 42);
        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        var mat = new Material(baseMat != null ? baseMat.shader : VisualMaterials.LitShader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.025f);
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
        if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);
        mat.doubleSidedGI = true;

        return go;
    }
}
