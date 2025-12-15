using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Weapon/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Info")]
    public string weaponName;
    public Sprite icon;
    public string description;

    [Header("Stats")]
    public float damage = 20f;
    public float attackRate = 2f; // Attacks per second
    public float attackRange = 0.5f;

    [Header("Type")]
    public bool isRanged = false;
    public GameObject projectilePrefab; // Only for Ranged
    public float projectileSpeed = 10f;

    [Header("Visuals")]
    public Color weaponColor = Color.white; // To tint the sword sprite
}