using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float hoverHeight = 2.0f;
    public float hoverForce = 50.0f;
    public float forwardSpeed = 30.0f;
    public float turnSpeed = 10.0f;
    public int maxShield = 100;
    
    [HideInInspector]
    public int currentShield = 100;

    private Rigidbody rb;
    private float targetTurn = 0f;
    private float currentTurn = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        currentShield = maxShield;
    }

    void Update()
    {
        // Simple touch/mouse input for steering
        if (Input.GetMouseButton(0))
        {
            float screenX = Input.mousePosition.x;
            float screenWidth = Screen.width;
            targetTurn = (screenX / screenWidth) * 2f - 1f;
        }
        else
        {
            targetTurn = 0f;
        }
    }

    void FixedUpdate()
    {
        if (rb == null || currentShield <= 0) return;

        // 1. Anti-Gravity Hover Physics (360 Degree)
        Vector3 hitNormal = ApplyHoverPhysics();

        // Align ship UP to surface normal
        Vector3 currentUp = transform.up;
        Vector3 targetUp = hitNormal;
        
        Vector3 forward = -transform.forward; // Assuming -Z is forward like Cocos, or Z in Unity? Unity uses +Z as forward usually.
        // Let's adopt standard Unity +Z forward
        forward = transform.forward;
        
        Vector3 right = Vector3.Cross(targetUp, forward).normalized;
        Vector3 newForward = Vector3.Cross(right, targetUp).normalized;

        Quaternion targetQuat = Quaternion.LookRotation(newForward, targetUp);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetQuat, Time.fixedDeltaTime * 5.0f);

        // 2. Movement
        Vector3 forwardVel = transform.forward * forwardSpeed;
        
        currentTurn = Mathf.Lerp(currentTurn, targetTurn, Time.fixedDeltaTime * turnSpeed);
        Vector3 turnVel = transform.right * (currentTurn * turnSpeed);

        // Combine velocities
        Vector3 currentVelocity = rb.velocity;
        Vector3 localVelocity = forwardVel + turnVel;
        
        float verticalDot = Vector3.Dot(currentVelocity, targetUp);
        Vector3 verticalVel = targetUp * verticalDot;
        
        rb.velocity = localVelocity + verticalVel;
        
        // Bank the ship based on turn (visual only, ideally applied to a child mesh)
        // float bankAngle = -currentTurn * 30.0f;
        // transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y, bankAngle);
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

    public void ResetVelocity()
    {
        if (rb)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            targetTurn = 0f;
            currentTurn = 0f;
            currentShield = maxShield;
        }
    }

    public bool TakeDamage(int amount)
    {
        currentShield -= amount;
        if (currentShield <= 0)
        {
            currentShield = 0;
            return true;
        }
        return false;
    }
}
