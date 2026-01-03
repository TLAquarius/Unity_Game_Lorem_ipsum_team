using UnityEngine;

// 1. Automatically add an AudioSource component if missing
[RequireComponent(typeof(AudioSource))]
public class TreasureChest : MonoBehaviour, IInteractable
{
    public bool isOpen = false;

    [Header("Visuals")]
    public Sprite openSprite; // Drag image of open chest here

    [Header("Loot")]
    public GameObject itemToDrop; // Drag WeaponPickup or Potion prefab here

    [Header("Audio")]
    public AudioClip openSound; // <--- Drag your chest opening sound here

    private AudioSource audioSource;

    void Start()
    {
        // 2. Get the reference to the AudioSource component
        audioSource = GetComponent<AudioSource>();
    }

    public void Interact()
    {
        if (isOpen) return;

        Debug.Log("Chest Opened!");
        isOpen = true;

        // 3. Play Sound
        if (audioSource != null && openSound != null)
        {
            audioSource.PlayOneShot(openSound);
        }

        // Change visual
        if (openSprite != null)
            GetComponent<SpriteRenderer>().sprite = openSprite;

        // Drop Item
        if (itemToDrop != null)
        {
            Instantiate(itemToDrop, transform.position + Vector3.up, Quaternion.identity);
        }
    }
}