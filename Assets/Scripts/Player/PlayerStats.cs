using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Player Stats")]
    public int level = 1;
    public float maxHP = 100f;
    public float currentHP;
    public float defense = 0f;

    [Header("XP System")]
    public float currentXP = 0f;
    public float xpRequired; // How much needed for next level

    void Start()
    {
        currentHP = maxHP;
        CalculateXPRequired(); // Set initial target
    }

    public void CalculateXPRequired()
    {
        // Formula from Proposal: 40 * level^2 + 60 * level
        xpRequired = (40 * (level * level)) + (60 * level);
        Debug.Log("Level " + level + " | XP to next level: " + xpRequired);
    }

    public void AddXP(float amount)
    {
        currentXP += amount;
        Debug.Log("Gained " + amount + " XP. Total: " + currentXP + "/" + xpRequired);

        if (currentXP >= xpRequired)
        {
            LevelUp();
        }
    }

    void LevelUp()
    {
        currentXP -= xpRequired; // Carry over extra XP
        level++;

        // Reward: Increase HP and Heal
        maxHP += 20f; // Increase Max HP
        currentHP = maxHP; // Full heal on level up

        // Recalculate requirement for NEXT level
        CalculateXPRequired();

        Debug.Log("<color=yellow>LEVEL UP! Now Level " + level + "</color>");
        // TODO: Show "Upgrade Menu" or "Skill Tree" here later
    }

    public void TakeDamage(float damageAmount)
    {
        float finalDamage = Mathf.Max(damageAmount - defense, 0);
        currentHP -= finalDamage;
        Debug.Log("Player HP: " + currentHP);

        if (currentHP <= 0) Die();
    }

    public void Heal(float amount)
    {
        currentHP += amount;
        if (currentHP > maxHP) currentHP = maxHP;
    }

    void Die()
    {
        Debug.Log("Player Died!");
        gameObject.SetActive(false); // Disable player
    }
}