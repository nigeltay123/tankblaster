using UnityEngine;

public class EnemyShooting : MonoBehaviour
{
    [Header("Shooting")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireRate = 1.5f;

    [Header("Rules")]
    public float shootRange = 6.0f;        // max distance to shoot
    public LayerMask wallMask;             // walls/obstacles for LOS
    public float aimInaccuracy = 4f;       // ± degrees
    public float fireAngleTolerance = 6f;  // only shoot when roughly aligned

    float _cooldown;

    void Update()
    {
        if (_cooldown > 0f) _cooldown -= Time.deltaTime;
    }

    public void TryShootAt(Transform target)
    {
        if (!target || !firePoint || !bulletPrefab) return;

        // simple distance gate
        float dist = Vector2.Distance(target.position, transform.position);
        if (dist > shootRange) return;

        // line of sight gate
        if (!HasLineOfSight(target)) return;

        // optional alignment gate (use body's UP vs desired angle)
        Vector2 toT = (Vector2)(target.position - transform.position);
        float desiredAngle = Mathf.Atan2(toT.y, toT.x) * Mathf.Rad2Deg - 90f;
        float angleErr = Mathf.DeltaAngle(transform.eulerAngles.z, desiredAngle);
        if (Mathf.Abs(angleErr) > fireAngleTolerance) return;

        // cooldown
        if (_cooldown > 0f) return;
        _cooldown = fireRate;

        // fire with small jitter
        float jitter = Random.Range(-aimInaccuracy, aimInaccuracy);
        Quaternion shotRot = firePoint.rotation * Quaternion.Euler(0, 0, jitter);
        Instantiate(bulletPrefab, firePoint.position, shotRot);
    }

    public bool HasLineOfSight(Transform t)
    {
        if (!t || !firePoint) return false;
        Vector2 from = firePoint.position;
        Vector2 to = t.position;
        Vector2 dir = to - from;
        float len = dir.magnitude;
        if (len < 0.001f) return true;
        dir /= len;
        return !Physics2D.Raycast(from, dir, len, wallMask);
    }
}
