using UnityEngine;

public class TreasureChest : MonoBehaviour, IInteractable // Uses the Interface
{
    public bool isOpen = false;
    public Sprite openSprite; // Drag an image of an open chest here
    public GameObject itemToDrop; // Drag your WeaponPickup or Potion prefab here

    public void Interact()
    {
        if (isOpen) return;

        Debug.Log("Chest Opened!");
        isOpen = true;

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