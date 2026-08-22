using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Процедурний диск Місяця (R≈2000 м): heightmap, круглі C2-кратери,
/// cool-gray albedo у world-UV. Без зовнішніх тайлів.
/// </summary>
public static class LunarTerrainMesh
{
    /// <summary>Рівна зона під палубою (berm ~R64); кратери одразу за краєм.</summary>
    public const float PadClearRadius = 68f;

    /// <summary>Радіус cratered-диска (HorizonDisk ≤ цього).</summary>
    public const float TerrainRadius = 2000f;

    public sealed class BuildOutput
    {
        public Mesh mesh;
        public Texture2D albedo;
        public Texture2D normal;
    }

    public static Mesh Build(out Texture2D albedoTex, out Texture2D normalTex,
        int resolution = 256, float radius = -1f, int seed = 42)
    {
        var box = new BuildOutput();
        Drain(BuildRoutine(box, resolution, radius, seed));
        albedoTex = box.albedo;
        normalTex = box.normal;
        return box.mesh;
    }

    /// <summary>Run nested IEnumerators to completion (sync). Unity coroutines do this automatically.</summary>
    public static void Drain(IEnumerator e)
    {
        if (e == null) return;
        while (e.MoveNext())
        {
            if (e.Current is IEnumerator nested)
                Drain(nested);
        }
    }

    /// <summary>Same as Build, but yields every few rows so splash spinner can keep spinning.</summary>
    public static IEnumerator BuildRoutine(BuildOutput box,
        int resolution = 256, float radius = -1f, int seed = 42)
    {
        if (box == null) yield break;
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
            if ((iz & 15) == 0) yield return null;
        }

        // Heavy smooth → circular bowls, no faceted rims
        SmoothHeightField(height, n, 6);
        yield return null;

        int texSize = Mathf.ClosestPowerOfTwo(Mathf.Clamp(resolution * 5, 1536, 2048));
        Texture2D albedoTex = null;
        Texture2D normalTex = null;
        yield return BuildSurfaceMapsRoutine(craters, radius, texSize, seed,
            t => albedoTex = t, t => normalTex = t);

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
                // Very soft outer lip
                float edge = Mathf.Clamp01((radius - dist) / (radius * 0.04f));
                if (edge < 1f) h = Mathf.Lerp(h - 1.2f, h, Quintic01(edge));

