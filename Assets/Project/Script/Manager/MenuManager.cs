using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class MenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject panelMainMenu;
    public GameObject panelSettings;
    public GameObject panelCredits;
    public GameObject panelInstructions;
    public GameObject panelLoading;
    
    [Header("Transition")]
    public Animator fadeAnimator; // assign Image_Fade Animator
    public UnityEngine.UI.Slider progressBar; 

    [Header("Menu Music")]
    [Tooltip("The exploration/menu music clip to play")]
    public AudioClip menuMusic;
    
    [Tooltip("Volume for menu music (0-1)")]
    [Range(0f, 1f)]
    public float menuMusicVolume = 0.7f;
    
    [Header("Settings UI")]
    [Tooltip("Scrollbar or Slider for music volume in settings panel")]
    public Scrollbar musicVolumeScrollbar;
    
    [Tooltip("Alternative: Slider for music volume")]
    public Slider musicVolumeSlider;
    
    [Tooltip("Text to display volume percentage (optional)")]
    public TextMeshProUGUI volumeText;
    
    // Audio source for menu music
    private AudioSource menuAudioSource;
    
    // PlayerPrefs key for volume persistence
    private const string VOLUME_PREF_KEY = "MusicVolume";

    void Awake()
    {
        // Setup audio source for menu music
        SetupMenuAudio();
    }

    void Start()
    {
        // Load saved volume setting
        LoadVolumeSettings();
        
        // Setup volume slider/scrollbar listeners
        SetupVolumeControls();
        
        // Play menu music
        PlayMenuMusic();
        
        // Ensure cursor is visible in menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Ensure time is running
        Time.timeScale = 1f;
    }

    #region Audio Setup

    void SetupMenuAudio()
    {
        // Check if MusicManager exists (singleton from gameplay)
        if (MusicManager.Instance != null)
        {
            // Use the MusicManager's audio - it will handle the music
            return;
        }
        
        // Create our own audio source for menu if no MusicManager exists
        menuAudioSource = gameObject.AddComponent<AudioSource>();
        menuAudioSource.loop = true;
        menuAudioSource.playOnAwake = false;
        menuAudioSource.volume = menuMusicVolume;
    }

    void PlayMenuMusic()
    {
        // If MusicManager exists (came from gameplay), use it
        if (MusicManager.Instance != null)
        {
            // Tell MusicManager to play exploration music
            MusicManager.Instance.PlayExplorationMusicImmediate();
            
            // Apply saved volume
            MusicManager.Instance.SetMasterVolume(menuMusicVolume);
            
            Debug.Log("[MENU] Playing menu music via MusicManager");
            return;
        }
        
        // Otherwise use local audio source
        if (menuAudioSource != null && menuMusic != null)
        {
            menuAudioSource.clip = menuMusic;
            menuAudioSource.volume = menuMusicVolume;
            menuAudioSource.Play();
            
            Debug.Log("[MENU] Playing menu music via local AudioSource");
        }
        else if (menuMusic == null)
        {
            Debug.LogWarning("[MENU] Menu music clip is not assigned!");
        }
    }

    void StopMenuMusic()
    {
        // If using MusicManager, it will handle the transition
        if (MusicManager.Instance != null)
        {
            // Don't stop - let the game scene handle music
            return;
        }
        
        // Stop local audio source
        if (menuAudioSource != null)
        {
            menuAudioSource.Stop();
        }
    }

    #endregion

    #region Volume Settings

    void LoadVolumeSettings()
    {
        // Load saved volume or use default
        if (PlayerPrefs.HasKey(VOLUME_PREF_KEY))
        {
            menuMusicVolume = PlayerPrefs.GetFloat(VOLUME_PREF_KEY);
        }
        
        // Also sync with MusicManager if it exists
        if (MusicManager.Instance != null)
        {
            menuMusicVolume = MusicManager.Instance.masterVolume;
        }
    }

    void SetupVolumeControls()
    {
        // Setup Scrollbar if assigned
        if (musicVolumeScrollbar != null)
        {
            musicVolumeScrollbar.value = menuMusicVolume;
            musicVolumeScrollbar.onValueChanged.AddListener(OnVolumeChanged);
        }
        
        // Setup Slider if assigned
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = menuMusicVolume;
            musicVolumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }
        
        // Update volume text
        UpdateVolumeText(menuMusicVolume);
    }

    void OnVolumeChanged(float value)
    {
        menuMusicVolume = value;
        
        // Apply to MusicManager if it exists
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.SetMasterVolume(value);
        }
        
        // Apply to local audio source
        if (menuAudioSource != null)
        {
            menuAudioSource.volume = value;
        }
        
        // Save to PlayerPrefs for persistence
        PlayerPrefs.SetFloat(VOLUME_PREF_KEY, value);
        PlayerPrefs.Save();
        
        // Update text display
        UpdateVolumeText(value);
    }

    void UpdateVolumeText(float value)
    {
        if (volumeText != null)
        {
            int percentage = Mathf.RoundToInt(value * 100f);
            volumeText.text = $"{percentage}%";
        }
    }

    #endregion

    #region Menu Navigation

    public void StartGame()
    {
        StartCoroutine(LoadGameScene());
    }

    public void OpenSettings()
    {
        panelMainMenu.SetActive(false);
        panelSettings.SetActive(true);
    }
    
    private IEnumerator LoadGameScene()
    { 
        // Trigger fade animation
        fadeAnimator.SetTrigger("isFadeOut"); 
        
        // Wait for fade duration (match your animation length)
        yield return new WaitForSeconds(3f); 
        
        // Show loading panel
        panelMainMenu.SetActive(false); 
        panelLoading.SetActive(true); 
        
        // Reset progress bar
        if (progressBar != null) progressBar.value = 0f;
        
        // Start async scene load
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("IntroScene"); 
        asyncLoad.allowSceneActivation = false;
        
        float minimumLoadTime = 1.5f; // Minimum seconds to show loading
        float elapsedTime = 0f;
        
        while (!asyncLoad.isDone) 
        { 
            elapsedTime += Time.deltaTime;
            
            // Update progress bar (smooth fill based on time)
            float progress = Mathf.Min(elapsedTime / minimumLoadTime, asyncLoad.progress / 0.9f);
            if (progressBar != null) progressBar.value = progress;
            
            // When ready AND minimum time passed, activate scene
            if (asyncLoad.progress >= 0.9f && elapsedTime >= minimumLoadTime) 
            {
                if (progressBar != null) progressBar.value = 1f;
                yield return new WaitForSeconds(0.5f); // Brief pause at 100%
                asyncLoad.allowSceneActivation = true; 
            }
            
            yield return null; 
        } 
    }
    
    public void OpenCredits()
    {
        panelMainMenu.SetActive(false);
        panelCredits.SetActive(true);
    }

    public void OpenInstructions()
    {
        panelMainMenu.SetActive(false);
        panelInstructions.SetActive(true);
    }

    public void BackToMenu()
    {   
        panelSettings.SetActive(false);
        panelCredits.SetActive(false);
        panelInstructions.SetActive(false);
        panelMainMenu.SetActive(true);
        fadeAnimator.SetTrigger("isFadeIn");
    }

    public void QuitGame()
    {
        Debug.Log("Game quit");
    #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
    #else
            Application.Quit();
    #endif
    }

    #endregion

    void OnDestroy()
    {
        // Clean up listeners
        if (musicVolumeScrollbar != null)
        {
            musicVolumeScrollbar.onValueChanged.RemoveListener(OnVolumeChanged);
        }
        
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
        }
    }
}