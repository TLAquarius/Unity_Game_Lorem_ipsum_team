using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // Look for the Inventory script instead of Stats
            PlayerInventory inventory = collision.GetComponent<PlayerInventory>();

            if (inventory != null)
            {
                // Only destroy the item if the player successfully picked it up
                // (e.g., Inventory wasn't full)
                if (inventory.potionCount < inventory.maxPotions)
                {
                    inventory.AddPotion();
                    Destroy(gameObject); // Disappear from world
                }
            }
        }
    }
}