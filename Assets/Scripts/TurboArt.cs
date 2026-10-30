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
        if (n.y < 0f) n = -n;
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

    public static void AddLitQuad(List<Vector3> verts, List<Color> cols, List<int> tris,
        Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color baseCol, float specular = 0.6f)
    {
        Color shaded = ShadeFacet(baseCol, a, b, c, specular);
        AddQuad(verts, cols, tris, a, b, c, d, shaded, shaded, shaded, shaded);
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

        int shape = spec.Id == "juggernaut" ? 1 : (spec.Id == "neon-flash" ? 2 : 0);
        Color primary = Hex(spec.HullPrimary);
        Color secondary = Hex(shape == 1 ? 0x44403Cu : (shape == 2 ? 0x312E81u : 0x334155u));
        Color accent = Hex(spec.Accent);
        Color trim = Hex(shape == 1 ? 0xFFF0D1u : (shape == 2 ? 0x38BDF8u : 0xE77D51u));
        Color canopy = Hex(0x0EA5E9);
        Color nozzleRim = Hex(0x0F172A);

        Vector3 nose = new Vector3(0f, 0.08f, shape == 2 ? 3.35f : 2.95f);
        Vector3 foreL = new Vector3(-0.42f, 0.18f, 1.45f);
        Vector3 foreR = new Vector3(0.42f, 0.18f, 1.45f);
        Vector3 chineL = new Vector3(-0.74f, 0.16f, 0.45f);
        Vector3 chineR = new Vector3(0.74f, 0.16f, 0.45f);

        Vector3 canopyF = new Vector3(0f, 0.34f, 1.35f);
        Vector3 canopyPeak = new Vector3(0f, 0.64f, 0.12f);
        Vector3 canopySideL = new Vector3(-0.34f, 0.42f, 0.05f);
        Vector3 canopySideR = new Vector3(0.34f, 0.42f, 0.05f);
        Vector3 canopyRear = new Vector3(0f, 0.54f, -0.78f);

        Vector3 spineMidL = new Vector3(-0.24f, 0.46f, -1.15f);
        Vector3 spineMidR = new Vector3(0.24f, 0.46f, -1.15f);
        Vector3 spineTail = new Vector3(0f, 0.36f, -2.08f);

        Vector3 nacInL = new Vector3(-0.24f, 0.40f, -1.92f);
        Vector3 nacOutL = new Vector3(-0.78f, 0.34f, -1.92f);
        Vector3 nacTopL = new Vector3(-0.50f, 0.48f, -1.88f);
        Vector3 nacFrontL = new Vector3(-0.58f, 0.38f, -0.55f);

        Vector3 nacInR = new Vector3(0.24f, 0.40f, -1.92f);
        Vector3 nacOutR = new Vector3(0.78f, 0.34f, -1.92f);
        Vector3 nacTopR = new Vector3(0.50f, 0.48f, -1.88f);
        Vector3 nacFrontR = new Vector3(0.58f, 0.38f, -0.55f);

        float wingSpan = shape == 1 ? 2.12f : (shape == 2 ? 1.98f : 1.92f);
        float wingTipZ = shape == 2 ? -0.78f : -1.42f;
        Vector3 wingLeadL = new Vector3(-1.28f, 0.12f, -0.15f);
        Vector3 wingLeadR = new Vector3(1.28f, 0.12f, -0.15f);
        Vector3 wingTipFL = new Vector3(-wingSpan, shape == 2 ? 0.18f : 0.06f, wingTipZ + 0.38f);
        Vector3 wingTipBL = new Vector3(-wingSpan * 0.96f, shape == 2 ? 0.18f : 0.06f, wingTipZ - 0.32f);
        Vector3 wingTipFR = new Vector3(wingSpan, shape == 2 ? 0.18f : 0.06f, wingTipZ + 0.38f);
        Vector3 wingTipBR = new Vector3(wingSpan * 0.96f, shape == 2 ? 0.18f : 0.06f, wingTipZ - 0.32f);
        Vector3 wingRearL = new Vector3(-1.18f, 0.18f, -1.86f);
        Vector3 wingRearR = new Vector3(1.18f, 0.18f, -1.86f);

        // 1. Nose & forebody
        AddLitTri(verts, cols, tris, nose, foreL, canopyF, primary, 0.75f);
        AddLitTri(verts, cols, tris, nose, canopyF, foreR, primary, 0.75f);
        AddLitQuad(verts, cols, tris, foreL, chineL, canopySideL, canopyF, primary, 0.65f);
        AddLitQuad(verts, cols, tris, foreR, canopyF, canopySideR, chineR, primary, 0.65f);

        // 2. Bubble cockpit canopy
        AddLitTri(verts, cols, tris, canopyF, canopySideL, canopyPeak, canopy, 1.2f);
        AddLitTri(verts, cols, tris, canopyF, canopyPeak, canopySideR, canopy, 1.2f);
        AddLitTri(verts, cols, tris, canopyPeak, canopySideL, canopyRear, canopy, 1.1f);
        AddLitTri(verts, cols, tris, canopyPeak, canopyRear, canopySideR, canopy, 1.1f);

        // 3. Sculpted central dorsal spine & twin engine nacelles
        AddLitTri(verts, cols, tris, canopyRear, canopySideL, spineMidL, primary, 0.85f);
        AddLitTri(verts, cols, tris, canopyRear, spineMidR, canopySideR, primary, 0.85f);
        AddLitQuad(verts, cols, tris, canopyRear, spineMidL, spineTail, spineMidR, accent, 0.95f);

        AddLitQuad(verts, cols, tris, canopySideL, nacFrontL, nacTopL, spineMidL, primary, 0.78f);
        AddLitQuad(verts, cols, tris, canopySideR, spineMidR, nacTopR, nacFrontR, primary, 0.78f);
        AddLitQuad(verts, cols, tris, nacFrontL, chineL, nacOutL, nacTopL, secondary, 0.55f);
        AddLitQuad(verts, cols, tris, nacFrontR, nacTopR, nacOutR, chineR, secondary, 0.55f);
        AddLitQuad(verts, cols, tris, spineMidL, nacTopL, nacInL, spineTail, secondary, 0.65f);
        AddLitQuad(verts, cols, tris, spineMidR, spineTail, nacInR, nacTopR, secondary, 0.65f);

        // 4. Swept multi-panel wings & diagonal livery stripes
        AddLitQuad(verts, cols, tris, chineL, wingLeadL, wingRearL, nacOutL, primary, 0.72f);
        AddLitQuad(verts, cols, tris, chineR, nacOutR, wingRearR, wingLeadR, primary, 0.72f);

        AddLitQuad(verts, cols, tris,
            new Vector3(-1.12f, 0.15f, 0.02f), new Vector3(-1.56f, 0.12f, wingTipZ + 0.42f),
            new Vector3(-1.52f, 0.13f, wingTipZ - 0.12f), new Vector3(-1.08f, 0.18f, -1.45f),
            accent, 0.9f);
        AddLitQuad(verts, cols, tris,
            new Vector3(1.12f, 0.15f, 0.02f), new Vector3(1.08f, 0.18f, -1.45f),
            new Vector3(1.52f, 0.13f, wingTipZ - 0.12f), new Vector3(1.56f, 0.12f, wingTipZ + 0.42f),
            accent, 0.9f);

        AddLitQuad(verts, cols, tris, wingLeadL, wingTipFL, wingTipBL, wingRearL, secondary, 0.65f);
        AddLitQuad(verts, cols, tris, wingLeadR, wingRearR, wingTipBR, wingTipFR, secondary, 0.65f);

        // Trailing-edge flaps
        AddLitQuad(verts, cols, tris, nacOutL, wingRearL, new Vector3(-1.24f, 0.18f, -1.96f), new Vector3(-0.82f, 0.20f, -1.98f), trim, 0.85f);
        AddLitQuad(verts, cols, tris, nacOutR, new Vector3(0.82f, 0.20f, -1.98f), new Vector3(1.24f, 0.18f, -1.96f), wingRearR, trim, 0.85f);

        // 5. Canted twin vertical tail fins
        AddLitQuad(verts, cols, tris,
            new Vector3(-1.16f, 0.20f, -1.05f), new Vector3(-1.34f, 0.68f, -1.42f),
            new Vector3(-1.30f, 0.64f, -1.96f), new Vector3(-1.12f, 0.22f, -1.88f),
            primary, 0.85f);
        AddLitQuad(verts, cols, tris,
            new Vector3(1.16f, 0.20f, -1.05f), new Vector3(1.12f, 0.22f, -1.88f),
            new Vector3(1.30f, 0.64f, -1.96f), new Vector3(1.34f, 0.68f, -1.42f),
            primary, 0.85f);

        if (shape == 1)
        {
            Vector3 spTipL = new Vector3(-1.05f, 0.12f, 2.35f);
            Vector3 spOutL = new Vector3(-1.42f, 0.14f, 0.65f);
            Vector3 spTipR = new Vector3(1.05f, 0.12f, 2.35f);
            Vector3 spOutR = new Vector3(1.42f, 0.14f, 0.65f);
            AddLitQuad(verts, cols, tris, spTipL, spOutL, wingLeadL, chineL, primary, 0.75f);
            AddLitQuad(verts, cols, tris, spTipR, chineR, wingLeadR, spOutR, primary, 0.75f);
        }
        else if (shape == 2)
        {
            Vector3 dorsalF = new Vector3(0f, 0.56f, -0.65f);
            Vector3 dorsalTop = new Vector3(0f, 0.96f, -1.58f);
            Vector3 dorsalB = new Vector3(0f, 0.44f, -2.06f);
            AddLitTri(verts, cols, tris, dorsalF, dorsalTop, dorsalB, accent, 1.0f);
            AddLitTri(verts, cols, tris, dorsalF, dorsalB, dorsalTop, accent, 1.0f);
        }

        // 6. Twin recessed engine nozzles & subtle compact exhaust core
        foreach (float side in new[] { -1f, 1f })
        {
            float nx = side * 0.50f;
            Vector3 nTop = new Vector3(nx, 0.44f, -1.92f);
            Vector3 nOut = new Vector3(nx + side * 0.24f, 0.28f, -1.92f);
            Vector3 nBot = new Vector3(nx, 0.14f, -1.94f);
            Vector3 nIn = new Vector3(nx - side * 0.22f, 0.28f, -1.92f);
            AddQuad(verts, cols, tris, nTop, nOut, nBot, nIn, nozzleRim, nozzleRim, nozzleRim, nozzleRim);

            Vector3 cTop = new Vector3(nx, 0.38f, -1.94f);
            Vector3 cOut = new Vector3(nx + side * 0.14f, 0.28f, -1.94f);
            Vector3 cBot = new Vector3(nx, 0.18f, -1.94f);
            Vector3 cIn = new Vector3(nx - side * 0.14f, 0.28f, -1.94f);
            Vector3 tipCore = new Vector3(nx, 0.28f, -2.32f);
            AddQuad(verts, cols, tris, cTop, cOut, cBot, cIn, Cyan, Cyan, Accent, Accent);
            AddTri(verts, cols, tris, cOut, cIn, tipCore, new Color(Accent.r, Accent.g, Accent.b, 0.55f));
            AddTri(verts, cols, tris, cTop, cBot, tipCore, new Color(Cream.r, Cream.g, Cream.b, 0.82f));
        }

        return CreateMeshObject(parent, "HovercraftVisual", verts, cols, tris).transform;
    }
}
