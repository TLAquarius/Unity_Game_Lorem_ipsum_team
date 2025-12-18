using UnityEngine;

public class DamageZone : MonoBehaviour
{
    [Header("Damage Settings")]
    public float damage = 10f;

    // Keep this for the instant impact when they first touch it
    void OnTriggerEnter2D(Collider2D other)
    {
        DealDamage(other);
    }

    // Add this to keep hurting them while they stand inside
    void OnTriggerStay2D(Collider2D other)
    {
        DealDamage(other);
    }

    // Helper function to avoid writing the same code twice
    void DealDamage(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerStats stats = other.GetComponent<PlayerStats>();

        // We can safely spam this because PlayerStats.cs checks 'isInvincible'
        if (stats != null)
        {
            stats.TakeDamage(damage);
        }
    }
}