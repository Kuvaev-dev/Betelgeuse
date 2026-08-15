using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Процедурний диск Місяця (R≈2000 м) для демо посадки.
/// Декартова heightmap + crater field; albedo/normal у world-space UV
/// (без викривлення і без bilinear-«сходинок» з coarse shade).
/// </summary>
public static class LunarTerrainMesh
{
    /// <summary>Рівна зона під палубою (berm ~R64); кратери одразу за краєм.</summary>
    public const float PadClearRadius = 68f;

    /// <summary>Радіус cratered-диска (HorizonDisk ≤ цього).</summary>
    public const float TerrainRadius = 2000f;

    public static Mesh Build(out Texture2D albedoTex, out Texture2D normalTex,
        int resolution = 256, float radius = -1f, int seed = 42)
    {
        if (radius < 1f) radius = TerrainRadius;
        resolution = Mathf.Clamp(resolution, 96, 448);
        var rng = new System.Random(seed);
        var craters = BuildCraterField(rng, radius);

        int n = resolution + 1;
        float half = radius;
        float step = (half * 2f) / resolution;
        var height = new float[n, n];

        for (int iz = 0; iz < n; iz++)
        {
            for (int ix = 0; ix < n; ix++)
            {
                float x = -half + ix * step;
                float z = -half + iz * step;
                height[ix, iz] = SampleHeight(x, z, craters, radius);
            }
        }

        // Легке згладжування — зберігає чаші, прибирає сіткові сходинки
        SmoothHeightField(height, n, 2);

        int texSize = Mathf.ClosestPowerOfTwo(Mathf.Clamp(resolution * 5, 1536, 2048));
        BuildSurfaceMaps(craters, radius, texSize, seed, out albedoTex, out normalTex);

        var vertList = new List<Vector3>(n * n / 2);
        var uvList = new List<Vector2>(n * n / 2);
        var map = new int[n, n];
        for (int iz = 0; iz < n; iz++)
            for (int ix = 0; ix < n; ix++)
                map[ix, iz] = -1;

        float r2 = radius * radius * 1.002f;
        float invR = 1f / Mathf.Max(1e-3f, radius);
        for (int iz = 0; iz < n; iz++)
        {
            for (int ix = 0; ix < n; ix++)
            {
                float x = -half + ix * step;
                float z = -half + iz * step;
                if (x * x + z * z > r2) continue;

                float dist = Mathf.Sqrt(x * x + z * z);
                float h = height[ix, iz];
                // М'який спад лише на 1.2% краю
                float edge = Mathf.Clamp01((radius - dist) / (radius * 0.012f));
                if (edge < 1f) h = Mathf.Lerp(h - 3.2f, h, Quintic01(edge));

                map[ix, iz] = vertList.Count;
                vertList.Add(new Vector3(x, h, z));
                // World-space UV: ідеально збігається з BuildSurfaceMaps
                uvList.Add(new Vector2(
                    (x * invR + 1f) * 0.5f,
                    (z * invR + 1f) * 0.5f));
            }
        }

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
        // Tangents for normal map (URP Lit)
        mesh.RecalculateTangents();

        Debug.Log($"[LunarTerrain] verts={verts.Length} tris={triArr.Length / 3} craters={craters.Length} tex={texSize}");
        return mesh;
    }

