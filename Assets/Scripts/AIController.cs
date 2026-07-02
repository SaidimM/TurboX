using UnityEngine;
using System.Collections.Generic;

public class AIController : MonoBehaviour
{
    public float hoverHeight = 2.0f;
    public float hoverForce = 50.0f;
    public float forwardSpeed = 28.0f;
    public float turnSpeed = 8.0f;
    public int maxShield = 100;
    
    public RaceManager raceManager;

    [HideInInspector]
    public int currentShield = 100;
    
    private Rigidbody rb;
    private float targetTurn = 0f;
    private float currentTurn = 0f;
    private WeaponSystem weaponSystem;
    private List<Transform> waypoints = new List<Transform>();
    private int currentWaypointIndex = 0;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        weaponSystem = GetComponent<WeaponSystem>();
        currentShield = maxShield;

        Invoke("FetchWaypoints", 1.0f);
    }

    private void FetchWaypoints()
    {
        if (raceManager != null && raceManager.trackGenerator != null)
        {
            waypoints = raceManager.trackGenerator.GetWaypoints();
        }
    }

    void FixedUpdate()
    {
        if (rb == null || currentShield <= 0) return;

        Vector3 hitNormal = ApplyHoverPhysics();
        CalculateWaypointTurn();
        HandleWeapons();

        // Align ship UP to surface normal
        Vector3 targetUp = hitNormal;
        Vector3 forward = transform.forward;
        Vector3 right = Vector3.Cross(targetUp, forward).normalized;
        Vector3 newForward = Vector3.Cross(right, targetUp).normalized;

        Quaternion targetQuat = Quaternion.LookRotation(newForward, targetUp);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetQuat, Time.fixedDeltaTime * 5.0f);

        // Movement
        Vector3 forwardVel = transform.forward * forwardSpeed;
        
        currentTurn = Mathf.Lerp(currentTurn, targetTurn, Time.fixedDeltaTime * turnSpeed);
        Vector3 turnVel = transform.right * (currentTurn * turnSpeed);

        Vector3 currentVelocity = rb.velocity;
        Vector3 localVelocity = forwardVel + turnVel;
        
        float verticalDot = Vector3.Dot(currentVelocity, targetUp);
        Vector3 verticalVel = targetUp * verticalDot;
        
        rb.velocity = localVelocity + verticalVel;
    }

    private Vector3 ApplyHoverPhysics()
    {
        Vector3 rayStart = transform.position;
        Vector3 downVector = -transform.up;
        
        if (Physics.Raycast(rayStart, downVector, out RaycastHit hit, hoverHeight * 3f))
        {
            if (hit.distance < hoverHeight)
            {
                float forceMagnitude = (hoverHeight - hit.distance) * hoverForce;
                rb.AddForce(hit.normal * forceMagnitude, ForceMode.Acceleration);
            }
            return hit.normal;
        }
        return Vector3.up;
    }

    private void CalculateWaypointTurn()
    {
        if (waypoints.Count == 0) return;

        if (currentWaypointIndex >= waypoints.Count)
        {
            currentWaypointIndex = 0;
        }

        Transform targetWaypoint = waypoints[currentWaypointIndex];
        
        float dist = Vector3.Distance(transform.position, targetWaypoint.position);
        if (dist < 20.0f)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= waypoints.Count)
            {
                currentWaypointIndex = 0;
            }
        }

        Vector3 targetPos = waypoints[currentWaypointIndex].position;
        Vector3 localTarget = (targetPos - transform.position).normalized;
        float rightDot = Vector3.Dot(localTarget, transform.right);
        
        targetTurn = Mathf.Clamp(rightDot * 2.0f, -1f, 1f);
    }

    private void HandleWeapons()
    {
        if (weaponSystem != null)
        {
            if (Random.value < 0.005f)
            {
                weaponSystem.DropMine();
            }
        }
    }

    public void TakeDamage(int amount)
    {
        currentShield -= amount;
        if (currentShield <= 0)
        {
            currentShield = 0;
            Debug.Log("AI Ship destroyed!");
            Destroy(gameObject);
        }
    }
}
