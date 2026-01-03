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
    private Animator anim;

    public Audio playerAudio;

    // 2. RESPAWN TRACKER
    // This remembers where you last saved so we can teleport you back
    private Vector3 currentRespawnPosition;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
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
    // --- UPDATED DEATH LOGIC ---
    void Die()
    {
        // 1. Trigger Animation
        anim.SetTrigger("Death");

        // 2. Play Sound
        if (playerAudio != null) playerAudio.PlayDeath();

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        Debug.Log("Player Died! Playing animation...");
        StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        
        yield return null;

        float deathAnimLength = anim.GetCurrentAnimatorStateInfo(0).length;

       
        yield return new WaitForSeconds(deathAnimLength + 1.0f);

        transform.position = currentRespawnPosition; 
        currentHP = maxHP; // Full Heal

        anim.Rebind(); 
        anim.Update(0f);


        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic; // Turn gravity back on
        GetComponent<PlayerController>().enabled = true;


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