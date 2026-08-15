using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Високополігональні круглі меші (Unity Cylinder/Sphere ≈ 20 граней — кутасті).
/// </summary>
public static class SmoothMesh
{
    const int DefaultSeg = 96;
    const int SphereLat = 48;
    const int SphereLon = 64;

    static Mesh cachedDisc;
    static Mesh cachedCylinder;
    static Mesh cachedSphere;
    static Mesh cachedCapsule;

    /// <summary>Плоский диск у площині XZ, нормаль +Y, радіус 0.5 (scale.x/z = діаметр).</summary>
    public static Mesh Disc(int segments = DefaultSeg)
    {
        segments = Mathf.Clamp(segments, 32, 256);
        if (cachedDisc != null && cachedDisc.vertexCount == segments + 1)
            return cachedDisc;

        var mesh = new Mesh { name = $"SmoothDisc_{segments}" };
        var verts = new Vector3[segments + 1];
        var norms = new Vector3[segments + 1];
        var uvs = new Vector2[segments + 1];
        var tris = new int[segments * 3];

        verts[0] = Vector3.zero;
        norms[0] = Vector3.up;
        uvs[0] = new Vector2(0.5f, 0.5f);

        for (int i = 0; i < segments; i++)
        {
            float a = i * Mathf.PI * 2f / segments;
            float x = Mathf.Cos(a) * 0.5f;
            float z = Mathf.Sin(a) * 0.5f;
            verts[i + 1] = new Vector3(x, 0f, z);
            norms[i + 1] = Vector3.up;
            uvs[i + 1] = new Vector2(x + 0.5f, z + 0.5f);
            tris[i * 3] = 0;
            tris[i * 3 + 1] = i + 1;
            tris[i * 3 + 2] = (i + 1) % segments + 1;
        }

        mesh.vertices = verts;
        mesh.normals = norms;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        cachedDisc = mesh;
        return mesh;
    }

    /// <summary>
    /// Кільце (annulus) у XZ: outerR=0.5, inner = 0.5 * innerRatio.
    /// scale.x/z = зовнішній діаметр.
    /// </summary>
    public static Mesh Ring(float innerRatio = 0.92f, int segments = DefaultSeg)
    {
        segments = Mathf.Clamp(segments, 32, 256);
        innerRatio = Mathf.Clamp(innerRatio, 0.05f, 0.98f);
        var mesh = new Mesh { name = $"SmoothRing_{segments}" };
        int vCount = segments * 2;
        var verts = new Vector3[vCount];
        var norms = new Vector3[vCount];
        var uvs = new Vector2[vCount];
        var tris = new int[segments * 6];

        float ri = 0.5f * innerRatio;
        float ro = 0.5f;
        for (int i = 0; i < segments; i++)
        {
            float a = i * Mathf.PI * 2f / segments;
            float c = Mathf.Cos(a);
            float s = Mathf.Sin(a);
            verts[i * 2] = new Vector3(c * ri, 0f, s * ri);
            verts[i * 2 + 1] = new Vector3(c * ro, 0f, s * ro);
            norms[i * 2] = norms[i * 2 + 1] = Vector3.up;
            uvs[i * 2] = new Vector2(c * ri + 0.5f, s * ri + 0.5f);
            uvs[i * 2 + 1] = new Vector2(c * ro + 0.5f, s * ro + 0.5f);

            int i0 = i * 2;
            int i1 = i * 2 + 1;
            int j0 = ((i + 1) % segments) * 2;
            int j1 = j0 + 1;
            int t = i * 6;
            tris[t] = i0; tris[t + 1] = i1; tris[t + 2] = j1;
            tris[t + 3] = i0; tris[t + 4] = j1; tris[t + 5] = j0;
        }

        mesh.vertices = verts;
        mesh.normals = norms;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateBounds();
        return mesh;
    }

