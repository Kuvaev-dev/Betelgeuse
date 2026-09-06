using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Процедурний диск поверхні LZ (R≈2000 м). За замовчуванням — <b>Earth</b>
/// (поле/аеродром, без кратерів). Клас збережено для сумісності імен API.
/// </summary>
public static class LunarTerrainMesh
{
    /// <summary>true = земна LZ (трава/ґрунт); false = legacy lunar (не використовується в демо).</summary>
    public static bool EarthSurface = true;

    /// <summary>Рівна зона під палубою (berm ~R64).</summary>
    public const float PadClearRadius = 68f;

    /// <summary>Радіус диска місцевості (HorizonDisk ≤ цього).</summary>
    public const float TerrainRadius = 2000f;

    // Кеш згладженого height field — для props/beacons (bilinear sample)
    static float[,] _hCache;
    static int _hN;
    static float _hHalf;
    static float _hStep;
    static MeshCollider _terrainCol;
    static Transform _terrainXf;

    public sealed class BuildOutput
    {
        public Mesh mesh;
        public Texture2D albedo;
        public Texture2D normal;
    }

    /// <summary>Зареєструвати collider mesh для точного raycast props.</summary>
    public static void BindTerrainCollider(MeshCollider col)
    {
        _terrainCol = col;
        _terrainXf = col != null ? col.transform : null;
    }

    /// <summary>Y поверхні: raycast по mesh, fallback — smoothed height cache.</summary>
    public static float SampleSurfaceY(float x, float z)
    {
        // Точний hit по реальному terrain mesh (усуває «паріння»)
        if (_terrainCol != null)
        {
            float top = 400f;
            var origin = new Vector3(x, top, z);
            if (_terrainCol.Raycast(new Ray(origin, Vector3.down), out RaycastHit hit, 800f))
                return hit.point.y;
        }

        if (_hCache == null || _hN < 2)
            return ApproximateHeight(x, z);

        float fx = (x + _hHalf) / _hStep;
        float fz = (z + _hHalf) / _hStep;
        int ix = Mathf.FloorToInt(fx);
        int iz = Mathf.FloorToInt(fz);
        float tx = fx - ix;
        float tz = fz - iz;
        ix = Mathf.Clamp(ix, 0, _hN - 2);
        iz = Mathf.Clamp(iz, 0, _hN - 2);
        float h00 = _hCache[ix, iz];
        float h10 = _hCache[ix + 1, iz];
        float h01 = _hCache[ix, iz + 1];
        float h11 = _hCache[ix + 1, iz + 1];
        float h = Mathf.Lerp(Mathf.Lerp(h00, h10, tx), Mathf.Lerp(h01, h11, tx), tz);

        float dist = Mathf.Sqrt(x * x + z * z);
        float edge = Mathf.Clamp01((TerrainRadius - dist) / (TerrainRadius * 0.04f));
        if (edge < 1f)
            h = Mathf.Lerp(h - 1.2f, h, Quintic01(edge));
        return h;
    }

