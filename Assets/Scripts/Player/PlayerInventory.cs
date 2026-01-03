using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))] // Ensures Player has AudioSource
public class PlayerInventory : MonoBehaviour
{
    [Header("Potions")]
    public int potionCount = 0;
    public int maxPotions = 3;
    public float healAmount = 30f;

    [Header("Audio")]
    public AudioClip potionPickupSound; // Drag your 'Glug' or 'Pickup' sound here

    [Header("UI Reference")]
    public Text potionCountText;

    private PlayerStats stats;
    private GameInput input;
    private AudioSource audioSource; // Reference to audio source

    void Start()
    {
        stats = GetComponent<PlayerStats>();
        input = GetComponent<GameInput>();
        audioSource = GetComponent<AudioSource>(); // Get the component
        UpdateUI();
    }

    void Update()
    {
        if (input.IsHealPressed())
        {
            UsePotion();
        }
    }

    public void AddPotion()
    {
        if (potionCount < maxPotions)
        {
            potionCount++;

            // PLAY SOUND HERE
            if (audioSource != null && potionPickupSound != null)
            {
                audioSource.PlayOneShot(potionPickupSound);
            }

            UpdateUI();
            Debug.Log("Picked up Potion! Count: " + potionCount);
        }
        else
        {
            Debug.Log("Inventory Full!");
        }
    }

    void UsePotion()
    {
        if (potionCount <= 0) return;
        if (stats.currentHP >= stats.maxHP) return;

        stats.Heal(healAmount);
        potionCount--;
        UpdateUI();
    }

    void UpdateUI()
    {
        if (potionCountText != null)
        {
            potionCountText.text = "x " + potionCount;
        }
    }

    public void SaveData(SaveData data)
    {
        data.potionCount = this.potionCount;
    }

    public void LoadData(SaveData data)
    {
        this.potionCount = data.potionCount;
        UpdateUI();
    }
}