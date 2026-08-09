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

    /// <summary>Дзвін сопла (усічений конус) — tipR/baseR у частках 0.5 scale.</summary>
    public static Mesh Bell(int segments = 64)
    {
        segments = Mathf.Clamp(segments, 24, 128);
        // height 2 (-1..1), bottom radius 0.5, top radius 0.22
        var mesh = new Mesh { name = $"SmoothBell_{segments}" };
        float rBot = 0.5f;
        float rTop = 0.22f;
        int sideV = (segments + 1) * 2;
        var verts = new Vector3[sideV];
        var norms = new Vector3[sideV];
        var uvs = new Vector2[sideV];

        Vector3 slope = new Vector3(rBot - rTop, 2f, 0f).normalized;
        // outward normal tilted
        for (int i = 0; i <= segments; i++)
        {
            float a = i * Mathf.PI * 2f / segments;
            float c = Mathf.Cos(a), s = Mathf.Sin(a);
            verts[i * 2] = new Vector3(c * rBot, -1f, s * rBot);
            verts[i * 2 + 1] = new Vector3(c * rTop, 1f, s * rTop);
            Vector3 n = new Vector3(c * slope.y, (rBot - rTop) * 0.5f, s * slope.y).normalized;
            norms[i * 2] = norms[i * 2 + 1] = n;
            float u = i / (float)segments;
            uvs[i * 2] = new Vector2(u, 0f);
            uvs[i * 2 + 1] = new Vector2(u, 1f);
        }

        var tris = new int[segments * 6];
        for (int i = 0; i < segments; i++)
        {
            int b = i * 2, t = b + 1, b2 = (i + 1) * 2, t2 = b2 + 1;
            int o = i * 6;
            tris[o] = b; tris[o + 1] = t; tris[o + 2] = t2;
            tris[o + 3] = b; tris[o + 4] = t2; tris[o + 5] = b2;
        }

        mesh.vertices = verts;
        mesh.normals = norms;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateBounds();
        return mesh;
    }

    public static GameObject MakeBell(string name, Transform parent, Vector3 pos, float diameter, float halfHeight, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = new Vector3(diameter, halfHeight, diameter);
        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = Bell(DefaultSeg);
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        mr.receiveShadows = true;
        return go;
    }
}
