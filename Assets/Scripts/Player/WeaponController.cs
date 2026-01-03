using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class WeaponController : MonoBehaviour
{
    [Header("Weapon Slots")]
    public WeaponData mainWeapon;
    public WeaponData subWeapon;
    private WeaponData activeWeapon;

    [Header("Audio")]
    public AudioClip equipSound; // Drag your "Sword Draw" sound here
    private AudioSource audioSource;

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
        audioSource = GetComponent<AudioSource>(); // Get Audio Component

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

    void ResetShootingState()
    {
        isShooting = false;
        if (playerController != null) playerController.SetShooting(false);

        if (rb != null)
        {
            rb.gravityScale = defaultGravity;
        }
    }

    // --- UPDATED EQUIP LOGIC ---
    // Added 'playAudio' parameter with default = true
    public void EquipWeapon(WeaponData newWeapon, bool isMain, bool playAudio = true)
    {
        if (isMain) mainWeapon = newWeapon;
        else subWeapon = newWeapon;

        UpdateVisuals(newWeapon);

        // Play Sound (only if requested)
        if (playAudio && audioSource != null && equipSound != null)
        {
            audioSource.PlayOneShot(equipSound);
        }
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
                anim.Play("Block", -1, 0f);
                ShootProjectile(weapon);
                StartCoroutine(BriefStopRoutine());
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

    void ShootProjectile(WeaponData weapon)
    {
        if (weapon.projectilePrefab != null)
        {
            Vector2 direction = (transform.localScale.x > 0) ? Vector2.right : Vector2.left;
            GameObject bulletObj = Instantiate(weapon.projectilePrefab, firePoint.position, Quaternion.identity);

            Bullet b = bulletObj.GetComponent<Bullet>();
            if (b != null)
            {
                b.damage = weapon.damage;
                b.speed = weapon.projectileSpeed;
                b.Setup(direction);
            }
        }
    }

    IEnumerator BriefStopRoutine()
    {
        isShooting = true;
        if (playerController != null) playerController.SetShooting(true);
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f;
        }

        yield return new WaitForSeconds(0.3f);
        ResetShootingState();
    }

    public void AnimationEvent_DealDamage()
    {
        if (activeWeapon != null && activeWeapon.isRanged) return;
        ResetShootingState();
        if (activeWeapon == null) return;

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, currentAttackRadius, enemyLayers);
        foreach (Collider2D enemy in hitEnemies)
        {
            EnemyStats stats = enemy.GetComponent<EnemyStats>();
            if (stats != null) stats.TakeDamage(activeWeapon.damage);
        }
    }

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

        // IMPORTANT: Pass 'false' here so we don't hear "SHWING!" when loading the game
        if (mainWeapon != null) EquipWeapon(mainWeapon, true, false);
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(attackPoint.position, currentAttackRadius);
    }
}