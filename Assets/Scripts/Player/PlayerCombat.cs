using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Melee Stats (Z Key)")]
    public float meleeDamage = 20f;
    public float meleeRate = 2f;
    public float meleeRange = 0.5f;
    public Transform attackPoint;
    public LayerMask enemyLayers;

    [Header("Ranged Stats (X Key)")]
    public GameObject bulletPrefab; // Drag your Bullet Prefab here!
    public Transform firePoint;     // Where the bullet spawns

    private float nextAttackTime = 0f;

    void Update()
    {
        if (Time.time >= nextAttackTime)
        {
           
            if (Input.GetKeyDown(KeyCode.Z))
            {
                MeleeAttack();
                nextAttackTime = Time.time + 1f / meleeRate;
            }
           
            else if (Input.GetKeyDown(KeyCode.X))
            {
                RangedAttack();
                nextAttackTime = Time.time + 1f / meleeRate;
            }
        }
    }

    void MeleeAttack()
    {
        // (Old Melee Logic)
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, meleeRange, enemyLayers);
        foreach (Collider2D enemy in hitEnemies)
        {
            EnemyStats stats = enemy.GetComponent<EnemyStats>();
            if (stats != null) stats.TakeDamage(meleeDamage);
        }
    }

    void RangedAttack()
    {
        // 1. Check Player's Facing Direction
        // If Scale X is negative, we are facing Left.
        Quaternion bulletRotation = Quaternion.identity; // Default = Pointing Right

        if (transform.localScale.x < 0)
        {
            // Rotate 180 degrees around Z axis to point Left
            bulletRotation = Quaternion.Euler(0f, 0f, 180f);
        }

        // 2. Spawn the bullet with the CORRECT rotation
        Instantiate(bulletPrefab, firePoint.position, bulletRotation);
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(attackPoint.position, meleeRange);
    }
}