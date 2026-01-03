using UnityEngine;

public class Campfire : MonoBehaviour, IInteractable
{
    [Header("Visuals")]
    public ParticleSystem fireParticles; // Optional: Drag particles here

    public void Interact()
    {
        Debug.Log("Resting at Campfire...");

        // 1. Heal the Player
        PlayerStats stats = FindFirstObjectByType<PlayerStats>();
        if (stats != null)
        {
            stats.Heal(999); // Fully heal
        }

        // 2. Save the Game (Record position, inventory, etc.)
        SaveManager.Instance.SaveGame();

        // 3. Visual Feedback
        if (fireParticles != null) fireParticles.Play();

        Debug.Log("Game Saved & Player Healed!");
    }
}