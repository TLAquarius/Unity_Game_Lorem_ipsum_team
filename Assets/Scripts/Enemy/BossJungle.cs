using UnityEngine;
using System.Collections;

public class BossJungle : BossBase
{
    [Header("Jungle Boss Visuals")]
    public bool spriteFacesLeft = true; // Check if your sprite looks Left by default
    private Vector3 initialScale;

    [Header("Projectile Settings")]
    public GameObject projectilePrefab;
    public Transform firePoint;

    [Header("Combat Stats")]
    public float keepDistance = 6f; // Ideal range
    public float moveSpeed = 3f;
    public float fireRate = 1.5f;
    public float phase2FireRate = 0.8f; // Faster shooting in Phase 2

    // Delays the bullet spawn to match the animation frame
    private float attackAnimDelay = 0.4f;

    private float nextFireTime;
    private Rigidbody2D rb;

    protected override void Start()
    {
        base.Start(); // Run BossBase setup
        rb = GetComponent<Rigidbody2D>();

        // 1. CAPTURE THE SIZE YOU SET IN THE INSPECTOR
        initialScale = transform.localScale;
    }

    void Update()
    {
        if (player == null || isDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        HandleMovement();
        HandleShooting();
        HandleFacing();
    }

    void HandleMovement()
    {
        float dist = Vector2.Distance(transform.position, player.position);
        float currentSpeed = isPhase2 ? moveSpeed * 1.5f : moveSpeed;

        // 1. Too Close? Run Away (Kiting)
        if (dist < keepDistance - 1f)
        {
            MoveAway(player.position, currentSpeed);
        }
        // 2. Too Far? Chase
        else if (dist > keepDistance + 1f)
        {
            MoveTowards(player.position, currentSpeed);
        }
        // 3. Just Right? Stop.
        else
        {
            rb.linearVelocity = Vector2.zero;
        }

        // Animation
        if (anim) anim.SetBool("isMoving", rb.linearVelocity.magnitude > 0.1f);
    }

    void HandleShooting()
    {
        if (Time.time >= nextFireTime)
        {
            // Stop moving briefly to shoot
            rb.linearVelocity = Vector2.zero;

            if (isPhase2)
            {
                StartCoroutine(ShootShotgunRoutine()); // 3 Bullets
                nextFireTime = Time.time + phase2FireRate;
            }
            else
            {
                StartCoroutine(ShootSingleRoutine()); // 1 Bullet
                nextFireTime = Time.time + fireRate;
            }
        }
    }

    // --- ATTACK ROUTINES (COROUTINES) ---

    IEnumerator ShootSingleRoutine()
    {
        if (anim) anim.SetTrigger("Attack");

        // Wait for the arm to swing (Sync with Cast.anim)
        yield return new WaitForSeconds(attackAnimDelay);

        SpawnBullet(0); // 0 degree offset
    }

    IEnumerator ShootShotgunRoutine()
    {
        if (anim) anim.SetTrigger("Attack");

        // Wait for the arm to swing
        yield return new WaitForSeconds(attackAnimDelay);

        SpawnBullet(0);   // Center
        SpawnBullet(15);  // Up angle
        SpawnBullet(-15); // Down angle
    }

    void SpawnBullet(float angleOffset)
    {
        if (projectilePrefab == null || firePoint == null) return;

        GameObject bullet = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);

        // Calculate direction to player
        Vector2 dir = (player.position - firePoint.position).normalized;

        // Apply angle offset
        float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float finalAngle = baseAngle + angleOffset;

        // Rotate bullet sprite
        bullet.transform.rotation = Quaternion.Euler(0, 0, finalAngle);

        // Apply Velocity (Using the bullet's Rigidbody)
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            // Convert angle back to vector for velocity
            Vector2 velDir = new Vector2(Mathf.Cos(finalAngle * Mathf.Deg2Rad), Mathf.Sin(finalAngle * Mathf.Deg2Rad));
            bulletRb.linearVelocity = velDir * 10f; // Bullet speed
        }
    }

    // --- MOVEMENT HELPERS ---

    void MoveTowards(Vector2 target, float speed)
    {
        float dirX = Mathf.Sign(target.x - transform.position.x);
        rb.linearVelocity = new Vector2(dirX * speed, rb.linearVelocity.y);
    }

    void MoveAway(Vector2 target, float speed)
    {
        float dirX = Mathf.Sign(transform.position.x - target.x); // Opposite direction
        rb.linearVelocity = new Vector2(dirX * speed, rb.linearVelocity.y);
    }

    void HandleFacing()
    {
        // Always face the player
        float dirX = Mathf.Sign(player.position.x - transform.position.x);
        float scaleX = spriteFacesLeft ? -dirX : dirX;

        // 2. APPLY THE SAVED SCALE (Fixes the shrinking bug)
        transform.localScale = new Vector3(scaleX * Mathf.Abs(initialScale.x), initialScale.y, initialScale.z);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, keepDistance);
    }
}