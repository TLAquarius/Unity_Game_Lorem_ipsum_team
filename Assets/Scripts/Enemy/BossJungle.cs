using UnityEngine;
using System.Collections;

public class BossJungle : BossBase
{
    [Header("Jungle Boss Visuals")]
    public bool spriteFacesLeft = true;
    private Vector3 initialScale;

    [Header("Projectile Settings")]
    public GameObject projectilePrefab;
    public Transform firePoint;

    [Header("Combat Stats")]
    public float detectionRange = 15f; // --- NEW: Max range to start fighting
    public float keepDistance = 6f;    // Ideal kiting range
    public float moveSpeed = 3f;
    public float fireRate = 1.5f;
    public float phase2FireRate = 0.8f;

    private float attackAnimDelay = 0.4f;
    private float nextFireTime;
    private Rigidbody2D rb;

    protected override void Start()
    {
        base.Start();
        rb = GetComponent<Rigidbody2D>();
        initialScale = transform.localScale;
    }

    void Update()
    {
        if (player == null || isDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // --- NEW: Distance Check Logic ---
        float dist = Vector2.Distance(transform.position, player.position);

        // If player is outside detection range, Boss stays idle
        if (dist > detectionRange)
        {
            rb.linearVelocity = Vector2.zero;
            if (anim) anim.SetBool("isMoving", false);
            return; // Exit Update early so he doesn't shoot or move
        }

        // If inside range, proceed with combat logic
        HandleMovement(dist); // Pass distance to avoid recalculating
        HandleShooting();
        HandleFacing();
    }

    void HandleMovement(float dist)
    {
        float currentSpeed = isPhase2 ? moveSpeed * 1.5f : moveSpeed;

        // 1. Too Close? Run Away (Kiting)
        if (dist < keepDistance - 1f)
        {
            MoveAway(player.position, currentSpeed);
        }
        // 2. Too Far? Chase (But only if within detectionRange, which is checked in Update)
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
            rb.linearVelocity = Vector2.zero; // Stop to shoot

            if (isPhase2)
            {
                StartCoroutine(ShootShotgunRoutine());
                nextFireTime = Time.time + phase2FireRate;
            }
            else
            {
                StartCoroutine(ShootSingleRoutine());
                nextFireTime = Time.time + fireRate;
            }
        }
    }

    // --- ATTACK ROUTINES ---

    IEnumerator ShootSingleRoutine()
    {
        if (anim) anim.SetTrigger("Attack");
        yield return new WaitForSeconds(attackAnimDelay);
        SpawnBullet(0);
    }

    IEnumerator ShootShotgunRoutine()
    {
        if (anim) anim.SetTrigger("Attack");
        yield return new WaitForSeconds(attackAnimDelay);
        SpawnBullet(0);
        SpawnBullet(15);
        SpawnBullet(-15);
    }

    void SpawnBullet(float angleOffset)
    {
        if (projectilePrefab == null || firePoint == null) return;

        GameObject bullet = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        Vector2 dir = (player.position - firePoint.position).normalized;

        float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float finalAngle = baseAngle + angleOffset;

        bullet.transform.rotation = Quaternion.Euler(0, 0, finalAngle);

        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            Vector2 velDir = new Vector2(Mathf.Cos(finalAngle * Mathf.Deg2Rad), Mathf.Sin(finalAngle * Mathf.Deg2Rad));
            bulletRb.linearVelocity = velDir * 10f;
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
        float dirX = Mathf.Sign(transform.position.x - target.x);
        rb.linearVelocity = new Vector2(dirX * speed, rb.linearVelocity.y);
    }

    void HandleFacing()
    {
        float dirX = Mathf.Sign(player.position.x - transform.position.x);
        float scaleX = spriteFacesLeft ? -dirX : dirX;
        transform.localScale = new Vector3(scaleX * Mathf.Abs(initialScale.x), initialScale.y, initialScale.z);
    }

    // --- NEW: Visual Debugging ---
    void OnDrawGizmosSelected()
    {
        // Green circle = Ideal Kiting Range
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, keepDistance);

        // Red circle = Max Aggro Range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}