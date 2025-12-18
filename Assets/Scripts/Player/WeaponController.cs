using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [Header("Weapon Slots")]
    public WeaponData mainWeapon;
    public WeaponData subWeapon;
    private WeaponData activeWeapon;

    [Header("Combo System")]
    public float comboResetTime = 0.8f;
    private int comboStep = 0;
    private float lastAttackTime = -999f;

    // Input Buffer
    private float lastInputTime = -999f;
    private float inputBufferTime = 0.2f;
    private bool bufferedMainAttack = false;

    // --- NEW: SHOOTING LOCK ---
    // Prevents spamming from cancelling the shot before it spawns
    private bool isShooting = false;

    [Header("References")]
    public Transform attackPoint;
    public Transform firePoint;
    public LayerMask enemyLayers;
    public float currentAttackRadius = 0.5f;

    private Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();
        if (mainWeapon != null)
        {
            UpdateVisuals(mainWeapon);
            currentAttackRadius = mainWeapon.attackRange;
        }
    }

    void Update()
    {
        // 1. SAFETY RESET
        // If we get stuck in "Shooting" state for too long (e.g. got hit/interrupted), reset it
        if (isShooting && Time.time - lastAttackTime > 1.0f)
        {
            isShooting = false;
        }

        // 2. BUFFER INPUT
        if (Input.GetKeyDown(KeyCode.Z))
        {
            lastInputTime = Time.time;
            bufferedMainAttack = true;
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            lastInputTime = Time.time;
            bufferedMainAttack = false;
        }

        // 3. CHECK COMBO RESET
        if (Time.time - lastAttackTime > comboResetTime)
        {
            comboStep = 0;
        }

        // 4. EXECUTE ATTACK
        if (Time.time - lastInputTime < inputBufferTime)
        {
            // --- NEW CHECK: IS SHOOTING? ---
            // If we are currently winding up a shot, DO NOT allow a new attack yet.
            if (isShooting) return;
            if (mainWeapon != null || subWeapon != null)
            {
                float timeBetweenAttacks = 1f / (bufferedMainAttack ? mainWeapon.attackRate : subWeapon.attackRate);

                if (Time.time >= lastAttackTime + timeBetweenAttacks)
                {
                    if (bufferedMainAttack && mainWeapon != null) Attack(mainWeapon);
                    else if (!bufferedMainAttack && subWeapon != null) Attack(subWeapon);

                    lastInputTime = -999f;
                }
            }
        }
    }

    public void EquipWeapon(WeaponData newWeapon, bool isMain)
    {
        if (isMain) mainWeapon = newWeapon;
        else subWeapon = newWeapon;
        UpdateVisuals(newWeapon);
    }

    void UpdateVisuals(WeaponData weapon)
    {
        if (weapon == null) return;
        if (GetComponent<SpriteRenderer>() != null) GetComponent<SpriteRenderer>().color = weapon.weaponColor;
    }

    void Attack(WeaponData weapon)
    {
        activeWeapon = weapon;
        lastAttackTime = Time.time;
        currentAttackRadius = weapon.attackRange;

        if (anim != null)
        {
            if (weapon.isRanged)
            {
                // --- LOCK INPUT ---
                isShooting = true;

                // Use 'Play' to force start, but because 'isShooting' is true, 
                // Update() won't call this again until the bullet spawns.
                anim.Play("Block", -1, 0f);
                comboStep = 0;
            }
            else
            {
                // Melee logic remains the same (spam friendly)
                comboStep++;
                if (comboStep > 3) comboStep = 1;
                anim.Play("Attack" + comboStep, -1, 0f);
            }
        }
    }

    // TRIGGERED BY ANIMATION EVENT
    public void AnimationEvent_DealDamage()
    {
        // --- UNLOCK INPUT ---
        // The bullet has spawned, so the player is allowed to attack again now.
        isShooting = false;

        if (activeWeapon == null) return;

        if (activeWeapon.isRanged)
        {
            if (activeWeapon.projectilePrefab != null)
            {
                GameObject bullet = Instantiate(activeWeapon.projectilePrefab, firePoint.position, firePoint.rotation);
                Bullet b = bullet.GetComponent<Bullet>();
                if (b != null)
                {
                    b.speed = activeWeapon.projectileSpeed;
                    b.damage = activeWeapon.damage;
                }
                // Flip bullet logic
                if (transform.localScale.x < 0)
                {
                    b.transform.localScale = new Vector3(-1, 1, 1);
                    b.speed *= -1;
                }
            }
        }
        else
        {
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, currentAttackRadius, enemyLayers);
            foreach (Collider2D enemy in hitEnemies)
            {
                EnemyStats stats = enemy.GetComponent<EnemyStats>();
                if (stats != null) stats.TakeDamage(activeWeapon.damage);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(attackPoint.position, currentAttackRadius);
    }
}