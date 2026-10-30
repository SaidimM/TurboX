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
        int steps = 240;
        float halfW = circuit.TrackWidth * 0.5f;
        float curbW = 0.75f;
        float curbH = 0.68f;
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

            Color stripeCol = (i % 4 == 0) ? Color.Lerp(roadCenter, curbGlow, 0.38f) : roadCenter;
            TurboArt.AddQuad(verts, cols, tris, aL, aC, bC, bL, roadEdge, stripeCol, stripeCol, roadEdge);
            TurboArt.AddQuad(verts, cols, tris, aC, aR, bR, bC, stripeCol, roadEdge, roadEdge, stripeCol);

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

            Vector3 aBotL = aL - a.Up * deckThickness;
            Vector3 bBotL = bL - b.Up * deckThickness;
            Vector3 aBotR = aR - a.Up * deckThickness;
            Vector3 bBotR = bR - b.Up * deckThickness;
            TurboArt.AddQuad(verts, cols, tris, aBotL, bBotL, bBotR, aBotR, underbelly, underbelly, underbelly, underbelly);

            if (i == 0)
            {
                Vector3 gTL = aL + a.Up * 6.5f;
                Vector3 gTR = aR + a.Up * 6.5f;
                Vector3 gBL = bL + b.Up * 5.4f;
                Vector3 gBR = bR + b.Up * 5.4f;
                TurboArt.AddQuad(verts, cols, tris, aL, gTL, bL + b.Up * 6.5f, bL, curbGlow, curbGlow, underbelly, underbelly);
                TurboArt.AddQuad(verts, cols, tris, aR, bR, bR + b.Up * 6.5f, gTR, curbGlow, underbelly, underbelly, curbGlow);
                TurboArt.AddQuad(verts, cols, tris, gTL, gTR, gBR, gBL, TurboArt.Cream, TurboArt.Cream, TurboArt.Accent, TurboArt.Accent);
            }
            else if (i % 12 == 0)
            {
                // Overhead holographic neon gate & support pier
                Vector3 archTL = aL + a.Up * 5.2f + a.Right * 1.4f;
                Vector3 archTR = aR + a.Up * 5.2f - a.Right * 1.4f;
                TurboArt.AddQuad(verts, cols, tris, aCurbTopL, archTL, archTL + a.Forward * 1.2f, bCurbTopL, curbGlow, curbGlow, underbelly, underbelly);
                TurboArt.AddQuad(verts, cols, tris, aCurbTopR, bCurbTopR, archTR + a.Forward * 1.2f, archTR, curbGlow, underbelly, underbelly, curbGlow);
                TurboArt.AddQuad(verts, cols, tris, archTL, archTR, archTR - a.Up * 0.45f, archTL - a.Up * 0.45f, curbGlow, curbGlow, TurboArt.Cream, TurboArt.Cream);

                Vector3 pierTopL = aC - a.Right * 1.4f;
                Vector3 pierTopR = aC + a.Right * 1.4f;
                Vector3 pierBotL = pierTopL + new Vector3(0f, -48f, 0f);
                Vector3 pierBotR = pierTopR + new Vector3(0f, -48f, 0f);
                TurboArt.AddQuad(verts, cols, tris, pierTopL, pierTopR, pierBotR, pierBotL, roadEdge, roadEdge, underbelly, underbelly);
            }
        }

        // Double-chevron Speed Pads & 3D Floating Weapon Crystals
        foreach (var pad in circuit.Pads)
        {
            if (pad.IsWeapon)
            {
                TrackSample s0 = circuit.Evaluate(pad.Distance - 1.8f);
                TrackSample s1 = circuit.Evaluate(pad.Distance + 1.8f);
                TrackSample sMid = circuit.Evaluate(pad.Distance);
                Vector3 p0L = s0.Position + s0.Right * (pad.LateralOffset - 1.3f) + s0.Up * 0.08f;
                Vector3 p0R = s0.Position + s0.Right * (pad.LateralOffset + 1.3f) + s0.Up * 0.08f;
                Vector3 p1C = s1.Position + s1.Right * pad.LateralOffset + s1.Up * 0.08f;
                Color vCol = TurboArt.Hex(0xA855F7);
                TurboArt.AddTri(verts, cols, tris, p0L, p1C, p0R, vCol);

                Vector3 cCenter = sMid.Position + sMid.Right * pad.LateralOffset + sMid.Up * 1.55f;
                Vector3 cTop = cCenter + sMid.Up * 0.75f;
                Vector3 cBot = cCenter - sMid.Up * 0.75f;
                Vector3 cL = cCenter - sMid.Right * 0.55f;
                Vector3 cR = cCenter + sMid.Right * 0.55f;
                Vector3 cF = cCenter + sMid.Forward * 0.55f;
                TurboArt.AddLitTri(verts, cols, tris, cTop, cL, cF, TurboArt.Hex(0xD8B4FE), 1.0f);
                TurboArt.AddLitTri(verts, cols, tris, cTop, cF, cR, vCol, 1.0f);
                TurboArt.AddLitTri(verts, cols, tris, cBot, cF, cL, vCol, 0.6f);
                TurboArt.AddLitTri(verts, cols, tris, cBot, cR, cF, vCol, 0.6f);
            }
            else
            {
                for (int ch = 0; ch < 2; ch++)
                {
                    float offS = pad.Distance + (ch - 0.5f) * 2.1f;
                    TrackSample sBack = circuit.Evaluate(offS - 1.2f);
                    TrackSample sTip = circuit.Evaluate(offS + 1.4f);
                    Vector3 pBL = sBack.Position + sBack.Right * (pad.LateralOffset - 1.45f) + sBack.Up * 0.09f;
                    Vector3 pBR = sBack.Position + sBack.Right * (pad.LateralOffset + 1.45f) + sBack.Up * 0.09f;
                    Vector3 pTip = sTip.Position + sTip.Right * pad.LateralOffset + sTip.Up * 0.09f;
                    TurboArt.AddTri(verts, cols, tris, pBL, pTip, pBR, ch == 0 ? TurboArt.Hex(0x7DD3FC) : TurboArt.Cyan);
                }
            }
        }

        TurboArt.CreateMeshObject(artwork, "RibbonTrackMesh", verts, cols, tris);
    }

    public List<Transform> GetWaypoints() => waypoints;
}
