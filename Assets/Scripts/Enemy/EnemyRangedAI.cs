using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyStats))]
public class EnemyRangedAI : MonoBehaviour
{
    [Header("References")]
    public GameObject bulletPrefab; // Drag EnemyBullet prefab here
    public Transform firePoint;     // Empty child object where bullet spawns
    private Transform player;
    private Rigidbody2D rb;
    private SpriteRenderer sr;

    [Header("Settings")]
    public float moveSpeed = 3f;
    public float detectionRange = 8f; // Can see further than Melee
    public float shootingRange = 5f;  // Stops here to shoot
    public float retreatRange = 2f;   // Runs away if player is too close

    [Header("Combat")]
    public float fireRate = 1.5f;     // Seconds between shots
    private float nextFireTime;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void Update()
    {
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);

        // 1. IDLE: Player too far
        if (dist > detectionRange)
        {
            rb.linearVelocity = Vector2.zero; // Stand still
        }
        // 2. CHASE: Player seen, but out of range
        else if (dist > shootingRange)
        {
            MoveTowards(player.position, 1f); // 1 = move forward
        }
        // 3. RETREAT: Player too close! (Kiting)
        else if (dist < retreatRange)
        {
            MoveTowards(player.position, -1f); // -1 = move backward
        }
        // 4. ATTACK: In ideal range
        else
        {
            rb.linearVelocity = Vector2.zero; // Stop moving
            // Face the player
            float dir = Mathf.Sign(player.position.x - transform.position.x);
            transform.localScale = new Vector3(dir, 1, 1);

            if (Time.time >= nextFireTime)
            {
                Shoot();
                nextFireTime = Time.time + fireRate;
            }
        }
    }

    void MoveTowards(Vector2 target, float directionMult)
    {
        float dirX = Mathf.Sign(target.x - transform.position.x);

        // If directionMult is -1, we move OPPOSITE to the target
        float finalDir = dirX * directionMult;

        rb.linearVelocity = new Vector2(finalDir * moveSpeed, rb.linearVelocity.y);

        // Always face the player, even when running away
        transform.localScale = new Vector3(dirX, 1, 1);
    }

    void Shoot()
    {
        // 1. Calculate direction to player
        Vector3 direction = (player.position - firePoint.position).normalized;

        // 2. Calculate rotation (Right axis points to player)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0, 0, angle);

        // 3. Spawn Bullet
        Instantiate(bulletPrefab, firePoint.position, rotation);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, shootingRange);
        Gizmos.color = Color.blue; Gizmos.DrawWireSphere(transform.position, retreatRange);
    }
}