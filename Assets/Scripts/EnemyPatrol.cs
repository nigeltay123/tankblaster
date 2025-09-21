using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyShooting))]
public class EnemyPatrol : MonoBehaviour
{
    public enum State { Patrol, Engage }

    [Header("Patrol Movement")]
    public float patrolSpeed = 2.6f;
    public float wanderTurnDegPerSec = 70f;   // smoother wander
    public float turnSpeedDeg = 420f;         // normal turn rate

    [Header("Wall Avoidance")]
    public LayerMask wallMask;
    public float frontProbe = 1.4f;
    public float sideOffset = 0.35f;
    public float sideProbe = 1.1f;
    public float avoidTurnBoost = 240f;

    [Header("Corner Sliding")]
    public float cornerCastDistance = 0.6f;
    public Vector2 cornerCastBoxScale = new Vector2(0.9f, 1.2f);
    public float cornerAlignStrength = 300f;

    [Header("Separation (vs other enemies)")]
    public float separationRadius = 1.2f;
    public float separationPush = 3f;

    [Header("Stuck Handling")]
    public float stuckSpeedEps = 0.05f;
    public float stuckTime = 0.35f;
    public float unstickDuration = 0.35f;
    public float unstickBackSpeed = 2.2f;
    public float unstickTurnDegPerSec = 480f;
    [Tooltip("If nose is almost touching & slow, trigger a hard reverse escape.")]
    public float hardEscapeFrontDist = 0.25f;
    public float hardEscapeTurnBoost = 2.0f;   // x turn speed while escaping
    public float hardEscapeAngle = 90f;        // approx degrees to rotate out

    [Header("Engage Player")]
    public float engageRange = 6.0f;
    public float giveUpRange = 8.0f;
    public float stopDistance = 1.2f;

    [Header("Refs")]
    public Transform target;
    public void SetTarget(Transform t) => target = t;

