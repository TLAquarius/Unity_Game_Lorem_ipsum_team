using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("Settings")]
    public bool oneTimeUse = false; // Should it save every time or just once?
    
    [Header("Visuals")]
    public GameObject activeLight; // Optional: Drag a light/particle here
    
    private bool activated = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Check if it is the player
        if (other.CompareTag("Player"))
        {
            // 2. Avoid spamming saves (optional)
            if (oneTimeUse && activated) return;

            ActivateCheckpoint();
        }
    }

    void ActivateCheckpoint()
    {
        activated = true;

        // 3. Visual Feedback
        if (activeLight != null) activeLight.SetActive(true);
        Debug.Log("Checkpoint Reached! Saving...");

        // 4. TRIGGER THE SAVE
        // This captures the player's CURRENT position as the new load spot.
        SaveManager.Instance.SaveGame();
        
        // Optional: Refill potions or Health here?
        // PlayerStats stats = FindFirstObjectByType<PlayerStats>();
        // stats.Heal(999);
    }
}