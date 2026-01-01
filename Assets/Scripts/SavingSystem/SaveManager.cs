using UnityEngine;
using System.IO;
using System.Linq; // Important for finding scripts

public class SaveManager : MonoBehaviour
{
	public static SaveManager Instance;
	private string savePath;

	private void Awake()
	{
		if (Instance == null) Instance = this;
		else Destroy(gameObject);

		savePath = Application.persistentDataPath + "/savegame.json";
	}

	public void SaveGame()
	{
		SaveData data = new SaveData();

		// 1. Find all objects that implement ISaveable (Player, Inventory, etc.)
		// Note: This finds objects in the current scene only
		var saveables = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<ISaveable>();

		foreach (var saveable in saveables)
		{
			saveable.SaveData(data);
		}

		// 2. Write to File
		string json = JsonUtility.ToJson(data, true);
		File.WriteAllText(savePath, json);
		Debug.Log("Game Saved to: " + savePath);
	}

	public void LoadGame()
	{
		if (!File.Exists(savePath))
		{
			Debug.Log("No save file found.");
			return;
		}

		// 1. Read from File
		string json = File.ReadAllText(savePath);
		SaveData data = JsonUtility.FromJson<SaveData>(json);

		// 2. Distribute data back to scripts
		var saveables = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<ISaveable>();

		foreach (var saveable in saveables)
		{
			saveable.LoadData(data);
		}

		Debug.Log("Game Loaded.");
	}
}