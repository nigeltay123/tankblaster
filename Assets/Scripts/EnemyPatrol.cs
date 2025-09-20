using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyShooting))]
public class EnemyPatrol : MonoBehaviour
{
    public enum State { Patrol, Engage }

    [Header("Patrol")]
    public float patrolSpeed = 2.0f;
    public float randomTurnEvery = 1.5f;
    public float randomTurnAngle = 20f;

    [Header("Engage Player")]
    public float engageRange = 6.0f;   // start engaging when <= this
    public float giveUpRange = 8.0f;   // return to patrol when >= this
    public float stopDistance = 1.2f;  // stop moving when this close
    public float turnSpeedDeg = 540f;

    [Header("Refs")]
    public Transform target;           // assign from spawner or drag Player
    public void SetTarget(Transform t) => target = t;

    Rigidbody2D _rb;
    EnemyShooting _shooter;
    State _state = State.Patrol;
    float _randomTimer;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _shooter = GetComponent<EnemyShooting>();
    }

    void Update()
    {
        if (!target) { _state = State.Patrol; return; }

        float dist   = Vector2.Distance(target.position, transform.position);
        bool canSee  = _shooter.HasLineOfSight(target);  // only LOS check here

        if (_state == State.Patrol && dist <= engageRange && canSee) _state = State.Engage;
        else if (_state == State.Engage && (!canSee || dist >= giveUpRange)) _state = State.Patrol;
    }

    void FixedUpdate()
    {
        if (_state == State.Patrol) TickPatrol();
        else                        TickEngage();
    }

    void TickPatrol()
    {
        // random heading jitter
        _randomTimer += Time.fixedDeltaTime;
        if (_randomTimer >= randomTurnEvery)
        {
            _randomTimer = 0f;
            float jitter = Random.Range(-randomTurnAngle, randomTurnAngle);
            _rb.MoveRotation(_rb.rotation + jitter);
        }

        // move forward (UP is forward / firePoint front)
        _rb.velocity = transform.up * patrolSpeed;
    }

    void TickEngage()
    {
        if (!target) { TickPatrol(); return; }

        Vector2 toT = (Vector2)(target.position - transform.position);
        float dist = toT.magnitude;
        if (dist < 0.001f) { _rb.velocity = Vector2.zero; return; }

        // Rotate so UP (firePoint direction) faces player
        float desiredAngle = Mathf.Atan2(toT.y, toT.x) * Mathf.Rad2Deg - 90f;
        float newAngle = Mathf.MoveTowardsAngle(_rb.rotation, desiredAngle, turnSpeedDeg * Time.fixedDeltaTime);
        _rb.MoveRotation(newAngle);

        // advance until close enough
        float speed = (dist > stopDistance) ? patrolSpeed : 0f;
        _rb.velocity = transform.up * speed;

        // ask shooter to handle firing (it does its own range/LOS)
        _shooter.TryShootAt(target);
    }
}
