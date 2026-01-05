using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyStats))]
public class EnemyRangedAI : EnemyBase
{
    public enum State { Idle, Patrol, Chase, Attack, Hit }
    public State currentState;

    [Header("Sprite Settings")]
    public bool spriteFacesLeft = true;

    [Header("References")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public Transform groundCheck;
    public LayerMask obstacleLayer;  // Set to "Ground"
    public LayerMask detectionLayer; // Set to "Player"

    private Transform player;
    private Rigidbody2D rb;
    private Animator anim;
    private EnemyStats stats;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float patrolWaitTime = 2f;

    // --- AUTOMATIC PATROL ---
    public float patrolRadius = 6f;
    private Vector2 originalPosition;

    [Header("Combat Ranges")]
    public float detectionRange = 10f;
    public float verticalDetectionRange = 2f; // Don't shoot floors above/below
    public float shootingRange = 6f;   // Distance to stop and shoot
    public float retreatRange = 3f;    // Distance to run away

    [Header("Attack Settings")]
    public float fireRate = 2f;
    public float attackWindUp = 0.5f;

    [Header("Hurt Settings")]
    public float hurtDuration = 0.5f;
    public Vector2 selfKnockback = new Vector2(3f, 2f);

    private float nextFireTime;
    private bool isProvoked = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        stats = GetComponent<EnemyStats>();

        rb.gravityScale = 1f;

        if (stats != null) stats.OnTakeDamage += ReactToDamage;

        // Capture spawn point
        originalPosition = transform.position;

        currentState = State.Patrol;
        StartCoroutine(MainLogic());
    }

    void Update()
    {
        if (anim != null)
        {
            // Simple check: Are we moving?
            bool moving = Mathf.Abs(rb.linearVelocity.x) > 0.1f;
            anim.SetBool("isMoving", moving);
            HandleWalkSound(moving);
        }
    }

    void ReactToDamage()
    {
        if (stats.currentHP <= 0) return;
        StopAllCoroutines();
        StartCoroutine(HurtRoutine());
    }

    IEnumerator HurtRoutine()
    {
        PlayHitFeedback();
        currentState = State.Hit;
        if (anim) anim.SetTrigger("Hurt");

        rb.linearVelocity = Vector2.zero;
        if (player != null)
        {
            float dirX = Mathf.Sign(transform.position.x - player.position.x);
            rb.AddForce(new Vector2(dirX * selfKnockback.x, selfKnockback.y), ForceMode2D.Impulse);
        }

        yield return new WaitForSeconds(hurtDuration);

        currentState = State.Chase;
        isProvoked = true;
        StartCoroutine(ResetProvocation());
        StartCoroutine(MainLogic());
    }

    IEnumerator ResetProvocation()
    {
        yield return new WaitForSeconds(4.0f);
        isProvoked = false;
    }

    IEnumerator MainLogic()
    {
        while (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            else yield return new WaitForSeconds(0.5f);
        }

        while (true)
        {
            if (player == null) yield break;

            switch (currentState)
            {
                case State.Patrol:
                    yield return StartCoroutine(PatrolRoutine());
                    break;
                case State.Chase:
                    yield return StartCoroutine(ChaseRoutine());
                    break;
                case State.Attack:
                    yield return StartCoroutine(AttackRoutine());
                    break;
                case State.Hit:
                    yield return null;
                    break;
            }
            yield return null;
        }
    }

