using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject pauseMenuUI;   // The main buttons (Resume, Save, Settings, Quit)
    public GameObject settingsPanel; // The reusable Settings Prefab
    public GameObject controlsPanel; // The Key Binding panel

    [Header("Auto-Link Buttons (Optional)")]
    // Drag the buttons from your Prefabs into these slots to ensure they work
    public Button settingsCloseButton; // The "Back" button in Settings
    public Button controlsOpenButton;  // The "Controls" button in Settings
    public Button controlsBackButton;  // The "Back" button in Controls Panel

    public static bool IsPaused = false;

    void Start()
    {
        Resume(); // Ensure game starts in a clean state

        // --- DYNAMICALLY LINK BUTTONS ---
        // This ensures buttons work even if connections break in the Inspector
        if (settingsCloseButton != null)
        {
            settingsCloseButton.onClick.RemoveAllListeners();
            settingsCloseButton.onClick.AddListener(CloseSettings);
        }

        if (controlsOpenButton != null)
        {
            controlsOpenButton.onClick.RemoveAllListeners();
            controlsOpenButton.onClick.AddListener(OpenControls);
        }

        if (controlsBackButton != null)
        {
            controlsBackButton.onClick.RemoveAllListeners();
            controlsBackButton.onClick.AddListener(CloseControls);
        }
    }

    void Update()
    {
        // 1. USE THE NEW INPUT SYSTEM
        // We check GameInput.Instance to ensure it exists before accessing
        if (GameInput.Instance != null && GameInput.Instance.IsPausePressed())
        {
            if (IsPaused)
                Resume();
            else
                Pause();
        }
    }

    // --- PAUSE LOGIC ---

    public void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0f; // Freeze Physics & Time
        pauseMenuUI.SetActive(true);

        // Ensure other panels are closed when we first open Pause
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);
    }

    public void Resume()
    {
        IsPaused = false;
        Time.timeScale = 1f; // Unfreeze Time

        // Hide ALL panels
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);
    }

    // --- NAVIGATION FUNCTIONS ---

    public void OpenSettings()
    {
        pauseMenuUI.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        pauseMenuUI.SetActive(true); // Go back to Main Pause Menu
    }

    public void OpenControls()
    {
        settingsPanel.SetActive(false); // Hide Settings
        controlsPanel.SetActive(true);  // Show Controls
    }

    public void CloseControls()
    {
        controlsPanel.SetActive(false); // Hide Controls
        settingsPanel.SetActive(true);  // Go back to Settings
    }

    // --- GAME ACTIONS ---

    public void SaveGame()
    {
        // Safety check to prevent crashing if SaveManager is missing
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGame();
        }
        else
        {
            Debug.LogError("SaveManager Instance is null! Is it in the scene?");
        }
    }

    public void QuitToMainMenu()
    {
        Time.timeScale = 1f; // CRITICAL: Always unfreeze time before leaving!
        SceneManager.LoadScene("MainMenu");
    }
}