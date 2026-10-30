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

        if (gas)
            forwardSpeed = Mathf.MoveTowards(forwardSpeed, topSpeed, spec.Accel * (boosting ? 1.6f : 1f) * dt);
        else if (brake)
            forwardSpeed = Mathf.MoveTowards(forwardSpeed, 0f, spec.Accel * 1.8f * dt);
        else
            forwardSpeed = Mathf.MoveTowards(forwardSpeed, 0f, 12f * dt);

        float targetLatVel = steer * spec.Handling * 2.15f;
        lateralVelocity = Mathf.Lerp(lateralVelocity, targetLatVel, 1f - Mathf.Exp(-10f * dt));
        lateralOffset += lateralVelocity * dt;

        float maxLat = circuit.TrackWidth * 0.5f - 1.25f;
        if (Mathf.Abs(lateralOffset) > maxLat)
        {
            lateralOffset = Mathf.Sign(lateralOffset) * maxLat;
            lateralVelocity = -lateralVelocity * 0.45f;
            forwardSpeed *= 0.92f;
            currentShield = Mathf.Max(0f, currentShield - 12f * dt);
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
        float hoverBob = Mathf.Sin(Time.time * 6f) * 0.08f;
        transform.position = sample.Position + sample.Right * lateralOffset + sample.Up * (hoverHeight + hoverBob);
        Quaternion frameRot = Quaternion.LookRotation(sample.Forward, sample.Up);
        transform.rotation = frameRot * Quaternion.Euler(visualPitch, 0f, visualRoll);
    }

    void UpdateCamera(float dt)
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        TurboCircuit circuit = TurboLevels.Current;
        TrackSample behind = circuit.Evaluate(trackDistance - 7.8f);
        TrackSample ahead = circuit.Evaluate(trackDistance + 14f);

        Vector3 targetPos = behind.Position + behind.Right * (lateralOffset * 0.65f) + behind.Up * 3.7f;
        Vector3 lookTarget = ahead.Position + ahead.Right * (lateralOffset * 0.45f) + ahead.Up * 1.1f;

        cam.transform.position = Vector3.Lerp(cam.transform.position, targetPos, 1f - Mathf.Exp(-14f * dt));
        Quaternion targetRot = Quaternion.LookRotation((lookTarget - cam.transform.position).normalized, behind.Up);
        cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, targetRot, 1f - Mathf.Exp(-14f * dt));

        float targetFov = (boostTimer > 0f) ? 76f : 64f + Mathf.Clamp01(forwardSpeed / 75f) * 6f;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, 1f - Mathf.Exp(-8f * dt));
    }

    public float GetSpeedKmh() => forwardSpeed * 3.6f;
}