    // internals
    Rigidbody2D _rb;
    EnemyShooting _shooter;
    Collider2D _col;
    State _state = State.Patrol;
    float _noiseSeedX, _noiseSeedY;
    float _lowSpeedTimer, _unstickTimer;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        _shooter = GetComponent<EnemyShooting>();
        _col = GetComponent<Collider2D>();
        _noiseSeedX = Random.Range(0f, 1000f);
        _noiseSeedY = Random.Range(0f, 1000f);
    }

    void Update()
    {
        if (!target) { _state = State.Patrol; return; }
        float dist = Vector2.Distance(target.position, transform.position);
        bool canSee = _shooter.HasLineOfSight(target);

        if (_state == State.Patrol && dist <= engageRange && canSee) _state = State.Engage;
        else if (_state == State.Engage && (!canSee || dist >= giveUpRange)) _state = State.Patrol;
    }

    void FixedUpdate()
    {
        if (_state == State.Patrol) TickPatrol();
        else                        TickEngage();
    }

    // ---------------- PATROL ----------------
    void TickPatrol()
    {
        float dt = Time.fixedDeltaTime;

        // HARD ESCAPE: nose almost touching + slow => reverse & big turn, skip rest
        if (HardEscapeIfPinned())
            return;

        // 1) Smooth wander (Perlin)
        float n = (Mathf.PerlinNoise(_noiseSeedX + Time.time * 0.35f, _noiseSeedY) - 0.5f) * 2f;
        float wanderTurn = n * wanderTurnDegPerSec;

        // 2) Whisker avoidance
        float avoidTurn = ComputeAvoidanceTurn();

        // 3) Corner prediction & sliding
        RaycastHit2D cornerHit;
        float cornerTurn = ComputeCornerTurn(out cornerHit);

        // 4) Stuck logic (gentle reverse wobble)
        float speedNow = _rb.velocity.magnitude;
        if (speedNow < stuckSpeedEps) _lowSpeedTimer += dt; else _lowSpeedTimer = 0f;

        bool doingUnstick = false;
        if (_unstickTimer > 0f) { _unstickTimer -= dt; doingUnstick = true; }
        else if (_lowSpeedTimer >= stuckTime) { _unstickTimer = unstickDuration; _lowSpeedTimer = 0f; doingUnstick = true; }

        // 5) Rotation
        float desiredAngle = _rb.rotation + (wanderTurn + avoidTurn + cornerTurn) * dt;
        if (doingUnstick) desiredAngle += unstickTurnDegPerSec * dt * RandomSign();
        float newAngle = Mathf.MoveTowardsAngle(_rb.rotation, desiredAngle, turnSpeedDeg * dt);
        _rb.MoveRotation(newAngle);

        // 6) Velocity (with corner slide)
        if (doingUnstick)
            _rb.velocity = (Vector2)(-transform.up) * unstickBackSpeed;
        else
            _rb.velocity = VelocityWithCornerSlide(patrolSpeed);

        // 7) Separation
        ApplySeparation();
    }

    // ---------------- ENGAGE ----------------
    void TickEngage()
    {
        if (!target) { TickPatrol(); return; }

        // HARD ESCAPE first
        if (HardEscapeIfPinned())
            return;

        float dt = Time.fixedDeltaTime;
        Vector2 toT = (Vector2)(target.position - transform.position);
        float dist = toT.magnitude;
        if (dist < 0.001f) { _rb.velocity = Vector2.zero; return; }

        // Rotate toward target, but still bias by avoidance/corner steering
        float targetAngle = Mathf.Atan2(toT.y, toT.x) * Mathf.Rad2Deg - 90f;
        float avoidTurn = ComputeAvoidanceTurn();

        RaycastHit2D cornerHit;
        float cornerTurn = ComputeCornerTurn(out cornerHit);

        float desiredAngle = Mathf.MoveTowardsAngle(_rb.rotation, targetAngle, turnSpeedDeg * dt);
        desiredAngle += (avoidTurn + cornerTurn) * dt;
        float newAngle = Mathf.MoveTowardsAngle(_rb.rotation, desiredAngle, turnSpeedDeg * dt);
        _rb.MoveRotation(newAngle);

        // Approach player; slide when blocked
        float speed = (dist > stopDistance) ? patrolSpeed : 0f;
        _rb.velocity = VelocityWithCornerSlide(speed);

        // Separation while chasing
        ApplySeparation();

        // Fire when possible
        _shooter.TryShootAt(target);
    }

    // ---------------- HARD ESCAPE ----------------
    // If front ray sees a wall very close AND we are slow, back up and turn out strongly.
    bool HardEscapeIfPinned()
    {
        RaycastHit2D frontHit = Physics2D.Raycast(_rb.position, transform.up, frontProbe * 0.5f, wallMask);
        if (frontHit && _rb.velocity.magnitude < 0.2f && frontHit.distance <= hardEscapeFrontDist)
        {
            // reverse
            _rb.velocity = -transform.up * (unstickBackSpeed * 1.2f);

            // fast rotate ~90° to a random side
            float escapeAngle = _rb.rotation + (RandomSign() * hardEscapeAngle);
            float fastTurn = turnSpeedDeg * hardEscapeTurnBoost * Time.fixedDeltaTime;
            float newAngle = Mathf.MoveTowardsAngle(_rb.rotation, escapeAngle, fastTurn);
            _rb.MoveRotation(newAngle);
            return true; // skip normal logic this frame
        }
        return false;
    }

    // ---------------- HELPERS ----------------
    float ComputeAvoidanceTurn()
    {
        float turn = 0f;
        Vector2 pos = _rb.position;
        Vector2 fwd = transform.up;
        Vector2 right = transform.right;

        // Center probe (+ hard bias if very close)
        RaycastHit2D hit = Physics2D.Raycast(pos, fwd, frontProbe, wallMask);
        if (hit)
        {
            if (hit.distance < 0.25f)
            {
                float sign = Vector2.SignedAngle(fwd, hit.normal) > 0 ? 1f : -1f;
                turn += sign * avoidTurnBoost * 3f;
            }
            else
            {
                bool hitL = Physics2D.Raycast(pos - right * 0.15f, fwd, frontProbe * 0.9f, wallMask);
                bool hitR = Physics2D.Raycast(pos + right * 0.15f, fwd, frontProbe * 0.9f, wallMask);
                float bias = hitL && !hitR ? -1f : (!hitL && hitR ? 1f : RandomSign());
                turn += avoidTurnBoost * bias;
            }
        }

        // Side whiskers
        Vector2 lp = pos - right * sideOffset;
        Vector2 rp = pos + right * sideOffset;
        var lHit = Physics2D.Raycast(lp, fwd, sideProbe, wallMask);
        var rHit = Physics2D.Raycast(rp, fwd, sideProbe, wallMask);
        if (lHit)
        {
            float closeness = 1f - Mathf.Clamp01(lHit.distance / Mathf.Max(0.01f, sideProbe));
            turn -= avoidTurnBoost * closeness;
        }
        if (rHit)
        {
            float closeness = 1f - Mathf.Clamp01(rHit.distance / Mathf.Max(0.01f, sideProbe));
            turn += avoidTurnBoost * closeness;
        }
        return turn;
    }

    float ComputeCornerTurn(out RaycastHit2D hit)
    {
        hit = default;
        if (!_col) return 0f;

        Bounds b = _col.bounds;
        Vector2 size = new Vector2(b.size.x * cornerCastBoxScale.x, b.size.y * cornerCastBoxScale.y);

        hit = Physics2D.BoxCast(_rb.position, size, _rb.rotation, transform.up, cornerCastDistance, wallMask);
        if (!hit) return 0f;

        Vector2 nrm = hit.normal.normalized;
        Vector2 tangent = new Vector2(-nrm.y, nrm.x);
        float desired = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg - 90f;
        float delta = Mathf.DeltaAngle(_rb.rotation, desired);
        return Mathf.Clamp(delta, -cornerAlignStrength, cornerAlignStrength);
    }

    float FrontHitSlowFactor()
    {
        Vector2 pos = _rb.position;
        Vector2 fwd = transform.up;
        var hit = Physics2D.Raycast(pos, fwd, frontProbe, wallMask);
        if (!hit) return 1f;
        float closeness = 1f - Mathf.Clamp01(hit.distance / Mathf.Max(0.01f, frontProbe));
        return Mathf.Lerp(1f, 0.45f, closeness);
    }

    Vector2 VelocityWithCornerSlide(float baseSpeed)
    {
        RaycastHit2D h;
        ComputeCornerTurn(out h);
        Vector2 v = transform.up * (baseSpeed * FrontHitSlowFactor());
        if (h)
        {
            Vector2 nrm = h.normal.normalized;
            Vector2 tangent = new Vector2(-nrm.y, nrm.x);
            float sign = Vector2.SignedAngle(transform.up, tangent) >= 0 ? 1f : -1f;
            tangent *= sign;
            v = Vector2.Dot(v, tangent) * tangent.normalized;
            if (v.sqrMagnitude < 0.01f) v = tangent.normalized * baseSpeed * 0.6f;
        }
        return v;
    }

    void ApplySeparation()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, separationRadius);
        foreach (var h in hits)
        {
            if (h.attachedRigidbody != null && h.attachedRigidbody != _rb)
            {
                Vector2 away = (Vector2)(transform.position - h.transform.position);
                float dist = away.magnitude;
                if (dist > 0.01f && dist < separationRadius)
                {
                    away /= dist;
                    _rb.velocity += away * (separationPush * (1f - dist / separationRadius));
                }
            }
        }
    }

    static int RandomSign() => Random.value < 0.5f ? -1 : 1;

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!_rb) _rb = GetComponent<Rigidbody2D>();
        if (!_col) _col = GetComponent<Collider2D>();

        Gizmos.color = Color.cyan;
        Vector3 pos = _rb ? (Vector3)_rb.position : transform.position;
        Vector3 fwd = transform.up;
        Vector3 right = transform.right;

        // whiskers
        Gizmos.DrawLine(pos, pos + fwd * frontProbe);
        Gizmos.DrawLine(pos - right * sideOffset, pos - right * sideOffset + fwd * sideProbe);
        Gizmos.DrawLine(pos + right * sideOffset, pos + right * sideOffset + fwd * sideProbe);

        // corner box preview
        if (_col)
        {
            Bounds b = _col.bounds;
            Vector2 size = new Vector2(b.size.x * cornerCastBoxScale.x, b.size.y * cornerCastBoxScale.y);
            Matrix4x4 m = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(pos + fwd * cornerCastDistance,
                Quaternion.Euler(0, 0, _rb ? _rb.rotation : transform.eulerAngles.z), Vector3.one);
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.7f);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(size.x, size.y, 0.01f));
            Gizmos.matrix = m;
        }

        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, engageRange);
        Gizmos.color = Color.gray;   Gizmos.DrawWireSphere(transform.position, giveUpRange);
    }
#endif
}
