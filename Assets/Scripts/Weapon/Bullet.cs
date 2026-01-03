using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    public float speed = 10f;
    public float damage = 10f;
    public float lifeTime = 2f;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // CALL THIS FUNCTION IMMEDIATELY AFTER INSTANTIATING THE BULLET
    public void Setup(Vector2 moveDirection)
    {
        // 1. Calculate the angle from the direction vector
        // Mathf.Atan2(y, x) gives radians, convert to degrees
        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;

        // 2. Apply Rotation with Offset
        // Since your sprite faces DOWN, we ADD 90 degrees to make it face the move direction.
        transform.rotation = Quaternion.Euler(0, 0, angle + 90f);

        // 3. Set Velocity
        // We use the original direction vector for movement
        rb.linearVelocity = moveDirection.normalized * speed;

        // 4. Destroy after lifetime
        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter2D(Collider2D hitInfo)
    {
        if (hitInfo.CompareTag("Player")) return;

        // Using your "EnemyStats" script reference
        EnemyStats enemy = hitInfo.GetComponent<EnemyStats>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
        }

        if (!hitInfo.isTrigger)
        {
            Destroy(gameObject);
        }
    }
}