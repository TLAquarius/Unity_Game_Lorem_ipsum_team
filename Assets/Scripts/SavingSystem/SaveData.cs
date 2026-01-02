using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    // --- PROGRESSION ---
    public int currentLevel = 1;
    public float currentHP;

    // --- INVENTORY ---
    public int potionCount;
    public List<string> weaponIDs = new List<string>(); // List of owned weapons

    // --- WORLD STATE ---
    public List<string> unlockedFlags = new List<string>(); // "DoubleJump", "Boss1Dead", "DoorAOpen"

    // --- LOCATION ---
    public float positionX, positionY, positionZ;
    public string sceneName;
}