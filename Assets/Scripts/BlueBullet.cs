using UnityEngine;

public class BlueBullet : MonoBehaviour
{
    [Header("Motion")]
    public float speed = 12f;       // good starting value for PPU=16
    public float maxLifetime = 3f;

    [Header("Damage")]
    public float damage = 1f;
    [Tooltip("Layers this bullet can hit (e.g., Enemy, Walls).")]
    public LayerMask hitLayers;

    Rigidbody2D _rb;
    float _lifeTimer;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (_rb) _rb.gravityScale = 0f;
    }

    void OnEnable() => _lifeTimer = 0f;

    void Update()
    {
        // Assumes the barrel/firePoint points along +X (right)
        if (_rb) _rb.velocity = transform.right * speed;
        else     transform.position += transform.right * speed * Time.deltaTime;

        _lifeTimer += Time.deltaTime;
        if (_lifeTimer >= maxLifetime) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Ignore non-target layers
        if (((1 << other.gameObject.layer) & hitLayers) == 0) return;

        // Optional: deal damage if the target supports it
        var dmg = other.GetComponent<IDamageable>();
        if (dmg != null) dmg.TakeDamage(damage);

        Destroy(gameObject);
    }
}

// Optional interface for your enemies to implement
public interface IDamageable
{
    void TakeDamage(float amount);
}