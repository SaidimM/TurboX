using UnityEngine;
using System.Collections.Generic;

public class RaceManager : MonoBehaviour
{
    public PlayerController player { get; private set; }
    public readonly List<AIController> rivals = new List<AIController>();
    public readonly List<Projectile> projectiles = new List<Projectile>();

    void Start()
    {
        EnsureRacers();
    }

    public void EnsureRacers()
    {
        if (player == null)
        {
            player = FindObjectOfType<PlayerController>();
            if (player == null) player = new GameObject("PlayerShip").AddComponent<PlayerController>();
        }
        while (rivals.Count < 3)
        {
            var ai = new GameObject("RivalShip_" + (rivals.Count + 1)).AddComponent<AIController>();
            rivals.Add(ai);
        }
        ResetRace();
    }

    public void ResetRace()
    {
        foreach (var p in projectiles)
            if (p != null) Destroy(p.gameObject);
        projectiles.Clear();

        if (player != null)
        {
            player.RebuildShipVisual();
            player.ResetShip(0f, -2.5f);
        }
        float[] startDist = { 4f, 8f, 12f };
        float[] startLat = { 2.5f, -2.5f, 2.5f };
        for (int i = 0; i < rivals.Count; i++)
            rivals[i].Init(i, startDist[i], startLat[i]);
    }

    void Update()
    {
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null && (gm.IsSelecting || gm.IsPaused)) return;
        if (Time.timeScale == 0f) return;

        float dt = Mathf.Min(Time.deltaTime, 0.05f);
        foreach (var ai in rivals) ai.StepAI(dt);

        for (int i = projectiles.Count - 1; i >= 0; i--)
        {
            if (projectiles[i] == null || !projectiles[i].StepProjectile(dt, this))
            {
                if (projectiles[i] != null) Destroy(projectiles[i].gameObject);
                projectiles.RemoveAt(i);
            }
        }
    }

    public void FirePlayerPulse(float startS, float startLat)
    {
        var obj = new GameObject("PulseMissile");
        var proj = obj.AddComponent<Projectile>();
        proj.InitPulse(startS + 3.5f, startLat);
        projectiles.Add(proj);
    }

    public int GetPlayerPlace()
    {
        if (player == null) return 1;
        int place = 1;
        foreach (var ai in rivals)
            if (ai.trackDistance > player.trackDistance) place++;
        return place;
    }
}
