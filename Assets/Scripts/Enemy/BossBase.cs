using UnityEngine;
using UnityEngine.UI;

// Inherits from EnemyBase so it gets Hit Sounds, Flash Effects, and Knockback logic
[RequireComponent(typeof(EnemyStats))]
public class BossBase : EnemyBase
{
    [Header("Boss UI")]
    public string bossName = "Boss";
    public Slider healthBar; // Drag a UI Slider here
    public GameObject bossCanvas; // The whole UI object (to hide/show)

    [Header("Level Completion")]
    public VictoryScreen victoryScreen; // DRAG THE VICTORY MANAGER HERE

    [Header("Phase Settings")]
    public bool hasPhase2 = true;
    public float phase2Threshold = 0.5f; // 50% HP
    protected bool isPhase2 = false;
    protected bool isDead = false;

    protected Transform player;
    protected EnemyStats stats;
    protected Animator anim;

    protected virtual void Start()
    {
        // Setup Components
        stats = GetComponent<EnemyStats>();
        anim = GetComponent<Animator>();
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        // Listen to Damage Event
        if (stats != null)
        {
            stats.OnTakeDamage += UpdateHealthUI;

            // Setup UI Max Values
            if (healthBar != null)
            {
                healthBar.maxValue = stats.maxHP;
                healthBar.value = stats.currentHP;
            }
        }

        // Show Health Bar
        if (bossCanvas != null) bossCanvas.SetActive(true);
    }

    // Called automatically when EnemyStats takes damage
    protected virtual void UpdateHealthUI()
    {
        if (isDead) return;

        // 1. Update Slider
        if (healthBar != null) healthBar.value = stats.currentHP;

        // 2. Play Hurt Effect (From EnemyBase)
        PlayHitFeedback();

        // 3. Check Phase 2
        if (hasPhase2 && !isPhase2 && stats.currentHP <= stats.maxHP * phase2Threshold)
        {
            EnterPhase2();
        }

        // 4. Check Death
        if (stats.currentHP <= 0)
        {
            Die();
        }
    }

    protected virtual void EnterPhase2()
    {
        isPhase2 = true;
        Debug.Log(bossName + " ENRAGED!");

        // Visual Feedback: Turn Red
        GetComponent<SpriteRenderer>().color = new Color(1f, 0.5f, 0.5f); // Red tint

        // Push player away slightly on phase change
        if (player != null)
        {
            Vector2 pushDir = (player.position - transform.position).normalized;
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null) pc.ApplyKnockback(pushDir * 10f);
        }
    }

    protected virtual void Die()
    {
        isDead = true;
        if (anim) anim.SetTrigger("Death");

        Debug.Log("Boss Defeated!");

        // Hide Boss Health Bar
        if (healthBar != null) healthBar.gameObject.SetActive(false);
        if (bossCanvas != null) bossCanvas.SetActive(false);

        // Disable Physics
        GetComponent<Collider2D>().enabled = false;
        GetComponent<Rigidbody2D>().simulated = false;

        // --- TRIGGER VICTORY ---
        if (victoryScreen != null)
        {
            victoryScreen.ShowVictory(); // Call the UI
        }
        else
        {
            Debug.LogWarning("Victory Screen not assigned to Boss!");
        }

        // Destroy object handled by EnemyStats death delay (usually 1-2 seconds)
        // Note: Destroying the boss object is fine, provided the Victory Screen script is on a separate GameManager object.
    }
}