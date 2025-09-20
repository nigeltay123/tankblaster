using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    [Header("Motion")]
    public float speed = 8f;        // slightly slower than player bullet
    public float maxLifetime = 3f;

    [Header("Damage")]
    public int damage = 1;
    public LayerMask hitLayers;     // should include "Player" + "Wall"

    private Rigidbody2D _rb;
    private float _lifeTimer;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (_rb) _rb.gravityScale = 0f;
    }

    void OnEnable() => _lifeTimer = 0f;

    void Update()
    {
        // Always move along +X (local right)
        if (_rb) _rb.velocity = transform.right * speed;
        else     transform.position += transform.right * speed * Time.deltaTime;

        _lifeTimer += Time.deltaTime;
        if (_lifeTimer >= maxLifetime) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Only interact with layers we care about
        if (((1 << other.gameObject.layer) & hitLayers) == 0) return;

        // Deal damage if the object has Health
        var hp = other.GetComponent<Health>();
        if (hp != null) hp.TakeDamage(damage);

        Destroy(gameObject);
    }
}
