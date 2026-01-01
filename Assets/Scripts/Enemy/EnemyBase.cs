using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    [Header("Combat Configuration")]
    [Tooltip("Damage dealt when the player touches this enemy's body.")]
    public float touchDamage = 5f;

    [Tooltip("Damage dealt when this enemy lands an attack.")]
    public float attackDamage = 10f;

    [Header("Knockback Settings")]
    [Tooltip("X: Horizontal Push, Y: Vertical Lift")]
    public Vector2 knockbackProfile = new Vector2(8f, 5f);

    // 1. TOUCH DAMAGE (Automatic Collision)
    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Determine which side the player is on (Left or Right)
            float hitDirection = Mathf.Sign(collision.transform.position.x - transform.position.x);

            // Apply Damage & Knockback
            ApplyCustomKnockback(collision.gameObject, touchDamage, hitDirection);
        }
    }

    // 2. HELPER FUNCTION (Call this from AI Scripts)
    public void ApplyCustomKnockback(GameObject target, float damageAmount, float hitDirectionX)
    {
        // A. Deal HP Damage
        PlayerStats hp = target.GetComponent<PlayerStats>();
        if (hp != null)
        {
            hp.TakeDamage(damageAmount);
        }

        // B. Calculate Physics Force
        // We use the Inspector values (knockbackProfile) but flip the X based on direction
        Vector2 finalForce = new Vector2(knockbackProfile.x * hitDirectionX, knockbackProfile.y);

        // C. Apply to Player Controller
        PlayerController movement = target.GetComponent<PlayerController>();
        if (movement != null)
        {
            movement.ApplyKnockback(finalForce);
        }
    }
}