using UnityEngine;

public class PlayerTankShooting : MonoBehaviour
{
    [Header("References")]
    public Transform firePoint;           // the barrel tip
    public GameObject blueBulletPrefab;   // the bullet prefab

    [Header("Fire Settings")]
    public float fireRate = 6f;           // bullets per second (0 = unlimited)
    public bool autoFire = true;          // hold LMB if true

    private float nextFireTime;

    void Update()
    {
        bool pressed = autoFire ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);

        if (pressed && (fireRate <= 0f || Time.time >= nextFireTime))
        {
            Fire();
            if (fireRate > 0f)
                nextFireTime = Time.time + 1f / fireRate;
        }
    }

    void Fire()
    {
        if (!firePoint || !blueBulletPrefab) return;

        Instantiate(blueBulletPrefab, firePoint.position, firePoint.rotation);
    }
}
