using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    public WeaponData weaponToDrop; // Which weapon is this?

    // Optional: Visual setup
    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null && weaponToDrop != null)
        {
            sr.sprite = weaponToDrop.icon; // Show the correct sword/bow icon on ground
            sr.color = weaponToDrop.weaponColor;
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            WeaponController playerWeapon = collision.GetComponent<WeaponController>();
            if (playerWeapon != null)
            {
                // Equip the new weapon
                playerWeapon.EquipWeapon(weaponToDrop);

                // Destroy the pickup object
                Destroy(gameObject);
            }
        }
    }
}