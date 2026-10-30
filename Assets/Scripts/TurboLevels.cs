using UnityEngine;

public sealed class TurboShipSpec
{
    public readonly string Id, Name, Role, Description;
    public readonly float MaxSpeed, Accel, Handling, MaxShield;
    public readonly uint HullPrimary, HullSecondary, Accent;

    public TurboShipSpec(string id, string name, string role, string desc,
        float maxSpeed, float accel, float handling, float maxShield,
        uint primary, uint secondary, uint accent)
    {
        Id = id; Name = name; Role = role; Description = desc;
        MaxSpeed = maxSpeed; Accel = accel; Handling = handling; MaxShield = maxShield;
        HullPrimary = primary; HullSecondary = secondary; Accent = accent;
    }
}

public struct TrackSample
{
    public Vector3 Position;
    public Vector3 Forward;
    public Vector3 Right;
    public Vector3 Up;
    public float BankRad;
}

public struct TrackPad
{
    public float Distance;
    public float LateralOffset;
    public bool IsWeapon; // false = Boost pad, true = Weapon pad
}

public sealed class TurboCircuit
{
    public readonly string Id, Name, Tag, Description;
    public readonly float TrackWidth;
    public readonly int Laps;
    public readonly Vector3[] ControlPoints;
    public readonly float[] BankAnglesDeg;
    public readonly uint SkyTop, SkyHorizon, MountainFar, MountainNear, RoadSurface, RoadEdge, CurbGlow;
    public readonly float TotalLength;
    public readonly TrackPad[] Pads;

    readonly Vector3[] sampledPoints;
    readonly float[] sampledBanks;
    readonly float[] cumulativeLengths;

    public TurboCircuit(string id, string name, string tag, string desc, float width, int laps,
        Vector3[] controlPoints, float[] bankDeg,
        uint skyTop, uint skyHorizon, uint mountainFar, uint mountainNear, uint road, uint edge, uint curb)
    {
        Id = id; Name = name; Tag = tag; Description = desc;
        TrackWidth = width; Laps = laps;
        ControlPoints = controlPoints; BankAnglesDeg = bankDeg;
        SkyTop = skyTop; SkyHorizon = skyHorizon;
        MountainFar = mountainFar; MountainNear = mountainNear;
        RoadSurface = road; RoadEdge = edge; CurbGlow = curb;

        int n = controlPoints.Length;
        int subdivisions = 16;
        int totalSamples = n * subdivisions;
        sampledPoints = new Vector3[totalSamples + 1];
        sampledBanks = new float[totalSamples + 1];
        cumulativeLengths = new float[totalSamples + 1];

        float dist = 0f;
        for (int i = 0; i <= totalSamples; i++)
        {
            float u = (float)(i % totalSamples) / subdivisions;
            int seg = Mathf.FloorToInt(u) % n;
            float t = u - Mathf.Floor(u);
            Vector3 p0 = controlPoints[(seg - 1 + n) % n];
            Vector3 p1 = controlPoints[seg];
            Vector3 p2 = controlPoints[(seg + 1) % n];
            Vector3 p3 = controlPoints[(seg + 2) % n];
            sampledPoints[i] = CatmullRom(p0, p1, p2, p3, t);

            float b1 = bankDeg[seg];
            float b2 = bankDeg[(seg + 1) % n];
            float smoothT = t * t * (3f - 2f * t);
            sampledBanks[i] = Mathf.Lerp(b1, b2, smoothT) * Mathf.Deg2Rad;

            if (i > 0) dist += Vector3.Distance(sampledPoints[i - 1], sampledPoints[i]);
            cumulativeLengths[i] = dist;
        }
        TotalLength = dist;

        int padCount = 18;
        Pads = new TrackPad[padCount];
        for (int i = 0; i < padCount; i++)
        {
            float d = (i + 0.6f) * (TotalLength / padCount);
            float lane = ((i % 3) - 1) * (TrackWidth * 0.24f);
            bool isWeapon = (i % 3) == 1;
            Pads[i] = new TrackPad { Distance = d, LateralOffset = lane, IsWeapon = isWeapon };
        }
    }