    /// <summary>Циліндр радіусом 0.5, висотою 2 (як Unity default).</summary>
    public static Mesh Cylinder(int segments = DefaultSeg)
    {
        segments = Mathf.Clamp(segments, 32, 256);
        if (cachedCylinder != null && cachedCylinder.name == $"SmoothCyl_{segments}")
            return cachedCylinder;

        var mesh = new Mesh { name = $"SmoothCyl_{segments}" };
        int sideV = (segments + 1) * 2;
        int capV = segments + 1;
        var verts = new Vector3[sideV + capV * 2];
        var norms = new Vector3[verts.Length];
        var uvs = new Vector2[verts.Length];

        for (int i = 0; i <= segments; i++)
        {
            float a = i * Mathf.PI * 2f / segments;
            float x = Mathf.Cos(a) * 0.5f;
            float z = Mathf.Sin(a) * 0.5f;
            float u = (float)i / segments;
            verts[i * 2] = new Vector3(x, -1f, z);
            verts[i * 2 + 1] = new Vector3(x, 1f, z);
            Vector3 n = new Vector3(x, 0f, z).normalized;
            norms[i * 2] = norms[i * 2 + 1] = n;
            uvs[i * 2] = new Vector2(u, 0f);
            uvs[i * 2 + 1] = new Vector2(u, 1f);
        }

        int top0 = sideV;
        verts[top0] = new Vector3(0f, 1f, 0f);
        norms[top0] = Vector3.up;
        uvs[top0] = new Vector2(0.5f, 0.5f);
        for (int i = 0; i < segments; i++)
        {
            float a = i * Mathf.PI * 2f / segments;
            verts[top0 + 1 + i] = new Vector3(Mathf.Cos(a) * 0.5f, 1f, Mathf.Sin(a) * 0.5f);
            norms[top0 + 1 + i] = Vector3.up;
            uvs[top0 + 1 + i] = new Vector2(Mathf.Cos(a) * 0.5f + 0.5f, Mathf.Sin(a) * 0.5f + 0.5f);
        }

        int bot0 = sideV + capV;
        verts[bot0] = new Vector3(0f, -1f, 0f);
        norms[bot0] = Vector3.down;
        uvs[bot0] = new Vector2(0.5f, 0.5f);
        for (int i = 0; i < segments; i++)
        {
            float a = i * Mathf.PI * 2f / segments;
            verts[bot0 + 1 + i] = new Vector3(Mathf.Cos(a) * 0.5f, -1f, Mathf.Sin(a) * 0.5f);
            norms[bot0 + 1 + i] = Vector3.down;
            uvs[bot0 + 1 + i] = new Vector2(Mathf.Cos(a) * 0.5f + 0.5f, Mathf.Sin(a) * 0.5f + 0.5f);
        }

        var tris = new System.Collections.Generic.List<int>(segments * 12);
        for (int i = 0; i < segments; i++)
        {
            int b = i * 2;
            int t = b + 1;
            int b2 = (i + 1) * 2;
            int t2 = b2 + 1;
            tris.Add(b); tris.Add(t); tris.Add(t2);
            tris.Add(b); tris.Add(t2); tris.Add(b2);
        }
        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            tris.Add(top0); tris.Add(top0 + 1 + next); tris.Add(top0 + 1 + i);
            tris.Add(bot0); tris.Add(bot0 + 1 + i); tris.Add(bot0 + 1 + next);
        }

