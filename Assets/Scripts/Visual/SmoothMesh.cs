using UnityEngine;

/// <summary>
/// Високополігональні круглі меші (Unity Cylinder ≈ 20 граней — виглядає «кутастим»).
/// </summary>
public static class SmoothMesh
{
    static Mesh cachedDisc;
    static Mesh cachedRing;
    static Mesh cachedCylinder;

    /// <summary>Плоский диск у площині XZ, нормаль +Y, радіус 0.5 (scale.x/z = діаметр).</summary>
    public static Mesh Disc(int segments = 96)
    {
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
    public static Mesh Ring(float innerRatio = 0.92f, int segments = 96)
    {
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

    /// <summary>Циліндр радіусом 0.5, висотою 2 (як Unity default), segments граней.</summary>
    public static Mesh Cylinder(int segments = 64)
    {
        if (cachedCylinder != null && cachedCylinder.name == $"SmoothCyl_{segments}")
            return cachedCylinder;

        var mesh = new Mesh { name = $"SmoothCyl_{segments}" };
        // side + top + bottom
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

    public static GameObject MakeDisc(string name, Transform parent, Vector3 pos, float diameter, float yThickness, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = new Vector3(diameter, 1f, diameter);
        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = Disc(96);
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        // тонка «товщина» через другий диск знизу — візуально плоско
        if (yThickness > 0.02f)
        {
            go.transform.localPosition = pos + Vector3.up * (yThickness * 0.5f);
        }
        return go;
    }

    public static GameObject MakeRing(string name, Transform parent, Vector3 pos, float outerD, float innerRatio, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = new Vector3(outerD, 1f, outerD);
        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = Ring(innerRatio, 96);
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
        mf.sharedMesh = Cylinder(64);
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = true;
        return go;
    }
}
