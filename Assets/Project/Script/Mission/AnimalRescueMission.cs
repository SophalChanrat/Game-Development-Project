using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Animal Rescue Mission - Player rescues trapped animals
/// Uses existing RescueInteractable logic
/// Requires talking to Tree Spirit first!
/// </summary>
public class AnimalRescueMission : MonoBehaviour
{
    [Header("Mission Info")]
    [Tooltip("Mission name shown to player")]
    public string missionName = "Rescue the Animals";
    
    [Tooltip("Mission description")]
    [TextArea(3, 5)]
    public string missionDescription = "Rescue all trapped animals before time runs out!";
    
    [Header("Interaction")]
    [Tooltip("Distance player must be to interact")]
    public float interactionDistance = 3f;
    
    [Tooltip("Show prompt above mission giver")]
    public bool showInteractionPrompt = true;
    
    [Header("Mission Marker")]
    [Tooltip("Show floating marker above mission location")]
    public bool showMissionMarker = true;
    
    [Tooltip("Marker height above mission giver")]
    public float markerHeight = 3f;
    
    [Tooltip("Marker bob speed")]
    public float markerBobSpeed = 2f;
    
    [Tooltip("Marker bob amount")]
    public float markerBobAmount = 0.3f;
    
    // Mission marker state
    private bool missionUnlocked = false;
    
    [Header("Animal Rescue Settings")]
    [Tooltip("Animal prefab to spawn (must have RescueInteractable component)")]
    public GameObject animalPrefab;
    
    [Tooltip("Animal spawn points (where to spawn trapped animals)")]
    public Transform[] animalSpawnPoints;
    
    [Tooltip("Number of animals to spawn and rescue")]
    public int animalsToRescueCount = 4;
    
    [Tooltip("Time limit (seconds, 0 = no limit)")]
    public float timeLimit = 0f;
    
    [Header("Enemy Spawning (Optional)")]
    [Tooltip("Enemy prefabs to spawn for this mission")]
    public GameObject[] missionEnemyPrefabs;
    
    [Tooltip("Spawn points for enemies")]
    public Transform[] enemySpawnPoints;
    
    [Tooltip("Number of enemies per wave")]
    public int enemiesPerWave = 3;
    
    [Tooltip("Delay between waves (seconds)")]
    public float waveDelay = 10f;
    
    [Tooltip("Total number of waves (0 = infinite)")]
    public int totalWaves = 2;
    
    [Header("Rewards")]
    [Tooltip("Gold reward on completion")]
    public int goldReward = 150;
    
    [Tooltip("Experience reward on completion")]
    public int expReward = 75;
    
    [Tooltip("Items to spawn on completion")]
    public GameObject[] rewardItems;
    
    [Header("UI Settings")]
    [Tooltip("UI Canvas for mission info")]
    public Canvas missionUICanvas;
    
    [Tooltip("Mission UI script reference")]
    public MissionUI missionUI;
    
    [Header("Audio")]
    public AudioClip missionStartSound;
    public AudioClip missionCompleteSound;
    public AudioClip missionFailSound;
    public AudioClip animalRescuedSound;
    
    [Header("Events")]
    public UnityEvent OnMissionStart;
    public UnityEvent OnMissionComplete;
    public UnityEvent OnMissionFail;
    public UnityEvent<int, int> OnAnimalRescued; // current, total
    
    // Private variables
    private Transform player;
    private PlayerHealth playerHealth;
    private bool missionActive = false;
    private bool missionCompleted = false;
    private bool missionFailed = false;
    private float missionTimer = 0f;
    private int currentWave = 0;
    private AudioSource audioSource;
    private bool playerInRange = false;
    
    // Animal tracking
    private int animalsRescued = 0;
    private List<RescueInteractable> trackedAnimals = new List<RescueInteractable>();
    private List<GameObject> spawnedAnimals = new List<GameObject>();
    
    // Enemy tracking (optional)
    private List<GameObject> spawnedEnemies = new List<GameObject>();
    
    void Start()
    {
        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerHealth = playerObj.GetComponent<PlayerHealth>();
        }
        
        // Setup audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        
        // Find Mission UI if not assigned
        if (missionUI == null)
        {
            missionUI = FindObjectOfType<MissionUI>();
        }
        
        // Hide mission UI initially
        if (missionUICanvas != null)
        {
            missionUICanvas.gameObject.SetActive(false);
        }
        
