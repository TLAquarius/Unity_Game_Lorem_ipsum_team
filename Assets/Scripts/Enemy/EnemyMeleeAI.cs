using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyStats))]
public class EnemyMeleeAI : EnemyBase // INHERITS FROM ENEMYBASE
{
    public enum State { Idle, Patrol, Chase, Attack, Hit }
    public State currentState;

    [Header("Class Settings")]
    public bool isTank = false; // Check this for Golems/Knights

    [Header("References")]
    public Transform attackPoint;
    public Transform groundCheck;
    public LayerMask detectionLayer; // Set to "Player"

    private Transform player;
    private Rigidbody2D rb;
    private Animator anim;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float patrolWaitTime = 2f;
    public Transform[] patrolPoints;

    [Header("Combat AI")]
    public float detectionRange = 6f;
    public float attackTriggerRange = 1.5f;
    public float attackRate = 1.5f;
    public float attackWindUp = 0.4f; // Telegraph time
    public float attackRange = 0.8f;  // Hitbox size
    public float lungeForce = 5f;     // Forward jump during attack

    private float nextAttackTime;
    private int patrolIndex;
    private bool isAttacking;
    private bool isProvoked = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        // Get the stats component
        EnemyStats myStats = GetComponent<EnemyStats>();
        if (myStats != null)
        {
            // Subscribe to the damage event
            myStats.OnTakeDamage += ReactToDamage;
        }

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        currentState = State.Patrol;
        StartCoroutine(MainLogic());
    }

    void Update()
    {
        if (anim != null)
        {
            anim.SetBool("isMoving", Mathf.Abs(rb.linearVelocity.x) > 0.1f);
            anim.SetBool("isTank", isTank);
        }
    }

    void ReactToDamage()
    {
        // If we are already dead, do nothing (optional check depending on your death logic)
        if (currentState == State.Idle || currentState == State.Patrol)
        {
            // Force the enemy to turn and face the player immediately
            if (player != null) FaceTarget(player.position);

            // Switch to Chase
            currentState = State.Chase;

            // Provoke them: They will chase for 3 seconds even if player is far away
            StopCoroutine("ResetProvocation");
            StartCoroutine(ResetProvocation());
        }
    }

    IEnumerator ResetProvocation()
    {
        isProvoked = true;
        yield return new WaitForSeconds(3.0f); // How long they stay angry without seeing player
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
        if (patrolPoints.Length == 0) { currentState = State.Idle; yield break; }
        Transform target = patrolPoints[patrolIndex];

        while (currentState == State.Patrol)
        {
            if (Vector2.Distance(transform.position, player.position) < detectionRange)
            {
                currentState = State.Chase;
                yield break;
            }

            MoveTowards(target.position, moveSpeed * 0.5f);

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

            if (dist < attackTriggerRange && Time.time >= nextAttackTime)
            {
                currentState = State.Attack;
                yield break;
            }

            // MODIFIED LOGIC: Only stop chasing if NOT provoked AND too far
            if (!isProvoked && dist > detectionRange * 1.5f)
            {
                currentState = State.Patrol;
                yield break;
            }

            if (CheckGroundAhead()) MoveTowards(player.position, moveSpeed);
            else rb.linearVelocity = Vector2.zero;

            yield return null;
        }
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;
        FaceTarget(player.position);

        if (anim) anim.SetTrigger("Attack");

        yield return new WaitForSeconds(attackWindUp); // Telegraph

        if (!isTank) // Lunge forward
        {
            float dir = Mathf.Sign(transform.localScale.x);
            rb.AddForce(new Vector2(dir * lungeForce, 0), ForceMode2D.Impulse);
        }

        // HITBOX CHECK
        Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, detectionLayer);
        foreach (Collider2D p in hitPlayers)
        {
            if (p.CompareTag("Player"))
            {
                // Push player based on facing direction
                float facingDir = Mathf.Sign(transform.localScale.x);
                ApplyCustomKnockback(p.gameObject, attackDamage, facingDir);
            }
        }

        yield return new WaitForSeconds(isTank ? 1.0f : 0.5f); // Recovery

        nextAttackTime = Time.time + attackRate;
        isAttacking = false;
        currentState = State.Chase;
    }

    // HELPER FUNCTIONS
    void MoveTowards(Vector2 target, float speed)
    {
        if (isAttacking) return;
        float direction = Mathf.Sign(target.x - transform.position.x);
        transform.localScale = new Vector3(direction, 1, 1);
        rb.linearVelocity = new Vector2(direction * speed, rb.linearVelocity.y);
    }

    void FaceTarget(Vector2 target)
    {
        float direction = Mathf.Sign(target.x - transform.position.x);
        transform.localScale = new Vector3(direction, 1, 1);
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
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }
}