    static float ApproximateHeight(float x, float z)
    {
        return SampleHeight(x, z, System.Array.Empty<Crater>(), TerrainRadius);
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

    /// <summary>Прогнати вкладені IEnumerator до кінця (sync). Unity-корутини роблять це автоматично.</summary>
    public static void Drain(IEnumerator e)
    {
        if (e == null) return;
        while (e.MoveNext())
        {
            if (e.Current is IEnumerator nested)
                Drain(nested);
        }
    }

    /// <summary>Опційний progress 0…1 під час довгої генерації (splash bar).</summary>
    public static System.Action<float, string, string> ProgressHook;

    static void ReportProgress(float t, string uk, string en)
    {
        ProgressHook?.Invoke(t, uk, en);
        // Fallback на Splash напряму
        if (ProgressHook == null)
        {
            var s = SplashScreenUI.Instance;
            if (s != null)
                s.SetProgress(t, UILocale.IsUK ? uk : en);
        }
    }

    /// <summary>Як Build, але yield кожні кілька рядків, щоб splash-спінер крутився.</summary>
    public static IEnumerator BuildRoutine(BuildOutput box,
        int resolution = 256, float radius = -1f, int seed = 42)
    {
        if (box == null) yield break;
        if (radius < 1f) radius = TerrainRadius;
        resolution = Mathf.Clamp(resolution, 96, 320);
        var rng = new System.Random(seed);
        // Earth LZ: без кратерів; lunar legacy лише якщо EarthSurface=false
        var craters = EarthSurface ? System.Array.Empty<Crater>() : BuildCraterField(rng, radius);

        int n = resolution + 1;
        float half = radius;
        float step = (half * 2f) / resolution;
        var height = new float[n, n];

        ReportProgress(0.60f, "Меш: висоти…", "Mesh: heights…");
        for (int iz = 0; iz < n; iz++)
        {
            for (int ix = 0; ix < n; ix++)
            {
                float x = -half + ix * step;
                float z = -half + iz * step;
                height[ix, iz] = SampleHeight(x, z, craters, radius);
            }
            if ((iz & 31) == 0)
            {
                float p = 0.60f + 0.03f * (iz / (float)Mathf.Max(1, n - 1));
                ReportProgress(p, "Меш: висоти…", "Mesh: heights…");
                yield return null;
            }
        }

        // Згладити → круглі чаші без multi-pass зависання
        SmoothHeightField(height, n, EarthSurface ? 2 : 3);
        // Кеш для SampleSurfaceY (props сідають на mesh)
        _hCache = height;
        _hN = n;
        _hHalf = half;
        _hStep = step;
        yield return null;

        ReportProgress(0.63f, "Меш: albedo…", "Mesh: albedo…");
        yield return null;
        // 512² — швидко; 1M GetPixel + PH sample раніше блокував splash
        int texSize = Mathf.ClosestPowerOfTwo(Mathf.Clamp(resolution * 2, 256, 512));
        Texture2D albedoTex = null;
        Texture2D normalTex = null;
        yield return BuildSurfaceMapsRoutine(craters, radius, texSize, seed,
            t => albedoTex = t, t => normalTex = t);
        ReportProgress(0.645f, "Меш: вершини…", "Mesh: vertices…");
        yield return null;

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
                // Дуже м’яка зовнішня кромка
                float edge = Mathf.Clamp01((radius - dist) / (radius * 0.04f));
                if (edge < 1f) h = Mathf.Lerp(h - 1.2f, h, Quintic01(edge));

                map[ix, iz] = vertList.Count;
                vertList.Add(new Vector3(x, h, z));
                uvList.Add(new Vector2(
                    (x * invR + 1f) * 0.5f,
                    (z * invR + 1f) * 0.5f));
            }
            if ((iz & 31) == 0)
            {
                float p = 0.645f + 0.01f * (iz / (float)Mathf.Max(1, n - 1));
                ReportProgress(p, "Меш: вершини…", "Mesh: vertices…");
                yield return null;
            }
        }

        // Snap only the circular-cull silhouette verts onto the exact radius
        // so the disk rim reads as a smooth arc (cartesian interior unchanged).
        CircularizeOuterRing(vertList, uvList, map, n, radius, invR);
        yield return null;

        ReportProgress(0.655f, "Меш: трикутники…", "Mesh: triangles…");
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
            if ((iz & 63) == 0)
            {
                float p = 0.655f + 0.008f * (iz / (float)Mathf.Max(1, resolution));
                ReportProgress(p, "Меш: трикутники…", "Mesh: triangles…");
                yield return null;
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
            if ((t & 16383) == 0 && t > 0) yield return null;
        }
        for (int i = 0; i < norms.Length; i++)
        {
            if (norms[i].sqrMagnitude > 1e-12f) norms[i].Normalize();
            else norms[i] = Vector3.up;
        }
        yield return null;

