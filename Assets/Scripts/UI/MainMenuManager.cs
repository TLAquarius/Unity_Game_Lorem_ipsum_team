using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;

public class MainMenuManager : MonoBehaviour
{
    [Header("Main Buttons")]
    public Button loadGameButton;
    public string firstLevelName = "Level1"; // CHANGE to your Scene Name

    [Header("Panels")]
    public GameObject mainPanel;      // The group of Play/Load/Options buttons
    public GameObject settingsPanel;  // The black box with slider/toggle
    public GameObject controlsPanel;  // The panel with rebind buttons

    [Header("Settings Components")]
    public Slider volumeSlider;
    public Toggle fullscreenToggle;

    private string savePath;

    void Start()
    {
        // 1. Setup Save Path (Matches SaveManager)
        savePath = Application.persistentDataPath + "/savegame.json";

        // 2. Check if Save File Exists
        if (File.Exists(savePath))
        {
            loadGameButton.interactable = true;
        }
        else
        {
            loadGameButton.interactable = false; // Grey out if no save
                                                 // Optional: Make it look transparent
            var colors = loadGameButton.colors;
            colors.disabledColor = new Color(1, 1, 1, 0.3f);
            loadGameButton.colors = colors;
        }

        // 3. Initialize Panels
        mainPanel.SetActive(true);
        settingsPanel.SetActive(false);
        controlsPanel.SetActive(false);

        // 4. Initialize Settings Values
        volumeSlider.value = AudioListener.volume;
        fullscreenToggle.isOn = Screen.fullScreen;

        // 5. Add Listeners
        volumeSlider.onValueChanged.AddListener(SetVolume);
        fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
    }

    // --- MAIN MENU FUNCTIONS ---

    public void OnPlayClicked()
    {
        // START NEW GAME
        SaveManager.ShouldLoadOnStart = false; // Reset flag
        SceneManager.LoadScene(firstLevelName);
    }

    public void OnLoadClicked()
    {
        // CONTINUE GAME
        SaveManager.ShouldLoadOnStart = true; // Tell next scene to load data
        SceneManager.LoadScene(firstLevelName);
    }

    public void OnQuitClicked()
    {
        Application.Quit();
        Debug.Log("Quit Game");
    }

    // --- PANEL NAVIGATION ---

    public void OpenSettings()
    {
        mainPanel.SetActive(false);     // Hide Main Menu
        settingsPanel.SetActive(true);  // Show Settings
        volumeSlider.Select();          // Select slider for Keyboard support
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        mainPanel.SetActive(true);

        // Select the Options button so keyboard navigation isn't lost
        // (You'll need to drag the Options button here if you want strict logic, 
        // or just let Unity find the first button)
    }

    public void OpenControls()
    {
        settingsPanel.SetActive(false);
        controlsPanel.SetActive(true);
        // Select first rebind button (logic handled in UI setup)
    }

    public void CloseControls()
    {
        controlsPanel.SetActive(false);
        settingsPanel.SetActive(true);
        // Select the Controls button again
    }

    // --- SETTINGS LOGIC ---
    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }
}