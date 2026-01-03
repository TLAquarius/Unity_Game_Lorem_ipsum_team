using UnityEngine;
using System.Collections;

// 1. ADD ISaveable INTERFACE
public class PlayerStats : MonoBehaviour, ISaveable
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
    public float mercyTime = 1.0f;
    public bool isInvincible = false;
    private SpriteRenderer sr;

    public Audio playerAudio;

    // 2. RESPAWN TRACKER
    // This remembers where you last saved so we can teleport you back
    private Vector3 currentRespawnPosition;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        CalculateXPRequired();

        // Default respawn point is where you start the game
        currentRespawnPosition = transform.position;

        // Ensure HP is set if this is a fresh start
        if (currentHP <= 0) currentHP = maxHP;
    }

    // --- XP & LEVELING (Same as before) ---
    public void CalculateXPRequired()
    {
        xpRequired = (40 * (level * level)) + (60 * level);
    }

    public void AddXP(float amount)
    {
        currentXP += amount;
        if (currentXP >= xpRequired) LevelUp();
    }

    void LevelUp()
    {
        currentXP -= xpRequired;
        level++;
        maxHP += 20f;
        currentHP = maxHP; // Full heal on Level Up
        CalculateXPRequired();
        Debug.Log("LEVEL UP! Now Level " + level);
    }

    // --- HEALTH & DAMAGE ---
    public void TakeDamage(float damageAmount)
    {
        if (isInvincible) return;

        float finalDamage = Mathf.Max(damageAmount - defense, 0);
        currentHP -= finalDamage;
        playerAudio.PlayHurt();
        if (currentHP <= 0)
        {
            currentHP = 0;
            Die();
        }
        else
        {
            StartCoroutine(MercyRoutine());
        }
    }

    public void Heal(float amount)
    {
        currentHP += amount;
        if (currentHP > maxHP) currentHP = maxHP;
    }

    // --- 3. UPDATED DEATH LOGIC ---
    void Die()
    {
        playerAudio.PlayDeath();
        Debug.Log("Player Died! Respawning...");
        StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        // A. FREEZE PLAYER
        GetComponent<PlayerController>().enabled = false; // Stop moving
        GetComponent<Collider2D>().enabled = false;       // Can't get hit
        GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero; // Stop physics

        // Visuals: Fade out or turn red
        if (sr != null) sr.enabled = false;

        // B. WAIT
        yield return new WaitForSeconds(2.0f); // 2 Seconds delay

        // C. RESPAWN (Teleport to last Campfire/Save)
        transform.position = currentRespawnPosition;
        currentHP = maxHP; // Fully Heal

        // D. UNFREEZE
        if (sr != null) sr.enabled = true;
        GetComponent<Collider2D>().enabled = true;
        GetComponent<PlayerController>().enabled = true;

        // Optional: Trigger mercy invincibility so you don't die instantly upon respawn
        StartCoroutine(MercyRoutine());
    }

    // --- HELPER FUNCTIONS ---
    public void SetInvincible(bool state) { isInvincible = state; }

    IEnumerator MercyRoutine()
    {
        isInvincible = true;
        float flashDelay = 0.1f;
        for (float t = 0; t < mercyTime; t += flashDelay)
        {
            if (sr != null) sr.color = new Color(1f, 1f, 1f, 0.5f);
            yield return new WaitForSeconds(flashDelay / 2);
            if (sr != null) sr.color = Color.white;
            yield return new WaitForSeconds(flashDelay / 2);
        }
        isInvincible = false;
    }

    // --- 4. IMPLEMENT SAVING ---
    public void SaveData(SaveData data)
    {
        // Save Stats
        data.currentLevel = this.level;
        data.currentHP = this.currentHP;
        // NOTE: We could save currentXP here too if we added it to SaveData.cs

        // CRITICAL: When we save (at a Campfire), update the Respawn Point!
        currentRespawnPosition = transform.position;
    }

    public void LoadData(SaveData data)
    {
        this.level = data.currentLevel;
        this.currentHP = data.currentHP;
        CalculateXPRequired();

        // Update respawn point to the loaded position
        currentRespawnPosition = new Vector3(data.positionX, data.positionY, data.positionZ);
    }
}