    // --- AUTOMATIC PATROL LOGIC ---
    IEnumerator PatrolRoutine()
    {
        Vector2 leftTarget = originalPosition + Vector2.left * patrolRadius;
        Vector2 rightTarget = originalPosition + Vector2.right * patrolRadius;
        Vector2 currentTarget = rightTarget;

        while (currentState == State.Patrol)
        {
            // 1. Detection Check
            float dist = Vector2.Distance(transform.position, player.position);
            float yDiff = Mathf.Abs(transform.position.y - player.position.y);

            if (dist < detectionRange && yDiff < verticalDetectionRange)
            {
                currentState = State.Chase;
                yield break;
            }

            // 2. Move Logic with Safety
            bool groundAhead = CheckGroundAhead();
            bool wallAhead = CheckWallAhead();

            if (!groundAhead || wallAhead)
            {
                rb.linearVelocity = Vector2.zero;
                yield return new WaitForSeconds(0.5f);
                // Turn around
                if (currentTarget == rightTarget) currentTarget = leftTarget;
                else currentTarget = rightTarget;
            }
            else
            {
                MoveTowards(currentTarget, moveSpeed * 0.5f);
            }

            // 3. Arrival Check (X-axis only)
            if (Mathf.Abs(transform.position.x - currentTarget.x) < 0.5f)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                yield return new WaitForSeconds(patrolWaitTime);
                if (currentTarget == rightTarget) currentTarget = leftTarget;
                else currentTarget = rightTarget;
            }
            yield return null;
        }
    }

    // --- KITING LOGIC ---
    IEnumerator ChaseRoutine()
    {
        while (currentState == State.Chase)
        {
            float dist = Vector2.Distance(transform.position, player.position);

            // Give up if too far
            if (!isProvoked && dist > detectionRange * 1.5f)
            {
                currentState = State.Patrol;
                yield break;
            }

            // A. TOO CLOSE? RUN AWAY (Retreat)
            if (dist < retreatRange)
            {
                // Only run back if there is ground behind us
                if (CheckGroundBackwards() && !CheckWallBackwards())
                {
                    MoveAway(player.position, moveSpeed);
                }
                else
                {
                    // Cornered! Stand ground and fight
                    rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                    if (Time.time >= nextFireTime)
                    {
                        currentState = State.Attack;
                        yield break;
                    }
                }
            }
            // B. TOO FAR? CHASE
            else if (dist > shootingRange)
            {
                if (CheckGroundAhead() && !CheckWallAhead())
                {
                    MoveTowards(player.position, moveSpeed);
                }
                else
                {
                    rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                }
            }
            // C. PERFECT RANGE? ATTACK
            else
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                if (Time.time >= nextFireTime)
                {
                    currentState = State.Attack;
                    yield break;
                }
            }

            yield return null;
        }
    }

    IEnumerator AttackRoutine()
    {
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        FaceTarget(player.position);

        if (anim) anim.SetTrigger("Attack");

        yield return new WaitForSeconds(attackWindUp);

        Shoot();

        float cooldown = 1f / fireRate;
        nextFireTime = Time.time + cooldown;

        yield return new WaitForSeconds(0.5f); // Recovery pose

        currentState = State.Chase;
    }

    // --- HELPERS ---

    void Shoot()
    {
        if (bulletPrefab == null || firePoint == null) return;

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

        Vector2 dir = (player.position - firePoint.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    void MoveTowards(Vector2 target, float speed)
    {
        float directionX = Mathf.Sign(target.x - transform.position.x);
        float scaleX = spriteFacesLeft ? -directionX : directionX;
        transform.localScale = new Vector3(scaleX, 1, 1);
        rb.linearVelocity = new Vector2(directionX * speed, rb.linearVelocity.y);
    }

    void MoveAway(Vector2 target, float speed)
    {
        // Calculate direction AWAY from target
        float directionX = Mathf.Sign(transform.position.x - target.x);

        // Face the player while backing up (Strafing)
        FaceTarget(target);

        rb.linearVelocity = new Vector2(directionX * speed, rb.linearVelocity.y);
    }

    void FaceTarget(Vector2 target)
    {
        float directionX = Mathf.Sign(target.x - transform.position.x);
        float scaleX = spriteFacesLeft ? -directionX : directionX;
        transform.localScale = new Vector3(scaleX, 1, 1);
    }

    // --- RAYCAST CHECKS ---

    bool CheckGroundAhead()
    {
        if (groundCheck == null) return true;
        return Physics2D.Raycast(groundCheck.position, Vector2.down, 1.5f, obstacleLayer);
    }

    bool CheckWallAhead()
    {
        if (groundCheck == null) return false;
        // Check in direction of movement (Scale determines facing)
        float facingDir = Mathf.Sign(transform.localScale.x);
        if (spriteFacesLeft) facingDir *= -1;

        return Physics2D.Raycast(transform.position, Vector2.right * facingDir, 1f, obstacleLayer);
    }

    bool CheckGroundBackwards()
    {
        if (groundCheck == null) return true;
        // Determine "Backward" direction based on current facing
        float facingDir = Mathf.Sign(transform.localScale.x);
        if (spriteFacesLeft) facingDir *= -1;

        // Check slightly behind the feet
        Vector2 checkPos = (Vector2)groundCheck.position - new Vector2(facingDir * 0.8f, 0); // 0.8f offset behind

        return Physics2D.Raycast(checkPos, Vector2.down, 1.5f, obstacleLayer);
    }

    bool CheckWallBackwards()
    {
        if (groundCheck == null) return false;
        float facingDir = Mathf.Sign(transform.localScale.x);
        if (spriteFacesLeft) facingDir *= -1;

        // Check Ray backwards
        return Physics2D.Raycast(transform.position, Vector2.right * -facingDir, 1f, obstacleLayer);
    }

    void OnDrawGizmosSelected()
    {
        // Patrol Range
        Gizmos.color = Color.green;
        Vector3 center = Application.isPlaying ? (Vector3)originalPosition : transform.position;
        Gizmos.DrawLine(center + Vector3.left * patrolRadius, center + Vector3.right * patrolRadius);

        // Ranges
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange); // Vision
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, shootingRange); // Attack
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, retreatRange); // Run Away
    }
}