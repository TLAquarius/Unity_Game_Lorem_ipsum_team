using UnityEngine;
using System.Collections; // Required for Coroutines (IEnumerator)

public class PlayerStats : MonoBehaviour
{
    [Header("Player Stats")]
    public int level = 1;
    public float maxHP = 100f;
    public float currentHP;
    public float defense = 0f;

    [Header("XP System")]
    public float currentXP = 0f;
    public float xpRequired;

    [Header("Invincibility Settings")]
    public float mercyTime = 1.0f;    // Duration of safety after getting hit
    public bool isInvincible = false; // Controlled by Dash or Mercy
    private SpriteRenderer sr;        // Used to flash the character

    void Start()
    {
        currentHP = maxHP;
        sr = GetComponent<SpriteRenderer>(); // Get reference to visuals
        CalculateXPRequired();
    }

    // --- XP & LEVELING LOGIC ---

    public void CalculateXPRequired()
    {
        
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
        currentXP -= xpRequired;
        level++;

        // Reward: Increase HP and fully heal
        maxHP += 20f;
        currentHP = maxHP;

        CalculateXPRequired();

        Debug.Log("<color=yellow>LEVEL UP! Now Level " + level + "</color>");
    }

    // --- HEALTH & DAMAGE LOGIC ---

    public void TakeDamage(float damageAmount)
    {
        // 1. CHECK INVINCIBILITY (Dash or Mercy)
        if (isInvincible) return;

        // 2. APPLY DAMAGE
        float finalDamage = Mathf.Max(damageAmount - defense, 0);
        currentHP -= finalDamage;
        Debug.Log("Player HP: " + currentHP);

        if (currentHP <= 0)
        {
            Die();
        }
        else
        {
            // 3. TRIGGER MERCY INVINCIBILITY
            StartCoroutine(MercyRoutine());
        }
    }

    public void Heal(float amount)
    {
        currentHP += amount;
        if (currentHP > maxHP) currentHP = maxHP;
    }

    void Die()
    {
        Debug.Log("Player Died!");
        gameObject.SetActive(false);
    }

    // --- HELPER FUNCTIONS ---

    // Called by Dash/Dodge script to force safety
    public void SetInvincible(bool state)
    {
        isInvincible = state;
    }

    // Flashes the character transparently to indicate safety
    IEnumerator MercyRoutine()
    {
        isInvincible = true;

        float flashDelay = 0.1f;
        // Flash for 'mercyTime' seconds
        for (float t = 0; t < mercyTime; t += flashDelay)
        {
            if (sr != null) sr.color = new Color(1f, 1f, 1f, 0.5f); // 50% Transparent
            yield return new WaitForSeconds(flashDelay / 2);

            if (sr != null) sr.color = Color.white; // Normal
            yield return new WaitForSeconds(flashDelay / 2);
        }

        isInvincible = false;
    }
}