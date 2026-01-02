using UnityEngine;
using System.Collections;

public class SafeGroundTracker : MonoBehaviour
{
    [Header("Settings")]
    public LayerMask safeGroundLayer; // Set this to "Ground"
    public LayerMask hazardLayer;     // Set this to "Spikes/Traps"
    public float checkInterval = 0.5f;

    private Vector3 lastSafePosition;
    private Rigidbody2D rb;
    private PlayerStats stats; // Assumes you have this based on PlayerInventory reference

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        stats = GetComponent<PlayerStats>();
        lastSafePosition = transform.position;
        StartCoroutine(TrackPositionRoutine());
    }

    IEnumerator TrackPositionRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);
            if (IsSafeToRecord())
            {
                lastSafePosition = transform.position;
            }
        }
    }

    bool IsSafeToRecord()
    {
        // 1. Must be stable (not jumping/falling)
        if (Mathf.Abs(rb.linearVelocity.y) > 0.1f) return false;

        // 2. Solid Footing Check (BoxCast)
        // Checks if feet are firmly on the ground, not hanging off an edge
        Vector2 boxSize = new Vector2(0.5f, 0.1f);
        RaycastHit2D hit = Physics2D.BoxCast(transform.position, boxSize, 0f, Vector2.down, 1.0f, safeGroundLayer);

        if (hit.collider == null) return false;

        // 3. Danger Zone Check
        // Don't save if we are too close to a spike (1 unit radius)
        bool hazardNearby = Physics2D.OverlapCircle(transform.position, 1.0f, hazardLayer);

        return !hazardNearby;
    }

    public void Respawn(int damage)
    {
        StartCoroutine(RespawnSequence(damage));
    }

    IEnumerator RespawnSequence(int damage)
    {
        // 1. Stop Physics
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        // 2. Deal Damage
        if (stats != null) stats.TakeDamage(damage);

        // 3. Optional: Screen Fade Black here

        // 4. Teleport
        transform.position = lastSafePosition;
        yield return new WaitForSeconds(0.2f);

        // 5. Restore Physics
        rb.bodyType = RigidbodyType2D.Dynamic;
    }

    // DEBUG VISUALS
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 1.0f); // Hazard check radius
    }
}