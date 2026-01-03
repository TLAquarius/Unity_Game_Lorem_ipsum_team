using UnityEngine;
using System.Collections;

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

    // --- SHOOTING LOCK & PHYSICS ---
    private bool isShooting = false;
    private Rigidbody2D rb;
    private float defaultGravity;

    [Header("References")]
    public Transform attackPoint;
    public Transform firePoint;
    public LayerMask enemyLayers;
    public float currentAttackRadius = 0.5f;

    private Animator anim;
    private GameInput input;
    private PlayerController playerController;

    void Start()
    {
        anim = GetComponent<Animator>();
        input = GetComponent<GameInput>();
        playerController = GetComponent<PlayerController>();

        rb = GetComponent<Rigidbody2D>();
        if (rb != null) defaultGravity = rb.gravityScale;

        if (mainWeapon != null)
        {
            UpdateVisuals(mainWeapon);
            currentAttackRadius = mainWeapon.attackRange;
        }
    }

    void Update()
    {
        // 1. SAFETY RESET: If stuck in shooting state for > 1 second, force reset
        if (isShooting && Time.time - lastAttackTime > 1.0f)
        {
            ResetShootingState();
        }

        // 2. BUFFER INPUT
        if (input.IsAttackMainPressed())
        {
            lastInputTime = Time.time;
            bufferedMainAttack = true;
        }
        else if (input.IsAttackSubPressed())
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
            if (isShooting) return; // Cannot interrupt shooting loop

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

    // --- HELPER TO RESET STATE ---
    void ResetShootingState()
    {
        isShooting = false;
        if (playerController != null) playerController.SetShooting(false); // Unfreeze Player

        if (rb != null)
        {
            rb.gravityScale = defaultGravity;
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
                // --- RANGED ATTACK LOGIC ---

                // 1. Play Animation (Visual only)
                // Use a generic "Attack1" or specific "Shoot" animation if you have one.
                // If "Block" was a placeholder, change it to your actual shoot animation name.
                anim.Play("Block", -1, 0f);

                // 2. SHOOT INSTANTLY (New Logic)
                ShootProjectile(weapon);

                // 3. Freeze player briefly for recoil effect
                StartCoroutine(BriefStopRoutine());

                comboStep = 0;
            }
            else
            {
                // --- MELEE ATTACK LOGIC ---
                comboStep++;
                if (comboStep > 3) comboStep = 1;
                anim.Play("Attack" + comboStep, -1, 0f);
            }
        }
    }

    // --- NEW SHOOTING FUNCTION ---
    void ShootProjectile(WeaponData weapon)
    {
        if (weapon.projectilePrefab != null)
        {
            // 1. Calculate Direction based on Player Facing
            // We check localScale.x to see if player is facing Right (1) or Left (-1)
            Vector2 direction = (transform.localScale.x > 0) ? Vector2.right : Vector2.left;

            // 2. Spawn the Bullet
            GameObject bulletObj = Instantiate(weapon.projectilePrefab, firePoint.position, Quaternion.identity);

            // 3. Setup the Bullet Script
            Bullet b = bulletObj.GetComponent<Bullet>();
            if (b != null)
            {
                b.damage = weapon.damage;
                b.speed = weapon.projectileSpeed;

                // CRITICAL: Call the Setup function to fix rotation and velocity
                b.Setup(direction);
            }
        }
    }

    // --- RECOIL ROUTINE ---
    IEnumerator BriefStopRoutine()
    {
        isShooting = true;

        // Lock player movement
        if (playerController != null) playerController.SetShooting(true);
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f; // Optional: Float while shooting
        }

        // Wait for a short duration (e.g., 0.3 seconds)
        yield return new WaitForSeconds(0.3f);

        // Unlock player
        ResetShootingState();
    }

    // --- ANIMATION EVENT (MODIFIED) ---
    // This is now ONLY used for MELEE damage synchronization.
    // Ranged attacks are handled immediately in Attack(), so we ignore them here.
    public void AnimationEvent_DealDamage()
    {
        // Safety: If this triggers for a ranged weapon, do nothing (we already shot)
        if (activeWeapon != null && activeWeapon.isRanged) return;

        // Restore gravity for melee (in case it was altered)
        ResetShootingState();

        if (activeWeapon == null) return;

        // --- MELEE HITBOX LOGIC ---
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, currentAttackRadius, enemyLayers);
        foreach (Collider2D enemy in hitEnemies)
        {
            EnemyStats stats = enemy.GetComponent<EnemyStats>();
            if (stats != null) stats.TakeDamage(activeWeapon.damage);
        }
    }

    // ... Load/Save Logic (Unchanged) ...
    private WeaponData LoadWeaponByName(string name) { return Resources.Load<WeaponData>("Weapons/" + name); }
    public void SaveData(SaveData data)
    {
        data.weaponIDs.Clear();
        if (mainWeapon != null) data.weaponIDs.Add(mainWeapon.name);
        if (subWeapon != null) data.weaponIDs.Add(subWeapon.name);
    }
    public void LoadData(SaveData data)
    {
        if (data.weaponIDs.Count > 0) mainWeapon = LoadWeaponByName(data.weaponIDs[0]);
        if (data.weaponIDs.Count > 1) subWeapon = LoadWeaponByName(data.weaponIDs[1]);
        if (mainWeapon != null) EquipWeapon(mainWeapon, true);
    }
    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(attackPoint.position, currentAttackRadius);
    }
}