    /// <summary>
    /// Albedo + normal у тій самій world-space проєкції, що й mesh UV.
    /// Кратери растеризуються в повному tex-розрішенні (без upscale shade-grid).
    /// </summary>
    static void BuildSurfaceMaps(Crater[] craters, float terrainRadius, int texSize, int seed,
        out Texture2D albedoTex, out Texture2D normalTex)
    {
        var hBuf = new float[texSize * texSize];
        var aBuf = new float[texSize * texSize]; // grayscale albedo factor 0..1
        var rng = new System.Random(seed + 91);

        // Mare patches
        int mareCount = 5 + rng.Next(0, 3);
        var mareCx = new float[mareCount];
        var mareCz = new float[mareCount];
        var mareR = new float[mareCount];
        for (int i = 0; i < mareCount; i++)
        {
            float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
            float dist = 420f + (float)rng.NextDouble() * terrainRadius * 0.52f;
            mareCx[i] = Mathf.Cos(ang) * dist;
            mareCz[i] = Mathf.Sin(ang) * dist;
            mareR[i] = 300f + (float)rng.NextDouble() * 480f;
        }

        float half = terrainRadius;
        float metersPerTexel = (half * 2f) / texSize;
        float invHalf = 1f / half;

        // ── Base height + compositional albedo (full res) ──
        for (int y = 0; y < texSize; y++)
        {
            float wz = -half + (y + 0.5f) * metersPerTexel;
            for (int x = 0; x < texSize; x++)
            {
                float wx = -half + (x + 0.5f) * metersPerTexel;
                int idx = y * texSize + x;
                float dist = Mathf.Sqrt(wx * wx + wz * wz);

                float h = 0f;
                float g = 0.50f; // highland regolith

                if (dist <= PadClearRadius)
                {
                    h = Noise2(wx * 0.35f, wz * 0.35f) * 0.01f;
                    float padT = dist / Mathf.Max(1f, PadClearRadius);
                    g = Mathf.Lerp(0.36f, 0.48f, padT * padT);
                }
                else
                {
                    h += Noise2(wx * 0.0022f, wz * 0.0022f) * 4.0f;
                    h += Noise2(wx * 0.0065f + 11f, wz * 0.0065f - 7f) * 1.8f;
                    h += Noise2(wx * 0.018f, wz * 0.018f) * 0.7f;
                    h += Noise2(wx * 0.055f + 3f, wz * 0.055f) * 0.28f;
                    h += Noise2(wx * 0.14f - 5f, wz * 0.14f + 2f) * 0.1f;
                    h += Noise2(wx * 0.35f + 1f, wz * 0.35f - 2f) * 0.035f;
                    float edgeN = dist * invHalf;
                    h += edgeN * edgeN * 1.8f;

                    float blend = Mathf.SmoothStep(0f, 1f, (dist - PadClearRadius) / 22f);
                    h *= blend;

                    // Multi-scale regolith grain (continuous, no grid)
                    float grain = 0f;
                    grain += Noise2(wx * 0.045f, wz * 0.045f) * 0.018f;
                    grain += Noise2(wx * 0.14f + 4f, wz * 0.14f - 3f) * 0.010f;
                    grain += Noise2(wx * 0.38f - 2f, wz * 0.38f + 7f) * 0.005f;
                    grain += Noise2(wx * 1.1f + 9f, wz * 1.1f - 5f) * 0.0025f;
                    g = 0.50f + grain;

                    // Mare darkening (compositional basalt)
                    float mare = 0f;
                    for (int m = 0; m < mareCount; m++)
                    {
                        float md = Mathf.Sqrt(
                            (wx - mareCx[m]) * (wx - mareCx[m]) +
                            (wz - mareCz[m]) * (wz - mareCz[m]));
                        float mr = mareR[m];
                        if (md < mr)
                        {
                            float t = 1f - md / mr;
                            t = t * t * (3f - 2f * t);
                            // soft noise edge so mare boundary isn't a circle stamp
                            float edgeNoise = 0.5f + 0.5f * Noise2(
                                wx * 0.008f + m * 3.1f, wz * 0.008f - m * 2.7f);
                            t *= Mathf.Lerp(0.75f, 1.1f, edgeNoise);
                            t = Mathf.Clamp01(t);
                            mare = Mathf.Max(mare, t * 0.22f);
                        }
                    }
                    g *= 1f - mare;

                    // Soft pad apron darkening
                    if (dist < PadClearRadius + 55f)
                    {
                        float pt = Mathf.SmoothStep(0f, 1f, dist / (PadClearRadius + 55f));
                        g *= Mathf.Lerp(0.82f, 1f, pt);
                    }
                }

                // Outside disk — dark void (mesh clips circle; keep corners quiet)
                if (dist > terrainRadius * 1.001f)
                {
                    h = -4f;
                    g = 0.02f;
                }

                hBuf[idx] = h;
                aBuf[idx] = g;
            }
        }

        // ── Rasterize craters largest-first (stable soft-min stacking) ──
        var order = new int[craters.Length];
        for (int i = 0; i < order.Length; i++) order[i] = i;
        System.Array.Sort(order, (ia, ib) => craters[ib].radius.CompareTo(craters[ia].radius));
        RasterizeCratersInto(hBuf, aBuf, craters, order, terrainRadius, texSize, half, metersPerTexel);

        // Micro-craters: albedo + normal detail only (not in mesh)
        StampMicroCraters(hBuf, aBuf, terrainRadius, texSize, half, metersPerTexel, seed);

        // Mild height blur (1 pass) kills single-texel spikes without smearing bowls
        BlurBuffer(hBuf, texSize, 1);
        BlurBuffer(aBuf, texSize, 1);

        // ── Encode textures ──
        albedoTex = new Texture2D(texSize, texSize, TextureFormat.RGB24, true, false);
        albedoTex.name = "LunarAlbedo";
        albedoTex.wrapMode = TextureWrapMode.Clamp;
        albedoTex.filterMode = FilterMode.Trilinear;
        albedoTex.anisoLevel = 8;

        // linear normal map
        normalTex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, true, true);
        normalTex.name = "LunarNormal";
        normalTex.wrapMode = TextureWrapMode.Clamp;
        normalTex.filterMode = FilterMode.Trilinear;
        normalTex.anisoLevel = 8;

