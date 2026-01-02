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
    private PlayerController playerController; // <--- NEW REF

    void Start()
    {
        anim = GetComponent<Animator>();
        input = GetComponent<GameInput>();
        playerController = GetComponent<PlayerController>(); // <--- GET REF

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
        // 1. SAFETY RESET
        // If stuck in shooting state for > 1 second, force reset
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
                // --- LOCK INPUT & SUSPEND GRAVITY ---
                isShooting = true;

                // Tell PlayerController to stop processing inputs
                if (playerController != null) playerController.SetShooting(true);

                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero; // Stop instantly
                    rb.gravityScale = 0f;       // Float in air
                }

                anim.Play("Block", -1, 0f); // Replace "Block" with your Bow animation name
                comboStep = 0;
            }
            else
            {
                comboStep++;
                if (comboStep > 3) comboStep = 1;
                anim.Play("Attack" + comboStep, -1, 0f);
            }
        }
    }

    // TRIGGERED BY ANIMATION EVENT
    public void AnimationEvent_DealDamage()
    {
        // --- RESTORE GRAVITY ---
        // As soon as the bullet fires, gravity returns and player falls
        ResetShootingState();

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
                // Handle shooting left
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

    // ... Load/Save Logic (Same as before) ...
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