using UnityEngine;
using UnityEngine.SceneManagement;

public class VictoryScreen : MonoBehaviour
{
	[Header("Settings")]
	public string nextSceneName; // Name of the next level (e.g., "Level2")
	public GameObject uiPanel;   // Drag the UI Panel here

	void Start()
	{
		// Ensure the screen is hidden when the game starts
		if (uiPanel != null) uiPanel.SetActive(false);
	}

	// Called by the Boss when it dies
	public void ShowVictory()
	{
		Debug.Log("Victory! Showing UI.");
		if (uiPanel != null) uiPanel.SetActive(true);

		// Optional: Pause the game so physics stops
		Time.timeScale = 0f;
	}

	// Link this function to your "Next Level" Button
	public void OnNextLevelClicked()
	{
		// 1. Unpause the game (IMPORTANT)
		Time.timeScale = 1f;

		// 2. Save progress (HP, Inventory, etc.) before leaving
		if (SaveManager.Instance != null)
		{
			SaveManager.Instance.SaveGame();
		}

		// 3. Load the next scene
		SceneManager.LoadScene(nextSceneName);
	}

	// Link this to a "Main Menu" button if you have one
	public void OnMainMenuClicked()
	{
		Time.timeScale = 1f;
		if (SaveManager.Instance != null) SaveManager.Instance.SaveGame();
		SceneManager.LoadScene("MainMenu");
	}
}