using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [Header("Current Weapon")]
    public WeaponData currentWeapon; // Drag your 'Rusty Sword' data here as default

    [Header("References")]
    public Transform attackPoint; // The empty object for melee hit detection
    public Transform firePoint;   // The empty object for shooting
    public SpriteRenderer weaponRenderer; // The Sprite of the weapon in player's hand
    public LayerMask enemyLayers;

    private float nextAttackTime = 0f;

    void Start()
    {
        EquipWeapon(currentWeapon);
    }

    void Update()
    {
        if (currentWeapon == null) return;

        // Unified Input: 'Z' for attack (Melee or Ranged depending on weapon)
        if (Time.time >= nextAttackTime)
        {
            if (Input.GetKeyDown(KeyCode.Z))
            {
                PerformAttack();
                nextAttackTime = Time.time + (1f / currentWeapon.attackRate);
            }
        }
    }

    public void EquipWeapon(WeaponData newWeapon)
    {
        currentWeapon = newWeapon;

        // Update Visuals
        if (weaponRenderer != null)
        {
            weaponRenderer.sprite = newWeapon.icon;
            weaponRenderer.color = newWeapon.weaponColor;
        }

        Debug.Log("Equipped: " + newWeapon.weaponName);
    }

    void PerformAttack()
    {
        if (currentWeapon.isRanged)
        {
            // Ranged Logic
            if (currentWeapon.projectilePrefab != null)
            {
                GameObject bullet = Instantiate(currentWeapon.projectilePrefab, firePoint.position, firePoint.rotation);

                // Set Bullet Speed/Damage dynamically
                Bullet b = bullet.GetComponent<Bullet>();
                if (b != null)
                {
                    b.speed = currentWeapon.projectileSpeed;
                    b.damage = currentWeapon.damage;
                }

                // Handle Rotation (Left/Right)
                if (transform.localScale.x < 0)
                {
                    b.transform.localScale = new Vector3(-1, 1, 1);
                    b.speed *= -1; // Or rotate logic from previous steps
                }
            }
        }
        else
        {
            // Melee Logic
            // Use the range from the DATA, not the script
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, currentWeapon.attackRange, enemyLayers);
            foreach (Collider2D enemy in hitEnemies)
            {
                EnemyStats stats = enemy.GetComponent<EnemyStats>();
                if (stats != null)
                {
                    stats.TakeDamage(currentWeapon.damage);
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null || currentWeapon == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(attackPoint.position, currentWeapon.attackRange);
    }
}