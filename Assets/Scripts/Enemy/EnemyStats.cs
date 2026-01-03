using UnityEngine;

public class EnemyStats : MonoBehaviour
{
    // REMOVED: broken 'Audio' variable. We now use EnemyBase for audio.

    [Header("Base Stats")]
    public float maxHP = 100f;
    public float currentHP;
    public float xpReward = 20f;
    public float deathDelay = 0.5f;

    [Header("Loot Settings")]
    public LootTable lootTable; // Drag your LootTable asset here

    [Header("Combat Feedback")]
    public float knockbackResistance = 0f; // 0 = full knockback, 1 = immune

    // Events
    public delegate void DamageEvent();
    public event DamageEvent OnTakeDamage;

    // Reference to the main base script to access Audio
    private EnemyBase enemyBase;

    void Start()
    {
        currentHP = maxHP;
        enemyBase = GetComponent<EnemyBase>(); // Automatically find the base script on this object
    }

    public void TakeDamage(float amount)
    {
        currentHP -= amount;

        if (OnTakeDamage != null)
        {
            OnTakeDamage.Invoke();
        }

        if (currentHP <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        // 1. Play Audio (Fixed Logic)
        if (enemyBase != null)
        {
            enemyBase.PlayDeathSound();
        }

        // 2. Give XP to Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerStats playerStats = player.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.AddXP(xpReward);
            }
        }

        // 3. Drop Loot
        if (lootTable != null)
        {
            GameObject drop = lootTable.GetDrop();
            if (drop != null)
            {
                Instantiate(drop, transform.position, Quaternion.identity);
            }
        }

        Animator anim = GetComponent<Animator>();
        if (anim) anim.SetTrigger("Death");

        // Disable Physics so body doesn't hurt player
        GetComponent<Collider2D>().enabled = false;
        GetComponent<Rigidbody2D>().simulated = false;
        this.enabled = false; // Turn off this script

        // Destroy after animation finishes
        Destroy(gameObject, deathDelay);
    }
}