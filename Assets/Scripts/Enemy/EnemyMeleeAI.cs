using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyStats))]
public class EnemyMeleeAI : EnemyBase 
{
    public enum State { Idle, Patrol, Chase, Attack, Hit }
    public State currentState;

    [Header("Sprite Settings")]
    public bool spriteFacesLeft = true; // <--- MAKE SURE THIS IS CHECKED IN INSPECTOR

    [Header("Class Settings")]
    public bool isTank = false;

    [Header("Hurt Settings")]
    public float hurtDuration = 0.5f; // Time they are stunned
    public Vector2 selfKnockback = new Vector2(3f, 0f);

    [Header("References")]
    public Transform attackPoint;
    public Transform groundCheck;
    public LayerMask detectionLayer; 

    private Transform player;
    private Rigidbody2D rb;
    private Animator anim;
    private EnemyStats stats; // Fixed the missing variable error

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float patrolWaitTime = 2f;
    public Transform[] patrolPoints;

    [Header("Combat AI")]
    public float detectionRange = 6f;
    public float attackTriggerRange = 1.5f;
    public float attackRate = 1.5f;
    public float attackWindUp = 0.4f; 
    public float attackRange = 0.8f; 
    public float lungeForce = 5f; 

    private float nextAttackTime;
    private int patrolIndex;
    private bool isAttacking;
    private bool isProvoked = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        stats = GetComponent<EnemyStats>();

        // Fix: Ensure gravity is normal
        rb.gravityScale = 1f; 

        if (stats != null) stats.OnTakeDamage += ReactToDamage;

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
        // 1. Interrupt everything
        StopAllCoroutines();

        // 2. Start the Hurt/Stun routine
        StartCoroutine(HurtRoutine());
    }

    IEnumerator HurtRoutine()
    {
        // A. Set State to HIT (Stops movement logic)
        currentState = State.Hit;
        isAttacking = false;

        // B. Visual Feedback
        if (anim) anim.SetTrigger("Hurt");

        // C. Physics Knockback (Push Enemy away from Player)
        // Reset speed first so the knockback is consistent
        rb.linearVelocity = Vector2.zero;

        if (player != null)
        {
            // Determine direction: If player is to the Left, push Right.
            float dirX = Mathf.Sign(transform.position.x - player.position.x);

            // Apply force to self
            rb.AddForce(new Vector2(dirX * selfKnockback.x, selfKnockback.y), ForceMode2D.Impulse);
        }

        // D. Wait (The Stun)
        yield return new WaitForSeconds(hurtDuration);

        // E. Recover and Chase
        currentState = State.Chase;

        // Mark as provoked so they chase even if far away
        isProvoked = true;
        StartCoroutine(ResetProvocation());

        // Restart the Main Logic Loop
        StartCoroutine(MainLogic());
    }

    IEnumerator ResetProvocation()
    {
        yield return new WaitForSeconds(3.0f);
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
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
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

            // STOP moving earlier to prevent running inside the player
            if (dist < attackTriggerRange && Time.time >= nextAttackTime)
            {
                rb.linearVelocity = Vector2.zero; // <--- HARD STOP
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
        
        // 1. Face Player Correctly
        FaceTarget(player.position); 

        if (anim) anim.SetTrigger("Attack");

        // 2. Wind Up
        yield return new WaitForSeconds(attackWindUp); 

        // 3. Lunge Logic (FIXED)
        // Only lunge if we are NOT touching the player yet
        float distToPlayer = Vector2.Distance(transform.position, player.position);
        if (!isTank && distToPlayer > 1.0f) 
        {
            float dir = Mathf.Sign(player.position.x - transform.position.x); // Lunge towards PLAYER, not self facing
            rb.AddForce(new Vector2(dir * lungeForce, 0), ForceMode2D.Impulse);
        }

        // Note: Damage is triggered by Animation Event calling TriggerAttackHitbox()

        // 4. Recovery
        yield return new WaitForSeconds(isTank ? 1.0f : 0.5f);

        nextAttackTime = Time.time + attackRate;
        isAttacking = false;
        currentState = State.Chase;
    }

    public void TriggerAttackHitbox()
    {
        if (attackPoint == null) return;
        Debug.Log("Attack Hitbox Active!");

        Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, detectionLayer);

        foreach (Collider2D p in hitPlayers)
        {
            if (p.CompareTag("Player"))
            {
                float facingDir = Mathf.Sign(transform.localScale.x);
                // Invert direction for Knockback if sprite faces left
                if(spriteFacesLeft) facingDir *= -1; 

                ApplyCustomKnockback(p.gameObject, attackDamage, facingDir);
            }
        }
    }

    // --- HELPER FUNCTIONS ---
    
    // FIXED: Now uses the "Sprite Faces Left" boolean correctly
    void MoveTowards(Vector2 target, float speed)
    {
        if (isAttacking) return;

        float directionX = Mathf.Sign(target.x - transform.position.x);
        
        // FLIP LOGIC
        // If sprite is drawn facing LEFT:
        // Moving Right (1) -> Scale (-1)
        // Moving Left (-1) -> Scale (1)
        float scaleX = spriteFacesLeft ? -directionX : directionX;
        
        transform.localScale = new Vector3(scaleX, 1, 1);
        rb.linearVelocity = new Vector2(directionX * speed, rb.linearVelocity.y);
    }

    // FIXED: Matches the MoveTowards logic exactly
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
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }
}