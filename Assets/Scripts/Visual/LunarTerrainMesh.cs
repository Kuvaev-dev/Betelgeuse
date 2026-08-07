using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Круглий диск Місяця: декартова сітка, обрізана по колу
/// (кратери — круглі чаші, не «радіальні лінії»).
/// Albedo-текстура: дно темніше, вал світліший — видно зверху.
/// </summary>
public static class LunarTerrainMesh
{
    /// <summary>Рівна зона лише під палубою pad.</summary>
    public const float PadClearRadius = 58f;

    public static Mesh Build(out Texture2D albedoTex, int resolution = 220, float radius = 2800f, int seed = 42)
    {
        resolution = Mathf.Clamp(resolution, 64, 320);
        var rng = new System.Random(seed);
        var craters = BuildCraterField(rng, radius);

        // Height + shade grids
        int n = resolution + 1;
        float half = radius;
        float step = (half * 2f) / resolution;
        var height = new float[n, n];
        var shade = new float[n, n]; // 0 dark floor … 1 bright rim

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

        // Albedo texture from shade (кратери видно навіть без тіней)
        int texSize = Mathf.ClosestPowerOfTwo(Mathf.Clamp(resolution, 128, 512));
        albedoTex = BuildAlbedo(shade, n, texSize);

        // Build only verts inside circle (+ margin)
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

                // Край: легкий спад
                float dist = Mathf.Sqrt(x * x + z * z);
                float h = height[ix, iz];
                float edge = Mathf.Clamp01((radius - dist) / (radius * 0.04f));
                if (edge < 1f) h = Mathf.Lerp(h - 10f, h, edge);

                map[ix, iz] = vertList.Count;
                vertList.Add(new Vector3(x, h, z));
                uvList.Add(new Vector2(ix / (float)resolution, iz / (float)resolution));
            }
        }

        // Triangles (top + bottom for solid look from below)
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

                // Top (CCW, +Y)
                tris.Add(i00); tris.Add(i01); tris.Add(i10);
                tris.Add(i10); tris.Add(i01); tris.Add(i11);
                // Bottom (CW) — знизу суцільна поверхня, не «лінії»
                tris.Add(i00); tris.Add(i10); tris.Add(i01);
                tris.Add(i10); tris.Add(i11); tris.Add(i01);
            }
        }

        var verts = vertList.ToArray();
        var norms = new Vector3[verts.Length];
        for (int i = 0; i < norms.Length; i++) norms[i] = Vector3.zero;

        var triArr = tris.ToArray();
        // Accumulate only top-facing tris for smooth top normals
        for (int t = 0; t < triArr.Length; t += 6) // skip bottom pairs
        {
            for (int k = 0; k < 6; k += 3)
            {
                int i0 = triArr[t + k], i1 = triArr[t + k + 1], i2 = triArr[t + k + 2];
                Vector3 faceN = Vector3.Cross(verts[i1] - verts[i0], verts[i2] - verts[i0]);
                if (faceN.y < 0f) continue; // bottom face
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

    static Texture2D BuildAlbedo(float[,] shade, int n, int texSize)
    {
        var tex = new Texture2D(texSize, texSize, TextureFormat.RGB24, true);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        var cols = new Color[texSize * texSize];

        // shade: 0 = дно (майже чорно-сіре), 0.5 = рівнина, 1 = вал (трохи світліший)
        for (int y = 0; y < texSize; y++)
        {
            for (int x = 0; x < texSize; x++)
            {
                float u = x / (float)(texSize - 1);
                float v = y / (float)(texSize - 1);
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

                // Натуральний місячний контраст:
                // дно ~0.08–0.18 (темне), рівнина ~0.55–0.60, вал ~0.68–0.75
                float g;
                if (s < 0.25f)
                    g = Mathf.Lerp(0.07f, 0.20f, s / 0.25f);          // дно — майже чорно-сіре
                else if (s < 0.50f)
                    g = Mathf.Lerp(0.20f, 0.50f, (s - 0.25f) / 0.25f); // стінка
                else if (s < 0.70f)
                    g = Mathf.Lerp(0.50f, 0.60f, (s - 0.50f) / 0.20f); // рівнина
                else
                    g = Mathf.Lerp(0.60f, 0.76f, (s - 0.70f) / 0.30f); // вал

                float nse = (Hash(x * 13, y * 17) - 0.5f) * 0.012f;
                g = Mathf.Clamp01(g + nse);
                cols[y * texSize + x] = new Color(g, g, g, 1f);
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
    }

    static Crater[] BuildCraterField(System.Random rng, float terrainRadius)
    {
        const float clear = PadClearRadius + 6f;
        var list = new List<Crater>(500);

        // Біля pad — помірно (не «піна»)
        for (int i = 0; i < 18; i++)
        {
            float ang = i * (Mathf.PI * 2f / 18f) + 0.2f;
            float R = 14f + (float)rng.NextDouble() * 36f;
            float dist = clear + R * 1.4f + (float)rng.NextDouble() * 100f;
            float x = Mathf.Cos(ang) * dist;
            float z = Mathf.Sin(ang) * dist;
            if (Fits(x, z, R * 1.55f, clear, terrainRadius))
                list.Add(Make(x, z, R, 2.5f + R * 0.11f, false, false, rng));
        }

        // Великі (гарантовані) — рідше, глибші
        float[] bigR = { 260f, 210f, 300f, 180f, 240f, 200f, 280f, 170f };
        for (int i = 0; i < bigR.Length; i++)
        {
            float R = bigR[i];
            float ang = i * (Mathf.PI * 2f / bigR.Length) + 0.4f;
            float dist = 380f + i * 50f + R * 0.35f;
            dist = Mathf.Min(dist, terrainRadius * 0.7f);
            float x = Mathf.Cos(ang) * dist;
            float z = Mathf.Sin(ang) * dist;
            if (Fits(x, z, R * 1.75f, clear, terrainRadius))
                list.Add(Make(x, z, R, R * 0.14f, false, true, rng));
        }

        for (int i = 0; i < 10; i++)
            TryAdd(list, rng, terrainRadius, clear, 150f, 290f, 14f, 30f, false, true, 50);
        for (int i = 0; i < 55; i++)
            TryAdd(list, rng, terrainRadius, clear, 45f, 125f, 5f, 15f, false, false, 40);
        for (int i = 0; i < 90; i++)
            TryAdd(list, rng, terrainRadius, clear, 14f, 45f, 1.5f, 5.5f, true, false, 25);
        // Мікро — мало, щоб не було «піни»
        for (int i = 0; i < 70; i++)
            TryAdd(list, rng, terrainRadius, clear, 5f, 14f, 0.6f, 2.0f, true, false, 18);

        // Secondary — лише біля дуже великих
        int n0 = list.Count;
        for (int i = 0; i < n0 && list.Count < 320; i++)
        {
            if (list[i].radius < 120f) continue;
            int ns = 3 + rng.Next(0, 5);
            for (int s = 0; s < ns; s++)
            {
                float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
                float d = list[i].radius * (1.35f + (float)rng.NextDouble() * 1.6f);
                float sx = list[i].x + Mathf.Cos(ang) * d;
                float sz = list[i].z + Mathf.Sin(ang) * d;
                float sR = 8f + (float)rng.NextDouble() * 22f;
                if (!Fits(sx, sz, sR * 1.6f, clear, terrainRadius)) continue;
                list.Add(Make(sx, sz, sR, 1.2f + (float)rng.NextDouble() * 2.8f, true, false, rng));
            }
        }

        return list.ToArray();
    }

    static void TryAdd(List<Crater> list, System.Random rng, float terrainRadius,
        float clearZone, float rMin, float rMax, float dMin, float dMax,
        bool small, bool complex, int attempts)
    {
        for (int attempt = 0; attempt < attempts; attempt++)
        {
            float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
            float R = rMin + (float)rng.NextDouble() * (rMax - rMin);
            float ejecta = 1.55f + (float)rng.NextDouble() * 0.5f;
            float minD = clearZone + R * ejecta + 8f;
            float maxD = terrainRadius * 0.82f;
            if (minD >= maxD) return;
            float dist = minD + (float)rng.NextDouble() * (maxD - minD);
            float x = Mathf.Cos(ang) * dist;
            float z = Mathf.Sin(ang) * dist;
            if (!Fits(x, z, R * ejecta, clearZone, terrainRadius)) continue;
            float depth = dMin + (float)rng.NextDouble() * (dMax - dMin);
            list.Add(Make(x, z, R, depth, small, complex, rng));
            return;
        }
    }

    static bool Fits(float x, float z, float inflR, float clear, float terrainR)
    {
        float d = Mathf.Sqrt(x * x + z * z);
        if (d > terrainR * 0.88f) return false;
        return d - inflR >= clear;
    }

    static Crater Make(float x, float z, float R, float depth, bool small, bool complex, System.Random rng)
    {
        var c = new Crater
        {
            x = x, z = z, radius = R,
            depth = Mathf.Max(depth, R * 0.1f),
            ejecta = 1.55f + (float)rng.NextDouble() * 0.5f,
            floorFrac = 0.32f + (float)rng.NextDouble() * 0.12f,
            aspect = 0.92f + (float)rng.NextDouble() * 0.16f,
            rot = (float)rng.NextDouble() * Mathf.PI,
            peakH = 0f, peakR = 0f
        };
        c.rimH = c.depth * (0.35f + (float)rng.NextDouble() * 0.25f);
        if (small) { c.rimH *= 0.9f; c.ejecta *= 0.9f; }
        if (complex && R > 100f)
        {
            c.peakH = c.depth * (0.28f + (float)rng.NextDouble() * 0.22f);
            c.peakR = R * (0.1f + (float)rng.NextDouble() * 0.08f);
        }
        return c;
    }

    /// <returns>height; shade 0..1 for albedo</returns>
    static float SampleHeight(float x, float z, Crater[] craters, float terrainRadius, out float shade)
    {
        float dist = Mathf.Sqrt(x * x + z * z);
        shade = 0.55f; // base regolith

        if (dist <= PadClearRadius)
        {
            shade = 0.58f;
            return 0f;
        }

        float h = 0f;
        h += Noise2(x * 0.004f, z * 0.004f) * 1.8f;
        h += Noise2(x * 0.014f + 11f, z * 0.014f - 7f) * 0.6f;
        h += Noise2(x * 0.04f, z * 0.04f) * 0.2f;
        h += (dist / terrainRadius) * (dist / terrainRadius) * 1.2f;

        float blend = Mathf.Clamp01((dist - PadClearRadius) / 14f);
        h *= blend * blend;

        float craterH = 0f;
        // shade = мінімум (дно завжди темніше за все інше)
        float minShade = shade;

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
            craterH += p;
            if (localShade < minShade)
                minShade = localShade;
        }

        shade = minShade;
        // Дно кратера — жорстко темне (натуральний shadow fill)
        if (craterH < -2f)
            shade = Mathf.Min(shade, 0.06f);
        else if (craterH < -0.8f)
            shade = Mathf.Min(shade, 0.12f);
        else if (craterH < -0.25f)
            shade = Mathf.Min(shade, 0.22f);

        return h + craterH;
    }

    static float CraterProfile(float d, Crater c, out float shade)
    {
        float R = Mathf.Max(1f, c.radius);
        float t = d / R;
        float floorT = c.floorFrac;
        shade = 0.55f;

        if (c.peakH > 0.05f && d < c.peakR * 2.5f && t < floorT * 1.15f)
        {
            float pt = d / Mathf.Max(0.2f, c.peakR);
            float peak = c.peakH * Mathf.Exp(-pt * pt * 1.2f);
            shade = 0.22f;
            return -c.depth + peak;
        }

        if (t <= floorT)
        {
            // Плоске темне дно (як у реальних lunar craters — shadow)
            shade = 0.04f;
            return -c.depth;
        }

        if (t <= 1f)
        {
            float u = (t - floorT) / Mathf.Max(0.08f, 1f - floorT);
            u = Mathf.Clamp01(u);
            // Трохи крутіша стінка (не «піна»)
            u = u * u * u * (u * (u * 6f - 15f) + 10f); // smootherstep
            shade = Mathf.Lerp(0.06f, 0.80f, u);
            return Mathf.Lerp(-c.depth, c.rimH, u);
        }

        float te = (t - 1f) / Mathf.Max(0.08f, c.ejecta - 1f);
        if (te >= 1f) { shade = 0.55f; return 0f; }
        te = Mathf.Clamp01(te);
        float fall = 1f - te;
        fall = fall * fall * fall; // швидкий спад ejecta — чіткий край
        shade = Mathf.Lerp(0.75f, 0.55f, te);
        return c.rimH * fall * 0.8f;
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
        int resolution = 220, float radius = 2800f)
    {
        var go = new GameObject("LunarTerrain");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;

        var mesh = Build(out Texture2D albedo, resolution, radius, 42);
        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        // Albedo обов’язково на _BaseMap (URP) — інакше дно не темніє
        var mat = new Material(baseMat != null ? baseMat.shader : VisualMaterials.LitShader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.03f);
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