        var albedoCols = new Color[texSize * texSize];
        var normalCols = new Color[texSize * texSize];
        float nScale = 1f / Mathf.Max(1e-4f, metersPerTexel);

        for (int y = 0; y < texSize; y++)
        {
            for (int x = 0; x < texSize; x++)
            {
                int idx = y * texSize + x;
                float g = Mathf.Clamp01(aBuf[idx]);

                // Neutral cool-gray regolith (no warm brown)
                float rC = Mathf.Clamp01(g * 0.985f);
                float gC = g;
                float bC = Mathf.Clamp01(g * 1.025f);
                albedoCols[idx] = new Color(rC, gC, bC, 1f);

                // Height → object normal, then to tangent space for URP Lit.
                // Mesh UV: U↔X, V↔Z; RecalculateTangents → T=+X, B=+Z, N=+Y
                // worldN=(nx,ny,nz) → tangentN=(nx, nz, ny)
                float hL = hBuf[y * texSize + Mathf.Max(0, x - 1)];
                float hR = hBuf[y * texSize + Mathf.Min(texSize - 1, x + 1)];
                float hD = hBuf[Mathf.Max(0, y - 1) * texSize + x];
                float hU = hBuf[Mathf.Min(texSize - 1, y + 1) * texSize + x];
                float nx = -(hR - hL) * nScale * 0.5f;
                float ny = 1f;
                float nz = -(hU - hD) * nScale * 0.5f;
                float len = Mathf.Sqrt(nx * nx + ny * ny + nz * nz);
                if (len > 1e-8f) { nx /= len; ny /= len; nz /= len; }
                float tx = nx;   // along tangent (+X)
                float ty = nz;   // along bitangent (+Z)
                float tz = ny;   // along mesh normal (+Y)
                normalCols[idx] = new Color(
                    tx * 0.5f + 0.5f,
                    ty * 0.5f + 0.5f,
                    tz * 0.5f + 0.5f,
                    1f);
            }
        }