    static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

    public float WrapDistance(float s)
    {
        if (TotalLength <= 0f) return 0f;
        float r = s % TotalLength;
        return r < 0f ? r + TotalLength : r;
    }

    public TrackSample Evaluate(float s)
    {
        float d = WrapDistance(s);
        int lo = 0, hi = cumulativeLengths.Length - 1;
        while (lo < hi - 1)
        {
            int mid = (lo + hi) >> 1;
            if (cumulativeLengths[mid] <= d) lo = mid;
            else hi = mid;
        }
        float segLen = Mathf.Max(0.0001f, cumulativeLengths[hi] - cumulativeLengths[lo]);
        float t = Mathf.Clamp01((d - cumulativeLengths[lo]) / segLen);
        Vector3 pos = Vector3.Lerp(sampledPoints[lo], sampledPoints[hi], t);
        float bank = Mathf.Lerp(sampledBanks[lo], sampledBanks[hi], t);

        int next = (lo + 1) % (sampledPoints.Length - 1);
        int prev = (lo - 1 + (sampledPoints.Length - 1)) % (sampledPoints.Length - 1);
        Vector3 fwd = (sampledPoints[next] - sampledPoints[prev]).normalized;
        if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;

        Vector3 flatRight = Vector3.Cross(Vector3.up, fwd).normalized;
        if (flatRight.sqrMagnitude < 0.001f) flatRight = Vector3.right;
        Vector3 baseUp = Vector3.Cross(fwd, flatRight).normalized;

        Quaternion bankRot = Quaternion.AngleAxis(bank * Mathf.Rad2Deg, fwd);
        Vector3 right = bankRot * flatRight;
        Vector3 up = bankRot * baseUp;

        return new TrackSample { Position = pos, Forward = fwd, Right = right, Up = up, BankRad = bank };
    }
}

public static class TurboLevels
{
    public static readonly TurboShipSpec[] Ships = {
        new TurboShipSpec("striker", "The Striker", "BALANCED",
            "Balanced anti-gravity hull with crisp cornering and steady shields.",
            68f, 34f, 10.5f, 100f, 0xE2E8F0, 0x1E293B, 0x38BDF8),
        new TurboShipSpec("juggernaut", "Heavy Juggernaut", "ARMORED",
            "Twin-boom heavy interceptor built to shrug off missile hits and wall scrapes.",
            63f, 29f, 8.2f, 150f, 0xFDE68A, 0x292524, 0xF59E0B),
        new TurboShipSpec("neon-flash", "Neon Flash", "HYPER / AGILE",
            "Featherweight needle-nose dart with razor-sharp banking and top-end speed.",
            74f, 40f, 13.5f, 75f, 0xFFE4E6, 0x1F1624, 0xF43F5E)
    };

