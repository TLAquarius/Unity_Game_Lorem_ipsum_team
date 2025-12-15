using UnityEngine;
using System.Collections;

public class BossJungle : BossBase
{
    public GameObject poisonProjectile;
    public Transform firePoint;

    private float nextFireTime;

    void Update()
    {
        if (player == null || isDead) return;

        // Boss runs AWAY if player is close (Kiting)
        float dist = Vector2.Distance(transform.position, player.position);

        if (dist < 5f)
        {
            // Run away logic
            Vector2 dir = (transform.position - player.position).normalized;
            transform.Translate(dir * 3f * Time.deltaTime);
        }

        // Shooting Logic
        if (Time.time > nextFireTime)
        {
            ShootPoison();
            nextFireTime = Time.time + (isPhase2 ? 1.0f : 2.0f); // Shoots faster in Phase 2
        }
    }

    void ShootPoison()
    {
        Instantiate(poisonProjectile, firePoint.position, Quaternion.identity);
    }

    // Override Phase 2 to add special logic
    protected override void EnterPhase2()
    {
        base.EnterPhase2();
        // Maybe spawn 2 minions here?
    }
}