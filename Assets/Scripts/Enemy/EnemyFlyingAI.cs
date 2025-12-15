using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyStats))]
public class EnemyFlyingAI : MonoBehaviour
{
    [Header("Flying Settings")]
    public float moveSpeed = 3f;
    public float stopDistance = 4f;    // Stops closing in at this distance
    public float retreatDistance = 2.5f; // Backs away if you get this close
    public float maxHeightAbovePlayer = 2.5f; // CONSTRAINT: Won't fly higher than this above player

    [Header("Combat")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float fireRate = 2f;
    private float nextFireTime;

    private Transform player;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f; // Ensure gravity is off

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void Update()
    {
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);

        // --- MOVEMENT LOGIC ---
        Vector2 moveDir = Vector2.zero;

        // 1. If too far, fly closer
        if (dist > stopDistance)
        {
            moveDir = (player.position - transform.position).normalized;
        }
        // 2. If too close, retreat (BUT keep it horizontal-focused)
        else if (dist < retreatDistance)
        {
            // Calculate direction away from player
            Vector2 awayDir = (transform.position - player.position).normalized;

            // FLATTEN the Y component so it backs up horizontally, not vertically up
            // We multiply Y by 0.2 to dampen vertical retreat
            moveDir = new Vector2(awayDir.x, awayDir.y * 0.2f).normalized;
        }
        // 3. Otherwise, hover/stop
        else
        {
            // Apply a tiny bit of friction to stop sliding
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.deltaTime * 5f);
            moveDir = Vector2.zero;
        }

        // --- HEIGHT CONSTRAINT (The Fix) ---
        // If enemy is too high above player, force it down
        float heightDiff = transform.position.y - player.position.y;
        if (heightDiff > maxHeightAbovePlayer)
        {
            // Force Y velocity downwards strongly
            moveDir.y = -1f;
        }

        // Apply Movement if we calculated a direction
        if (moveDir != Vector2.zero)
        {
            rb.linearVelocity = moveDir * moveSpeed;
        }

        // --- FACING & SHOOTING ---
        // Face Player
        if (player.position.x > transform.position.x) transform.localScale = new Vector3(1, 1, 1);
        else transform.localScale = new Vector3(-1, 1, 1);

        // Shoot
        if (Time.time > nextFireTime && dist < stopDistance * 1.5f)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    void Shoot()
    {
        if (projectilePrefab != null && firePoint != null)
        {
            Vector2 dir = (player.position - firePoint.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            Instantiate(projectilePrefab, firePoint.position, Quaternion.Euler(0, 0, angle));
        }
    }

    void OnDrawGizmosSelected()
    {
        // Visual Debug
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stopDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, retreatDistance);

        // Visualize the height ceiling
        if (player != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 ceilingLine = player.position;
            ceilingLine.y += maxHeightAbovePlayer;
            Gizmos.DrawLine(ceilingLine - Vector3.right * 2, ceilingLine + Vector3.right * 2);
        }
    }
}