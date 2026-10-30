using UnityEngine;
using System.Collections.Generic;

public static class TurboArt
{
    static Material material;
    public static readonly Color Ink = Hex(0x162226);
    public static readonly Color Sand = Hex(0xDBA078);
    public static readonly Color Cream = Hex(0xFFF0D1);
    public static readonly Color Accent = Hex(0xE77D51);
    public static readonly Color Cyan = Hex(0x38BDF8);
    static readonly Vector3 LightDir = new Vector3(-0.42f, 0.78f, -0.45f).normalized;

    public static Color Hex(uint rgb, float alpha = 1f) =>
        new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, alpha);

    public static Color ShadeFacet(Color baseCol, Vector3 a, Vector3 b, Vector3 c, float specular = 0.5f)
    {
        Vector3 n = Vector3.Cross(b - a, c - a).normalized;
        float ndl = Vector3.Dot(n, LightDir);
        float light = Mathf.Clamp(0.62f + 0.44f * ndl + 0.12f * n.y, 0.35f, 1.22f);
        float spec = specular > 0f ? Mathf.Pow(Mathf.Max(0f, ndl), 8f) * specular * 0.28f : 0f;
        return new Color(
            Mathf.Clamp01(baseCol.r * light + spec),
            Mathf.Clamp01(baseCol.g * light + spec),
            Mathf.Clamp01(baseCol.b * light + spec),
            baseCol.a
        );
    }

    public static Material Material
    {
        get
        {
            if (material == null)
            {
                Shader sh = Shader.Find("Sprites/Default");
                if (sh == null) sh = Shader.Find("Unlit/Color");
                material = new Material(sh);
            }
            return material;
        }
    }

    public static void ClearGenerated(Transform root)
    {
        if (root == null) return;
        root.gameObject.SetActive(false);
        foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
            if (filter.sharedMesh != null) Object.Destroy(filter.sharedMesh);
        Object.Destroy(root.gameObject);
    }

    public static void AddQuad(List<Vector3> verts, List<Color> cols, List<int> tris,
        Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color colA, Color colB, Color colC, Color colD)
    {
        int i = verts.Count;
        verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
        cols.Add(colA); cols.Add(colB); cols.Add(colC); cols.Add(colD);
        tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
        tris.Add(i); tris.Add(i + 2); tris.Add(i + 3);
    }

    public static void AddTri(List<Vector3> verts, List<Color> cols, List<int> tris,
        Vector3 a, Vector3 b, Vector3 c, Color col)
    {
        int i = verts.Count;
        verts.Add(a); verts.Add(b); verts.Add(c);
        cols.Add(col); cols.Add(col); cols.Add(col);
        tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
    }

    public static void AddLitTri(List<Vector3> verts, List<Color> cols, List<int> tris,
        Vector3 a, Vector3 b, Vector3 c, Color baseCol, float specular = 0.6f)
    {
        AddTri(verts, cols, tris, a, b, c, ShadeFacet(baseCol, a, b, c, specular));
    }

    public static GameObject CreateMeshObject(Transform parent, string name, List<Vector3> verts, List<Color> cols, List<int> tris)
    {
        var obj = new GameObject(name);
        if (parent != null) obj.transform.SetParent(parent, false);
        var mesh = new Mesh { name = name };
        if (verts.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(verts);
        mesh.SetColors(cols);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        obj.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = obj.AddComponent<MeshRenderer>();
        mr.sharedMaterial = Material;
        return obj;
    }

    public static Transform CreateShipVisual(Transform parent, TurboShipSpec spec)
    {
        var verts = new List<Vector3>();
        var cols = new List<Color>();
        var tris = new List<int>();

        Color primary = Hex(spec.HullPrimary);
        Color secondary = Hex(spec.HullSecondary);
        Color accent = Hex(spec.Accent);
        Color canopy = Hex(0x0F172A);
        Color glow = Hex(0x7DD3FC);

        int shape = spec.Id == "juggernaut" ? 1 : (spec.Id == "neon-flash" ? 2 : 0);

        Vector3 nose = new Vector3(0f, 0.06f, shape == 2 ? 3.15f : 2.75f);
        Vector3 canopyFront = new Vector3(0f, 0.38f, 0.90f);
        Vector3 canopyPeak = new Vector3(0f, 0.58f, -0.32f);
        Vector3 sternTop = new Vector3(0f, 0.34f, -1.95f);
        Vector3 sternBottom = new Vector3(0f, -0.18f, -2.05f);
        Vector3 chineL = new Vector3(-0.70f, 0.15f, 0.58f);
        Vector3 chineR = new Vector3(0.70f, 0.15f, 0.58f);
        Vector3 wingTipL = new Vector3(shape == 1 ? -1.95f : -1.78f, shape == 2 ? 0.12f : 0.02f, shape == 2 ? -0.95f : -1.48f);
        Vector3 wingTipR = new Vector3(shape == 1 ? 1.95f : 1.78f, shape == 2 ? 0.12f : 0.02f, shape == 2 ? -0.95f : -1.48f);
        Vector3 wingRearL = new Vector3(-0.98f, 0.14f, -1.98f);
        Vector3 wingRearR = new Vector3(0.98f, 0.14f, -1.98f);
        Vector3 finTopL = new Vector3(-1.55f, 0.58f, -1.82f);
        Vector3 finTopR = new Vector3(1.55f, 0.58f, -1.82f);

        AddLitTri(verts, cols, tris, nose, chineL, canopyFront, primary, 0.7f);
        AddLitTri(verts, cols, tris, nose, canopyFront, chineR, primary, 0.7f);

        AddLitTri(verts, cols, tris, canopyFront, chineL, canopyPeak, canopy, 1.1f);
        AddLitTri(verts, cols, tris, canopyFront, canopyPeak, chineR, canopy, 1.1f);
        AddLitTri(verts, cols, tris, canopyPeak, chineL, sternTop, secondary, 0.4f);
        AddLitTri(verts, cols, tris, canopyPeak, sternTop, chineR, secondary, 0.4f);

        AddLitTri(verts, cols, tris, nose, wingTipL, chineL, accent, 0.8f);
        AddLitTri(verts, cols, tris, nose, chineR, wingTipR, accent, 0.8f);
        AddLitTri(verts, cols, tris, chineL, wingTipL, wingRearL, primary, 0.6f);
        AddLitTri(verts, cols, tris, chineR, wingRearR, wingTipR, primary, 0.6f);
        AddLitTri(verts, cols, tris, chineL, wingRearL, sternTop, secondary, 0.5f);
        AddLitTri(verts, cols, tris, chineR, sternTop, wingRearR, secondary, 0.5f);

        AddLitTri(verts, cols, tris, wingTipL, finTopL, wingRearL, accent, 0.9f);
        AddLitTri(verts, cols, tris, wingTipL, wingRearL, finTopL, accent, 0.9f);
        AddLitTri(verts, cols, tris, wingTipR, wingRearR, finTopR, accent, 0.9f);
        AddLitTri(verts, cols, tris, wingTipR, finTopR, wingRearR, accent, 0.9f);

        if (shape == 1)
        {
            Vector3 sponsonL = new Vector3(-1.12f, 0.08f, 2.25f);
            Vector3 sponsonR = new Vector3(1.12f, 0.08f, 2.25f);
            AddLitTri(verts, cols, tris, sponsonL, wingTipL, chineL, primary, 0.6f);
            AddLitTri(verts, cols, tris, sponsonR, chineR, wingTipR, primary, 0.6f);
        }
        else if (shape == 2)
        {
            Vector3 dorsalTop = new Vector3(0f, 0.88f, -1.55f);
            AddLitTri(verts, cols, tris, canopyPeak, dorsalTop, sternTop, accent, 1.0f);
            AddLitTri(verts, cols, tris, canopyPeak, sternTop, dorsalTop, accent, 1.0f);
        }

        AddLitTri(verts, cols, tris, nose, sternBottom, wingTipL, secondary, 0.2f);
        AddLitTri(verts, cols, tris, nose, wingTipR, sternBottom, secondary, 0.2f);
        AddQuad(verts, cols, tris,
            new Vector3(-0.6f, 0.24f, -1.98f), new Vector3(0.6f, 0.24f, -1.98f),
            new Vector3(0.45f, -0.10f, -2.02f), new Vector3(-0.45f, -0.10f, -2.02f),
            glow, glow, accent, accent);

        return CreateMeshObject(parent, "HovercraftVisual", verts, cols, tris).transform;
    }
}
