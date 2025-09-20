using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Stats")]
    public float maxHP = 3f;
    public float moveSpeed = 2.2f;
    public float stopDistance = 1.1f;   // don't sit on top of the player

    Transform _target;
    float _hp;

    void Awake()
    {
        _hp = maxHP;
    }

    // Spawner calls this to set target and apply difficulty scaling
    public void Init(Transform player, float hpScale = 1f, float speedScale = 1f)
    {
        _target = player;
        _hp = maxHP * hpScale;
        moveSpeed *= speedScale;
    }

    void Update()
    {
        if (_target == null) return;

        Vector2 to = _target.position - transform.position;
        float d = to.magnitude;

        if (d > stopDistance)
        {
            Vector2 dir = to.normalized;
            transform.position += (Vector3)(dir * moveSpeed * Time.deltaTime);

            // face movement direction (optional)
            float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, ang);
        }
    }

    public void TakeDamage(int amount)
    {
        _hp -= amount;
        if (_hp <= 0f) Die();
    }

    void Die()
    {
        EnemySpawner.NotifyEnemyDied();   // let the spawner know
        Destroy(gameObject);
    }
}