        albedoTex.SetPixels(albedoCols);
        albedoTex.Apply(true, true);
        normalTex.SetPixels(normalCols);
        normalTex.Apply(true, true);
    }

    static void RasterizeCratersInto(float[] hBuf, float[] aBuf, Crater[] craters, int[] order,
        float terrainRadius, int texSize, float half, float metersPerTexel)
    {
        // Fresh crater height delta + shade accumulators
        var cH = new float[hBuf.Length];
        var cShade = new float[hBuf.Length];
        var cW = new float[hBuf.Length];
        for (int i = 0; i < cShade.Length; i++)
        {
            cShade[i] = 0.54f;
            cW[i] = 0f;
        }

        for (int oi = 0; oi < order.Length; oi++)
        {
            Crater c = craters[order[oi]];
            float outerR = c.radius * c.ejecta;
            float pad = outerR + metersPerTexel * 2f;
            int x0 = Mathf.Clamp(Mathf.FloorToInt((c.x - pad + half) / metersPerTexel), 0, texSize - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt((c.x + pad + half) / metersPerTexel), 0, texSize - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt((c.z - pad + half) / metersPerTexel), 0, texSize - 1);
            int y1 = Mathf.Clamp(Mathf.CeilToInt((c.z + pad + half) / metersPerTexel), 0, texSize - 1);

            float ca = Mathf.Cos(c.rot), sa = Mathf.Sin(c.rot);
            float invAspect = 1f / Mathf.Max(0.5f, c.aspect);

            for (int y = y0; y <= y1; y++)
            {
                float wz = -half + (y + 0.5f) * metersPerTexel;
                for (int x = x0; x <= x1; x++)
                {
                    float wx = -half + (x + 0.5f) * metersPerTexel;
                    float wdist = Mathf.Sqrt(wx * wx + wz * wz);
                    float blend = wdist <= PadClearRadius
                        ? 0f
                        : Mathf.SmoothStep(0f, 1f, (wdist - PadClearRadius) / 22f);
                    if (blend <= 1e-4f) continue;

                    float dx = wx - c.x;
                    float dz = wz - c.z;
                    float lx = (dx * ca + dz * sa) * invAspect;
                    float lz = -dx * sa + dz * ca;
                    float d = Mathf.Sqrt(lx * lx + lz * lz);
                    if (d > outerR) continue;

                    float p = CraterProfile(d, c, out float localShade) * blend;
                    int idx = y * texSize + x;

                    if (p < 0f)
                        cH[idx] = SoftMin(cH[idx], p, 1.2f);
                    else
                        cH[idx] = SoftMax(cH[idx], p, 0.9f);

                    // Weight shade by influence strength
                    float infl = 1f - Mathf.Clamp01(d / outerR);
                    infl = infl * infl;
                    float w = infl * blend;
                    cShade[idx] = Mathf.Lerp(cShade[idx], Mathf.Min(cShade[idx], localShade), w * 0.9f);
                    // Prefer darker crater interiors
                    if (localShade < cShade[idx])
                        cShade[idx] = Mathf.Lerp(cShade[idx], localShade, Mathf.Clamp01(w * 1.1f));
                    cW[idx] = Mathf.Max(cW[idx], w);
                }
            }
        }

        for (int i = 0; i < hBuf.Length; i++)
        {
            hBuf[i] += cH[i];

            if (cW[i] > 0.02f)
            {
                float s = Mathf.Clamp01(cShade[i]);
                // Compositional albedo from crater shade (not directional light)
                float cg = SmoothAlbedoCurve(s);
                // Depth reinforces near-black floors
                if (cH[i] < -0.15f)
                {
                    float depthT = Mathf.Clamp01((-cH[i]) / 10f);
                    cg = Mathf.Min(cg, Mathf.Lerp(cg, 0.025f, Quintic01(depthT)));
                }
                float k = Mathf.Clamp01(cW[i] * 1.15f);
                aBuf[i] = Mathf.Lerp(aBuf[i], cg, k);
            }
        }
    }

    static void StampMicroCraters(float[] hBuf, float[] aBuf,
        float terrainRadius, int texSize, float half, float metersPerTexel, int seed)
    {
        var rng = new System.Random(seed + 407);
        int count = 1100;
        float clear = PadClearRadius + 4f;

        for (int i = 0; i < count; i++)
        {
            float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
            float dist = clear + 8f + (float)rng.NextDouble() * (terrainRadius * 0.94f - clear);
            float cx = Mathf.Cos(ang) * dist;
            float cz = Mathf.Sin(ang) * dist;
            float R = 1.1f + (float)rng.NextDouble() * 4.2f;
            float depth = R * (0.20f + (float)rng.NextDouble() * 0.12f);
            float rim = depth * 0.18f;
            float outer = R * 1.4f;

            int x0 = Mathf.Clamp(Mathf.FloorToInt((cx - outer + half) / metersPerTexel), 0, texSize - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt((cx + outer + half) / metersPerTexel), 0, texSize - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt((cz - outer + half) / metersPerTexel), 0, texSize - 1);
            int y1 = Mathf.Clamp(Mathf.CeilToInt((cz + outer + half) / metersPerTexel), 0, texSize - 1);

            for (int y = y0; y <= y1; y++)
            {
                float wz = -half + (y + 0.5f) * metersPerTexel;
                for (int x = x0; x <= x1; x++)
                {
                    float wx = -half + (x + 0.5f) * metersPerTexel;
                    float dx = wx - cx;
                    float dz = wz - cz;
                    float d = Mathf.Sqrt(dx * dx + dz * dz);
                    if (d > outer) continue;

                    float t = d / R;
                    float dh;
                    float dg;
                    if (t < 0.55f)
                    {
                        float u = t / 0.55f;
                        dh = -depth * (1f - 0.08f * Quintic01(u));
                        dg = Mathf.Lerp(0.035f, 0.11f, u);
                    }
                    else if (t <= 1f)
                    {
                        float u = (t - 0.55f) / 0.45f;
                        float w = Quintic01(u);
                        dh = Mathf.Lerp(-depth, rim, w);
                        dg = Mathf.Lerp(0.11f, 0.52f, w);
                    }
                    else
                    {
                        float u = (t - 1f) / 0.45f;
                        float fall = 1f - Quintic01(Mathf.Clamp01(u));
                        dh = rim * fall * 0.45f;
                        dg = Mathf.Lerp(0.52f, 0.50f, Mathf.Clamp01(u));
                    }

                    int idx = y * texSize + x;
                    float k = 1f - Mathf.Clamp01(d / outer);
                    k = k * k * (3f - 2f * k);
                    hBuf[idx] += dh * k;
                    aBuf[idx] = Mathf.Lerp(aBuf[idx], dg, k * 0.8f);
                }
            }
        }
    }

    static void BlurBuffer(float[] buf, int n, int passes)
    {
        if (passes <= 0) return;
        var tmp = new float[buf.Length];
        for (int p = 0; p < passes; p++)
        {
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float sum = 0f, w = 0f;
                for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int xx = x + dx, yy = y + dy;
                    if (xx < 0 || yy < 0 || xx >= n || yy >= n) continue;
                    float ww = (dx == 0 && dy == 0) ? 4f : ((dx == 0 || dy == 0) ? 2f : 1f);
                    sum += buf[yy * n + xx] * ww;
                    w += ww;
                }
                tmp[y * n + x] = sum / Mathf.Max(1e-6f, w);
            }
            System.Array.Copy(tmp, buf, buf.Length);
        }
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
        const float clear = PadClearRadius + 2f;
        var list = new List<Crater>(800);

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
        if (d + inflR * 0.35f > terrainR * 0.985f) return false;
        if (d > terrainR * 0.97f) return false;
        return d - inflR * 0.85f >= clear;
    }

    static Crater Make(float x, float z, float R, float depth, bool small, bool complex, System.Random rng)
    {
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
            // Keep aspect near 1 — strong ellipses look like stretched/crooked textures
            aspect = 0.96f + (float)rng.NextDouble() * 0.08f,
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

    static float SampleHeight(float x, float z, Crater[] craters, float terrainRadius)
    {
        float dist = Mathf.Sqrt(x * x + z * z);

        if (dist <= PadClearRadius)
            return Noise2(x * 0.35f, z * 0.35f) * 0.012f;

        float h = 0f;
        h += Noise2(x * 0.0022f, z * 0.0022f) * 4.0f;
        h += Noise2(x * 0.0065f + 11f, z * 0.0065f - 7f) * 1.8f;
        h += Noise2(x * 0.018f, z * 0.018f) * 0.7f;
        h += Noise2(x * 0.055f + 3f, z * 0.055f) * 0.28f;
        h += Noise2(x * 0.14f - 5f, z * 0.14f + 2f) * 0.1f;
        h += Noise2(x * 0.35f + 1f, z * 0.35f - 2f) * 0.035f;
        float edgeN = dist / Mathf.Max(1f, terrainRadius);
        h += edgeN * edgeN * 1.8f;

        float blend = Mathf.SmoothStep(0f, 1f, (dist - PadClearRadius) / 22f);
        h *= blend;

        float floorH = 0f;
        float rimH = 0f;
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

            float p = CraterProfile(d, c, out _) * blend;
            anyCrater = true;

            if (p < 0f)
                floorH = SoftMin(floorH, p, 1.2f);
            else
                rimH = SoftMax(rimH, p, 0.9f);
        }

        float craterH = anyCrater ? floorH + rimH : 0f;
        return h + craterH;
    }

    /// <summary>
    /// Природний lunar bowl: C2-гладкі переходи floor→wall→rim→ejecta.
    /// </summary>
    static float CraterProfile(float d, Crater c, out float shade)
    {
        float R = Mathf.Max(1f, c.radius);
        float t = d / R;
        float floorT = Mathf.Clamp(c.floorFrac, 0.12f, 0.55f);
        shade = 0.54f;

        if (c.peakH > 0.05f && t < floorT * 1.15f)
        {
            float pt = d / Mathf.Max(0.35f, c.peakR);
            float peak = c.peakH * Mathf.Exp(-pt * pt * 1.55f);
            float floorBase = -c.depth + 0.04f * c.depth * (t / Mathf.Max(0.05f, floorT));
            shade = Mathf.Lerp(0.05f, 0.11f, Mathf.Clamp01(pt * 0.5f));
            return floorBase + peak;
        }

        if (t <= floorT)
        {
            float ft = t / Mathf.Max(0.05f, floorT);
            float bowl = Quintic01(ft) * 0.05f * c.depth;
            shade = Mathf.Lerp(0.02f, 0.07f, ft);
            return -c.depth + bowl;
        }

        if (t <= 1f)
        {
            float u = (t - floorT) / Mathf.Max(0.08f, 1f - floorT);
            u = Mathf.Clamp01(u);
            float wall = Quintic01(u);

            if (c.complex && c.terrace > 0.15f && c.terrace < 0.9f)
            {
                float tw = 0.14f;
                float td = (u - c.terrace) / tw;
                float k = Mathf.Exp(-td * td * 2.2f);
                wall = Mathf.Clamp01(wall + k * 0.045f);
            }

            shade = Mathf.Lerp(0.05f, 0.56f, wall);
            float h = Mathf.Lerp(-c.depth, c.rimH, wall);

            float crestW = 0.18f;
            float crestT = (u - (1f - crestW)) / crestW;
            if (crestT > 0f && crestT < 1f)
            {
                float bump = crestT * (1f - crestT) * 4f;
                h += c.rimH * 0.1f * bump;
                shade = Mathf.Min(0.60f, shade + 0.03f * bump);
            }
            return h;
        }

        float te = (t - 1f) / Mathf.Max(0.12f, c.ejecta - 1f);
        if (te >= 1f) { shade = 0.54f; return 0f; }
        te = Mathf.Clamp01(te);
        float fall = 1f - Quintic01(te);
        // Soft ejecta blanket — slightly lighter, NO radial rays
        shade = Mathf.Lerp(0.57f, 0.54f, te);
        return c.rimH * fall * 0.55f;
    }

    static float Quintic01(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * t * (t * (t * 6f - 15f) + 10f);
    }

    static float SoftMin(float a, float b, float k)
    {
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
        if (s < 0.12f)
            return Mathf.Lerp(0.025f, 0.09f, Quintic01(s / 0.12f));
        if (s < 0.32f)
            return Mathf.Lerp(0.09f, 0.30f, Quintic01((s - 0.12f) / 0.20f));
        if (s < 0.52f)
            return Mathf.Lerp(0.30f, 0.48f, Quintic01((s - 0.32f) / 0.20f));
        if (s < 0.72f)
            return Mathf.Lerp(0.48f, 0.53f, Quintic01((s - 0.52f) / 0.20f));
        return Mathf.Lerp(0.53f, 0.58f, Quintic01((s - 0.72f) / 0.28f));
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
        // quintic fade — fewer grid artifacts than cubic
        fx = fx * fx * fx * (fx * (fx * 6f - 15f) + 10f);
        fy = fy * fy * fy * (fy * (fy * 6f - 15f) + 10f);
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

        var mesh = Build(out Texture2D albedo, out Texture2D normalMap,
            Mathf.Max(resolution, 420), radius, 42);
        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        var mat = new Material(baseMat != null ? baseMat.shader : VisualMaterials.LitShader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.035f);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.035f);

        if (mat.HasProperty("_BaseMap"))
        {
            mat.SetTexture("_BaseMap", albedo);
            mat.EnableKeyword("_BASEMAP");
        }
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", albedo);
        mat.mainTexture = albedo;

        // Normal map — high-frequency relief without mesh density
        if (mat.HasProperty("_BumpMap") && normalMap != null)
        {
            mat.SetTexture("_BumpMap", normalMap);
            mat.EnableKeyword("_NORMALMAP");
            if (mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", 1.35f);
        }
        if (mat.HasProperty("_DetailNormalMapScale")) mat.SetFloat("_DetailNormalMapScale", 0f);

        // Very low specular — dry regolith
        if (mat.HasProperty("_SpecularHighlights")) mat.SetFloat("_SpecularHighlights", 0f);
        if (mat.HasProperty("_EnvironmentReflections")) mat.SetFloat("_EnvironmentReflections", 0f);

        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        mr.receiveShadows = true;
        if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 2f);
        mat.doubleSidedGI = false;

        return go;
    }
}
