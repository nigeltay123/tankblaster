using UnityEngine;

public class BlueBullet : MonoBehaviour
{
    [Header("Motion")]
    public float speed = 12f;       // good starting value for PPU=16
    public float maxLifetime = 3f;

    [Header("Damage")]
    public int damage = 1;  // integer, works with Health.TakeDamage(int)
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

        // Damage if the target has Health
        var health = other.GetComponent<Health>();
        if (health != null)
        {
            health.TakeDamage(damage);
        }

        // Bullet always disappears on hit
        Destroy(gameObject);
    }
}
