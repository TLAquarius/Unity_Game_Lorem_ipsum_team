using UnityEngine;
using UnityEngine.UI; // For HP Bar

public class BossBase : MonoBehaviour
{
    [Header("Base Stats")]
    public float maxHP = 500f;
    public float currentHP;
    public string bossName = "Boss";
    public Slider healthBar; // Drag UI Slider here

    [Header("Phases")]
    public bool hasPhase2 = false;
    public float phase2Threshold = 0.5f; // 50% HP
    protected bool isPhase2 = false;
    protected bool isDead = false;

    protected Transform player;
    protected EnemyStats stats; // Reuse your existing EnemyStats for damage logic

    protected virtual void Start()
    {
        currentHP = maxHP;
        player = GameObject.FindGameObjectWithTag("Player").transform;

        // Setup UI
        if (healthBar != null)
        {
            healthBar.maxValue = maxHP;
            healthBar.value = currentHP;
        }
    }

    // Called by EnemyStats when hit
    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHP -= damage;
        if (healthBar != null) healthBar.value = currentHP;

        // Check Phase 2
        if (hasPhase2 && !isPhase2 && currentHP <= maxHP * phase2Threshold)
        {
            EnterPhase2();
        }

        // Check Death
        if (currentHP <= 0)
        {
            Die();
        }
    }

    protected virtual void EnterPhase2()
    {
        isPhase2 = true;
        Debug.Log(bossName + " enters Phase 2!");
        // Play Roar Animation / Change Color
        GetComponent<SpriteRenderer>().color = Color.red;
    }

    protected virtual void Die()
    {
        isDead = true;
        Debug.Log(bossName + " Defeated!");
        Destroy(gameObject);
        // Trigger Level Complete UI here
    }
}