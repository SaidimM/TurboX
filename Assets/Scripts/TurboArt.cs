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

    public static Color Hex(uint rgb, float alpha = 1f) =>
        new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, alpha);

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

    // Builds a sleek low-poly 3D anti-gravity delta hovercraft mesh
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

        Vector3 nose = new Vector3(0f, 0.05f, 2.75f);
        Vector3 canopyFront = new Vector3(0f, 0.36f, 0.85f);
        Vector3 canopyPeak = new Vector3(0f, 0.56f, -0.35f);
        Vector3 sternTop = new Vector3(0f, 0.32f, -1.95f);
        Vector3 sternBottom = new Vector3(0f, -0.18f, -2.05f);
        Vector3 chineL = new Vector3(-0.68f, 0.14f, 0.55f);
        Vector3 chineR = new Vector3(0.68f, 0.14f, 0.55f);
        Vector3 wingTipL = new Vector3(-1.75f, 0.02f, -1.45f);
        Vector3 wingTipR = new Vector3(1.75f, 0.02f, -1.45f);
        Vector3 wingRearL = new Vector3(-0.95f, 0.12f, -1.95f);
        Vector3 wingRearR = new Vector3(0.95f, 0.12f, -1.95f);
        Vector3 finTopL = new Vector3(-1.55f, 0.52f, -1.78f);
        Vector3 finTopR = new Vector3(1.55f, 0.52f, -1.78f);

        // Forward deck facets
        AddTri(verts, cols, tris, nose, chineL, canopyFront, primary);
        AddTri(verts, cols, tris, nose, canopyFront, chineR, primary);

        // Canopy glass
        AddTri(verts, cols, tris, canopyFront, chineL, canopyPeak, canopy);
        AddTri(verts, cols, tris, canopyFront, canopyPeak, chineR, canopy);
        AddTri(verts, cols, tris, canopyPeak, chineL, sternTop, secondary);
        AddTri(verts, cols, tris, canopyPeak, sternTop, chineR, secondary);

        // Main delta wings
        AddTri(verts, cols, tris, nose, wingTipL, chineL, accent);
        AddTri(verts, cols, tris, nose, chineR, wingTipR, accent);
        AddTri(verts, cols, tris, chineL, wingTipL, wingRearL, primary);
        AddTri(verts, cols, tris, chineR, wingRearR, wingTipR, primary);
        AddTri(verts, cols, tris, chineL, wingRearL, sternTop, secondary);
        AddTri(verts, cols, tris, chineR, sternTop, wingRearR, secondary);

        // Twin vertical stabilizer fins
        AddTri(verts, cols, tris, wingTipL, finTopL, wingRearL, accent);
        AddTri(verts, cols, tris, wingTipL, wingRearL, finTopL, accent);
        AddTri(verts, cols, tris, wingTipR, wingRearR, finTopR, accent);
        AddTri(verts, cols, tris, wingTipR, finTopR, wingRearR, accent);

        // Belly & glowing thruster exhaust
        AddTri(verts, cols, tris, nose, sternBottom, wingTipL, secondary);
        AddTri(verts, cols, tris, nose, wingTipR, sternBottom, secondary);
        AddQuad(verts, cols, tris,
            new Vector3(-0.6f, 0.24f, -1.98f), new Vector3(0.6f, 0.24f, -1.98f),
            new Vector3(0.45f, -0.10f, -2.02f), new Vector3(-0.45f, -0.10f, -2.02f),
            glow, glow, accent, accent);

        return CreateMeshObject(parent, "HovercraftVisual", verts, cols, tris).transform;
    }
}
