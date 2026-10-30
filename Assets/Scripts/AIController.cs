using UnityEngine;

public class AIController : MonoBehaviour
{
    public int rivalIndex;
    public float hoverHeight = 1.35f;
    public float forwardSpeed { get; private set; }
    public float lateralOffset { get; private set; }
    public float trackDistance { get; private set; }
    public bool finished { get; private set; }

    float targetLane;
    float baseSpeed;
    float boostTimer;
    float stunTimer;
    Transform visualRoot;

    public void Init(int index, float startDist, float startLat)
    {
        rivalIndex = index;
        TurboShipSpec spec = TurboLevels.Ships[(index + 1) % TurboLevels.Ships.Length];
        TurboArt.ClearGenerated(visualRoot);
        visualRoot = TurboArt.CreateShipVisual(transform, spec);

        trackDistance = startDist;
        lateralOffset = startLat;
        targetLane = startLat;
        forwardSpeed = 0f;
        boostTimer = 0f;
        stunTimer = 0f;
        finished = false;
        baseSpeed = 54f + index * 3.6f + TurboLevels.CurrentCircuitIndex * 1.8f;
        SyncTransform();
    }

    public void StepAI(float dt)
    {
        if (finished) return;
        TurboCircuit circuit = TurboLevels.Current;

        if (stunTimer > 0f) stunTimer = Mathf.Max(0f, stunTimer - dt);
        if (boostTimer > 0f) boostTimer = Mathf.Max(0f, boostTimer - dt);

        float desiredSpeed = baseSpeed * (boostTimer > 0f ? 1.24f : 1f) * (stunTimer > 0f ? 0.55f : 1f);
        forwardSpeed = Mathf.MoveTowards(forwardSpeed, desiredSpeed, 28f * dt);
        trackDistance += forwardSpeed * dt;

        // Periodically shift racing line toward upcoming boost pads
        float wrapped = circuit.WrapDistance(trackDistance);
        foreach (var pad in circuit.Pads)
        {
            float distAhead = pad.Distance - wrapped;
            if (distAhead > 4f && distAhead < 32f && !pad.IsWeapon)
            {
                targetLane = pad.LateralOffset;
                break;
            }
            if (Mathf.Abs(distAhead) < 2.6f && Mathf.Abs(lateralOffset - pad.LateralOffset) < 2.2f && !pad.IsWeapon)
            {
                boostTimer = 1.15f;
            }
        }

        float steerDelta = targetLane - lateralOffset;
        lateralOffset += Mathf.Clamp(steerDelta * 2.5f, -9f, 9f) * dt;
        float maxLat = circuit.TrackWidth * 0.5f - 1.4f;
        lateralOffset = Mathf.Clamp(lateralOffset, -maxLat, maxLat);

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
        transform.position = sample.Position + sample.Right * lateralOffset + sample.Up * hoverHeight;
        transform.rotation = Quaternion.LookRotation(sample.Forward, sample.Up);
    }
}