        // Check if missions are already unlocked (e.g., from previous scene)
        missionUnlocked = DialogueManager.hasTalkedToTreeSpirit;
        
        Debug.Log("[ANIMAL RESCUE] Mission initialized: " + missionName + ". Unlocked: " + missionUnlocked);
    }
    
    // Called by DialogueManager when player finishes talking to Tree Spirit
    public void OnMissionsUnlockedByDialogue()
    {
        missionUnlocked = true;
        Debug.Log("[ANIMAL RESCUE] Animal Rescue Mission UNLOCKED by dialogue!");
    }
    
    void Update()
    {
        if (player == null) return;
        
        // Check if player died during mission
        if (missionActive && playerHealth != null && playerHealth.IsDead())
        {
            FailMission("Player died!");
            return;
        }
        
        // Check player distance for interaction
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        playerInRange = distanceToPlayer <= interactionDistance;
        
        // Update mission progress
        if (missionActive && !missionCompleted && !missionFailed)
        {
            UpdateMission();
        }
    }
    
    public void OnInteract(InputAction.CallbackContext context)
    {
        Debug.Log("[ANIMAL RESCUE] OnInteract called! Context started: " + context.started);
        
        if (!context.started)
        {
            Debug.Log("[ANIMAL RESCUE] Context not started, returning");
            return;
        }
        
        Debug.Log("[ANIMAL RESCUE] Player in range: " + playerInRange);
        Debug.Log("[ANIMAL RESCUE] Mission active: " + missionActive);
        Debug.Log("[ANIMAL RESCUE] Mission completed: " + missionCompleted);
        Debug.Log("[ANIMAL RESCUE] Mission unlocked: " + missionUnlocked);
        
        // Check if missions are unlocked
        if (!missionUnlocked)
        {
            Debug.LogWarning("[ANIMAL RESCUE] Cannot start - must talk to Tree Spirit first!");
            return;
        }
        
        // Start mission if in range and not active
        if (!missionActive && !missionCompleted && playerInRange)
        {
            Debug.Log("[ANIMAL RESCUE] All conditions met - starting mission!");
            StartMission();
        }
        else
        {
            if (missionActive)
                Debug.LogWarning("[ANIMAL RESCUE] Cannot start - mission already active");
            if (missionCompleted)
                Debug.LogWarning("[ANIMAL RESCUE] Cannot start - mission already completed");
            if (!playerInRange)
                Debug.LogWarning("[ANIMAL RESCUE] Cannot start - player not in range (distance: " + Vector3.Distance(transform.position, player.position) + ")");
        }
    }
    
    void UpdateMission()
    {
        // Update timer
        if (timeLimit > 0)
        {
            missionTimer += Time.deltaTime;
            if (missionTimer >= timeLimit)
            {
                FailMission("Time's up!");
                return;
            }
        }
        
        // Check if all animals rescued
        if (animalsRescued >= animalsToRescueCount)
        {
            CompleteMission();
        }
        
    }
    
    public void StartMission()
    {
        if (missionActive) return;
        if (!missionUnlocked) return;
        
        // Reset mission state
        missionActive = true;
        missionCompleted = false;
        missionFailed = false;
        missionTimer = 0f;
        currentWave = 0;
        animalsRescued = 0;
        
        Debug.Log("[ANIMAL RESCUE] Mission started: " + missionName);
        
        // Spawn animals OR register existing ones
        SpawnAnimals();
        
        // ALSO register any existing RescueInteractable in scene that don't have RescueDetector
        RegisterExistingAnimals();
        
        // Play sound
        if (audioSource != null && missionStartSound != null)
        {
            audioSource.PlayOneShot(missionStartSound);
        }
        
        // Show mission UI
        if (missionUICanvas != null)
        {
            missionUICanvas.gameObject.SetActive(true);
        }
        
        // Start spawning enemies (optional)
        if (missionEnemyPrefabs != null && missionEnemyPrefabs.Length > 0)
        {
            StartCoroutine(SpawnEnemyWaves());
        }
        
        OnMissionStart?.Invoke();
    }
    
    void RegisterExistingAnimals()
    {
        // Find ALL RescueInteractable objects in scene and add RescueDetector if missing
        RescueInteractable[] allAnimals = FindObjectsOfType<RescueInteractable>();
        
        Debug.Log("[ANIMAL RESCUE] Found " + allAnimals.Length + " total RescueInteractable objects in scene");
        
        foreach (RescueInteractable rescueScript in allAnimals)
        {
            if (rescueScript != null && rescueScript.gameObject.activeSelf)
            {
                // Check if already has RescueDetector
                RescueDetector detector = rescueScript.GetComponent<RescueDetector>();
                if (detector == null)
                {
                    // Add RescueDetector to this object
                    detector = rescueScript.gameObject.AddComponent<RescueDetector>();
                    detector.Initialize(this);
                    Debug.Log("[ANIMAL RESCUE] *** Added RescueDetector to: " + rescueScript.gameObject.name + " ***");
                }
                
                // Track if not already tracked
                if (!trackedAnimals.Contains(rescueScript))
                {
                    trackedAnimals.Add(rescueScript);
                }
            }
        }
        
        // Update count based on tracked animals
        if (animalsToRescueCount == 0 || animalsToRescueCount > trackedAnimals.Count)
        {
            animalsToRescueCount = trackedAnimals.Count;
        }
        
        Debug.Log("[ANIMAL RESCUE] Total tracked animals: " + trackedAnimals.Count + ", Need to rescue: " + animalsToRescueCount);
    }
    
    void SpawnAnimals()
    {
        // Clear any previously spawned animals
        ClearSpawnedAnimals();
        
        // Option 1: Spawn from prefab if configured
        if (animalPrefab != null && animalSpawnPoints != null && animalSpawnPoints.Length > 0)
        {
            int animalsToSpawn = Mathf.Min(animalsToRescueCount, animalSpawnPoints.Length);
            animalsToRescueCount = animalsToSpawn;

            for (int i = 0; i < animalsToSpawn; i++)
            {
                Transform spawnPoint = animalSpawnPoints[i];
                if (spawnPoint != null)
                {
                    GameObject animal = Instantiate(animalPrefab, spawnPoint.position, spawnPoint.rotation);
                    spawnedAnimals.Add(animal);
                    
                    RescueInteractable rescueScript = animal.GetComponent<RescueInteractable>();
                    if (rescueScript != null)
                    {
                        trackedAnimals.Add(rescueScript);
                        
                        // Add OnDisable callback to detect rescue
                        RescueDetector detector = animal.AddComponent<RescueDetector>();
                        detector.Initialize(this);
                        
                        Debug.Log("[ANIMAL RESCUE] Added RescueDetector to spawned: " + animal.name);
                    }
                    else
                    {
                        Debug.LogWarning("[ANIMAL RESCUE] Spawned animal missing RescueInteractable component!");
                    }
                }
            }
            
            Debug.Log("[ANIMAL RESCUE] Spawned " + spawnedAnimals.Count + " trapped animals");
        }
        // Option 2: Find all existing RescueInteractable objects in scene
        else
        {
            Debug.Log("[ANIMAL RESCUE] No prefab/spawn points - will find existing RescueInteractable objects...");
        }
    }
    
    // Called by RescueDetector when animal is disabled
    public void NotifyAnimalRescued()
    {
        if (missionActive)
        {
            OnAnimalRescuedInternal();
        }
        else
        {
            Debug.LogWarning("[ANIMAL RESCUE] Animal rescued but mission not active!");
        }
    }
    
    void OnAnimalRescuedInternal()
    {
        animalsRescued++;
        Debug.Log("[ANIMAL RESCUE] ========================================");
        Debug.Log("[ANIMAL RESCUE] Animal rescued: " + animalsRescued + "/" + animalsToRescueCount);
        Debug.Log("[ANIMAL RESCUE] Mission Active: " + missionActive);
        Debug.Log("[ANIMAL RESCUE] ========================================");
        
        // Play sound
        if (audioSource != null && animalRescuedSound != null)
        {
            audioSource.PlayOneShot(animalRescuedSound);
        }
        
        // Invoke event
        OnAnimalRescued?.Invoke(animalsRescued, animalsToRescueCount);
        
        // Check if mission complete
        if (animalsRescued >= animalsToRescueCount)
        {
            Debug.Log("[ANIMAL RESCUE] !!! ALL ANIMALS RESCUED - SHOULD COMPLETE ON NEXT UPDATE !!!");
        }
    }
    
    IEnumerator SpawnEnemyWaves()
    {
        while (missionActive && (totalWaves == 0 || currentWave < totalWaves))
        {
            currentWave++;
            Debug.Log("[ANIMAL RESCUE] Wave " + currentWave + " starting!");
            
            SpawnEnemyWave();
            
            yield return new WaitForSeconds(waveDelay);
        }
    }
    
    void SpawnEnemyWave()
    {
        if (missionEnemyPrefabs == null || missionEnemyPrefabs.Length == 0) return;
        if (enemySpawnPoints == null || enemySpawnPoints.Length == 0) return;
        
        for (int i = 0; i < enemiesPerWave; i++)
        {
            GameObject enemyPrefab = missionEnemyPrefabs[Random.Range(0, missionEnemyPrefabs.Length)];
            Transform spawnPoint = enemySpawnPoints[Random.Range(0, enemySpawnPoints.Length)];
            
            GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
            spawnedEnemies.Add(enemy);
        }
    }
    
    void ClearSpawnedAnimals()
    {
        foreach (GameObject animal in spawnedAnimals)
        {
            if (animal != null)
            {
                Destroy(animal);
            }
        }
        spawnedAnimals.Clear();
        trackedAnimals.Clear();
    }
    
    void ClearSpawnedEnemies()
    {
        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy);
            }
        }
        spawnedEnemies.Clear();
    }
    
    public void CompleteMission()
    {
        if (missionCompleted || missionFailed) return;
        
        missionActive = false;
        missionCompleted = true;
        
        Debug.Log("[ANIMAL RESCUE] ===== MISSION COMPLETE =====");
        Debug.Log("[ANIMAL RESCUE] Completed: " + missionName);
        
        // Play sound
        if (audioSource != null && missionCompleteSound != null)
        {
            audioSource.PlayOneShot(missionCompleteSound);
        }
        
        // Stop spawning
        StopAllCoroutines();
        
        // Clear spawned objects
        ClearSpawnedAnimals();
        ClearSpawnedEnemies();
        
        // Give rewards
        GiveRewards();
        
        // Show completion UI
        if (missionUI != null)
        {
            Debug.Log("[ANIMAL RESCUE] Showing completion UI");
            missionUI.ShowCompleted();
        }
        else
        {
            Debug.LogWarning("[ANIMAL RESCUE] MissionUI is null!");
        }
        
        // Hide mission UI after delay
        StartCoroutine(HideMissionUIAfterDelay(3f));
        
        OnMissionComplete?.Invoke();
    }
    
    public void FailMission(string reason)
    {
        if (missionCompleted || missionFailed) return;
        
        missionActive = false;
        missionFailed = true;
        
        Debug.Log("[ANIMAL RESCUE] Mission failed: " + reason);
        
        // Play sound
        if (audioSource != null && missionFailSound != null)
        {
            audioSource.PlayOneShot(missionFailSound);
        }
        
        // Stop spawning
        StopAllCoroutines();
        
        // Clear spawned objects
        ClearSpawnedAnimals();
        ClearSpawnedEnemies();
        
        // Show failure UI
        if (missionUI != null)
        {
            missionUI.ShowFailed();
        }
        
        // Hide mission UI after delay
        StartCoroutine(HideMissionUIAfterDelay(3f));
        
        OnMissionFail?.Invoke();
    }
    
    IEnumerator HideMissionUIAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (missionUICanvas != null)
        {
            missionUICanvas.gameObject.SetActive(false);
        }
    }
    
    void GiveRewards()
    {
        // Spawn reward items
        if (rewardItems != null && rewardItems.Length > 0)
        {
            foreach (GameObject rewardItem in rewardItems)
            {
                if (rewardItem != null)
                {
                    Vector3 spawnPos = transform.position + Vector3.up * 1f;
                    Instantiate(rewardItem, spawnPos, Quaternion.identity);
                }
            }
        }
        
        Debug.Log("[ANIMAL RESCUE] Rewards: " + goldReward + " gold, " + expReward + " exp");
    }
    
    // Public methods
    public bool IsMissionActive() => missionActive;
    public bool IsMissionCompleted() => missionCompleted;
    public bool IsPlayerInRange() => playerInRange;
    public bool IsMissionUnlocked() => missionUnlocked;
    public int GetAnimalsRescued() => animalsRescued;
    public int GetAnimalsToRescue() => animalsToRescueCount;
    public float GetTimeRemaining() => timeLimit > 0 ? timeLimit - missionTimer : 0f;
    
    // GUI for interaction prompt AND mission marker
    void OnGUI()
    {
        if (player == null) return;
        
        // Show mission marker when unlocked but not started/completed
        if (showMissionMarker && missionUnlocked && !missionActive && !missionCompleted)
        {
            DrawMissionMarker();
        }
        
        // Show "Talk to Tree Spirit first" if not unlocked
        if (!missionUnlocked && playerInRange && showInteractionPrompt)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f);
            
            if (screenPos.z > 0)
            {
                GUIStyle style = new GUIStyle();
                style.fontSize = 18;
                style.normal.textColor = Color.gray;
                style.alignment = TextAnchor.MiddleCenter;
                
                GUI.Label(new Rect(screenPos.x - 150, Screen.height - screenPos.y - 50, 300, 30), 
                          "Talk to Tree Spirit first", style);
            }
            return;
        }
        
        if (!showInteractionPrompt || missionActive || missionCompleted) return;
        if (!playerInRange) return;
        
        Vector3 promptPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f);
        
        if (promptPos.z > 0)
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 20;
            style.normal.textColor = Color.cyan;
            style.alignment = TextAnchor.MiddleCenter;
            
            GUI.Label(new Rect(promptPos.x - 100, Screen.height - promptPos.y - 50, 200, 30), 
                      "Press [F] to start", style);
            
            style.fontSize = 16;
            style.normal.textColor = Color.white;
            GUI.Label(new Rect(promptPos.x - 150, Screen.height - promptPos.y - 20, 300, 30), 
                      missionName, style);
        }
    }
    
    void DrawMissionMarker()
    {
        // Calculate bobbing position
        float bob = Mathf.Sin(Time.time * markerBobSpeed) * markerBobAmount;
        Vector3 markerWorldPos = transform.position + Vector3.up * (markerHeight + bob);
        
        Vector3 screenPos = Camera.main.WorldToScreenPoint(markerWorldPos);
        
        if (screenPos.z > 0)
        {
            GUIStyle markerStyle = new GUIStyle();
            markerStyle.fontSize = 30;
            markerStyle.normal.textColor = Color.cyan;
            markerStyle.alignment = TextAnchor.MiddleCenter;
            
            // Draw exclamation mark
            GUI.Label(new Rect(screenPos.x - 15, Screen.height - screenPos.y - 15, 30, 30), "!", markerStyle);
            
            // Draw mission name below marker
            markerStyle.fontSize = 14;
            markerStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(screenPos.x - 75, Screen.height - screenPos.y + 15, 150, 20), 
                      "🐰 " + missionName, markerStyle);
        }
    }
    
    // Gizmos
    void OnDrawGizmosSelected()
    {
        // Draw interaction range
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
        
        // Draw animal spawn points
        if (animalSpawnPoints != null)
        {
            Gizmos.color = Color.yellow;
            foreach (Transform spawnPoint in animalSpawnPoints)
            {
                if (spawnPoint != null)
                {
                    Gizmos.DrawWireSphere(spawnPoint.position, 1f);
                    Gizmos.DrawLine(transform.position, spawnPoint.position);
                }
            }
        }
        
        // Draw enemy spawn points
        if (enemySpawnPoints != null)
        {
            Gizmos.color = Color.red;
            foreach (Transform spawnPoint in enemySpawnPoints)
            {
                if (spawnPoint != null)
                {
                    Gizmos.DrawWireSphere(spawnPoint.position, 0.75f);
                    Gizmos.DrawLine(transform.position, spawnPoint.position);
                }
            }
        }
    }
}

/// <summary>
/// Helper component to detect when an animal is rescued (disabled)
/// </summary>
public class RescueDetector : MonoBehaviour
{
    private AnimalRescueMission mission;
    private bool hasNotified = false;
    
    public void Initialize(AnimalRescueMission missionRef)
    {
        mission = missionRef;
        hasNotified = false;
        Debug.Log("[RESCUE DETECTOR] Initialized on " + gameObject.name);
    }
    
    void OnDisable()
    {
        // Only notify once and only if mission exists
        if (mission != null && !hasNotified)
        {
            hasNotified = true;
            Debug.Log("[RESCUE DETECTOR] OnDisable triggered! Notifying mission: " + gameObject.name);
            mission.NotifyAnimalRescued();
        }
    }
}
