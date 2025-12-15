using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyBullet : MonoBehaviour
{
    public float speed = 7f;
    public float damage = 10f;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        // 1. Move in the direction the bullet is facing (Right Axis)
        rb.linearVelocity = transform.right * speed;
        Destroy(gameObject, 3f); // Destroy after 3 seconds
    }

    void OnTriggerEnter2D(Collider2D hitInfo)
    {
        // 1. If it hits the PLAYER
        if (hitInfo.CompareTag("Player"))
        {
            PlayerStats player = hitInfo.GetComponent<PlayerStats>();
            if (player != null)
            {
                player.TakeDamage(damage);
            }
            Destroy(gameObject);
        }
        // 2. If it hits GROUND (Layer check)
        else if (hitInfo.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
        }
        // Ignore other enemies
    }
}