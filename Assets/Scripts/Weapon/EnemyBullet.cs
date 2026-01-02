using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyBullet : MonoBehaviour
{
    [Header("Bullet Stats")]
    public float speed = 7f;
    public float damage = 10f;

    [Header("Knockback Settings")]
    [Tooltip("X: Horizontal Push, Y: Vertical Lift")]
    public Vector2 knockbackProfile = new Vector2(5f, 2f); // Lighter push than a melee hit

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        // 1. Move in the direction the bullet is facing
        rb.linearVelocity = transform.right * speed;

        // 2. Safety destroy
        Destroy(gameObject, 5f);
    }

    void OnTriggerEnter2D(Collider2D hitInfo)
    {
        // 1. If it hits the PLAYER
        if (hitInfo.CompareTag("Player"))
        {
            // A. Deal Health Damage
            PlayerStats hp = hitInfo.GetComponent<PlayerStats>();
            if (hp != null)
            {
                hp.TakeDamage(damage);
            }

            // B. Apply Knockback (NEW)
            PlayerController move = hitInfo.GetComponent<PlayerController>();
            if (move != null)
            {
                // Calculate push direction based on which way the bullet is flying
                float directionX = Mathf.Sign(rb.linearVelocity.x);

                // Construct the specific force vector
                Vector2 finalForce = new Vector2(knockbackProfile.x * directionX, knockbackProfile.y);

                // Send to Player
                move.ApplyKnockback(finalForce);
            }

            // C. Destroy Bullet
            Destroy(gameObject);
        }
        // 2. If it hits GROUND
        else if (hitInfo.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            // Optional: Instantiate a "Spark" or "Poof" particle effect here
            Destroy(gameObject);
        }
    }
}