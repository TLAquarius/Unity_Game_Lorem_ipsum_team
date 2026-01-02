using UnityEngine;

public class DamageZone : MonoBehaviour
{
    public float damage = 50f;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerStats stats = other.GetComponent<PlayerStats>();
        if (stats == null) return;

        stats.TakeDamage(damage);
    }
}
