using UnityEngine;

public class WeaponPickup : MonoBehaviour, IInteractable
{
    public WeaponData weaponToDrop;
    public bool equipToMainSlot = true; // Check this in Inspector if it's a Main Weapon

    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null && weaponToDrop != null)
        {
            sr.sprite = weaponToDrop.icon;
            sr.color = weaponToDrop.weaponColor;
        }
        // Ensure this object is on the "Interactable" Layer!
        gameObject.layer = LayerMask.NameToLayer("Interactable");
    }

    // This function runs when Player stands close and presses 'F'
    public void Interact()
    {
        // Find the player (who called this function?)
        // Since Interact() doesn't pass the player, we find them or use a singleton.
        // For simplicity, we assume Player is near.
        WeaponController playerWeapon = FindFirstObjectByType<WeaponController>();

        if (playerWeapon != null)
        {
            playerWeapon.EquipWeapon(weaponToDrop, equipToMainSlot);
            Destroy(gameObject); // Disappear
        }
    }
}