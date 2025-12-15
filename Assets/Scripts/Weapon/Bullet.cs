using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))] // This ensures the object actually HAS a Rigidbody
public class Bullet : MonoBehaviour
{
    public float speed = 10f;
    public float damage = 10f;

    // We remove 'public' so it doesn't clutter the Inspector. 
    // We will set it automatically in code.
    private Rigidbody2D rb;

    void Awake()
    {
        // AUTOMATICALLY find the Rigidbody on this object
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        // 1. Set the speed
        // Note: 'transform.right' moves it in the direction it is facing (Red Arrow)
        rb.linearVelocity = transform.right * speed;

        // 2. Destroy after 2 seconds to clean up memory
        Destroy(gameObject, 2f);
    }

    void OnTriggerEnter2D(Collider2D hitInfo)
    {
        // Ignore the player so you don't shoot yourself
        if (hitInfo.CompareTag("Player")) return;

        // Check for Enemy
        EnemyStats enemy = hitInfo.GetComponent<EnemyStats>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
        }

        // Destroy bullet if it hits anything solid (or the enemy)
        // We check !isTrigger so bullets don't explode on "Detector" circles
        if (!hitInfo.isTrigger)
        {
            Destroy(gameObject);
        }
    }
}