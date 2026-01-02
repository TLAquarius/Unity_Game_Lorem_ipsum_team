using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyStats))]
public class EnemyFlyingAI : EnemyBase // INHERITS FROM ENEMYBASE
{
    public enum State { Idle, Patrol, Chase, Attack, Hit }
    public State currentState;

    [Header("Sprite Settings")]
    public bool spriteFacesLeft = true; // Check this if your sprite faces LEFT by default

    [Header("References")]
    public GameObject projectilePrefab; // Optional: Assign if it shoots
    public Transform firePoint;         // Optional: Where bullet comes out

    private Transform player;
    private Rigidbody2D rb;
    private Animator anim;
    private EnemyStats stats;

    [Header("Movement Stats")]
    public float moveSpeed = 3f;
    public float chaseSpeed = 4.5f;
    public float acceleration = 10f; // How fast it turns

    [Header("Patrol Settings")]
    public float patrolWaitTime = 1f;
    public Transform[] patrolPoints;

    [Header("Combat AI")]
    public float detectionRange = 9f;
    public float attackRange = 5f;  // Distance to start attacking/shooting
    public float keepDistance = 3f; // If shooting, stay this far away
    public float heightOffset = 2f; // Fly above player's head

    [Header("Attack Settings")]
    public float fireRate = 2f;
    public float attackWindUp = 0.5f;

    [Header("Hurt Settings")]
    public float hurtDuration = 0.5f;
    public Vector2 selfKnockback = new Vector2(3f, 3f);

    private float nextFireTime;
    private int patrolIndex;
    private bool isProvoked = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        stats = GetComponent<EnemyStats>();

        // IMPORTANT: Flying enemies must not have gravity!
        rb.gravityScale = 0f;

        if (stats != null) stats.OnTakeDamage += ReactToDamage;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        currentState = State.Patrol;
        StartCoroutine(MainLogic());
    }

    void Update()
    {
        // Visual Rotation logic (Face Player or Movement)
        if (anim != null)
        {
            // Just check if we are moving significantly
            anim.SetBool("isMoving", rb.linearVelocity.magnitude > 0.1f);
        }

        // Handle Facing Direction
        if (currentState == State.Chase || currentState == State.Attack)
        {
            if (player != null) FaceTarget(player.position);
        }
        else if (rb.linearVelocity.x != 0)
        {
            FaceTarget(transform.position + (Vector3)rb.linearVelocity);
        }
    }

    // --- SMART REACTION ---
    void ReactToDamage()
    {
        if (stats.currentHP <= 0) return;
        StopAllCoroutines();
        StartCoroutine(HurtRoutine());
    }

    IEnumerator HurtRoutine()
    {
        currentState = State.Hit;
        if (anim) anim.SetTrigger("Hurt");

        // 1. Apply Knockback (In the air)
        rb.linearVelocity = Vector2.zero;
        if (player != null)
        {
            float dirX = Mathf.Sign(transform.position.x - player.position.x);
            // Push up and away
            rb.AddForce(new Vector2(dirX * selfKnockback.x, selfKnockback.y), ForceMode2D.Impulse);
        }

        // 2. Wait
        yield return new WaitForSeconds(hurtDuration);

        // 3. Recover & Chase
        currentState = State.Chase;
        isProvoked = true;
        StartCoroutine(ResetProvocation());
        StartCoroutine(MainLogic());
    }

    IEnumerator ResetProvocation()
    {
        yield return new WaitForSeconds(5.0f); // Chase for 5 seconds
        isProvoked = false;
    }

    // --- MAIN LOOP ---
    IEnumerator MainLogic()
    {
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
            }
            yield return null;
        }
    }

    IEnumerator PatrolRoutine()
    {
        if (patrolPoints.Length == 0)
        {
            // Idle Hover if no points
            while (currentState == State.Patrol)
            {
                rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.deltaTime);
                if (Vector2.Distance(transform.position, player.position) < detectionRange)
                    currentState = State.Chase;
                yield return null;
            }
            yield break;
        }

        Transform target = patrolPoints[patrolIndex];

        while (currentState == State.Patrol)
        {
            if (Vector2.Distance(transform.position, player.position) < detectionRange)
            {
                currentState = State.Chase;
                yield break;
            }

            MoveToPosition(target.position, moveSpeed);

            if (Vector2.Distance(transform.position, target.position) < 0.5f)
            {
                rb.linearVelocity = Vector2.zero;
                yield return new WaitForSeconds(patrolWaitTime);
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                target = patrolPoints[patrolIndex];
            }
            yield return null;
        }
    }

    IEnumerator ChaseRoutine()
    {
        while (currentState == State.Chase)
        {
            float dist = Vector2.Distance(transform.position, player.position);

            // 1. Give up
            if (!isProvoked && dist > detectionRange * 1.5f)
            {
                currentState = State.Patrol;
                yield break;
            }

            // 2. Attack Range
            if (dist < attackRange && Time.time >= nextFireTime)
            {
                currentState = State.Attack;
                yield break;
            }

            // 3. Movement (Hover above player)
            // If we have a projectile, hover ABOVE. If melee (Bat), fly INTO player.
            Vector2 targetPos;

            if (projectilePrefab != null)
            {
                // Ranged Flyer: Hover above head
                targetPos = new Vector2(player.position.x, player.position.y + heightOffset);
            }
            else
            {
                // Melee Flyer (Bat): Fly directly at body
                targetPos = player.position;
            }

            MoveToPosition(targetPos, chaseSpeed);

            yield return null;
        }
    }

    IEnumerator AttackRoutine()
    {
        // Brake slightly
        rb.linearVelocity = rb.linearVelocity * 0.5f;

        if (anim) anim.SetTrigger("Attack");

        yield return new WaitForSeconds(attackWindUp);

        // If we have a bullet, shoot. If not, this is just a melee lunge animation.
        if (projectilePrefab != null)
        {
            Shoot();
        }
        else
        {
            // Bat Lunge: Dash at player
            Vector2 dir = (player.position - transform.position).normalized;
            rb.AddForce(dir * 10f, ForceMode2D.Impulse);
        }

        float cooldown = 1f / fireRate;
        nextFireTime = Time.time + cooldown;

        yield return new WaitForSeconds(0.5f);

        currentState = State.Chase;
    }

    // --- PHYSICS HELPERS ---
    void MoveToPosition(Vector2 target, float speed)
    {
        // Smooth physics movement
        Vector2 dir = (target - (Vector2)transform.position).normalized;

        // Accelerate towards target
        rb.AddForce(dir * speed * acceleration);

        // Clamp speed so they don't go supersonic
        if (rb.linearVelocity.magnitude > speed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * speed;
        }
    }

    void FaceTarget(Vector3 target)
    {
        float directionX = Mathf.Sign(target.x - transform.position.x);
        float scaleX = spriteFacesLeft ? -directionX : directionX;
        transform.localScale = new Vector3(scaleX, 1, 1);
    }

    void Shoot()
    {
        if (projectilePrefab == null || firePoint == null) return;

        GameObject bullet = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        Vector2 dir = (player.position - firePoint.position).normalized;

        // Calculate angle
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}