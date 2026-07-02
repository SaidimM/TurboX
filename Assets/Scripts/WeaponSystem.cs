using UnityEngine;

public class WeaponSystem : MonoBehaviour
{
    public GameObject missilePrefab;
    public GameObject minePrefab;
    public float missileCooldown = 2.0f;
    public float mineCooldown = 2.0f;

    private float lastMissileTime = 0f;
    private float lastMineTime = 0f;

    public void FireMissile(Transform target = null)
    {
        if (Time.time - lastMissileTime < missileCooldown) return;

        if (missilePrefab != null)
        {
            Vector3 spawnPos = transform.position + transform.forward * 3f;
            GameObject missileObj = Instantiate(missilePrefab, spawnPos, transform.rotation);
            
            Projectile projectile = missileObj.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.InitAsMissile(target);
            }

            lastMissileTime = Time.time;
        }
    }

    public void DropMine()
    {
        if (Time.time - lastMineTime < mineCooldown) return;

        if (minePrefab != null)
        {
            Vector3 spawnPos = transform.position - transform.forward * 3f;
            GameObject mineObj = Instantiate(minePrefab, spawnPos, transform.rotation);
            
            Projectile projectile = mineObj.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.InitAsMine();
            }

            lastMineTime = Time.time;
        }
    }
}
