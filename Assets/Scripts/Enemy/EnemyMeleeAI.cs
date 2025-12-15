using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyStats))]
public class EnemyMeleeAI : MonoBehaviour
{
    // The States the enemy can be in
    public enum State { Idle, Patrol, Chase, Telegraph, Attack, Cooldown, Hit }
    public State currentState;

    [Header("References")]
    public Transform attackPoint; // Create an empty child object where the attack hits
    private Transform player;
    private Rigidbody2D rb;
    private EnemyStats stats;
    private SpriteRenderer sr;

    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float chaseSpeed = 4f;
    public float patrolWaitTime = 2f; // Time to stand still at patrol points
    public Transform[] patrolPoints;  // Drag empty GameObjects here in Inspector

    [Header("Detection Settings")]
    public float detectionRange = 6f;
    public float attackRange = 1.2f;

    [Header("Combat Settings")]
    public float telegraphTime = 0.5f; // Warning time (Red Flash)
    public float attackDuration = 0.2f; // Time the hitbox is active
    public float attackRadius = 0.8f;   // Size of the attack circle
    public float cooldownTime = 1.5f;   // Rest after attacking
    public LayerMask playerLayer;       // Select "Player" layer in Inspector

    // Internal Variables
    private int currentPatrolIndex = 0;
    private float nextMoveTime; // Timer for patrolling

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        stats = GetComponent<EnemyStats>();
        sr = GetComponent<SpriteRenderer>();

        // Subscribe to the damage event to play "Hurt" animation
        stats.OnTakeDamage += PlayHurtEffect;

        // Auto-find Player
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        currentState = State.Patrol;
    }

    void Update()
    {
        if (player == null) return;

        float distToPlayer = Vector2.Distance(transform.position, player.position);

        // STATE MACHINE SWITCH
        switch (currentState)
        {
            case State.Patrol:
                PatrolLogic(distToPlayer);
                break;
            case State.Chase:
                ChaseLogic(distToPlayer);
                break;
                // Other states (Attack, Cooldown) are handled by Coroutines
        }
    }

    // --- LOGIC FUNCTIONS ---

    void PatrolLogic(float distToPlayer)
    {
        // 1. Check for Player detection
        if (distToPlayer < detectionRange)
        {
            currentState = State.Chase;
            return;
        }

        // 2. Patrol Movement
        if (patrolPoints.Length == 0) return; // Safety check

        Transform target = patrolPoints[currentPatrolIndex];

        // If we reached the point, wait, then switch to next point
        if (Vector2.Distance(transform.position, target.position) < 0.5f)
        {
            rb.linearVelocity = Vector2.zero;
            if (Time.time > nextMoveTime)
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                nextMoveTime = Time.time + patrolWaitTime;
            }
        }
        else
        {
            // Move to point
            MoveTo(target.position, moveSpeed);
        }
    }

    void ChaseLogic(float distToPlayer)
    {
        // 1. If close enough, Attack
        if (distToPlayer <= attackRange)
        {
            StartCoroutine(AttackRoutine());
        }
        // 2. If too far, go back to Patrol
        else if (distToPlayer > detectionRange * 1.5f)
        {
            currentState = State.Patrol;
        }
        // 3. Otherwise, Run at player
        else
        {
            MoveTo(player.position, chaseSpeed);
        }
    }

    // --- COMBAT COROUTINE (The core Logic) ---
    IEnumerator AttackRoutine()
    {
        currentState = State.Telegraph;
        rb.linearVelocity = Vector2.zero; // Stop moving

        // 1. TELEGRAPH (Warning)
        sr.color = Color.red; // Visual warning (Change this to Animation later)
        yield return new WaitForSeconds(telegraphTime);

        // 2. ATTACK (Active Hitbox)
        currentState = State.Attack;
        sr.color = Color.white;

        // Check what we hit using a Physics Circle
        Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, playerLayer);
        foreach (Collider2D p in hitPlayers)
        {
            // Try to find a script on the player that can take damage
            // For now, we assume the player has a script with a 'TakeDamage' method
            p.SendMessage("TakeDamage", stats.damage, SendMessageOptions.DontRequireReceiver);
            Debug.Log("Hit Player for " + stats.damage + " damage!");
        }

        yield return new WaitForSeconds(attackDuration);

        // 3. COOLDOWN
        currentState = State.Cooldown;
        yield return new WaitForSeconds(cooldownTime);

        // 4. RESET
        currentState = State.Chase;
    }

    // --- HELPER FUNCTIONS ---

    void MoveTo(Vector2 target, float speed)
    {
        float direction = Mathf.Sign(target.x - transform.position.x);
        rb.linearVelocity = new Vector2(direction * speed, rb.linearVelocity.y);
        transform.localScale = new Vector3(direction, 1, 1); // Face direction
    }

    void PlayHurtEffect()
    {
        // Simple feedback when hit
        StartCoroutine(FlashWhite());
    }

    IEnumerator FlashWhite()
    {
        sr.color = Color.clear; // Flash transparent
        yield return new WaitForSeconds(0.1f);
        sr.color = Color.white;
    }

    // --- VISUAL DEBUGGING (Gizmos) ---
    void OnDrawGizmosSelected()
    {
        // Draw Detection Range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Draw Attack Trigger Range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Draw Actual Hitbox
        if (attackPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }
    }
}