        mesh.vertices = verts;
        mesh.normals = norms;
        mesh.uv = uvs;
        mesh.triangles = tris.ToArray();
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        cachedCylinder = mesh;
        return mesh;
    }

    /// <summary>Сфера радіусом 0.5 (scale = діаметр).</summary>
    public static Mesh Sphere(int lat = SphereLat, int lon = SphereLon)
    {
        lat = Mathf.Clamp(lat, 12, 96);
        lon = Mathf.Clamp(lon, 16, 128);
        if (cachedSphere != null && cachedSphere.name == $"SmoothSphere_{lat}x{lon}")
            return cachedSphere;

        var mesh = new Mesh { name = $"SmoothSphere_{lat}x{lon}" };
        int vCount = (lat + 1) * (lon + 1);
        var verts = new Vector3[vCount];
        var norms = new Vector3[vCount];
        var uvs = new Vector2[vCount];

        for (int y = 0; y <= lat; y++)
        {
            float v = y / (float)lat;
            float phi = v * Mathf.PI;
            float sy = Mathf.Cos(phi);
            float r = Mathf.Sin(phi);
            for (int x = 0; x <= lon; x++)
            {
                float u = x / (float)lon;
                float th = u * Mathf.PI * 2f;
                Vector3 p = new Vector3(Mathf.Cos(th) * r, sy, Mathf.Sin(th) * r) * 0.5f;
                int i = y * (lon + 1) + x;
                verts[i] = p;
                norms[i] = p.sqrMagnitude > 1e-12f ? p.normalized : Vector3.up;
                uvs[i] = new Vector2(u, 1f - v);
            }
        }

        var tris = new int[lat * lon * 6];
        int t = 0;
        for (int y = 0; y < lat; y++)
        {
            for (int x = 0; x < lon; x++)
            {
                int i0 = y * (lon + 1) + x;
                int i1 = i0 + 1;
                int i2 = i0 + (lon + 1);
                int i3 = i2 + 1;
                tris[t++] = i0; tris[t++] = i2; tris[t++] = i1;
                tris[t++] = i1; tris[t++] = i2; tris[t++] = i3;
            }
        }

        mesh.vertices = verts;
        mesh.normals = norms;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateBounds();
        cachedSphere = mesh;
        return mesh;
    }

    /// <summary>Капсула: циліндр + півсфери, total height ≈ 2, radius 0.5 (як Unity Capsule).</summary>
    public static Mesh Capsule(int segments = 48, int hemiRings = 12)
    {
        segments = Mathf.Clamp(segments, 24, 96);
        hemiRings = Mathf.Clamp(hemiRings, 6, 24);
        if (cachedCapsule != null && cachedCapsule.name == $"SmoothCap_{segments}_{hemiRings}")
            return cachedCapsule;

        // height 2, radius 0.5 → body height 1 (−0.5..+0.5), hemispheres radius 0.5
        float R = 0.5f;
        float halfBody = 0.5f;

        var verts = new System.Collections.Generic.List<Vector3>();
        var norms = new System.Collections.Generic.List<Vector3>();
        var uvs = new System.Collections.Generic.List<Vector2>();
        var tris = new System.Collections.Generic.List<int>();

        void AddRing(float y, float r, Vector3 nBias, float v)
        {
            for (int i = 0; i <= segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                float x = Mathf.Cos(a) * r;
                float z = Mathf.Sin(a) * r;
                verts.Add(new Vector3(x, y, z));
                Vector3 n = new Vector3(x, 0f, z);
                if (n.sqrMagnitude > 1e-10f) n.Normalize();
                n = (n + nBias).normalized;
                if (n.sqrMagnitude < 1e-10f) n = Vector3.up;
                norms.Add(n);
                uvs.Add(new Vector2(i / (float)segments, v));
            }
        }

        // Bottom hemisphere: south pole → equator at y = -halfBody
        for (int ring = 0; ring <= hemiRings; ring++)
        {
            float t = ring / (float)hemiRings;
            float phi = Mathf.PI - t * (Mathf.PI * 0.5f);
            float sy = Mathf.Cos(phi);
            float rr = Mathf.Sin(phi) * R;
            float y = -halfBody + sy * R;
            AddRing(y, rr, Vector3.up * sy, t * 0.3f);
        }

        // Cylindrical body (skip duplicate bottom equator)
        const int bodySteps = 2;
        for (int b = 1; b <= bodySteps; b++)
        {
            float t = b / (float)bodySteps;
            float y = Mathf.Lerp(-halfBody, halfBody, t);
            AddRing(y, R, Vector3.zero, 0.3f + t * 0.4f);
        }

        // Top hemisphere (skip equator already added as body end)
        for (int ring = 1; ring <= hemiRings; ring++)
        {
            float t = ring / (float)hemiRings;
            float phi = Mathf.PI * 0.5f - t * (Mathf.PI * 0.5f);
            float sy = Mathf.Cos(phi);
            float rr = Mathf.Sin(phi) * R;
            float y = halfBody + sy * R;
            AddRing(y, rr, Vector3.up * sy, 0.7f + t * 0.3f);
        }

        int rings = (hemiRings + 1) + bodySteps + hemiRings;
        int stride = segments + 1;
        for (int r = 0; r < rings - 1; r++)
        {
            for (int i = 0; i < segments; i++)
            {
                int i0 = r * stride + i;
                int i1 = i0 + 1;
                int i2 = i0 + stride;
                int i3 = i2 + 1;
                tris.Add(i0); tris.Add(i2); tris.Add(i1);
                tris.Add(i1); tris.Add(i2); tris.Add(i3);
            }
        }

        var mesh = new Mesh { name = $"SmoothCap_{segments}_{hemiRings}" };
        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        cachedCapsule = mesh;
        return mesh;
    }

    public static GameObject MakeDisc(string name, Transform parent, Vector3 pos, float diameter, float yThickness, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = new Vector3(diameter, 1f, diameter);
        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = Disc(DefaultSeg);
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        if (yThickness > 0.02f)
            go.transform.localPosition = pos + Vector3.up * (yThickness * 0.5f);
        return go;
    }

    public static GameObject MakeRing(string name, Transform parent, Vector3 pos, float outerD, float innerRatio, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = new Vector3(outerD, 1f, outerD);
        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = Ring(innerRatio, DefaultSeg);
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = true;
        return go;
    }

    public static GameObject MakeCylinder(string name, Transform parent, Vector3 pos, float diameter, float halfHeight, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = new Vector3(diameter, halfHeight, diameter);
        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = Cylinder(DefaultSeg);
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = true;
        return go;
    }

    public static GameObject MakeSphere(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = Sphere();
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        mr.receiveShadows = true;
        return go;
    }

    public static GameObject MakeCapsule(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = Capsule();
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        mr.receiveShadows = true;
        return go;
    }

    /// <summary>
    /// Дзвін сопла з криволінійним профілем (кілька кілець) — без «прямого конуса».
    /// height 2 (−1..1), exit r=0.5, throat r≈0.20.
    /// </summary>
    public static Mesh Bell(int segments = 64, int rings = 14)
    {
        segments = Mathf.Clamp(segments, 24, 128);
        rings = Mathf.Clamp(rings, 6, 32);
        var mesh = new Mesh { name = $"SmoothBell_{segments}x{rings}" };

        int stride = segments + 1;
        int vCount = stride * (rings + 1);
        var verts = new Vector3[vCount];
        var norms = new Vector3[vCount];
        var uvs = new Vector2[vCount];

        // Smooth bell radius: t=0 exit (bottom) → t=1 throat (top)
        float RadiusAt(float t)
        {
            t = Mathf.Clamp01(t);
            float exitR = 0.50f;
            float throatR = 0.195f;
            // Flare wider near exit; gentle neck toward throat
            float flare = Mathf.Pow(1f - t, 1.55f);
            return Mathf.Lerp(throatR, exitR, flare);
        }

        for (int r = 0; r <= rings; r++)
        {
            float t = r / (float)rings;          // 0 bottom .. 1 top
            float y = Mathf.Lerp(-1f, 1f, t);
            float rad = RadiusAt(t);
            // d(radius)/d(t): negative (shrinks upward)
            float t0 = Mathf.Max(0f, t - 0.02f);
            float t1 = Mathf.Min(1f, t + 0.02f);
            float drDt = (RadiusAt(t1) - RadiusAt(t0)) / Mathf.Max(1e-4f, t1 - t0);
            // Profile tangent in (radial, y): (drDt, 2) since y spans 2 over t∈[0,1]
            // Outward normal ⊥ tangent: (2, -drDt) in (radial, y)
            float nRad = 2f;
            float nY = -drDt;

            for (int i = 0; i <= segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                float c = Mathf.Cos(a), s = Mathf.Sin(a);
                int idx = r * stride + i;
                verts[idx] = new Vector3(c * rad, y, s * rad);
                Vector3 n = new Vector3(c * nRad, nY, s * nRad);
                if (n.sqrMagnitude > 1e-10f) n.Normalize();
                else n = new Vector3(c, 0f, s);
                norms[idx] = n;
                uvs[idx] = new Vector2(i / (float)segments, t);
            }
        }

        var tris = new int[rings * segments * 6];
        int o = 0;
        for (int r = 0; r < rings; r++)
        {
            for (int i = 0; i < segments; i++)
            {
                int i0 = r * stride + i;
                int i1 = i0 + 1;
                int i2 = i0 + stride;
                int i3 = i2 + 1;
                tris[o++] = i0; tris[o++] = i2; tris[o++] = i1;
                tris[o++] = i1; tris[o++] = i2; tris[o++] = i3;
            }
        }

        mesh.vertices = verts;
        mesh.normals = norms;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        return mesh;
    }

    public static GameObject MakeBell(string name, Transform parent, Vector3 pos, float diameter, float halfHeight, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = new Vector3(diameter, halfHeight, diameter);
        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = Bell(DefaultSeg, 16);
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        mr.receiveShadows = true;
        return go;
    }

    /// <summary>
    /// Усічений конус (frustum): height 2 (−1..1), bottom r=0.5, top r = 0.5 * topRatio.
    /// </summary>
    public static Mesh Frustum(float topRatio = 0.7f, int segments = 96, int rings = 10)
    {
        segments = Mathf.Clamp(segments, 24, 128);
        rings = Mathf.Clamp(rings, 2, 32);
        topRatio = Mathf.Clamp(topRatio, 0.05f, 1f);
        var mesh = new Mesh { name = $"SmoothFrustum_{segments}_{topRatio:F2}" };

        int stride = segments + 1;
        var verts = new Vector3[stride * (rings + 1)];
        var norms = new Vector3[verts.Length];
        var uvs = new Vector2[verts.Length];

        float rBot = 0.5f;
        float rTop = 0.5f * topRatio;
        float dr = rTop - rBot; // over t 0→1
        float nRad = 2f;
        float nY = -dr; // outward normal component

        for (int r = 0; r <= rings; r++)
        {
            float t = r / (float)rings;
            float y = Mathf.Lerp(-1f, 1f, t);
            float rad = Mathf.Lerp(rBot, rTop, t);
            for (int i = 0; i <= segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                float c = Mathf.Cos(a), s = Mathf.Sin(a);
                int idx = r * stride + i;
                verts[idx] = new Vector3(c * rad, y, s * rad);
                Vector3 n = new Vector3(c * nRad, nY, s * nRad);
                if (n.sqrMagnitude > 1e-10f) n.Normalize();
                else n = new Vector3(c, 0f, s);
                norms[idx] = n;
                uvs[idx] = new Vector2(i / (float)segments, t);
            }
        }

        var tris = new int[rings * segments * 6];
        int o = 0;
        for (int r = 0; r < rings; r++)
        for (int i = 0; i < segments; i++)
        {
            int i0 = r * stride + i;
            int i1 = i0 + 1;
            int i2 = i0 + stride;
            int i3 = i2 + 1;
            tris[o++] = i0; tris[o++] = i2; tris[o++] = i1;
            tris[o++] = i1; tris[o++] = i2; tris[o++] = i3;
        }

        mesh.vertices = verts;
        mesh.normals = norms;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        return mesh;
    }

    /// <summary>
    /// Tangent ogive nose: base r=0.5 at y=-1, smooth spherical tip at y=+1.
    /// Single continuous profile (no stacked spheres). tipBlunt = tip radius / base R.
    /// </summary>
    public static Mesh Ogive(float tipBlunt = 0.06f, int segments = 96, int rings = 36)
    {
        segments = Mathf.Clamp(segments, 48, 128);
        rings = Mathf.Clamp(rings, 16, 64);
        tipBlunt = Mathf.Clamp(tipBlunt, 0.02f, 0.14f);
        var mesh = new Mesh { name = $"SmoothOgive_{segments}x{rings}" };

        // Unit: height H=2 (−1..+1), base R=0.5
        const float H = 2f;
        const float R = 0.5f;
        float tipR = R * tipBlunt;
        // Classic tangent-ogive sphere radius for full height, then we cut early for tip sphere
        float rho = (R * R + H * H) / (2f * R);

        // Join ogive → spherical tip where slopes match (approx at tipR radius)
        // x from base: r(x) = sqrt(rho^2 - (H-x)^2) + R - rho
        // Tip sphere center sits on axis so it is tangent to ogive at join.
        float joinR = tipR * 1.15f; // slightly above tip radius on ogive
        float joinX = 0f;
        for (int iter = 0; iter < 24; iter++)
        {
            // binary-ish search x where ogive r ≈ joinR
            float lo = 0f, hi = H * 0.98f;
            for (int k = 0; k < 20; k++)
            {
                float mid = (lo + hi) * 0.5f;
                float under = rho * rho - (H - mid) * (H - mid);
                float rr = under > 0f ? Mathf.Sqrt(under) + R - rho : 0f;
                if (rr > joinR) lo = mid; else hi = mid;
            }
            joinX = (lo + hi) * 0.5f;
        }
        joinX = Mathf.Clamp(joinX, H * 0.55f, H * 0.92f);
        float underJ = rho * rho - (H - joinX) * (H - joinX);
        float rJoin = underJ > 0f ? Mathf.Sqrt(underJ) + R - rho : joinR;
        // Spherical tip center: on axis, radius tipR, passes through (rJoin, joinX) approx
        // (rJoin)^2 + (joinX - cY_from_base)^2 = tipR^2  → place center so apex is at H
        float tipCenterFromBase = H - tipR; // apex at H
        // Pull join to lie on that sphere if needed
        float maxROnSphere = Mathf.Sqrt(Mathf.Max(0f, tipR * tipR - (joinX - tipCenterFromBase) * (joinX - tipCenterFromBase)));
        if (maxROnSphere > 1e-4f && rJoin > maxROnSphere)
            rJoin = maxROnSphere;

        float RadiusAt(float t)
        {
            t = Mathf.Clamp01(t);
            float x = t * H; // from base
            if (x <= joinX)
            {
                float under = rho * rho - (H - x) * (H - x);
                float r = under > 0f ? Mathf.Sqrt(under) + R - rho : 0f;
                // smooth blend into sphere near join
                float blendStart = joinX * 0.88f;
                if (x > blendStart)
                {
                    float u = (x - blendStart) / Mathf.Max(1e-4f, joinX - blendStart);
                    u = u * u * (3f - 2f * u);
                    float dx = x - tipCenterFromBase;
                    float sr = Mathf.Sqrt(Mathf.Max(0f, tipR * tipR - dx * dx));
                    r = Mathf.Lerp(r, sr, u);
                }
                return Mathf.Max(0.001f, r);
            }
            // Spherical tip
            float d = x - tipCenterFromBase;
            if (d >= tipR) return 0.001f;
            return Mathf.Max(0.001f, Mathf.Sqrt(Mathf.Max(0f, tipR * tipR - d * d)));
        }

        int stride = segments + 1;
        int ringCount = rings + 1;
        var verts = new Vector3[stride * ringCount + 1]; // + pole
        var norms = new Vector3[verts.Length];
        var uvs = new Vector2[verts.Length];
        int pole = stride * ringCount;

        for (int r = 0; r <= rings; r++)
        {
            float t = r / (float)rings;
            float y = Mathf.Lerp(-1f, 1f, t);
            float rad = RadiusAt(t);
            float t0 = Mathf.Max(0f, t - 0.01f);
            float t1 = Mathf.Min(1f, t + 0.01f);
            float drDt = (RadiusAt(t1) - RadiusAt(t0)) / Mathf.Max(1e-4f, t1 - t0);
            float nRad = 2f;
            float nY = -drDt;

            for (int i = 0; i <= segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                float c = Mathf.Cos(a), s = Mathf.Sin(a);
                int idx = r * stride + i;
                verts[idx] = new Vector3(c * rad, y, s * rad);
                Vector3 n = new Vector3(c * nRad, nY, s * nRad);
                if (n.sqrMagnitude > 1e-10f) n.Normalize();
                else n = new Vector3(c, 0f, s);
                norms[idx] = n;
                uvs[idx] = new Vector2(i / (float)segments, t);
            }
        }

        // True pole (sharp-free tip)
        verts[pole] = new Vector3(0f, 1f, 0f);
        norms[pole] = Vector3.up;
        uvs[pole] = new Vector2(0.5f, 1f);

        var tris = new List<int>(rings * segments * 6 + segments * 3);
        for (int r = 0; r < rings; r++)
        for (int i = 0; i < segments; i++)
        {
            int i0 = r * stride + i;
            int i1 = i0 + 1;
            int i2 = i0 + stride;
            int i3 = i2 + 1;
            tris.Add(i0); tris.Add(i2); tris.Add(i1);
            tris.Add(i1); tris.Add(i2); tris.Add(i3);
        }
        // Cap last ring → pole (last ring is already at tip; fan improves tip)
        int last = rings * stride;
        for (int i = 0; i < segments; i++)
        {
            tris.Add(last + i);
            tris.Add(pole);
            tris.Add(last + i + 1);
        }

        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        return mesh;
    }

    /// <summary>
    /// Frustum GO: diameter = base diameter, topRatio = top/base, halfHeight = half height.
    /// </summary>
    public static GameObject MakeFrustum(string name, Transform parent, Vector3 pos,
        float baseDiameter, float halfHeight, float topRatio, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = new Vector3(baseDiameter, halfHeight, baseDiameter);
        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = Frustum(topRatio, DefaultSeg, 12);
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        mr.receiveShadows = true;
        return go;
    }

    /// <summary>
    /// Ogive GO: diameter = base diameter, halfHeight = half height of ogive.
    /// </summary>
    public static GameObject MakeOgive(string name, Transform parent, Vector3 pos,
        float baseDiameter, float halfHeight, Material mat, float tipBlunt = 0.06f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = new Vector3(baseDiameter, halfHeight, baseDiameter);
        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = Ogive(tipBlunt, DefaultSeg, 40);
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        mr.receiveShadows = true;
        return go;
    }
}
