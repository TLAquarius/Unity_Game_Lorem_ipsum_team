using UnityEngine;

public class EnemyStats : MonoBehaviour
{
    // --- ATTRIBUTES GO HERE (ABOVE the variables) ---
    [Header("Base Stats")]
    public float maxHP = 100f;
    public float currentHP;
    public float damage = 10f;
    public float xpReward = 20f;

    [Header("Combat Feedback")]
    public float knockbackResistance = 0f;

    // Delegate and Event for taking damage
    public delegate void DamageEvent();
    public event DamageEvent OnTakeDamage;

    // --- FUNCTIONS START HERE ---
    void Start()
    {
        currentHP = maxHP;
    }

    public void TakeDamage(float amount)
    {
        currentHP -= amount;
        Debug.Log(transform.name + " took " + amount + " damage.");

        // Notify other scripts (like the AI)
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
        // 1. Find the Player
        // (In a real game, you might want to cache this or use a Singleton, 
        // but FindWithTag is fine for this assignment level)
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            PlayerStats playerStats = player.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                // 2. Give XP
                playerStats.AddXP(xpReward);
            }
        }

        Debug.Log("Enemy Died! Dropping " + xpReward + " XP.");
        Destroy(gameObject);
    }
}