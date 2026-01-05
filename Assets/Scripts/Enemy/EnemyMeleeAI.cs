using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyStats))]
public class EnemyMeleeAI : EnemyBase
{
    public enum State { Idle, Patrol, Chase, Attack, Hit }
    public State currentState;

    [Header("Sprite Settings")]
    public bool spriteFacesLeft = true;

    [Header("Class Settings")]
    public bool isTank = false;

    [Header("Hurt Settings")]
    public float hurtDuration = 0.5f;
    public Vector2 selfKnockback = new Vector2(3f, 0f);

    [Header("References")]
    public Transform attackPoint;
    public Transform groundCheck;
    public LayerMask detectionLayer;

    private Transform player;
    private Rigidbody2D rb;
    private Animator anim;
    private EnemyStats stats;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float patrolWaitTime = 2f;

    // --- CHANGED: Automatic Patrol Settings ---
    public float patrolRadius = 4f; // Distance to walk left/right from spawn
    private Vector2 originalPosition; // Remembers where we started

    [Header("Combat AI")]
    public float detectionRange = 6f;
    public float attackTriggerRange = 1.5f;
    public float attackRate = 1.5f;
    public float attackWindUp = 0.4f;
    public float attackRange = 0.8f;
    public float lungeForce = 5f;

    private float nextAttackTime;
    private bool isAttacking;
    private bool isProvoked = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        stats = GetComponent<EnemyStats>();

        rb.gravityScale = 1f;

        if (stats != null) stats.OnTakeDamage += ReactToDamage;

        // Save the spawn position automatically
        originalPosition = transform.position;

        currentState = State.Patrol;
        StartCoroutine(MainLogic());
    }

    void Update()
    {
        if (anim != null)
        {
            bool moving = Mathf.Abs(rb.linearVelocity.x) > 0.1f;
            anim.SetBool("isMoving", moving);
            anim.SetBool("isTank", isTank);
            HandleWalkSound(moving);
        }
    }

    void ReactToDamage()
    {
        StopAllCoroutines();
        StartCoroutine(HurtRoutine());
    }

    IEnumerator HurtRoutine()
    {
        PlayHitFeedback();
        currentState = State.Hit;
        isAttacking = false;
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
        yield return new WaitForSeconds(3.0f);
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

    // --- FIX: Automatic Patrol Logic ---
    IEnumerator PatrolRoutine()
    {
        // Define targets based on spawn point
        Vector2 leftTarget = originalPosition + Vector2.left * patrolRadius;
        Vector2 rightTarget = originalPosition + Vector2.right * patrolRadius;

        Vector2 currentTarget = rightTarget; // Start moving right

        while (currentState == State.Patrol)
        {
            // 1. Simple Distance Check (No fancy vision)
            float distToPlayer = Vector2.Distance(transform.position, player.position);
            if (distToPlayer < detectionRange)
            {
                currentState = State.Chase;
                yield break;
            }

            // 2. Cliff Check: If no ground, turn around immediately
            if (!CheckGroundAhead())
            {
                rb.linearVelocity = Vector2.zero;
                yield return new WaitForSeconds(0.5f);
                // Switch target
                if (currentTarget == rightTarget) currentTarget = leftTarget;
                else currentTarget = rightTarget;
            }
            else
            {
                MoveTowards(currentTarget, moveSpeed * 0.5f);
            }

            // 3. Reached Target Check (Using X distance only to prevent bugs)
            if (Mathf.Abs(transform.position.x - currentTarget.x) < 0.5f)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                yield return new WaitForSeconds(patrolWaitTime);

                // Switch target
                if (currentTarget == rightTarget) currentTarget = leftTarget;
                else currentTarget = rightTarget;
            }

            yield return null;
        }
    }

    IEnumerator ChaseRoutine()
    {
        while (currentState == State.Chase)
        {
            float dist = Vector2.Distance(transform.position, player.position);

            if (dist < attackTriggerRange && Time.time >= nextAttackTime)
            {
                rb.linearVelocity = Vector2.zero;
                currentState = State.Attack;
                yield break;
            }

            if (!isProvoked && dist > detectionRange * 1.5f)
            {
                currentState = State.Patrol;
                yield break;
            }

            if (CheckGroundAhead()) MoveTowards(player.position, moveSpeed);
            else rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

            yield return null;
        }
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;

        FaceTarget(player.position);

        if (anim) anim.SetTrigger("Attack");

        yield return new WaitForSeconds(attackWindUp);

        float distToPlayer = Vector2.Distance(transform.position, player.position);
        if (!isTank && distToPlayer > 1.0f)
        {
            float dir = Mathf.Sign(player.position.x - transform.position.x);
            rb.AddForce(new Vector2(dir * lungeForce, 0), ForceMode2D.Impulse);
        }

        yield return new WaitForSeconds(isTank ? 1.0f : 0.5f);

        nextAttackTime = Time.time + attackRate;
        isAttacking = false;
        currentState = State.Chase;
    }

    public void TriggerAttackHitbox()
    {
        if (attackPoint == null) return;

        Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, detectionLayer);

        foreach (Collider2D p in hitPlayers)
        {
            if (p.CompareTag("Player"))
            {
                float facingDir = Mathf.Sign(transform.localScale.x);
                if (spriteFacesLeft) facingDir *= -1;

                ApplyCustomKnockback(p.gameObject, attackDamage, facingDir);
            }
        }
    }

    // --- HELPER FUNCTIONS ---

    void MoveTowards(Vector2 target, float speed)
    {
        if (isAttacking) return;

        float directionX = Mathf.Sign(target.x - transform.position.x);
        float scaleX = spriteFacesLeft ? -directionX : directionX;

        transform.localScale = new Vector3(scaleX, 1, 1);
        rb.linearVelocity = new Vector2(directionX * speed, rb.linearVelocity.y);
    }

    void FaceTarget(Vector2 target)
    {
        float directionX = Mathf.Sign(target.x - transform.position.x);
        float scaleX = spriteFacesLeft ? -directionX : directionX;
        transform.localScale = new Vector3(scaleX, 1, 1);
    }

    bool CheckGroundAhead()
    {
        if (groundCheck == null) return true;
        RaycastHit2D hit = Physics2D.Raycast(groundCheck.position, Vector2.down, 1f, LayerMask.GetMask("Ground"));
        return hit.collider != null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Draw Patrol Range
        Gizmos.color = Color.green;
        Vector3 center = Application.isPlaying ? (Vector3)originalPosition : transform.position;
        Gizmos.DrawLine(center + Vector3.left * patrolRadius, center + Vector3.right * patrolRadius);

        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }
}