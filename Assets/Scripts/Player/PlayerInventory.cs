using UnityEngine;
using UnityEngine.UI; // For the UI Text

public class PlayerInventory : MonoBehaviour
{
    [Header("Potions")]
    public int potionCount = 0;
    public int maxPotions = 3; // Limit how many they can carry
    public float healAmount = 30f;

    [Header("UI Reference")]
    public Text potionCountText; // Drag a UI Text here later

    private PlayerStats stats;
    private GameInput input;

    void Start()
    {
        stats = GetComponent<PlayerStats>();
        input = GetComponent<GameInput>();
        UpdateUI();
    }

    void Update()
    {
        // Check for Input
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
        // 1. Check if we have potions
        if (potionCount <= 0) return;

        // 2. Check if we actually need healing
        if (stats.currentHP >= stats.maxHP) return;

        // 3. Heal and Reduce Count
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

    // 3. Implement Load
    public void LoadData(SaveData data)
    {
        this.potionCount = data.potionCount;
        UpdateUI(); // Refresh UI immediately after loading
    }
}