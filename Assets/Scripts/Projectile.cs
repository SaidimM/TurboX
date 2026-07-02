using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 80.0f;
    public int damage = 40;
    public float lifetime = 5.0f;

    private bool isMine = false;
    private Transform target = null;
    private Vector3 forwardDir = Vector3.forward;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    public void InitAsMissile(Transform newTarget)
    {
        isMine = false;
        target = newTarget;
    }

    public void InitAsMine()
    {
        isMine = true;
    }

    void Update()
    {
        if (isMine) return;

        Vector3 moveDir = transform.forward;

        if (target != null)
        {
            moveDir = (target.position - transform.position).normalized;
            transform.rotation = Quaternion.LookRotation(moveDir);
        }

        transform.position += moveDir * speed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        bool hit = false;

        PlayerController playerCtrl = other.GetComponent<PlayerController>();
        if (playerCtrl != null)
        {
            playerCtrl.TakeDamage(damage);
            hit = true;
        }

        AIController aiCtrl = other.GetComponent<AIController>();
        if (aiCtrl != null)
        {
            aiCtrl.TakeDamage(damage);
            hit = true;
        }

        if (hit)
        {
            Destroy(gameObject);
        }
    }
}
