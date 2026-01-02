using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyStats))]
public class EnemyFlyingAI : EnemyBase
{
    public enum State { Idle, Patrol, Chase, Attack }
    public State currentState;

    [Header("References")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    private Transform player;
    private Rigidbody2D rb;
    private Animator anim;

    [Header("Movement Stats")]
    public float moveSpeed = 3f;
    public float chaseSpeed = 4.5f;
    public float acceleration = 5f; // Soft movement smoothing

    [Header("Patrol Settings")]
    public float patrolWaitTime = 1f;
    public Transform[] patrolPoints;

    [Header("Combat AI")]
    public float detectionRange = 9f;
    public float attackRange = 5f;
    public float keepDistance = 3f; // Don't get closer than this (hovering)
    public float heightOffset = 2f; // Try to fly this high above player

    [Header("Attack Settings")]
    public float fireRate = 2f;
    public float attackWindUp = 0.5f;
    private float nextFireTime;
    private int patrolIndex;
    private bool isProvoked = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        rb.gravityScale = 0f;

        EnemyStats myStats = GetComponent<EnemyStats>();
        if (myStats != null) myStats.OnTakeDamage += ReactToDamage; // Subscribe

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        currentState = State.Patrol;
        StartCoroutine(MainLogic());
    }

    void Update()
    {
        // Visual Rotation logic
        if (player != null && currentState == State.Chase || currentState == State.Attack)
        {
            // Face Player
            float dir = Mathf.Sign(player.position.x - transform.position.x);
            transform.localScale = new Vector3(dir, 1, 1);
        }
        else if (rb.linearVelocity.x != 0)
        {
            // Face Movement Direction
            float dir = Mathf.Sign(rb.linearVelocity.x);
            transform.localScale = new Vector3(dir, 1, 1);
        }
    }

    void ReactToDamage()
    {
        // Wake up the bat!
        if (currentState != State.Chase && currentState != State.Attack)
        {
            currentState = State.Chase;
            StopCoroutine("ResetProvocation");
            StartCoroutine(ResetProvocation());
        }
    }

    IEnumerator ResetProvocation()
    {
        isProvoked = true;
        yield return new WaitForSeconds(5.0f); // Flying enemies chase for a long time
        isProvoked = false;
    }

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
            // Just hover in place if no points
            while (currentState == State.Patrol)
            {
                if (Vector2.Distance(transform.position, player.position) < detectionRange)
                    currentState = State.Chase;

                yield return new WaitForSeconds(0.2f);
            }
            yield break;
        }

        Transform target = patrolPoints[patrolIndex];

        while (currentState == State.Patrol)
        {
            // 1. Check for Player
            if (Vector2.Distance(transform.position, player.position) < detectionRange)
            {
                currentState = State.Chase;
                yield break;
            }

            // 2. Move to Point
            MoveToPosition(target.position, moveSpeed);

            // 3. Check if reached
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

            // 1. Lost Player
            if (!isProvoked && dist > detectionRange * 1.5f)
            {
                currentState = State.Patrol;
                yield break;
            }

            // 2. Combat Logic
            if (dist < attackRange && Time.time >= nextFireTime)
            {
                currentState = State.Attack;
                yield break;
            }

            // 3. Movement Logic (Hover above player)
            // Target position is Player X, but Player Y + Height Offset
            Vector2 targetPos = new Vector2(player.position.x, player.position.y + heightOffset);

            // If we are too close horizontally, back off slightly?
            // Actually, for flying, just smoothing towards that top point is usually enough.

            MoveToPosition(targetPos, chaseSpeed);

            yield return null;
        }
    }

    IEnumerator AttackRoutine()
    {
        // Slow down while attacking
        rb.linearVelocity = rb.linearVelocity * 0.5f;

        if (anim) anim.SetTrigger("Attack");

        yield return new WaitForSeconds(attackWindUp);

        Shoot();

        float cooldown = 1f / fireRate;
        nextFireTime = Time.time + cooldown;

        yield return new WaitForSeconds(0.5f); // Short pause after shooting

        currentState = State.Chase;
    }

    // --- PHYSICS MOVEMENT ---
    void MoveToPosition(Vector2 target, float speed)
    {
        // Smooth force movement for flying enemies feels better than direct velocity set
        Vector2 dir = (target - (Vector2)transform.position).normalized;
        Vector2 force = dir * speed * acceleration;

        rb.AddForce(force);

        // Cap speed
        if (rb.linearVelocity.magnitude > speed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * speed;
        }
    }

    void Shoot()
    {
        if (projectilePrefab == null || firePoint == null) return;

        GameObject bullet = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        Vector2 dir = (player.position - firePoint.position).normalized;

        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            bulletRb.linearVelocity = dir * 8f; // Bullet speed
        }

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}