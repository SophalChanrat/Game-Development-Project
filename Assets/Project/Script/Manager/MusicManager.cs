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

    [Header("Combat Intro Settings")]
    [Tooltip("Enable dramatic pause before combat music starts")]
    public bool useCombatIntroDelay = true;

    [Tooltip("How long to wait in silence before combat music starts (5-10 seconds recommended)")]
    [Range(0f, 15f)]
    public float combatIntroDelay = 7f;

    [Tooltip("How quickly exploration music fades out when combat starts")]
    public float combatFadeOutSpeed = 2f;

    [Header("Debug")]
    [Tooltip("Show debug messages")]
    public bool showDebugLogs = true;

    // Audio sources
    private AudioSource explorationSource;
    private AudioSource combatSource;

    // State tracking
    private enum MusicState { Exploration, Combat, Transitioning, CombatIntro }
    private MusicState currentState = MusicState.Exploration;
    private MusicState targetState = MusicState.Exploration;

    // Combat tracking
    private int enemiesInCombat = 0;
    private float combatExitTimer = 0f;
    private float combatIntroTimer = 0f;
    private bool isInCombatIntro = false;
    
    // Mission lock - prevents automatic music transitions during missions
    private bool isMissionActive = false;

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
        // Skip automatic transitions if mission is active
        if (isMissionActive)
            return;
        
        // Handle combat intro delay (silence before combat music)
        if (isInCombatIntro)
        {
            combatIntroTimer += Time.deltaTime;
            
            if (combatIntroTimer >= combatIntroDelay)
            {
                // Intro delay finished, start combat music
                isInCombatIntro = false;
                combatIntroTimer = 0f;
                StartCombatMusic();
            }
        }
        
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
        // Check if audio sources already exist (in case of scene reload)
        AudioSource[] existingSources = GetComponents<AudioSource>();
        if (existingSources.Length >= 2)
        {
            explorationSource = existingSources[0];
            combatSource = existingSources[1];
        }
        else
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
        }

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
        // Skip if mission is controlling music
        if (isMissionActive) return;
        
        enemiesInCombat++;
        combatExitTimer = 0f;
        
        if (showDebugLogs)
        {
            Debug.Log($"[MUSIC] Enemy entered combat! Active enemies: {enemiesInCombat}");
        }
        
        // Only trigger combat transition if this is the first enemy and we're not already in combat
        if (enemiesInCombat == 1 && currentState != MusicState.Combat && !isInCombatIntro)
        {
            TransitionToCombat();
        }
    }

    /// <summary>
    /// Call this when an enemy stops chasing the player (dies, loses track, etc)
    /// </summary>
    public void OnEnemyExitCombat()
    {
        // Skip if mission is controlling music
        if (isMissionActive) return;
        
        enemiesInCombat = Mathf.Max(0, enemiesInCombat - 1);
        
        if (showDebugLogs)
        {
            Debug.Log($"[MUSIC] Enemy exited combat! Active enemies: {enemiesInCombat}");
        }
        
        if (enemiesInCombat == 0)
        {
            // If we're still in the intro phase (silence), cancel it and return to exploration
            if (isInCombatIntro)
            {
                isInCombatIntro = false;
                combatIntroTimer = 0f;
                TransitionToExploration();
                
                if (showDebugLogs)
                {
                    Debug.Log("[MUSIC] Combat ended during intro - returning to exploration music");
                }
            }
            else
            {
                combatExitTimer = 0f;
                // Will transition back to exploration after delay (handled in Update)
            }
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
        
        // Fade in from 0 to master volume
        combatSource.volume = 0f;
        
        if (showDebugLogs)
        {
            Debug.Log("[MUSIC] Playing combat music");
        }
    }
    
    void StartCombatMusic()
    {
        if (combatMusic == null)
        {
            Debug.LogWarning("[MUSIC] Combat music clip is not assigned!");
            return;
        }
        
        // Actually play the combat music
        combatSource.clip = combatMusic;
        combatSource.Play();
        combatSource.volume = masterVolume;
        currentState = MusicState.Combat;
        
        if (showDebugLogs)
        {
            Debug.Log("[MUSIC] Combat intro complete - starting combat music");
        }
    }

    void TransitionToCombat()
    {
        if (currentState == MusicState.Combat || isInCombatIntro)
            return;
        
        if (useCombatIntroDelay)
        {
            // Use dramatic intro: fade out exploration, wait, then start combat
            targetState = MusicState.CombatIntro;
            currentState = MusicState.Transitioning;
            isInCombatIntro = true;
            combatIntroTimer = 0f;
            
            if (showDebugLogs)
            {
                Debug.Log($"[MUSIC] Starting combat intro - fading out exploration music, will wait {combatIntroDelay}s before starting combat music");
            }
        }
        else
        {
            // Instant transition with crossfade (old behavior)
            targetState = MusicState.Combat;
            currentState = MusicState.Transitioning;
            
            // Start playing combat music immediately for crossfade
            if (!combatSource.isPlaying)
            {
                combatSource.clip = combatMusic;
                combatSource.Play();
            }
            
            if (showDebugLogs)
            {
                Debug.Log("[MUSIC] Transitioning to combat music (instant crossfade)");
            }
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
            // Old instant crossfade behavior (when useCombatIntroDelay is false)
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
        else if (targetState == MusicState.CombatIntro)
        {
            // Fade out exploration music quickly for dramatic effect
            float introFadeSpeed = 1f / combatFadeOutSpeed * Time.deltaTime;
            explorationSource.volume = Mathf.MoveTowards(explorationSource.volume, 0f, introFadeSpeed);
            combatSource.volume = 0f; // Keep combat silent during intro
            
            if (explorationSource.volume <= 0f)
            {
                // Exploration fully faded out, now waiting for combat intro delay
                currentState = MusicState.CombatIntro;
                if (showDebugLogs)
                {
                    Debug.Log($"[MUSIC] Exploration music faded out - waiting {combatIntroDelay}s for combat music...");
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
        isInCombatIntro = false;
        enemiesInCombat = 0;
        isMissionActive = false;
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

    /// <summary>
    /// Force play combat music immediately (used by missions)
    /// Locks music so enemies dying won't change it back
    /// </summary>
    public void PlayCombatMusicImmediate()
    {
        if (combatMusic == null)
        {
            Debug.LogWarning("[MUSIC] Combat music clip is not assigned!");
            return;
        }

        // Lock music control to mission
        isMissionActive = true;

        // Stop exploration music
        explorationSource.volume = 0f;
        explorationSource.Stop();
        
        // Reset combat intro state
        isInCombatIntro = false;
        combatIntroTimer = 0f;
        
        // Play combat music immediately
        combatSource.clip = combatMusic;
        combatSource.volume = masterVolume;
        combatSource.Play();
        currentState = MusicState.Combat;
        targetState = MusicState.Combat;

        if (showDebugLogs)
        {
            Debug.Log("[MUSIC] Force playing combat music (mission started) - music locked");
        }
    }

    /// <summary>
    /// Force play exploration music immediately (used by missions on win/lose, and menu)
    /// Unlocks music control
    /// </summary>
    public void PlayExplorationMusicImmediate()
    {
        if (explorationMusic == null)
        {
            Debug.LogWarning("[MUSIC] Exploration music clip is not assigned!");
            return;
        }

        // Unlock music control
        isMissionActive = false;

        // Stop combat music
        combatSource.volume = 0f;
        combatSource.Stop();
        
        // Reset combat states
        isInCombatIntro = false;
        combatIntroTimer = 0f;
        enemiesInCombat = 0;
        combatExitTimer = 0f;
        
        // Play exploration music immediately
        explorationSource.clip = explorationMusic;
        explorationSource.volume = masterVolume;
        explorationSource.Play();
        currentState = MusicState.Exploration;
        targetState = MusicState.Exploration;

        if (showDebugLogs)
        {
            Debug.Log("[MUSIC] Force playing exploration music - music unlocked");
        }
    }
    
    /// <summary>
    /// Check if mission is controlling music
    /// </summary>
    public bool IsMissionActive()
    {
        return isMissionActive;
    }

    #endregion
}