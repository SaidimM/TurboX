using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float hoverHeight = 1.35f;
    public float forwardSpeed { get; private set; }
    public float lateralOffset { get; private set; }
    public float lateralVelocity { get; private set; }
    public float trackDistance { get; private set; }
    public float currentShield { get; private set; }
    public float maxShield { get; private set; } = 100f;
    public float turboEnergy { get; private set; } = 100f;
    public float boostTimer { get; private set; }
    public bool hasWeapon { get; private set; }
    public int currentLap => Mathf.FloorToInt(trackDistance / Mathf.Max(1f, TurboLevels.Current.TotalLength)) + 1;
    public bool finished { get; private set; }

    [HideInInspector] public bool touchLeft, touchRight, touchBoost, touchFire, touchBrake;

    // Drop & Respawn mechanics
    public bool isFalling { get; private set; }
    float fallTimer;
    float fallAltitude;
    float fallLateral;
    float fallDistance;
    public float respawnTimer { get; private set; }

    Transform visualRoot;
    float visualRoll;
    float visualPitch;
    int lastPadIndex = -1;
    RaceManager raceManager;

    void Start()
    {
        raceManager = FindObjectOfType<RaceManager>();
        RebuildShipVisual();
        ResetShip(0f, -2.4f);
    }

    public void RebuildShipVisual()
    {
        TurboArt.ClearGenerated(visualRoot);
        visualRoot = TurboArt.CreateShipVisual(transform, TurboLevels.CurrentShip);
    }

    public void ResetShip(float startDist = 0f, float startLat = -2.4f)
    {
        TurboShipSpec spec = TurboLevels.CurrentShip;
        maxShield = spec.MaxShield;
        currentShield = maxShield;
        turboEnergy = 100f;
        forwardSpeed = 0f;
        lateralOffset = startLat;
        lateralVelocity = 0f;
        trackDistance = startDist;
        boostTimer = 0f;
        hasWeapon = false;
        finished = false;
        lastPadIndex = -1;
        touchLeft = touchRight = touchBoost = touchFire = touchBrake = false;
        isFalling = false;
        fallTimer = 0f;
        fallAltitude = 0f;
        fallLateral = 0f;
        fallDistance = 0f;
        respawnTimer = 0f;
        SyncTransform(0.016f);
    }

    void Update()
    {
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null && (gm.IsSelecting || gm.IsPaused)) return;
        if (Input.GetKeyDown(KeyCode.R))
        {
            gm?.Restart();
            return;
        }
        if (finished || Time.timeScale == 0f) return;

        bool steerL = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) || touchLeft;
        bool steerR = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) || touchRight;
        bool throttle = !Input.GetKey(KeyCode.S) && !Input.GetKey(KeyCode.DownArrow) && !touchBrake;
        bool manualBoost = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) || touchBoost;
        bool fire = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.F) || touchFire;
        touchFire = false;

        float dt = Mathf.Min(Time.deltaTime, 0.05f);
        StepPhysics(dt, throttle, !throttle, steerL ? -1f : (steerR ? 1f : 0f), manualBoost, fire);
        SyncTransform(dt);
        UpdateCamera(dt);
    }

    public void StepPhysics(float dt, bool gas, bool brake, float steer, bool manualBoost, bool fire)
    {
        TurboCircuit circuit = TurboLevels.Current;
        TurboShipSpec spec = TurboLevels.CurrentShip;

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
                lateralOffset = dropSide * (circuit.TrackWidth * 0.5f - 2.0f);
                trackDistance = fallDistance;
                visualRoll = 0f;
                visualPitch = 0f;
                respawnTimer = 1.0f;
            }
            return;
        }

        if (boostTimer > 0f) boostTimer = Mathf.Max(0f, boostTimer - dt);

        bool boosting = boostTimer > 0f;
        if (manualBoost && turboEnergy > 8f)
        {
            boosting = true;
            turboEnergy = Mathf.Max(0f, turboEnergy - 32f * dt);
        }
        else
        {
            turboEnergy = Mathf.Min(100f, turboEnergy + 14f * dt);
        }

        float topSpeed = spec.MaxSpeed * (boosting ? 1.34f : 1f);
        if (currentShield <= 0f) topSpeed *= 0.72f;

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

        if (gas)
            forwardSpeed = Mathf.MoveTowards(forwardSpeed, topSpeed * borderResistance, spec.Accel * (boosting ? 1.6f : 1f) * dt);
        else if (brake)
            forwardSpeed = Mathf.MoveTowards(forwardSpeed, 0f, spec.Accel * 1.8f * dt);
        else
            forwardSpeed = Mathf.MoveTowards(forwardSpeed, 0f, 12f * dt);

        float targetLatVel = steer * spec.Handling * 2.15f;
        lateralVelocity = Mathf.Lerp(lateralVelocity, targetLatVel, 1f - Mathf.Exp(-10f * dt));
        lateralOffset += lateralVelocity * dt;

        // Space Drop detection: plane falls into space if lateral distance exceeds road by > 1.6m
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
            return;
        }

        // Curb friction / scrape
        if (absLat > halfTrack - 0.75f)
        {
            currentShield = Mathf.Max(0f, currentShield - 14f * dt);
            forwardSpeed = Mathf.Max(0f, forwardSpeed - 18f * dt);
        }

        trackDistance += forwardSpeed * dt;

        // Check boost & weapon pads
        float wrappedS = circuit.WrapDistance(trackDistance);
        for (int i = 0; i < circuit.Pads.Length; i++)
        {
            TrackPad pad = circuit.Pads[i];
            if (Mathf.Abs(wrappedS - pad.Distance) < 3.0f && Mathf.Abs(lateralOffset - pad.LateralOffset) < 2.2f)
            {
                if (lastPadIndex != i)
                {
                    lastPadIndex = i;
                    if (pad.IsWeapon)
                    {
                        hasWeapon = true;
                        currentShield = Mathf.Min(maxShield, currentShield + 10f);
                    }
                    else
                    {
                        boostTimer = 1.55f;
                        forwardSpeed = Mathf.Max(forwardSpeed, spec.MaxSpeed * 1.28f);
                    }
                }
            }
        }

        if (fire && hasWeapon && raceManager != null)
        {
            hasWeapon = false;
            raceManager.FirePlayerPulse(trackDistance, lateralOffset);
        }

        visualRoll = Mathf.Lerp(visualRoll, -steer * 28f, 1f - Mathf.Exp(-12f * dt));
        visualPitch = Mathf.Lerp(visualPitch, boosting ? -3.5f : 0f, 1f - Mathf.Exp(-8f * dt));

        if (trackDistance >= circuit.TotalLength * circuit.Laps && !finished)
        {
            finished = true;
            FindObjectOfType<GameManager>()?.CompleteRace();
        }
    }

    public void ApplyPulseHit()
    {
        forwardSpeed *= 0.62f;
        currentShield = Mathf.Max(0f, currentShield - 22f);
    }

    void SyncTransform(float dt)
    {
        TurboCircuit circuit = TurboLevels.Current;
        TrackSample sample = circuit.Evaluate(trackDistance);
        float hoverBob = isFalling ? 0f : Mathf.Sin(Time.time * 6f) * 0.08f;
        transform.position = sample.Position + sample.Right * lateralOffset + sample.Up * (hoverHeight + hoverBob + fallAltitude);
        Quaternion frameRot = Quaternion.LookRotation(sample.Forward, sample.Up);
        transform.rotation = frameRot * Quaternion.Euler(visualPitch, 0f, visualRoll);

        if (visualRoot != null)
        {
            bool visible = !(respawnTimer > 0f && (Mathf.FloorToInt(Time.time * 15f) % 2 == 0));
            visualRoot.gameObject.SetActive(visible);
        }
    }

    void UpdateCamera(float dt)
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        TurboCircuit circuit = TurboLevels.Current;
        TrackSample behind = circuit.Evaluate(trackDistance - 7.8f);
        TrackSample ahead = circuit.Evaluate(trackDistance + 14f);

        Vector3 targetPos = behind.Position + behind.Right * (lateralOffset * 0.65f) + behind.Up * (3.7f + fallAltitude * 0.45f);
        Vector3 lookTarget = ahead.Position + ahead.Right * (lateralOffset * 0.45f) + ahead.Up * (1.1f + fallAltitude * 0.25f);

        cam.transform.position = Vector3.Lerp(cam.transform.position, targetPos, 1f - Mathf.Exp(-14f * dt));
        Quaternion targetRot = Quaternion.LookRotation((lookTarget - cam.transform.position).normalized, behind.Up);
        cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, targetRot, 1f - Mathf.Exp(-14f * dt));

        float targetFov = (boostTimer > 0f) ? 76f : 64f + Mathf.Clamp01(forwardSpeed / 75f) * 6f;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, 1f - Mathf.Exp(-8f * dt));
    }

    public float GetSpeedKmh() => forwardSpeed * 3.6f;
}
