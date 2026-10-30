using UnityEngine;
using System.Collections.Generic;

public class Projectile : MonoBehaviour
{
    public float trackDistance;
    public float lateralOffset;
    public float speed = 115f;
    public float lifeTime = 2.8f;

    public void InitPulse(float startS, float startLat)
    {
        trackDistance = startS;
        lateralOffset = startLat;
        var verts = new List<Vector3>();
        var cols = new List<Color>();
        var tris = new List<int>();
        Color c = TurboArt.Hex(0xA855F7);
        TurboArt.AddTri(verts, cols, tris, new Vector3(0, 0.2f, 1.4f), new Vector3(-0.45f, 0, -0.6f), new Vector3(0.45f, 0, -0.6f), c);
        TurboArt.CreateMeshObject(transform, "PulseBolt", verts, cols, tris);
    }

    public bool StepProjectile(float dt, RaceManager rm)
    {
        lifeTime -= dt;
        if (lifeTime <= 0f) return false;
        trackDistance += speed * dt;

        // Home toward closest rival ahead
        AIController target = null;
        float bestDelta = 90f;
        foreach (var ai in rm.rivals)
        {
            float d = ai.trackDistance - trackDistance;
            if (d > -2f && d < bestDelta)
            {
                bestDelta = d;
                target = ai;
            }
        }
        if (target != null)
        {
            lateralOffset = Mathf.MoveTowards(lateralOffset, target.lateralOffset, 18f * dt);
            if (Mathf.Abs(target.trackDistance - trackDistance) < 2.8f && Mathf.Abs(target.lateralOffset - lateralOffset) < 2.2f)
            {
                target.ApplyHit();
                return false;
            }
        }

        TrackSample s = TurboLevels.Current.Evaluate(trackDistance);
        transform.position = s.Position + s.Right * lateralOffset + s.Up * 1.35f;
        transform.rotation = Quaternion.LookRotation(s.Forward, s.Up);
        return true;
    }
}
