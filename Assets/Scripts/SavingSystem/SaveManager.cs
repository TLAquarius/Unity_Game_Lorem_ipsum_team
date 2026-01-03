using UnityEngine;
using System.IO;
using System.Linq;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;
    private string savePath;

    // --- 1. MISSING VARIABLE ADDED HERE ---
    public static bool ShouldLoadOnStart = false;

    // Flag: Should we restore data after the scene finishes loading?
    private bool isRestoringData = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        savePath = Application.persistentDataPath + "/savegame.json";
    }

    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // --- 2. CHECK THE FLAG HERE ---
        // If MainMenu said "Load Game", we trigger the load now.
        if (ShouldLoadOnStart)
        {
            ShouldLoadOnStart = false; // Reset flag so it doesn't happen again
            LoadGame();
            return; // LoadGame handles the rest
        }

        // If we just loaded a level because we were redirecting (e.g. Loading Level 2)
        if (isRestoringData)
        {
            RestoreDataToScripts();
            isRestoringData = false;
        }
    }

    public void SaveGame()
    {
        SaveData data = new SaveData();
        data.sceneName = SceneManager.GetActiveScene().name;

        var saveables = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<ISaveable>();
        foreach (var saveable in saveables)
        {
            saveable.SaveData(data);
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);
        Debug.Log("Game Saved (Scene: " + data.sceneName + ")");
    }

    public void LoadGame()
    {
        if (!File.Exists(savePath))
        {
            Debug.Log("No save file found.");
            return;
        }

        string json = File.ReadAllText(savePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        // CHECK: Are we in the correct scene?
        string activeScene = SceneManager.GetActiveScene().name;

        if (!string.IsNullOrEmpty(data.sceneName) && activeScene != data.sceneName)
        {
            // If we are in "Level 1" but save says "Level 2", load Level 2 first
            isRestoringData = true;
            SceneManager.LoadScene(data.sceneName);
        }
        else
        {
            // We are already in the right scene, just restore data immediately
            RestoreDataToScripts();
        }
    }

    private void RestoreDataToScripts()
    {
        if (!File.Exists(savePath)) return;
        string json = File.ReadAllText(savePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        var saveables = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<ISaveable>();
        foreach (var saveable in saveables)
        {
            saveable.LoadData(data);
        }
        Debug.Log("Data Restored successfully.");
    }
}