using UnityEngine;
using System.Collections.Generic;

public class ProceduralTrackGenerator : MonoBehaviour
{
    public GameObject waypointPrefab;
    Transform artwork;
    readonly List<Transform> waypoints = new List<Transform>();

    void Start() { Rebuild(); }

    public void Rebuild()
    {
        TurboArt.ClearGenerated(artwork);
        waypoints.Clear();
        artwork = new GameObject("CircuitArtwork").transform;
        artwork.SetParent(transform, false);

        TurboCircuit circuit = TurboLevels.Current;
        int steps = 220;
        float halfW = circuit.TrackWidth * 0.5f;
        float curbW = 0.75f;
        float curbH = 0.65f;
        float deckThickness = 0.85f;

        var verts = new List<Vector3>();
        var cols = new List<Color>();
        var tris = new List<int>();

        Color roadCenter = TurboArt.Hex(circuit.RoadSurface);
        Color roadEdge = TurboArt.Hex(circuit.RoadEdge);
        Color curbGlow = TurboArt.Hex(circuit.CurbGlow);
        Color underbelly = TurboArt.Ink;

        for (int i = 0; i < steps; i++)
        {
            float s0 = (i / (float)steps) * circuit.TotalLength;
            float s1 = ((i + 1) / (float)steps) * circuit.TotalLength;
            TrackSample a = circuit.Evaluate(s0);
            TrackSample b = circuit.Evaluate(s1);

            Vector3 aL = a.Position - a.Right * halfW;
            Vector3 aC = a.Position;
            Vector3 aR = a.Position + a.Right * halfW;
            Vector3 bL = b.Position - b.Right * halfW;
            Vector3 bC = b.Position;
            Vector3 bR = b.Position + b.Right * halfW;

            // Main ribbon surface (2 halves for center racing line contrast)
            Color stripeCol = (i % 4 == 0) ? Color.Lerp(roadCenter, curbGlow, 0.35f) : roadCenter;
            TurboArt.AddQuad(verts, cols, tris, aL, aC, bC, bL, roadEdge, stripeCol, stripeCol, roadEdge);
            TurboArt.AddQuad(verts, cols, tris, aC, aR, bR, bC, stripeCol, roadEdge, roadEdge, stripeCol);

            // Raised neon left & right curbs
            Color curbCol = (i % 2 == 0) ? curbGlow : roadEdge;
            Vector3 aCurbTopL = aL + a.Up * curbH;
            Vector3 bCurbTopL = bL + b.Up * curbH;
            Vector3 aCurbOutL = aCurbTopL - a.Right * curbW;
            Vector3 bCurbOutL = bCurbTopL - b.Right * curbW;
            TurboArt.AddQuad(verts, cols, tris, aL, aCurbTopL, bCurbTopL, bL, curbCol, curbCol, curbCol, curbCol);
            TurboArt.AddQuad(verts, cols, tris, aCurbTopL, aCurbOutL, bCurbOutL, bCurbTopL, curbCol, underbelly, underbelly, curbCol);

            Vector3 aCurbTopR = aR + a.Up * curbH;
            Vector3 bCurbTopR = bR + b.Up * curbH;
            Vector3 aCurbOutR = aCurbTopR + a.Right * curbW;
            Vector3 bCurbOutR = bCurbTopR + b.Right * curbW;
            TurboArt.AddQuad(verts, cols, tris, aR, bR, bCurbTopR, aCurbTopR, curbCol, curbCol, curbCol, curbCol);
            TurboArt.AddQuad(verts, cols, tris, aCurbTopR, bCurbTopR, bCurbOutR, aCurbOutR, curbCol, curbCol, underbelly, underbelly);

            // Track underside skirt
            Vector3 aBotL = aL - a.Up * deckThickness;
            Vector3 bBotL = bL - b.Up * deckThickness;
            Vector3 aBotR = aR - a.Up * deckThickness;
            Vector3 bBotR = bR - b.Up * deckThickness;
            TurboArt.AddQuad(verts, cols, tris, aBotL, bBotL, bBotR, aBotR, underbelly, underbelly, underbelly, underbelly);

            // Start/Finish overhead gantry at s = 0
            if (i == 0)
            {
                Vector3 gTL = aL + a.Up * 6.5f;
                Vector3 gTR = aR + a.Up * 6.5f;
                Vector3 gBL = bL + b.Up * 5.6f;
                Vector3 gBR = bR + b.Up * 5.6f;
                TurboArt.AddQuad(verts, cols, tris, aL, gTL, bL + b.Up * 6.5f, bL, curbGlow, curbGlow, underbelly, underbelly);
                TurboArt.AddQuad(verts, cols, tris, aR, bR, bR + b.Up * 6.5f, gTR, curbGlow, underbelly, underbelly, curbGlow);
                TurboArt.AddQuad(verts, cols, tris, gTL, gTR, gBR, gBL, TurboArt.Cream, TurboArt.Cream, TurboArt.Accent, TurboArt.Accent);
            }
        }

        // Speed Boost Pads & Weapon Pads
        foreach (var pad in circuit.Pads)
        {
            TrackSample s0 = circuit.Evaluate(pad.Distance - 2.2f);
            TrackSample s1 = circuit.Evaluate(pad.Distance + 2.2f);
            Vector3 p0L = s0.Position + s0.Right * (pad.LateralOffset - 1.35f) + s0.Up * 0.08f;
            Vector3 p0R = s0.Position + s0.Right * (pad.LateralOffset + 1.35f) + s0.Up * 0.08f;
            Vector3 p1C = s1.Position + s1.Right * pad.LateralOffset + s1.Up * 0.08f;
            Color padColor = pad.IsWeapon ? TurboArt.Hex(0xA855F7) : TurboArt.Hex(0x38BDF8);
            TurboArt.AddTri(verts, cols, tris, p0L, p1C, p0R, padColor);
        }

        TurboArt.CreateMeshObject(artwork, "RibbonTrackMesh", verts, cols, tris);
    }

    public List<Transform> GetWaypoints() => waypoints;
}
