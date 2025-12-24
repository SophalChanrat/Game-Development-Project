using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Manages game pause functionality with Resume, Settings, and Main Menu options
/// </summary>
public class PauseManager : MonoBehaviour
{
    [Header("UI Panels")]
    [Tooltip("Main pause menu panel")]
    public GameObject pauseMenuPanel;
    
    [Tooltip("Settings panel (shows when Settings button clicked)")]
    public GameObject settingsPanel;
    
    [Header("Settings UI")]
    [Tooltip("Slider to control music volume")]
    public Scrollbar musicVolumeSlider;
    
    [Tooltip("Text to display current volume percentage (optional)")]
    public TextMeshProUGUI volumeText;
    
    [Header("Pause Settings")]
    [Tooltip("Name of your main menu scene")]
    public string mainMenuSceneName = "MainMenu";
    
    // State tracking
    private bool isPaused = false;
    
    // Singleton
    public static PauseManager Instance { get; private set; }

    void Awake()
    {
        // Singleton setup (optional, but useful)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // Make sure pause menu is hidden at start
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
        
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
        
        // Initialize music volume slider
        if (musicVolumeSlider != null && MusicManager.Instance != null)
        {
            // Set slider to current music volume
            musicVolumeSlider.value = MusicManager.Instance.masterVolume;
            
            // Add listener to update volume when slider changes
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            
            // Update volume text if present
            UpdateVolumeText(musicVolumeSlider.value);
        }
    }

    // Input System callback - This is what the Input System will call
    public void OnBack(InputAction.CallbackContext context)
    {
        if (context.started)
        {   
            TogglePause();
        }
    }

    #region Public Methods - Called by UI Buttons

    /// <summary>
    /// Toggle pause state (can also be called by a pause button)
    /// </summary>
    public void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    /// <summary>
    /// Resume the game - Called by Resume button
    /// </summary>
    public void ResumeGame()
    {
        if (!isPaused) return;
        
        isPaused = false;
        
        // Hide pause menu
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
        
        // Hide settings panel if open
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
        
        // Resume time
        Time.timeScale = 1f;
        
        // Unlock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
    }

    public void PauseGame()
    {
        if (isPaused) return;
        
        isPaused = true;
        
        // Show pause menu
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);
        
        // Make sure settings panel is hidden
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
        
        // Pause time
        Time.timeScale = 0f;
        
        // Show and unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Open settings panel - Called by Settings button
    /// </summary>
    public void OpenSettings()
    {
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
        
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    /// <summary>
    /// Close settings and return to pause menu - Called by Back button in settings
    /// </summary>
    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
        
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);
    }

    /// <summary>
    /// Return to main menu - Called by Main Menu button
    /// </summary>
    public void ReturnToMainMenu()
    {
        // Resume time before loading scene (important!)
        Time.timeScale = 1f;
        
        // Load main menu scene
        SceneManager.LoadScene(mainMenuSceneName);
    }

    #endregion

    #region Settings - Music Volume

    /// <summary>
    /// Called when music volume slider changes
    /// </summary>
    void OnMusicVolumeChanged(float value)
    {
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.SetMasterVolume(value);
        }
        
        UpdateVolumeText(value);
    }

    /// <summary>
    /// Update volume percentage text (if present)
    /// </summary>
    void UpdateVolumeText(float value)
    {
        if (volumeText != null)
        {
            int percentage = Mathf.RoundToInt(value * 100f);
            volumeText.text = $"{percentage}%";
        }
    }

    #endregion

    #region Public Utility Methods

    /// <summary>
    /// Check if game is currently paused
    /// </summary>
    public bool IsPaused()
    {
        return isPaused;
    }

    /// <summary>
    /// Force pause (useful for cutscenes, dialogue, etc)
    /// </summary>
    public void ForcePause()
    {
        PauseGame();
    }

    /// <summary>
    /// Force resume (useful for cutscenes, dialogue, etc)
    /// </summary>
    public void ForceResume()
    {
        ResumeGame();
    }

    #endregion

    void OnDestroy()
    {
        // Remove slider listener
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
        }
        
        // Make sure time is resumed when script is destroyed
        Time.timeScale = 1f;
    }
}