        // Один Laplacian-прохід normals через shared edges → м’якші краї кратерів
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
            name = EarthSurface ? "EarthTerrainDisk" : "LunarTerrainDisk",
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
        Debug.Log($"[Terrain] earth={EarthSurface} verts={verts.Length} tris={triArr.Length / 3} craters={craters.Length} tex={texSize}");
    }

    /// <summary>
    /// Albedo + normal у тій самій world-space проєкції, що й mesh UV.
    /// Yield кожні кілька рядків, щоб splash-спінер крутився.
    /// </summary>
    static IEnumerator BuildSurfaceMapsRoutine(Crater[] craters, float terrainRadius, int texSize, int seed,
        System.Action<Texture2D> setAlbedo, System.Action<Texture2D> setNormal)
    {
        // Чистий procedural bake — без зовнішніх tiles (tiling = рвані шви).
        // Заповнити ВЕСЬ квадрат безперервно (без жорсткого чорного краю кола).
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
                // g = luminance proxy; для Earth перефарбовується в RGB нижче
                float g = EarthSurface ? 0.38f : 0.42f;

                if (dist <= PadClearRadius)
                {
                    float padT = dist / Mathf.Max(1f, PadClearRadius);
                    g = EarthSurface
                        ? Mathf.Lerp(0.32f, 0.40f, padT * padT)
                        : Mathf.Lerp(0.34f, 0.40f, padT * padT);
                    g += Noise2(wx * 0.2f, wz * 0.2f) * 0.01f;
                }
                else
                {
                    if (EarthSurface)
                    {
                        h += Noise2(wx * 0.0007f, wz * 0.0007f) * 7.5f;
                        h += Noise2(wx * 0.0016f + 3f, wz * 0.0016f - 2f) * 4.0f;
                        h += Noise2(wx * 0.0035f + 9f, wz * 0.0035f - 4f) * 2.2f;
                        h += Noise2(wx * 0.009f, wz * 0.009f) * 0.85f;
                        h += Noise2(wx * 0.025f, wz * 0.025f) * 0.28f;
                        float ridge = Mathf.Abs(Noise2(wx * 0.0012f + 1.5f, wz * 0.0012f));
                        h += (1f - ridge) * 1.4f;
                        float edgeN = dist * invHalf;
                        h *= 1f - edgeN * edgeN * 0.25f;
                        h -= edgeN * edgeN * 1.1f;
                    }
                    else
                    {
                        h += Noise2(wx * 0.0016f, wz * 0.0016f) * 2.8f;
                        h += Noise2(wx * 0.0048f + 11f, wz * 0.0048f - 7f) * 1.15f;
                        h += Noise2(wx * 0.012f, wz * 0.012f) * 0.28f;
                        float edgeN = dist * invHalf;
                        h += edgeN * edgeN * 1.1f;
                    }

                    float blend = Mathf.SmoothStep(0f, 1f, (dist - PadClearRadius) / 32f);
                    h *= blend;

                    float grain = 0f;
                    grain += Noise2(wx * 0.018f, wz * 0.018f) * 0.016f;
                    grain += Noise2(wx * 0.045f + 4f, wz * 0.045f - 3f) * 0.008f;
                    g = (EarthSurface ? 0.40f : 0.42f) + grain;

                    if (!EarthSurface)
                    {
                        float mare = Noise2(wx * 0.0009f + 2f, wz * 0.0009f - 1f);
                        mare = Mathf.SmoothStep(0.20f, 0.58f, mare * 0.5f + 0.5f);
                        g -= mare * 0.045f;
                    }
                    else
                    {
                        // Поля / смуги ґрунту
                        float field = Noise2(wx * 0.0011f + 3f, wz * 0.0011f - 2f);
                        g += field * 0.04f;
                    }

                    if (dist < PadClearRadius + 70f)
                    {
                        float pt = Mathf.SmoothStep(0f, 1f, dist / (PadClearRadius + 70f));
                        g = Mathf.Lerp(EarthSurface ? 0.36f : 0.38f, g, pt);
                    }
                }

                if (dist > terrainRadius)
                {
                    float over = (dist - terrainRadius) / Mathf.Max(1f, terrainRadius * 0.15f);
                    over = Mathf.Clamp01(over);
                    h = Mathf.Lerp(h, h - 0.8f, Quintic01(over));
                    g = Mathf.Lerp(g, EarthSurface ? 0.34f : 0.38f, Quintic01(over) * 0.35f);
                }

                hBuf[idx] = h;
                aBuf[idx] = g;
            }
            if ((y & 31) == 0) yield return null;
        }

        var order = new int[craters.Length];
        for (int i = 0; i < order.Length; i++) order[i] = i;
        System.Array.Sort(order, (ia, ib) => craters[ib].radius.CompareTo(craters[ia].radius));
        yield return null;
        RasterizeCratersInto(hBuf, aBuf, craters, order, terrainRadius, texSize, half, metersPerTexel);
        yield return null;

        // Легке розмиття — достатньо пом’якшити краї без multi-pass зависань
        BlurBuffer(hBuf, texSize, 2);
        yield return null;
        BlurBuffer(aBuf, texSize, 2);
        yield return null;

        var albedoTex = new Texture2D(texSize, texSize, TextureFormat.RGB24, true, false);
        albedoTex.name = EarthSurface ? "EarthAlbedo_Final" : "LunarAlbedo_Final";
        albedoTex.wrapMode = TextureWrapMode.Clamp;
        albedoTex.filterMode = FilterMode.Trilinear;
        albedoTex.anisoLevel = 8;

        // Плоска normal map (рельєф несе меш). Bump maps дають double-shade і рваний вигляд.
        var normalTex = new Texture2D(4, 4, TextureFormat.RGBA32, false, true);
        normalTex.name = EarthSurface ? "EarthNormal_Flat" : "LunarNormal_Flat";
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
                float g = Mathf.Clamp(aBuf[idx], 0.28f, 0.55f);
                float dist = Mathf.Sqrt(wx * wx + wz * wz);
                float rim = Mathf.SmoothStep(terrainRadius * 0.92f, terrainRadius * 1.02f, dist);
                g = Mathf.Lerp(g, EarthSurface ? 0.36f : 0.38f, rim * 0.18f);

                if (EarthSurface)
                {
                    float n1 = Noise2(wx * 0.0009f, wz * 0.0009f) * 0.5f + 0.5f;
                    float n2 = Noise2(wx * 0.0038f + 2.3f, wz * 0.0038f - 1.4f) * 0.5f + 0.5f;
                    float n3 = Noise2(wx * 0.012f, wz * 0.012f) * 0.5f + 0.5f;
                    float n4 = Noise2(wx * 0.035f + 5f, wz * 0.035f) * 0.5f + 0.5f;
                    float n5 = Noise2(wx * 0.09f, wz * 0.09f) * 0.5f + 0.5f;
                    float n6 = Noise2(wx * 0.18f + 7f, wz * 0.18f) * 0.5f + 0.5f;
                    float nearPad = 1f - Mathf.SmoothStep(PadClearRadius * 0.6f, PadClearRadius + 180f, dist);

                    // Meadow bake — та сама dark-olive сім’я, що й FoliageGreen у props
                    EnvironmentTextures.EnsureLoaded();
                    Color ph = EnvironmentTextures.SampleGrassWorld(wx, wz,
                        EnvironmentTextures.FoliageGreen.r,
                        EnvironmentTextures.FoliageGreen.g,
                        EnvironmentTextures.FoliageGreen.b);
                    float r = ph.r * (0.95f + n1 * 0.1f);
                    float grn = ph.g * (0.95f + n2 * 0.1f);
                    float b = ph.b * (0.95f + n1 * 0.08f);

                    // М’які варіації в межах тієї ж палітри (без яскравого green)
                    float lush = Mathf.SmoothStep(0.25f, 0.85f, n2) * Mathf.SmoothStep(0.2f, 0.7f, 1f - n1);
                    r = Mathf.Lerp(r, EnvironmentTextures.FoliageDark.r, lush * 0.25f);
                    grn = Mathf.Lerp(grn, EnvironmentTextures.FoliageGreen.g * 1.05f, lush * 0.3f);
                    b = Mathf.Lerp(b, EnvironmentTextures.FoliageDark.b, lush * 0.25f);

                    float forest = Mathf.SmoothStep(0.55f, 0.9f, n1) * 0.35f;
                    r = Mathf.Lerp(r, EnvironmentTextures.FoliageDark.r, forest);
                    grn = Mathf.Lerp(grn, EnvironmentTextures.FoliageDark.g, forest);
                    b = Mathf.Lerp(b, EnvironmentTextures.FoliageDark.b, forest);

                    float dry = Mathf.SmoothStep(0.55f, 0.95f, n2) * Mathf.SmoothStep(0.4f, 0.8f, n1) * 0.28f;
                    r = Mathf.Lerp(r, EnvironmentTextures.SoilBrown.r, dry);
                    grn = Mathf.Lerp(grn, EnvironmentTextures.SoilBrown.g, dry);
                    b = Mathf.Lerp(b, EnvironmentTextures.SoilBrown.b, dry);

                    // Exposed soil
                    float soil = Mathf.SmoothStep(0.62f, 0.96f, n3) * Mathf.SmoothStep(0.2f, 0.65f, n1) * 0.5f;
                    r = Mathf.Lerp(r, 0.36f + n5 * 0.04f, soil);
                    grn = Mathf.Lerp(grn, 0.27f + n5 * 0.03f, soil);
                    b = Mathf.Lerp(b, 0.17f, soil);

                    // Winding dirt tracks
                    float path = Mathf.Abs(Mathf.Sin(wx * 0.0045f + wz * 0.0024f + n1));
                    path = 1f - Mathf.SmoothStep(0.015f, 0.055f, path);
                    float path2 = Mathf.Abs(Mathf.Sin(wz * 0.004f - wx * 0.0017f + 1.4f));
                    path2 = 1f - Mathf.SmoothStep(0.015f, 0.05f, path2);
                    float tracks = Mathf.Max(path, path2) * (1f - nearPad * 0.85f) * 0.42f;
                    r = Mathf.Lerp(r, 0.38f, tracks);
                    grn = Mathf.Lerp(grn, 0.30f, tracks);
                    b = Mathf.Lerp(b, 0.19f, tracks);

                    // Service ring road around pad
                    float ring = Mathf.Abs(dist - (PadClearRadius + 55f));
                    float road = 1f - Mathf.SmoothStep(4f, 14f, ring);
                    road *= (1f - nearPad * 0.3f) * 0.55f;
                    r = Mathf.Lerp(r, 0.34f, road);
                    grn = Mathf.Lerp(grn, 0.31f, road);
                    b = Mathf.Lerp(b, 0.26f, road);

                    // Micro grain + subtle wildflower flecks
                    grn += (n5 - 0.5f) * 0.035f;
                    r += (n4 - 0.5f) * 0.012f;
                    float fleck = 1f - Mathf.SmoothStep(0.02f, 0.07f, Mathf.Abs(n6 - 0.68f));
                    fleck *= 0.08f * (1f - nearPad) * Mathf.SmoothStep(0.35f, 0.7f, n2);
                    r = Mathf.Lerp(r, 0.55f, fleck);
                    grn = Mathf.Lerp(grn, 0.4f, fleck);

                    // Warm apron
                    float apron = nearPad * 0.92f;
                    r = Mathf.Lerp(r, 0.44f + n3 * 0.03f, apron);
                    grn = Mathf.Lerp(grn, 0.39f + n3 * 0.02f, apron);
                    b = Mathf.Lerp(b, 0.29f + n2 * 0.02f, apron);

                    // Atmospheric edge
                    float edge = Mathf.SmoothStep(0.88f, 1.05f, dist / Mathf.Max(1f, terrainRadius));
                    r = Mathf.Lerp(r, 0.30f, edge * 0.55f);
                    grn = Mathf.Lerp(grn, 0.34f, edge * 0.5f);
                    b = Mathf.Lerp(b, 0.36f, edge * 0.6f);

                    if (apron < 0.35f)
                        grn = Mathf.Max(grn, r + 0.05f);

                    albedoCols[idx] = new Color(Mathf.Clamp01(r), Mathf.Clamp01(grn), Mathf.Clamp01(b), 1f);
                }
                else
                {
                    float v = Mathf.Lerp(0.40f, g, 0.88f);
                    albedoCols[idx] = new Color(
                        Mathf.Clamp01(v * 0.97f),
                        Mathf.Clamp01(v * 0.99f),
                        Mathf.Clamp01(v * 1.035f),
                        1f);
                }
            }
            if ((y & 31) == 0)
            {
                float p = 0.63f + 0.015f * (y / (float)Mathf.Max(1, texSize - 1));
                ReportProgress(p, "Меш: albedo…", "Mesh: albedo…");
                yield return null;
            }
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

                    // М’яка радіальна вага — без жорстких країв albedo кратерів
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
                // М’яке змішування — геометрія показує чашу, albedo лише натякає
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

    /// <summary>Розмиття albedo ~5×5 gaussian — прибирає залишкові «сходинки» на краях.</summary>
    static void BlurBufferWide(float[] buf, int n, int passes)
    {
        if (passes <= 0) return;
        var tmp = new float[buf.Length];
        // приблизно біноміальні ваги для radius 2
        float[] ker = { 1f, 4f, 6f, 4f, 1f };
        for (int p = 0; p < passes; p++)
        {
            // горизонтальний
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
            // вертикальний
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
        // Менше, добре рознесених круглих чаш — щільні поля виглядали рвано на меші
        const float clear = PadClearRadius + 8f;
        var list = new List<Crater>(220);

        // Кільце малих свіжих кратерів одразу за pad
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

        // Кілька великих характерних басейнів
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

        // Середні чаші
        for (int i = 0; i < 28; i++)
            TryAdd(list, rng, terrainRadius, clear, 45f, 120f, 50);

        // Малі чаші
        for (int i = 0; i < 55; i++)
            TryAdd(list, rng, terrainRadius, clear, 14f, 42f, 35);

        // Крихітні крапки (все ще достатні для роздільності меша)
        for (int i = 0; i < 40; i++)
            TryAdd(list, rng, terrainRadius, clear, 8f, 16f, 25);

        // Розріджений декор краю біля горизонту
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

        // Тримати центри на відстані, щоб чаші не рвали одна одну
        float minSep = R * 1.15f;
        for (int i = 0; i < list.Count; i++)
        {
            float dx = x - list[i].x;
            float dz = z - list[i].z;
            float sep = Mathf.Sqrt(dx * dx + dz * dz);
            float need = minSep + list[i].radius * 1.05f;
            // Дозволити м’яке вкладення малих у великі днища, не rim-on-rim
            if (list[i].radius > R * 2.2f && sep < list[i].radius * 0.55f)
                continue;
            if (sep < need * 0.72f) return false;
        }
        return true;
    }

    static Crater Make(float x, float z, float R, System.Random rng)
    {
        // Глибина ~12–22% діаметра для простих чаш; мілкіше для великих басейнів
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

        // М’який центральний пік лише на великих басейнах
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

        // Глибока яма під LZ, щоб меш pad не z-fight з диском рельєфу
        if (dist <= PadClearRadius)
        {
            float t = dist / Mathf.Max(1f, PadClearRadius);
            // Днище ~−1.6 м у центрі, підйом до ~0 на clear edge
            return Mathf.Lerp(-1.6f, -0.15f, t * t) + Noise2(x * 0.2f, z * 0.2f) * 0.02f;
        }

        float h = 0f;
        float edgeN = dist / Mathf.Max(1f, terrainRadius);
        if (EarthSurface)
        {
            // Багатооктавні пагорби / долини (синхронно з SampleApproxHeight)
            h += Noise2(x * 0.0007f, z * 0.0007f) * 7.5f;
            h += Noise2(x * 0.0016f + 3f, z * 0.0016f - 2f) * 4.0f;
            h += Noise2(x * 0.0035f + 9f, z * 0.0035f - 4f) * 2.2f;
            h += Noise2(x * 0.009f, z * 0.009f) * 0.85f;
            h += Noise2(x * 0.025f, z * 0.025f) * 0.28f;
            // М’які «хребти» полів
            float ridge = Mathf.Abs(Noise2(x * 0.0012f + 1.5f, z * 0.0012f));
            h += (1f - ridge) * 1.4f;
            h *= 1f - edgeN * edgeN * 0.25f;
            h -= edgeN * edgeN * 1.1f;
        }
        else
        {
            h += Noise2(x * 0.0016f, z * 0.0016f) * 2.8f;
            h += Noise2(x * 0.0048f + 11f, z * 0.0048f - 7f) * 1.15f;
            h += Noise2(x * 0.012f, z * 0.012f) * 0.35f;
            h += edgeN * edgeN * 1.1f;
        }

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
    /// Гладка кругла чаша: днище → стіна → гребінь краю → покривало ejecta.
    /// Єдиний C2-шлях, без терас/еліпсів (вони виглядали рвано).
    /// </summary>
    static float CraterProfile(float d, Crater c, out float shade)
    {
        float R = Mathf.Max(1f, c.radius);
        float t = d / R;
        float floorT = Mathf.Clamp(c.floorFrac, 0.22f, 0.48f);
        shade = 0.42f;

        // Центральний пік (опційно, великі басейни)
        float peak = 0f;
        if (c.peakH > 0.05f)
        {
            float pt = d / Mathf.Max(0.4f, c.peakR);
            peak = c.peakH * Mathf.Exp(-pt * pt * 1.8f);
        }

        if (t <= floorT)
        {
            float ft = t / Mathf.Max(1e-4f, floorT);
            // Майже плоске днище з ледь помітним підйомом до стіни
            float h = -c.depth + Quintic01(ft) * 0.04f * c.depth + peak;
            shade = Mathf.Lerp(0.34f, 0.38f, ft);
            return h;
        }

        if (t <= 1f)
        {
            float u = (t - floorT) / Mathf.Max(0.12f, 1f - floorT);
            u = Mathf.Clamp01(u);
            float wall = Quintic01(u); // C2 днище→край
            float h = Mathf.Lerp(-c.depth, c.rimH, wall) + peak * (1f - wall);

            // М’який gaussian-гребінь у центрі краю (t≈1), без гострої кромки
            float crest = Mathf.Exp(-((t - 1f) * (t - 1f)) / (2f * 0.07f * 0.07f));
            h += c.rimH * 0.12f * crest;

            shade = Mathf.Lerp(0.36f, 0.46f, wall);
            shade = Mathf.Min(0.48f, shade + 0.02f * crest);
            return h;
        }

        // Ejecta: плавний спад від краю до нуля
        float te = (t - 1f) / Mathf.Max(0.15f, c.ejecta - 1f);
        if (te >= 1f) { shade = 0.42f; return 0f; }
        te = Mathf.Clamp01(te);
        float fall = 1f - Quintic01(te);
        // Починати ejecta з висоти краю неперервно
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
        // Поліноміальний smooth minimum — k у тих самих одиницях, що висоти
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
        // М’яка презентабельна смуга — днища трохи темніші, краї — світліший пил
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
        // quintic fade — менше grid-артефактів, ніж cubic
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
        var go = new GameObject(EarthSurface ? "EarthTerrain" : "LunarTerrain");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;

        var box = new BuildOutput();
        yield return BuildRoutine(box, resolution, radius, 42);

        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = box.mesh;

        var albedo = box.albedo;
        var normalMap = box.normal;

        bool useRimShader = baseMat != null && baseMat.shader != null
            && baseMat.shader.name == "Betelgeuse/LandingRangeGround";
        var mat = new Material(baseMat != null ? baseMat.shader : VisualMaterials.LitShader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);

        if (useRimShader)
        {
            // Tiled grass detail stays on _BaseMap; baked disk albedo drives macro tint via world XZ.
            if (baseMat.HasProperty("_BaseMap") && mat.HasProperty("_BaseMap"))
            {
                var baseMap = baseMat.GetTexture("_BaseMap");
                if (baseMap != null)
                    baseMap.wrapMode = TextureWrapMode.Repeat;
                mat.SetTexture("_BaseMap", baseMap);
                mat.EnableKeyword("_BASEMAP");
            }
            if (baseMat.HasProperty("_BumpMap") && mat.HasProperty("_BumpMap"))
            {
                var bump = baseMat.GetTexture("_BumpMap");
                if (bump != null)
                    bump.wrapMode = TextureWrapMode.Repeat;
                mat.SetTexture("_BumpMap", bump);
                mat.EnableKeyword("_NORMALMAP");
                if (mat.HasProperty("_BumpScale"))
                    mat.SetFloat("_BumpScale", baseMat.HasProperty("_BumpScale") ? baseMat.GetFloat("_BumpScale") : 0.55f);
            }
            // Clamp macro bake: one non-tiling map over the terrain AABB (shader maps world XZ -> UV).
            if (albedo != null)
                albedo.wrapMode = TextureWrapMode.Clamp;
            if (mat.HasProperty("_MacroMap")) mat.SetTexture("_MacroMap", albedo);
            if (mat.HasProperty("_MacroStrength"))
                mat.SetFloat("_MacroStrength", baseMat.HasProperty("_MacroStrength") ? baseMat.GetFloat("_MacroStrength") : 0.45f);
            if (mat.HasProperty("_MacroBright"))
                mat.SetFloat("_MacroBright", baseMat.HasProperty("_MacroBright") ? baseMat.GetFloat("_MacroBright") : 1.35f);
            if (mat.HasProperty("_TileMeters"))
                mat.SetFloat("_TileMeters", baseMat.HasProperty("_TileMeters") ? baseMat.GetFloat("_TileMeters") : 16f);
            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", baseMat.HasProperty("_Smoothness") ? baseMat.GetFloat("_Smoothness") : 0.16f);
            if (mat.HasProperty("_TerrainRadius")) mat.SetFloat("_TerrainRadius", radius);
            if (mat.HasProperty("_RimFadeWidth"))
                mat.SetFloat("_RimFadeWidth", baseMat.HasProperty("_RimFadeWidth") ? Mathf.Clamp(baseMat.GetFloat("_RimFadeWidth"), 40f, 120f) : 80f);
            if (mat.HasProperty("_RimFogColor"))
                mat.SetColor("_RimFogColor", baseMat.HasProperty("_RimFogColor") ? baseMat.GetColor("_RimFogColor") : EnvironmentTextures.FogColor);
            mat.renderQueue = 3000;
        }
        else
        {
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.028f);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.028f);
            if (mat.HasProperty("_BaseMap"))
            {
                mat.SetTexture("_BaseMap", albedo);
                mat.EnableKeyword("_BASEMAP");
            }
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", albedo);
            mat.mainTexture = albedo;
            if (mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap", null);
                mat.DisableKeyword("_NORMALMAP");
                if (mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", 0f);
            }
            if (mat.HasProperty("_DetailNormalMapScale")) mat.SetFloat("_DetailNormalMapScale", 0f);
            if (mat.HasProperty("_SpecularHighlights")) mat.SetFloat("_SpecularHighlights", 0f);
            if (mat.HasProperty("_EnvironmentReflections")) mat.SetFloat("_EnvironmentReflections", 0f);
        }
        _ = normalMap;

        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        mr.receiveShadows = true;
        if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 2f);
        mat.doubleSidedGI = false;

        // Collider for SampleSurfaceY raycast (props sit on real mesh)
        var oldCol = go.GetComponent<MeshCollider>();
        if (oldCol != null) Object.Destroy(oldCol);
        var mc = go.AddComponent<MeshCollider>();
        mc.sharedMesh = box.mesh;
        mc.convex = false;
        BindTerrainCollider(mc);

        // Vertical skirt hides the paper-thin disk rim when the camera looks low.
        AttachRimSkirt(go.transform, radius, mat);

        onDone?.Invoke(go);
    }


    /// <summary>
    /// Project only the circular-cull boundary verts onto the exact terrain radius.
    /// Keeps the cartesian heightfield interior (no full polar rewrite) while making
    /// the visible outer silhouette a true circle — chords between adjacent boundary
    /// edges are ~one cell wide, so sagitta is sub-centimetre at R=2000.
    /// </summary>
    static void CircularizeOuterRing(
        List<Vector3> verts, List<Vector2> uvs, int[,] map, int n,
        float radius, float invR)
    {
        if (verts == null || map == null || n < 3 || radius < 1f) return;

        for (int iz = 0; iz < n; iz++)
        {
            for (int ix = 0; ix < n; ix++)
            {
                int vi = map[ix, iz];
                if (vi < 0) continue;

                bool boundary = ix == 0 || iz == 0 || ix == n - 1 || iz == n - 1;
                if (!boundary)
                {
                    if (map[ix - 1, iz] < 0 || map[ix + 1, iz] < 0 ||
                        map[ix, iz - 1] < 0 || map[ix, iz + 1] < 0)
                        boundary = true;
                    else if (map[ix - 1, iz - 1] < 0 || map[ix + 1, iz - 1] < 0 ||
                             map[ix - 1, iz + 1] < 0 || map[ix + 1, iz + 1] < 0)
                        boundary = true;
                }
                if (!boundary) continue;

                Vector3 v = verts[vi];
                float dist = Mathf.Sqrt(v.x * v.x + v.z * v.z);
                if (dist < 1e-4f) continue;

                float s = radius / dist;
                float x = v.x * s;
                float z = v.z * s;

                float h = v.y;
                if (_hCache != null && _hN > 1 && _hStep > 1e-6f)
                {
                    float fx = (x + _hHalf) / _hStep;
                    float fz = (z + _hHalf) / _hStep;
                    int hx = Mathf.Clamp(Mathf.FloorToInt(fx), 0, _hN - 2);
                    int hz = Mathf.Clamp(Mathf.FloorToInt(fz), 0, _hN - 2);
                    float tx = Mathf.Clamp01(fx - hx);
                    float tz = Mathf.Clamp01(fz - hz);
                    h = Mathf.Lerp(
                        Mathf.Lerp(_hCache[hx, hz], _hCache[hx + 1, hz], tx),
                        Mathf.Lerp(_hCache[hx, hz + 1], _hCache[hx + 1, hz + 1], tx), tz);
                }

                // Exact rim: full edge dip (matches BuildRoutine edge term at dist==radius).
                h -= 1.2f;

                verts[vi] = new Vector3(x, h, z);
                if (uvs != null && vi < uvs.Count)
                    uvs[vi] = new Vector2((x * invR + 1f) * 0.5f, (z * invR + 1f) * 0.5f);
            }
        }
    }

    /// <summary>
    /// Haze shell under the disk rim: deep flared skirt + underside cap (no outward apron).
    /// Hides paper-thin edge and fills the below-near-rim void (camera clear / sky bottom)
    /// with fog-matched geometry when the camera tilts low.
    /// </summary>
    static void AttachRimSkirt(Transform parent, float radius, Material groundMat)
    {
        if (parent == null || radius < 10f) return;

        const int segs = 384;
        // Deep enough that low outside views still hit the wall/cap instead of sky void.
        float skirtDepth = Mathf.Clamp(radius * 0.55f, 700f, 1400f);
        float flare = Mathf.Clamp(radius * 0.14f, 220f, 400f);
        float rTop = radius;
        float rBot = radius + flare;

        float ySum = 0f;
        int yCount = 0;
        for (int i = 0; i < 16; i++)
        {
            float a = (i / 16f) * Mathf.PI * 2f;
            ySum += SampleSurfaceY(Mathf.Cos(a) * rTop, Mathf.Sin(a) * rTop);
            yCount++;
        }
        float topYAvg = yCount > 0 ? ySum / yCount : 0f;
        float bottomY = topYAvg - skirtDepth;

        // Skirt (2 rings) + underside fan (center + bottom ring reuse via extra center vert).
        int skirtVertCount = segs * 2;
        int centerIndex = skirtVertCount;
        var verts = new Vector3[skirtVertCount + 1];
        var norms = new Vector3[skirtVertCount + 1];
        var uvs = new Vector2[skirtVertCount + 1];
        // skirt quads + underside fan
        var tris = new int[segs * 6 + segs * 3];

        for (int i = 0; i < segs; i++)
        {
            float t0 = (i / (float)segs) * Mathf.PI * 2f;
            float c = Mathf.Cos(t0);
            float s = Mathf.Sin(t0);
            float xTop = c * rTop;
            float zTop = s * rTop;
            float xBot = c * rBot;
            float zBot = s * rBot;
            float yTop = SampleSurfaceY(xTop, zTop);
            verts[i] = new Vector3(xTop, yTop, zTop);
            verts[i + segs] = new Vector3(xBot, bottomY, zBot);

            // Outward + slight downward so flared wall lights consistently.
            Vector3 outward = new Vector3(c, -0.15f, s).normalized;
            norms[i] = outward;
            norms[i + segs] = outward;
            float u = i / (float)segs;
            uvs[i] = new Vector2(u, 1f);
            uvs[i + segs] = new Vector2(u, 0f);

            int i1 = (i + 1) % segs;
            int t = i * 6;
            tris[t] = i;
            tris[t + 1] = i1;
            tris[t + 2] = i + segs;
            tris[t + 3] = i1;
            tris[t + 4] = i1 + segs;
            tris[t + 5] = i + segs;
        }

        verts[centerIndex] = new Vector3(0f, bottomY, 0f);
        norms[centerIndex] = Vector3.down;
        uvs[centerIndex] = new Vector2(0.5f, 0.5f);

        int fanBase = segs * 6;
        for (int i = 0; i < segs; i++)
        {
            int i1 = (i + 1) % segs;
            int t = fanBase + i * 3;
            // Downward-facing fan (viewed from outside / under the disk).
            tris[t] = centerIndex;
            tris[t + 1] = i1 + segs;
            tris[t + 2] = i + segs;
        }

        var mesh = new Mesh
        {
            name = "TerrainRimSkirt",
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt16
        };
        mesh.vertices = verts;
        mesh.normals = norms;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateBounds();

        Color haze = EnvironmentTextures.FogColor;
        if (groundMat != null && groundMat.HasProperty("_RimFogColor"))
            haze = groundMat.GetColor("_RimFogColor");
        // Stay close to fog/horizon so the shell reads as atmosphere, not a dark cliff.
        haze = Color.Lerp(haze, EnvironmentTextures.SkyHorizon, 0.25f);
        haze = Color.Lerp(haze, new Color(0.34f, 0.40f, 0.42f), 0.18f);
        haze.a = 1f;

        var skirtMat = VisualMaterials.Unlit(haze);
        skirtMat.name = "TerrainRimSkirtHaze";
        if (skirtMat.HasProperty("_Cull")) skirtMat.SetFloat("_Cull", 0f); // both sides
        skirtMat.renderQueue = 2000;

        var skirtGo = new GameObject("TerrainRimSkirt");
        skirtGo.transform.SetParent(parent, false);
        skirtGo.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = skirtGo.AddComponent<MeshRenderer>();
        mr.sharedMaterial = skirtMat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

}
