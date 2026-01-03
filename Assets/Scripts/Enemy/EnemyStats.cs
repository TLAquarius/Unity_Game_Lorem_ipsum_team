using UnityEngine;

public class EnemyStats : MonoBehaviour
{
    [Header("Enemy Audio")]
    public Audio enemyAudio;

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

    void Start()
    {
        currentHP = maxHP;
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
        enemyAudio.
        // 1. Give XP to Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerStats playerStats = player.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.AddXP(xpReward);
            }
        }

        // 2. Drop Loot
        if (lootTable != null)
        {
            GameObject drop = lootTable.GetDrop();
            if (drop != null)
            {
                Instantiate(drop, transform.position, Quaternion.identity);
            }
        }

        Animator anim = GetComponent<Animator>();
        if (anim) anim.SetTrigger("Death"); // Add "Death" trigger to Animator

        // Disable Physics so body doesn't hurt player
        GetComponent<Collider2D>().enabled = false;
        GetComponent<Rigidbody2D>().simulated = false;
        this.enabled = false; // Turn off this script

        // Destroy after animation finishes
        Destroy(gameObject, deathDelay);
    }
}