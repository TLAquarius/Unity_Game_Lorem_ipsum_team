using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewLootTable", menuName = "Game/Loot Table")]
public class LootTable : ScriptableObject
{
    [System.Serializable]
    public class Drop
    {
        public GameObject itemPrefab;
        [Range(0, 100)] public float dropChance;
    }

    public List<Drop> drops;

    public GameObject GetDrop()
    {
        foreach (var drop in drops)
        {
            if (Random.Range(0f, 100f) <= drop.dropChance)
            {
                return drop.itemPrefab;
            }
        }
        return null;
    }
}