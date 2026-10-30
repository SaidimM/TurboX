using UnityEngine;
using System.Collections.Generic;

[DefaultExecutionOrder(-100)]
public class TurboWorld : MonoBehaviour
{
    Transform scenery;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureWorld()
    {
        if (FindObjectOfType<TurboWorld>() == null)
            new GameObject("TurboX environment").AddComponent<TurboWorld>();
    }

    void Awake()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            var obj = new GameObject("Main Camera");
            obj.tag = "MainCamera";
            cam = obj.AddComponent<Camera>();
            obj.AddComponent<AudioListener>();
        }
        cam.orthographic = false;
        cam.fieldOfView = 64f;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 1200f;
        cam.clearFlags = CameraClearFlags.SolidColor;

        if (FindObjectOfType<ProceduralTrackGenerator>() == null)
            new GameObject("Track").AddComponent<ProceduralTrackGenerator>();
        if (FindObjectOfType<RaceManager>() == null)
            new GameObject("RaceManager").AddComponent<RaceManager>();
        if (FindObjectOfType<GameManager>() == null)
            new GameObject("HUD").AddComponent<GameManager>();

        ApplyCircuit();
    }

    public void ApplyCircuit()
    {
        TurboArt.ClearGenerated(scenery);
        scenery = new GameObject("LandscapeArtwork").transform;
        scenery.SetParent(transform, false);

        TurboCircuit c = TurboLevels.Current;
        if (Camera.main != null)
            Camera.main.backgroundColor = TurboArt.Hex(c.SkyHorizon);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = TurboArt.Hex(c.SkyHorizon);
        RenderSettings.fogStartDistance = 120f;
        RenderSettings.fogEndDistance = 620f;

        BuildMountains(c);
    }

    void BuildMountains(TurboCircuit c)
    {
        var verts = new List<Vector3>();
        var cols = new List<Color>();
        var tris = new List<int>();
        Color farCol = TurboArt.Hex(c.MountainFar);
        Color nearCol = TurboArt.Hex(c.MountainNear);
        Color baseCol = TurboArt.Ink;

        Vector3 center = new Vector3(130f, -38f, 90f);
        int peaks = 28;
        for (int i = 0; i < peaks; i++)
        {
            float ang = (i / (float)peaks) * Mathf.PI * 2f;
            float dist = 340f + (i % 3) * 55f;
            float h = 55f + Mathf.PerlinNoise(i * 0.7f + TurboLevels.CurrentCircuitIndex * 7f, 2.3f) * 85f;
            float w = 65f + (i % 4) * 18f;
            Vector3 peak = center + new Vector3(Mathf.Cos(ang) * dist, h, Mathf.Sin(ang) * dist);
            Vector3 pL = center + new Vector3(Mathf.Cos(ang - 0.18f) * (dist - w), -30f, Mathf.Sin(ang - 0.18f) * (dist - w));
            Vector3 pR = center + new Vector3(Mathf.Cos(ang + 0.18f) * (dist - w), -30f, Mathf.Sin(ang + 0.18f) * (dist - w));
            Vector3 pF = center + new Vector3(Mathf.Cos(ang) * (dist - w * 1.2f), -30f, Mathf.Sin(ang) * (dist - w * 1.2f));
            Color topC = (i % 2 == 0) ? farCol : nearCol;
            TurboArt.AddTri(verts, cols, tris, peak, pL, pF, topC);
            TurboArt.AddTri(verts, cols, tris, peak, pF, pR, baseCol);
        }
        TurboArt.CreateMeshObject(scenery, "CanyonPeaks", verts, cols, tris);
    }
}
