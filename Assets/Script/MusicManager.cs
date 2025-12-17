using UnityEngine;

/// <summary>
/// Manages dynamic music system - switches between exploration and combat music
/// </summary>
public class MusicManager : MonoBehaviour
{
    [Header("Music Tracks")]
    [Tooltip("Music that plays during normal exploration")]
    public AudioClip explorationMusic;
    
    [Tooltip("Music that plays during combat (when enemies are chasing)")]
    public AudioClip combatMusic;
    
    [Header("Audio Settings")]
    [Range(0f, 1f)]
    [Tooltip("Master volume for all music")]
    public float masterVolume = 0.7f;
    
    [Tooltip("How quickly music fades in/out when transitioning")]
    public float fadeDuration = 1.5f;
    
    [Tooltip("Delay before switching back to exploration music after combat ends")]
    public float combatExitDelay = 3f;
    
    [Header("Debug")]
    [Tooltip("Show debug messages")]
    public bool showDebugLogs = true;
    
    // Audio sources
    private AudioSource explorationSource;
    private AudioSource combatSource;
    
    // State tracking
    private enum MusicState { Exploration, Combat, Transitioning }
    private MusicState currentState = MusicState.Exploration;
    private MusicState targetState = MusicState.Exploration;
    
    // Combat tracking
    private int enemiesInCombat = 0;
    private float combatExitTimer = 0f;
    
    // Singleton pattern
    public static MusicManager Instance { get; private set; }

    void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Create audio sources
        SetupAudioSources();
    }

    void Start()
    {
        // Start with exploration music
        PlayExplorationMusic();
    }

    void Update()
    {
        // Handle combat exit timer
        if (enemiesInCombat == 0 && currentState == MusicState.Combat)
        {
            combatExitTimer += Time.deltaTime;
            
            if (combatExitTimer >= combatExitDelay)
            {
                TransitionToExploration();
            }
        }
        
        // Handle crossfading
        UpdateCrossfade();
    }

    #region Setup

    void SetupAudioSources()
    {
        // Create exploration audio source
        explorationSource = gameObject.AddComponent<AudioSource>();
        explorationSource.loop = true;
        explorationSource.playOnAwake = false;
        explorationSource.volume = 0f;
        
        // Create combat audio source
        combatSource = gameObject.AddComponent<AudioSource>();
        combatSource.loop = true;
        combatSource.playOnAwake = false;
        combatSource.volume = 0f;
        
        if (showDebugLogs)
        {
            Debug.Log("[MUSIC] Music Manager initialized with 2 audio sources");
        }
    }

    #endregion

    #region Public Methods - Called by Enemies

    /// <summary>
    /// Call this when an enemy starts chasing the player
    /// </summary>
    public void OnEnemyEnterCombat()
    {
        enemiesInCombat++;
        combatExitTimer = 0f;
        
        if (showDebugLogs)
        {
            Debug.Log($"[MUSIC] Enemy entered combat! Active enemies: {enemiesInCombat}");
        }
        
        if (enemiesInCombat == 1 && currentState != MusicState.Combat)
        {
            TransitionToCombat();
        }
    }

    /// <summary>
    /// Call this when an enemy stops chasing the player (dies, loses track, etc)
    /// </summary>
    public void OnEnemyExitCombat()
    {
        enemiesInCombat = Mathf.Max(0, enemiesInCombat - 1);
        
        if (showDebugLogs)
        {
            Debug.Log($"[MUSIC] Enemy exited combat! Active enemies: {enemiesInCombat}");
        }
        
        if (enemiesInCombat == 0)
        {
            combatExitTimer = 0f;
            // Will transition back to exploration after delay (handled in Update)
        }
    }

    #endregion

    #region Music Control

    void PlayExplorationMusic()
    {
        if (explorationMusic == null)
        {
            Debug.LogWarning("[MUSIC] Exploration music clip is not assigned!");
            return;
        }
        
        explorationSource.clip = explorationMusic;
        explorationSource.Play();
        explorationSource.volume = masterVolume;
        currentState = MusicState.Exploration;
        
        if (showDebugLogs)
        {
            Debug.Log("[MUSIC] Playing exploration music");
        }
    }

    void PlayCombatMusic()
    {
        if (combatMusic == null)
        {
            Debug.LogWarning("[MUSIC] Combat music clip is not assigned!");
            return;
        }
        
        if (!combatSource.isPlaying)
        {
            combatSource.clip = combatMusic;
            combatSource.Play();
        }
        
        if (showDebugLogs)
        {
            Debug.Log("[MUSIC] Playing combat music");
        }
    }

    void TransitionToCombat()
    {
        if (currentState == MusicState.Combat)
            return;
        
        targetState = MusicState.Combat;
        currentState = MusicState.Transitioning;
        
        PlayCombatMusic();
        
        if (showDebugLogs)
        {
            Debug.Log("[MUSIC] Transitioning to combat music");
        }
    }

    void TransitionToExploration()
    {
        if (currentState == MusicState.Exploration)
            return;
        
        targetState = MusicState.Exploration;
        currentState = MusicState.Transitioning;
        
        if (showDebugLogs)
        {
            Debug.Log("[MUSIC] Transitioning to exploration music");
        }
    }

    void UpdateCrossfade()
    {
        if (currentState != MusicState.Transitioning)
            return;
        
        float fadeSpeed = 1f / fadeDuration * Time.deltaTime;
        
        if (targetState == MusicState.Combat)
        {
            // Fade out exploration, fade in combat
            explorationSource.volume = Mathf.MoveTowards(explorationSource.volume, 0f, fadeSpeed);
            combatSource.volume = Mathf.MoveTowards(combatSource.volume, masterVolume, fadeSpeed);
            
            if (combatSource.volume >= masterVolume && explorationSource.volume <= 0f)
            {
                currentState = MusicState.Combat;
                if (showDebugLogs)
                {
                    Debug.Log("[MUSIC] Combat music fully active");
                }
            }
        }
        else if (targetState == MusicState.Exploration)
        {
            // Fade out combat, fade in exploration
            combatSource.volume = Mathf.MoveTowards(combatSource.volume, 0f, fadeSpeed);
            explorationSource.volume = Mathf.MoveTowards(explorationSource.volume, masterVolume, fadeSpeed);
            
            if (explorationSource.volume >= masterVolume && combatSource.volume <= 0f)
            {
                currentState = MusicState.Exploration;
                combatSource.Stop();
                if (showDebugLogs)
                {
                    Debug.Log("[MUSIC] Exploration music fully active");
                }
            }
        }
    }

    #endregion

    #region Public Utility Methods

    /// <summary>
    /// Change master volume at runtime
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        
        if (currentState == MusicState.Exploration)
        {
            explorationSource.volume = masterVolume;
        }
        else if (currentState == MusicState.Combat)
        {
            combatSource.volume = masterVolume;
        }
    }

    /// <summary>
    /// Stop all music
    /// </summary>
    public void StopAllMusic()
    {
        explorationSource.Stop();
        combatSource.Stop();
        explorationSource.volume = 0f;
        combatSource.volume = 0f;
    }

    /// <summary>
    /// Get current music state
    /// </summary>
    public string GetCurrentMusicState()
    {
        return currentState.ToString();
    }

    /// <summary>
    /// Get number of enemies currently in combat
    /// </summary>
    public int GetEnemiesInCombat()
    {
        return enemiesInCombat;
    }

    #endregion
}