    public static readonly TurboCircuit[] All = {
        new TurboCircuit(
            "solaris-ridge", "Solaris Ridge", "CANYON VIADUCT",
            "Sweeping desert canyon skyway with banked red-rock curves and long boost straights.",
            15f, 3,
            new[] {
                new Vector3(0, 0, 0), new Vector3(0, 2, 120), new Vector3(55, 8, 220),
                new Vector3(145, 14, 240), new Vector3(210, 6, 160), new Vector3(220, -4, 40),
                new Vector3(165, -6, -70), new Vector3(80, 0, -110), new Vector3(15, -2, -75)
            },
            new[] { 0f, -6f, -18f, -22f, -12f, 8f, -16f, -12f, 0f },
            0x1A1E2E, 0xE79B6B, 0x8F5B4A, 0x4E3636, 0x232B32, 0x37454D, 0x38BDF8
        ),
        new TurboCircuit(
            "cobalt-spire", "Cobalt Spire", "HIGH ALTITUDE",
            "High-altitude alpine ring with a steep rollercoaster dive and rapid S-curve chicane.",
            14.5f, 3,
            new[] {
                new Vector3(0, 0, 0), new Vector3(-10, 6, 135), new Vector3(45, 22, 245),
                new Vector3(150, 26, 275), new Vector3(245, 10, 195), new Vector3(210, -8, 85),
                new Vector3(255, -12, -30), new Vector3(165, -4, -125), new Vector3(55, 4, -100)
            },
            new[] { 0f, 10f, -24f, -20f, 16f, -22f, 18f, -15f, 0f },
            0x0F1D2D, 0x8AB6C9, 0x4E7388, 0x2C4657, 0x1D2833, 0x314353, 0x2DD4BF
        ),
        new TurboCircuit(
            "hyperion-loop", "Hyperion Loop", "NIGHT NEON",
            "Twilight orbital viaduct with fast elevation drops, tight hairpins and neon barriers.",
            14f, 3,
            new[] {
                new Vector3(0, 0, 0), new Vector3(0, 12, 140), new Vector3(85, 28, 250),
                new Vector3(205, 18, 230), new Vector3(280, -4, 120), new Vector3(220, -14, 10),
                new Vector3(120, -4, -20), new Vector3(170, 8, -120), new Vector3(55, 2, -115)
            },
            new[] { 0f, -14f, -26f, -18f, 22f, 20f, -24f, -18f, 0f },
            0x0B0C1B, 0x432C52, 0x2A1E3F, 0x1A142B, 0x171826, 0x2B2D42, 0xF43F5E
        ),
        new TurboCircuit(
            "valkyrie-pass", "Valkyrie Pass", "SUMMIT ENDURANCE",
            "Sustained summit climb, blind ridge crests and high-speed technical boost corridors.",
            13.5f, 3,
            new[] {
                new Vector3(0, 0, 0), new Vector3(-25, 10, 150), new Vector3(30, 26, 285),
                new Vector3(155, 36, 320), new Vector3(275, 24, 240), new Vector3(310, 6, 95),
                new Vector3(235, -10, -25), new Vector3(270, -6, -135), new Vector3(135, 4, -165),
                new Vector3(35, 0, -100)
            },
            new[] { 0f, 14f, -26f, -22f, -16f, 24f, -20f, 18f, -16f, 0f },
            0x111C24, 0xC47D5B, 0x5B4B49, 0x332E33, 0x1E242B, 0x343F4B, 0xF59E0B
        )
    };

    public static int CurrentCircuitIndex { get; private set; }
    public static int CurrentShipIndex
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt("turbox.ship", 0), 0, Ships.Length - 1);
        set { PlayerPrefs.SetInt("turbox.ship", Mathf.Clamp(value, 0, Ships.Length - 1)); PlayerPrefs.Save(); }
    }
    public static TurboCircuit Current => All[CurrentCircuitIndex];
    public static TurboShipSpec CurrentShip => Ships[CurrentShipIndex];
    public static int UnlockedCount => Mathf.Clamp(PlayerPrefs.GetInt("turbox.unlocked", 1), 1, All.Length);
    public static bool IsUnlocked(int index) => index >= 0 && index < UnlockedCount;
    public static float BestTime(int index) => PlayerPrefs.GetFloat("turbox.best." + All[index].Id, 0f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetSession() { CurrentCircuitIndex = 0; }

    public static bool SelectCircuit(int index)
    {
        if (!IsUnlocked(index)) return false;
        CurrentCircuitIndex = index;
        return true;
    }

    public static bool RecordFinish(int index, float seconds, int place)
    {
        if (!IsUnlocked(index) || seconds <= 0f || float.IsNaN(seconds) || float.IsInfinity(seconds)) return false;
        bool improved = false;
        if (place <= 3)
        {
            float best = BestTime(index);
            improved = best <= 0f || seconds < best;
            if (improved) PlayerPrefs.SetFloat("turbox.best." + All[index].Id, seconds);
            PlayerPrefs.SetInt("turbox.unlocked", Mathf.Max(UnlockedCount, Mathf.Min(index + 2, All.Length)));
            PlayerPrefs.Save();
        }
        return improved;
    }
}
