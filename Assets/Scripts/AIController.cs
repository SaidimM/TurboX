using UnityEngine;

public class AIController : MonoBehaviour
{
    public int rivalIndex;
    public float hoverHeight = 1.35f;
    public float forwardSpeed { get; private set; }
    public float lateralOffset { get; private set; }
    public float lateralVelocity { get; private set; }
    public float trackDistance { get; private set; }
    public bool finished { get; private set; }

    float targetLane;
    float boostTimer;
    float stunTimer;
    float currentShield;
    int lastPadIndex = -1;
    bool hasWeapon = false;
    float visualRoll;
    float visualPitch;

    // Drop & Respawn mechanics
    public bool isFalling { get; private set; }
    float fallTimer;
    float fallAltitude;
    float fallLateral;
    float fallDistance;
    public float respawnTimer { get; private set; }

    Transform visualRoot;
    TurboShipSpec spec;

    public void Init(int index, float startDist, float startLat)
    {
        rivalIndex = index;
        spec = TurboLevels.Ships[(index + 1) % TurboLevels.Ships.Length];
        TurboArt.ClearGenerated(visualRoot);
        visualRoot = TurboArt.CreateShipVisual(transform, spec);

        trackDistance = startDist;
        lateralOffset = startLat;
        lateralVelocity = 0f;
        targetLane = startLat;
        forwardSpeed = 0f;
        boostTimer = 0f;
        stunTimer = 0f;
        currentShield = spec.MaxShield;
        lastPadIndex = -1;
        hasWeapon = false;
        finished = false;
        visualRoll = 0f;
        visualPitch = 0f;
        isFalling = false;
        fallTimer = 0f;
        fallAltitude = 0f;
        fallLateral = 0f;
        fallDistance = 0f;
        respawnTimer = 0f;
        SyncTransform();
    }

    public void StepAI(float dt)
    {
        if (finished) return;
        TurboCircuit circuit = TurboLevels.Current;

        if (respawnTimer > 0f) respawnTimer = Mathf.Max(0f, respawnTimer - dt);

        if (isFalling)
        {
            fallTimer += dt;
            fallAltitude -= 34f * dt;
            lateralOffset += fallLateral * dt;
            visualPitch = Mathf.Lerp(visualPitch, 55f, 1f - Mathf.Exp(-6f * dt));
            visualRoll += (fallLateral > 0f ? 240f : -240f) * dt;

            if (fallTimer > 1.25f)
            {
                isFalling = false;
                fallTimer = 0f;
                fallAltitude = 0f;
                forwardSpeed = 0f;
                lateralVelocity = 0f;
                float dropSide = Mathf.Sign(lateralOffset);
                lateralOffset = dropSide * (circuit.TrackWidth * 0.5f - 2.2f);
                trackDistance = fallDistance;
                visualRoll = 0f;
                visualPitch = 0f;
                respawnTimer = 1.0f;
            }
            SyncTransform();
            return;
        }

        if (stunTimer > 0f) stunTimer = Mathf.Max(0f, stunTimer - dt);
        if (boostTimer > 0f) boostTimer = Mathf.Max(0f, boostTimer - dt);

        // Periodically shift racing line toward upcoming boost pads
        float wrapped = circuit.WrapDistance(trackDistance);
        for (int i = 0; i < circuit.Pads.Length; i++)
        {
            TrackPad pad = circuit.Pads[i];
            float distAhead = pad.Distance - wrapped;
            if (distAhead > 5f && distAhead < 36f && !pad.IsWeapon)
            {
                targetLane = pad.LateralOffset;
                break;
            }
            if (Mathf.Abs(distAhead) < 2.8f && Mathf.Abs(lateralOffset - pad.LateralOffset) < 2.3f)
            {
                if (lastPadIndex != i)
                {
                    lastPadIndex = i;
                    if (pad.IsWeapon)
                    {
                        hasWeapon = true;
                        currentShield = Mathf.Min(spec.MaxShield, currentShield + 10f);
                    }
                    else
                    {
                        boostTimer = 1.4f;
                        forwardSpeed = Mathf.Max(forwardSpeed, spec.MaxSpeed * 1.25f);
                    }
                }
            }
        }

        // Road border speed resistance
        float halfTrack = circuit.TrackWidth * 0.5f;
        float roadEdgeZone = halfTrack - 1.8f;
        float absLat = Mathf.Abs(lateralOffset);
        float borderResistance = 1f;
        if (absLat > roadEdgeZone)
        {
            float edgeDepth = Mathf.Clamp01((absLat - roadEdgeZone) / 1.8f);
            borderResistance = 1f - edgeDepth * 0.42f;
        }

        bool isBoosting = boostTimer > 0f;
        float targetSpeed = spec.MaxSpeed * (isBoosting ? 1.34f : 1f) * (stunTimer > 0f ? 0.55f : 1f) * borderResistance;
        if (currentShield <= 0f) targetSpeed *= 0.72f;

        forwardSpeed = Mathf.MoveTowards(forwardSpeed, targetSpeed, spec.Accel * (isBoosting ? 1.5f : 1f) * dt);

        // Lateral steering
        float steerDelta = targetLane - lateralOffset;
        float targetLatVel = Mathf.Clamp(steerDelta * 2.6f, -spec.Handling * 1.8f, spec.Handling * 1.8f);
        lateralVelocity = Mathf.Lerp(lateralVelocity, targetLatVel, 1f - Mathf.Exp(-9f * dt));
        lateralOffset += lateralVelocity * dt;

        // Space Drop detection: falls off edge into space
        float fallBoundary = halfTrack + 1.6f;
        if (Mathf.Abs(lateralOffset) > fallBoundary)
        {
            isFalling = true;
            fallTimer = 0f;
            fallAltitude = 0f;
            fallLateral = lateralVelocity != 0f ? lateralVelocity : Mathf.Sign(lateralOffset) * 6f;
            fallDistance = trackDistance;
            currentShield = Mathf.Max(0f, currentShield - 25f);
            forwardSpeed *= 0.2f;
            SyncTransform();
            return;
        }

        if (absLat > halfTrack - 0.75f)
        {
            forwardSpeed = Mathf.Max(0f, forwardSpeed - 16f * dt);
            currentShield = Mathf.Max(0f, currentShield - 12f * dt);
        }

        trackDistance += forwardSpeed * dt;
        visualRoll = Mathf.Lerp(visualRoll, -Mathf.Clamp(lateralVelocity / 9f, -1f, 1f) * 26f, 1f - Mathf.Exp(-10f * dt));
        visualPitch = Mathf.Lerp(visualPitch, isBoosting ? -3.2f : 0f, 1f - Mathf.Exp(-8f * dt));

        if (trackDistance >= circuit.TotalLength * circuit.Laps)
            finished = true;

        SyncTransform();
    }

    public void ApplyHit()
    {
        stunTimer = 1.4f;
        forwardSpeed *= 0.55f;
    }

    void SyncTransform()
    {
        TurboCircuit circuit = TurboLevels.Current;
        TrackSample sample = circuit.Evaluate(trackDistance);
        transform.position = sample.Position + sample.Right * lateralOffset + sample.Up * (hoverHeight + fallAltitude);
        transform.rotation = Quaternion.LookRotation(sample.Forward, sample.Up) * Quaternion.Euler(visualPitch, 0f, visualRoll);

        if (visualRoot != null)
        {
            bool visible = !(respawnTimer > 0f && (Mathf.FloorToInt(Time.time * 15f) % 2 == 0));
            visualRoot.gameObject.SetActive(visible);
        }
    }
}