                map[ix, iz] = vertList.Count;
                vertList.Add(new Vector3(x, h, z));
                uvList.Add(new Vector2(
                    (x * invR + 1f) * 0.5f,
                    (z * invR + 1f) * 0.5f));
            }
            if ((iz & 15) == 0) yield return null;
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
            if ((iz & 31) == 0) yield return null;
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
            if ((t & 4095) == 0 && t > 0) yield return null;
        }
        for (int i = 0; i < norms.Length; i++)
        {
            if (norms[i].sqrMagnitude > 1e-12f) norms[i].Normalize();
            else norms[i] = Vector3.up;
        }
        yield return null;

        // One Laplacian pass on normals via shared edges → softer crater rims
        {
            var acc = new Vector3[norms.Length];
            var cnt = new int[norms.Length];
            for (int t = 0; t < triArr.Length; t += 3)
            {
                int i0 = triArr[t], i1 = triArr[t + 1], i2 = triArr[t + 2];
                Vector3 nAvg = (norms[i0] + norms[i1] + norms[i2]) * (1f / 3f);
                acc[i0] += nAvg; cnt[i0]++;
                acc[i1] += nAvg; cnt[i1]++;
                acc[i2] += nAvg; cnt[i2]++;
            }
            for (int i = 0; i < norms.Length; i++)
            {
                if (cnt[i] <= 0) continue;
                Vector3 sn = Vector3.Lerp(norms[i], acc[i] / cnt[i], 0.55f);
                if (sn.sqrMagnitude > 1e-12f) norms[i] = sn.normalized;
            }
        }
        yield return null;

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
        mesh.RecalculateTangents();
        yield return null;

        box.mesh = mesh;
        box.albedo = albedoTex;
        box.normal = normalTex;
        Debug.Log($"[LunarTerrain] verts={verts.Length} tris={triArr.Length / 3} craters={craters.Length} tex={texSize}");
    }

    /// <summary>
    /// Albedo + normal у тій самій world-space проєкції, що й mesh UV.
    /// Yields every few rows so the splash spinner can keep rotating.
    /// </summary>
    static IEnumerator BuildSurfaceMapsRoutine(Crater[] craters, float terrainRadius, int texSize, int seed,
        System.Action<Texture2D> setAlbedo, System.Action<Texture2D> setNormal)
    {
        // Pure procedural bake — no external tiles (tiling = ragged seams).
        // Fill the FULL square continuously (no hard black circle edge).
        var hBuf = new float[texSize * texSize];
        var aBuf = new float[texSize * texSize];

        float half = terrainRadius;
        float metersPerTexel = (half * 2f) / texSize;
        float invHalf = 1f / half;

        for (int y = 0; y < texSize; y++)
        {
            float wz = -half + (y + 0.5f) * metersPerTexel;
            for (int x = 0; x < texSize; x++)
            {
                float wx = -half + (x + 0.5f) * metersPerTexel;
                int idx = y * texSize + x;
                float dist = Mathf.Sqrt(wx * wx + wz * wz);

                float h = 0f;
                // Presentation mid-gray: readable under sun, not chalk and not coal
                float g = 0.42f;

                if (dist <= PadClearRadius)
                {
                    h = Noise2(wx * 0.25f, wz * 0.25f) * 0.006f;
                    float padT = dist / Mathf.Max(1f, PadClearRadius);
                    g = Mathf.Lerp(0.36f, 0.41f, padT * padT);
                }
                else
                {
                    // Low-frequency undulation only — high-freq grain looks ragged when baked
                    h += Noise2(wx * 0.0016f, wz * 0.0016f) * 2.8f;
                    h += Noise2(wx * 0.0048f + 11f, wz * 0.0048f - 7f) * 1.15f;
                    h += Noise2(wx * 0.012f, wz * 0.012f) * 0.35f;
                    float edgeN = dist * invHalf;
                    h += edgeN * edgeN * 1.1f;

                    float blend = Mathf.SmoothStep(0f, 1f, (dist - PadClearRadius) / 32f);
                    h *= blend;

                    float grain = 0f;
                    grain += Noise2(wx * 0.018f, wz * 0.018f) * 0.016f;
                    grain += Noise2(wx * 0.045f + 4f, wz * 0.045f - 3f) * 0.008f;
                    g = 0.42f + grain;

                    // Mare — slightly darker basalt plains
                    float mare = Noise2(wx * 0.0009f + 2f, wz * 0.0009f - 1f);
                    mare = Mathf.SmoothStep(0.20f, 0.58f, mare * 0.5f + 0.5f);
                    g -= mare * 0.045f;

                    if (dist < PadClearRadius + 70f)
                    {
                        float pt = Mathf.SmoothStep(0f, 1f, dist / (PadClearRadius + 70f));
                        g = Mathf.Lerp(0.38f, g, pt);
                    }
                }

                // Outside geometric disk: KEEP continuous gray (never hard black —
                // black corners of the UV square caused ragged ring artifacts).
                if (dist > terrainRadius)
                {
                    float over = (dist - terrainRadius) / Mathf.Max(1f, terrainRadius * 0.15f);
                    over = Mathf.Clamp01(over);
                    h = Mathf.Lerp(h, h - 0.8f, Quintic01(over));
                    g = Mathf.Lerp(g, 0.38f, Quintic01(over) * 0.35f);
                }

                hBuf[idx] = h;
                aBuf[idx] = g;
            }
            if ((y & 7) == 0) yield return null;
        }

        var order = new int[craters.Length];
        for (int i = 0; i < order.Length; i++) order[i] = i;
        System.Array.Sort(order, (ia, ib) => craters[ib].radius.CompareTo(craters[ia].radius));
        yield return null;
        RasterizeCratersInto(hBuf, aBuf, craters, order, terrainRadius, texSize, half, metersPerTexel);
        yield return null;

        // Wide blur kills any remaining pixel stair-steps on rims
        BlurBuffer(hBuf, texSize, 3);
        yield return null;
        BlurBuffer(aBuf, texSize, 4);
        yield return null;
        BlurBufferWide(aBuf, texSize, 2);
        yield return null;

        var albedoTex = new Texture2D(texSize, texSize, TextureFormat.RGB24, true, false);
        albedoTex.name = "LunarAlbedo_Final";
        albedoTex.wrapMode = TextureWrapMode.Clamp;
        albedoTex.filterMode = FilterMode.Trilinear;
        albedoTex.anisoLevel = 8;

        // Flat normal map (mesh carries relief). Bump maps double-shade and look ragged.
        var normalTex = new Texture2D(4, 4, TextureFormat.RGBA32, false, true);
        normalTex.name = "LunarNormal_Flat";
        normalTex.wrapMode = TextureWrapMode.Clamp;
        normalTex.filterMode = FilterMode.Bilinear;
        var flatN = new Color(0.5f, 0.5f, 1f, 1f);
        var nPix = new Color[16];
        for (int i = 0; i < 16; i++) nPix[i] = flatN;
        normalTex.SetPixels(nPix);
        normalTex.Apply(false, true);

        var albedoCols = new Color[texSize * texSize];
        for (int y = 0; y < texSize; y++)
        {
            float wz = -half + (y + 0.5f) * metersPerTexel;
            for (int x = 0; x < texSize; x++)
            {
                float wx = -half + (x + 0.5f) * metersPerTexel;
                int idx = y * texSize + x;
                float g = Mathf.Clamp(aBuf[idx], 0.30f, 0.52f);

                // Soft radial vignette near disk edge (not a hard cut)
                float dist = Mathf.Sqrt(wx * wx + wz * wz);
                float rim = Mathf.SmoothStep(terrainRadius * 0.92f, terrainRadius * 1.02f, dist);
                g = Mathf.Lerp(g, 0.38f, rim * 0.18f);

                // Presentable cool silver-gray
                float v = Mathf.Lerp(0.40f, g, 0.88f);
                albedoCols[idx] = new Color(
                    Mathf.Clamp01(v * 0.97f),
                    Mathf.Clamp01(v * 0.99f),
                    Mathf.Clamp01(v * 1.035f),
                    1f);
            }
            if ((y & 7) == 0) yield return null;
        }

        albedoTex.SetPixels(albedoCols);
        albedoTex.Apply(true, true);
        yield return null;

        setAlbedo?.Invoke(albedoTex);
        setNormal?.Invoke(normalTex);
    }

    static void RasterizeCratersInto(float[] hBuf, float[] aBuf, Crater[] craters, int[] order,
        float terrainRadius, int texSize, float half, float metersPerTexel)
    {
        var cH = new float[hBuf.Length];
        var cA = new float[hBuf.Length];
        var cW = new float[hBuf.Length];

        for (int oi = 0; oi < order.Length; oi++)
        {
            Crater c = craters[order[oi]];
            float outerR = c.radius * c.ejecta;
            float pad = outerR + metersPerTexel * 4f;
            int x0 = Mathf.Clamp(Mathf.FloorToInt((c.x - pad + half) / metersPerTexel), 0, texSize - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt((c.x + pad + half) / metersPerTexel), 0, texSize - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt((c.z - pad + half) / metersPerTexel), 0, texSize - 1);
            int y1 = Mathf.Clamp(Mathf.CeilToInt((c.z + pad + half) / metersPerTexel), 0, texSize - 1);

            float sk = Mathf.Max(3.5f, c.depth * 0.45f);

            for (int y = y0; y <= y1; y++)
            {
                float wz = -half + (y + 0.5f) * metersPerTexel;
                for (int x = x0; x <= x1; x++)
                {
                    float wx = -half + (x + 0.5f) * metersPerTexel;
                    float wdist = Mathf.Sqrt(wx * wx + wz * wz);
                    float blend = wdist <= PadClearRadius
                        ? 0f
                        : Mathf.SmoothStep(0f, 1f, (wdist - PadClearRadius) / 32f);
                    if (blend <= 1e-4f) continue;

                    float dx = wx - c.x;
                    float dz = wz - c.z;
                    float d = Mathf.Sqrt(dx * dx + dz * dz);
                    if (d > outerR) continue;

                    float p = CraterProfile(d, c, out float localShade) * blend;
                    int idx = y * texSize + x;

                    if (p < 0f)
                        cH[idx] = SoftMin(cH[idx], p, sk);
                    else
                        cH[idx] = SoftMax(cH[idx], p, sk * 0.8f);

                    // Soft radial weight — no hard crater albedo edges
                    float infl = 1f - Mathf.Clamp01(d / outerR);
                    infl = Quintic01(infl);
                    float w = infl * blend;
                    cA[idx] = (cA[idx] * cW[idx] + localShade * w) / Mathf.Max(1e-5f, cW[idx] + w);
                    cW[idx] += w;
                }
            }
        }

        for (int i = 0; i < hBuf.Length; i++)
        {
            hBuf[i] += cH[i];
            if (cW[i] > 0.015f)
            {
                float cg = SmoothAlbedoCurve(Mathf.Clamp01(cA[i]));
                cg = Mathf.Clamp(cg, 0.32f, 0.48f);
                // Gentle mix — geometry shows the bowl, albedo only hints
                float k = Mathf.Clamp01(cW[i] * 0.50f);
                aBuf[i] = Mathf.Lerp(aBuf[i], cg, k);
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
                    int xx = Mathf.Clamp(x + dx, 0, n - 1);
                    int yy = Mathf.Clamp(y + dy, 0, n - 1);
                    float ww = (dx == 0 && dy == 0) ? 4f : ((dx == 0 || dy == 0) ? 2f : 1f);
                    sum += buf[yy * n + xx] * ww;
                    w += ww;
                }
                tmp[y * n + x] = sum / Mathf.Max(1e-6f, w);
            }
            System.Array.Copy(tmp, buf, buf.Length);
        }
    }

    /// <summary>5×5 gaussian-ish blur for albedo — removes remaining rim stair-steps.</summary>
    static void BlurBufferWide(float[] buf, int n, int passes)
    {
        if (passes <= 0) return;
        var tmp = new float[buf.Length];
        // binomial-ish weights for radius 2
        float[] ker = { 1f, 4f, 6f, 4f, 1f };
        for (int p = 0; p < passes; p++)
        {
            // horizontal
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float sum = 0f, w = 0f;
                for (int k = -2; k <= 2; k++)
                {
                    int xx = Mathf.Clamp(x + k, 0, n - 1);
                    float ww = ker[k + 2];
                    sum += buf[y * n + xx] * ww;
                    w += ww;
                }
                tmp[y * n + x] = sum / w;
            }
            // vertical
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float sum = 0f, w = 0f;
                for (int k = -2; k <= 2; k++)
                {
                    int yy = Mathf.Clamp(y + k, 0, n - 1);
                    float ww = ker[k + 2];
                    sum += tmp[yy * n + x] * ww;
                    w += ww;
                }
                buf[y * n + x] = sum / w;
            }
        }
    }

    struct Crater
    {
        public float x, z, radius, depth, rimH, ejecta, floorFrac;
        public float peakH, peakR;
    }

    static Crater[] BuildCraterField(System.Random rng, float terrainRadius)
    {
        // Fewer, well-spaced round bowls — dense fields looked ragged on the mesh
        const float clear = PadClearRadius + 8f;
        var list = new List<Crater>(220);

        // Ring of small fresh craters just outside the pad
        for (int i = 0; i < 18; i++)
        {
            float ang = i * (Mathf.PI * 2f / 18f) + 0.12f * (float)rng.NextDouble();
            float R = 8f + (float)rng.NextDouble() * 16f;
            float dist = clear + R * 1.15f + (float)rng.NextDouble() * 40f;
            float x = Mathf.Cos(ang) * dist;
            float z = Mathf.Sin(ang) * dist;
            if (Fits(list, x, z, R, clear, terrainRadius))
                list.Add(Make(x, z, R, rng));
        }

        // A handful of large landmark basins
        float[] bigR = { 260f, 200f, 300f, 170f, 230f, 185f, 275f, 155f };
        for (int i = 0; i < bigR.Length; i++)
        {
            float R = bigR[i];
            float ang = i * (Mathf.PI * 2f / bigR.Length) + 0.35f;
            float dist = Mathf.Min(280f + i * 55f + R * 0.15f, terrainRadius * 0.68f);
            float x = Mathf.Cos(ang) * dist;
            float z = Mathf.Sin(ang) * dist;
            if (Fits(list, x, z, R, clear, terrainRadius))
                list.Add(Make(x, z, R, rng));
        }

        // Medium bowls
        for (int i = 0; i < 28; i++)
            TryAdd(list, rng, terrainRadius, clear, 45f, 120f, 50);

        // Small bowls
        for (int i = 0; i < 55; i++)
            TryAdd(list, rng, terrainRadius, clear, 14f, 42f, 35);

        // Tiny dots (still large enough for mesh resolution)
        for (int i = 0; i < 40; i++)
            TryAdd(list, rng, terrainRadius, clear, 8f, 16f, 25);

        // Sparse rim decoration near horizon
        for (int i = 0; i < 30; i++)
        {
            float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
            float R = 12f + (float)rng.NextDouble() * 36f;
            float dist = terrainRadius * (0.80f + (float)rng.NextDouble() * 0.12f);
            float x = Mathf.Cos(ang) * dist;
            float z = Mathf.Sin(ang) * dist;
            if (!Fits(list, x, z, R, clear, terrainRadius)) continue;
            list.Add(Make(x, z, R, rng));
        }

        return list.ToArray();
    }

    static void TryAdd(List<Crater> list, System.Random rng, float terrainRadius,
        float clearZone, float rMin, float rMax, int attempts)
    {
        for (int attempt = 0; attempt < attempts; attempt++)
        {
            float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
            float R = rMin + (float)rng.NextDouble() * (rMax - rMin);
            float ejecta = 1.45f;
            float minD = clearZone + R * ejecta + 2f;
            float maxD = terrainRadius * 0.94f - R * 0.6f;
            if (minD >= maxD) return;
            float dist = minD + (float)rng.NextDouble() * (maxD - minD);
            float x = Mathf.Cos(ang) * dist;
            float z = Mathf.Sin(ang) * dist;
            if (!Fits(list, x, z, R, clearZone, terrainRadius)) continue;
            list.Add(Make(x, z, R, rng));
            return;
        }
    }

    static bool Fits(List<Crater> list, float x, float z, float R, float clear, float terrainR)
    {
        float d = Mathf.Sqrt(x * x + z * z);
        float infl = R * 1.45f;
        if (d + R * 0.4f > terrainR * 0.97f) return false;
        if (d - infl * 0.75f < clear) return false;

        // Keep centers apart so bowls don't tear each other
        float minSep = R * 1.15f;
        for (int i = 0; i < list.Count; i++)
        {
            float dx = x - list[i].x;
            float dz = z - list[i].z;
            float sep = Mathf.Sqrt(dx * dx + dz * dz);
            float need = minSep + list[i].radius * 1.05f;
            // Allow mild nesting of small into large floors, not rim-on-rim
            if (list[i].radius > R * 2.2f && sep < list[i].radius * 0.55f)
                continue;
            if (sep < need * 0.72f) return false;
        }
        return true;
    }

    static Crater Make(float x, float z, float R, System.Random rng)
    {
        // Depth ~ 12–22% of diameter for simple bowls; shallower for big basins
        float dRatio = R > 140f
            ? (0.07f + (float)rng.NextDouble() * 0.05f)
            : (0.12f + (float)rng.NextDouble() * 0.08f);
        float depth = Mathf.Clamp(R * 2f * dRatio * 0.5f, R * 0.08f, R * 0.42f);

        var c = new Crater
        {
            x = x,
            z = z,
            radius = R,
            depth = depth,
            rimH = depth * (0.18f + (float)rng.NextDouble() * 0.10f),
            ejecta = 1.35f + (float)rng.NextDouble() * 0.20f,
            floorFrac = R > 140f
                ? 0.36f + (float)rng.NextDouble() * 0.08f
                : 0.28f + (float)rng.NextDouble() * 0.08f,
            peakH = 0f,
            peakR = 0f
        };

        // Soft central peak only on large basins
        if (R > 160f && rng.NextDouble() < 0.65)
        {
            c.peakH = depth * (0.18f + (float)rng.NextDouble() * 0.18f);
            c.peakR = R * (0.07f + (float)rng.NextDouble() * 0.05f);
        }
        return c;
    }

    static float SampleHeight(float x, float z, Crater[] craters, float terrainRadius)
    {
        float dist = Mathf.Sqrt(x * x + z * z);

        if (dist <= PadClearRadius)
            return Noise2(x * 0.35f, z * 0.35f) * 0.01f;

        float h = 0f;
        h += Noise2(x * 0.0016f, z * 0.0016f) * 2.8f;
        h += Noise2(x * 0.0048f + 11f, z * 0.0048f - 7f) * 1.15f;
        h += Noise2(x * 0.012f, z * 0.012f) * 0.35f;
        float edgeN = dist / Mathf.Max(1f, terrainRadius);
        h += edgeN * edgeN * 1.1f;

        float blend = Mathf.SmoothStep(0f, 1f, (dist - PadClearRadius) / 32f);
        h *= blend;

        float craterH = 0f;
        for (int i = 0; i < craters.Length; i++)
        {
            Crater c = craters[i];
            float dx = x - c.x;
            float dz = z - c.z;
            float d = Mathf.Sqrt(dx * dx + dz * dz);
            float outerR = c.radius * c.ejecta;
            if (d > outerR) continue;

            float p = CraterProfile(d, c, out _) * blend;
            float sk = Mathf.Max(3.5f, c.depth * 0.45f);
            if (p < 0f)
                craterH = SoftMin(craterH, p, sk);
            else
                craterH = SoftMax(craterH, p, sk * 0.8f);
        }

        return h + craterH;
    }

    /// <summary>
    /// Smooth circular bowl: floor → wall → rim crest → ejecta blanket.
    /// Single C2 path, no terraces/ellipse (those looked ragged).
    /// </summary>
    static float CraterProfile(float d, Crater c, out float shade)
    {
        float R = Mathf.Max(1f, c.radius);
        float t = d / R;
        float floorT = Mathf.Clamp(c.floorFrac, 0.22f, 0.48f);
        shade = 0.42f;

        // Central peak (optional, large basins)
        float peak = 0f;
        if (c.peakH > 0.05f)
        {
            float pt = d / Mathf.Max(0.4f, c.peakR);
            peak = c.peakH * Mathf.Exp(-pt * pt * 1.8f);
        }

        if (t <= floorT)
        {
            float ft = t / Mathf.Max(1e-4f, floorT);
            // Almost flat floor with tiny rise toward wall
            float h = -c.depth + Quintic01(ft) * 0.04f * c.depth + peak;
            shade = Mathf.Lerp(0.34f, 0.38f, ft);
            return h;
        }

        if (t <= 1f)
        {
            float u = (t - floorT) / Mathf.Max(0.12f, 1f - floorT);
            u = Mathf.Clamp01(u);
            float wall = Quintic01(u); // C2 floor→rim
            float h = Mathf.Lerp(-c.depth, c.rimH, wall) + peak * (1f - wall);

            // Soft gaussian crest centered at rim (t≈1), no sharp lip
            float crest = Mathf.Exp(-((t - 1f) * (t - 1f)) / (2f * 0.07f * 0.07f));
            h += c.rimH * 0.12f * crest;

            shade = Mathf.Lerp(0.36f, 0.46f, wall);
            shade = Mathf.Min(0.48f, shade + 0.02f * crest);
            return h;
        }

        // Ejecta: smooth fall from rim to zero
        float te = (t - 1f) / Mathf.Max(0.15f, c.ejecta - 1f);
        if (te >= 1f) { shade = 0.42f; return 0f; }
        te = Mathf.Clamp01(te);
        float fall = 1f - Quintic01(te);
        // Start ejecta from rim height continuously
        shade = Mathf.Lerp(0.45f, 0.42f, te);
        return c.rimH * fall * 0.65f;
    }

    static float Quintic01(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * t * (t * (t * 6f - 15f) + 10f);
    }

    static float SoftMin(float a, float b, float k)
    {
        // Polynomial smooth minimum — k in same units as heights
        k = Mathf.Max(0.5f, k);
        float h = Mathf.Clamp01(0.5f + 0.5f * (b - a) / k);
        return Mathf.Lerp(b, a, h) - k * h * (1f - h);
    }

    static float SoftMax(float a, float b, float k)
    {
        k = Mathf.Max(0.5f, k);
        float h = Mathf.Clamp01(0.5f + 0.5f * (a - b) / k);
        return Mathf.Lerp(b, a, h) + k * h * (1f - h);
    }

    static float SmoothAlbedoCurve(float s)
    {
        // Soft presentable band — floors a touch darker, rims lighter dust
        s = Mathf.Clamp01(s);
        return Mathf.Lerp(0.34f, 0.46f, Quintic01(s));
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
        GameObject go = null;
        Drain(CreateRoutine(parent, baseMat, g => go = g, resolution, radius));
        return go;
    }

    public static IEnumerator CreateRoutine(Transform parent, Material baseMat,
        System.Action<GameObject> onDone, int resolution = 256, float radius = -1f)
    {
        if (radius < 1f) radius = TerrainRadius;
        var go = new GameObject("LunarTerrain");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;

        var box = new BuildOutput();
        yield return BuildRoutine(box, Mathf.Max(resolution, 420), radius, 42);

        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = box.mesh;

        var albedo = box.albedo;
        var normalMap = box.normal;

        var mat = new Material(baseMat != null ? baseMat.shader : VisualMaterials.LitShader);
        // Neutral multiply — brightness lives in the baked albedo
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.028f);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.028f);

        if (mat.HasProperty("_BaseMap"))
        {
            mat.SetTexture("_BaseMap", albedo);
            mat.EnableKeyword("_BASEMAP");
        }
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", albedo);
        mat.mainTexture = albedo;

        // No bump map — mesh normals alone. Bump + low-res height = ragged shading.
        if (mat.HasProperty("_BumpMap"))
        {
            mat.SetTexture("_BumpMap", null);
            mat.DisableKeyword("_NORMALMAP");
            if (mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", 0f);
        }
        if (mat.HasProperty("_DetailNormalMapScale")) mat.SetFloat("_DetailNormalMapScale", 0f);
        _ = normalMap; // kept in BuildOutput for API compat / future use

        if (mat.HasProperty("_SpecularHighlights")) mat.SetFloat("_SpecularHighlights", 0f);
        if (mat.HasProperty("_EnvironmentReflections")) mat.SetFloat("_EnvironmentReflections", 0f);

        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        mr.receiveShadows = true;
        if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 2f);
        mat.doubleSidedGI = false;

        onDone?.Invoke(go);
    }
}
