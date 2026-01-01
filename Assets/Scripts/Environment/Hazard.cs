using UnityEngine;

public class Hazard : MonoBehaviour
{
    public int damage = 1;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            SafeGroundTracker tracker = other.GetComponent<SafeGroundTracker>();
            if (tracker != null)
            {
                tracker.Respawn(damage);
            }
        }
    }
}