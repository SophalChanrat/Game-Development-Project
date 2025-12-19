using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Mission System - Player can interact to start missions
/// Missions have objectives, rewards, and custom enemy spawning
/// </summary>
public class MissionSystem : MonoBehaviour
{
    [Header("Mission Info")]
    [Tooltip("Mission name shown to player")]
    public string missionName = "Defend the Village";
    
    [Tooltip("Mission description")]
    [TextArea(3, 5)]
    public string missionDescription = "Defeat all enemies attacking the village!";
    
    [Tooltip("Mission type")]
    public MissionType missionType = MissionType.ProtectTrees;
    
    [Header("Interaction")]
    [Tooltip("Key to press to start mission (default: E)")]
    public KeyCode interactionKey = KeyCode.E;
    
    [Tooltip("Distance player must be to interact")]
    public float interactionDistance = 3f;
    
    [Tooltip("Show prompt above mission giver")]
    public bool showInteractionPrompt = true;
    
    [Header("Mission Objectives")]
    [Tooltip("Number of enemies to kill (all mission types)")]
    public int enemiesToKill = 10;
    
    [Tooltip("Time limit (seconds, 0 = no limit)")]
    public float timeLimit = 0f;
    
    [Header("Protect Trees Mission")]
    [Tooltip("Trees to protect from lumberjacks")]
    public GameObject[] treesToProtect;
    
    [Tooltip("Tree prefab to spawn (instead of using pre-placed trees)")]
    public GameObject treePrefab;
    
    [Tooltip("Tree spawn points (where to spawn trees)")]
    public Transform[] treeSpawnPoints;
    
    [Tooltip("Number of trees to spawn")]
    public int treesToSpawn = 5;
    
    [Tooltip("Number of trees that can be destroyed before mission fails")]
    public int allowedTreeLosses = 2;
    
    [Header("Rescue Animals Mission (To Be Implemented)")]
    [Tooltip("Animals to rescue")]
    public GameObject[] animalsToRescue;
    
    [Tooltip("Number of animals that need to be rescued")]
    public int animalsToRescueCount = 5;
    
    [Header("Enemy Spawning")]
    [Tooltip("Enemy prefabs to spawn for this mission")]
    public GameObject[] missionEnemyPrefabs;
    
    [Tooltip("Lumberjack prefabs to spawn (optional)")]
    public GameObject[] missionLumberjackPrefabs;
    
    [Tooltip("Spawn points for enemies")]
    public Transform[] spawnPoints;
    
    [Tooltip("Number of enemies per wave")]
    public int enemiesPerWave = 3;
    
    [Tooltip("Delay between waves (seconds)")]
    public float waveDelay = 5f;
    
    [Tooltip("Total number of waves (0 = infinite until objective met)")]
    public int totalWaves = 3;
    
    [Header("Rewards")]
    [Tooltip("Gold reward on completion")]
    public int goldReward = 100;
    
    [Tooltip("Experience reward on completion")]
    public int expReward = 50;
    
    [Tooltip("Items to spawn on completion")]
    public GameObject[] rewardItems;
    
    [Header("UI Settings")]
    [Tooltip("Show mission UI during mission")]
    public bool showMissionUI = true;
    
    [Tooltip("UI Canvas for mission info (optional)")]
    public Canvas missionUICanvas;
    
    [Tooltip("Mission UI script reference")]
    public MissionUI missionUI;
    
    [Header("Audio")]
    public AudioClip missionStartSound;
    public AudioClip missionCompleteSound;
    public AudioClip missionFailSound;
    
    [Header("Events")]
    public UnityEvent OnMissionStart;
    public UnityEvent OnMissionComplete;
    public UnityEvent OnMissionFail;
    public UnityEvent<int, int> OnObjectiveProgress; // current, total
    
    // Private variables
    private Transform player;
    private PlayerHealth playerHealth;
    private bool missionActive = false;
    private bool missionCompleted = false;
    private bool missionFailed = false;
    private int currentKillCount = 0;
    private int currentWave = 0;
    private float missionTimer = 0f;
    private float survivalTimer = 0f;
    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private AudioSource audioSource;
    private bool playerInRange = false;
    
    // Tree protection tracking
    private int treesDestroyed = 0;
    private List<TreeHealth> trackedTrees = new List<TreeHealth>();
    private List<GameObject> spawnedTrees = new List<GameObject>();
    
    // Animal rescue tracking (to be implemented)
    private int animalsRescued = 0;
    
    public enum MissionType
    {
        ProtectTrees,       // Protect trees from lumberjacks (enemies spawn too)
        RescueAnimals       // Rescue animals (to be implemented)
    }
    
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
        if (audioSource == null && (missionStartSound != null || missionCompleteSound != null))
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
        
        // Validate spawn points
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[MISSION] No spawn points assigned! Using mission position as spawn point.");
            spawnPoints = new Transform[] { transform };
        }
    }
    
    void SetupTreeProtection()
    {
        trackedTrees.Clear();
        
        // If we have a tree prefab and spawn points, spawn trees dynamically
        if (treePrefab != null && treeSpawnPoints != null && treeSpawnPoints.Length > 0)
        {
            SpawnTrees();
        }
        // Otherwise use pre-placed trees
        else if (treesToProtect != null && treesToProtect.Length > 0)
        {
            foreach (GameObject treeObj in treesToProtect)
            {
                if (treeObj != null)
                {
                    TreeHealth treeHealth = treeObj.GetComponent<TreeHealth>();
                    if (treeHealth != null)
                    {
                        trackedTrees.Add(treeHealth);
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("[MISSION] Protect Trees mission but no trees assigned or tree prefab/spawn points!");
            return;
        }
        
        Debug.Log("[MISSION] Tracking " + trackedTrees.Count + " trees for protection");
    }
    
    void SpawnTrees()
    {
        // Clear any previously spawned trees
        ClearSpawnedTrees();
        
        int treesToSpawnCount = Mathf.Min(treesToSpawn, treeSpawnPoints.Length);
        
        for (int i = 0; i < treesToSpawnCount; i++)
        {
            Transform spawnPoint = treeSpawnPoints[i];
            if (spawnPoint != null)
            {
                GameObject tree = Instantiate(treePrefab, spawnPoint.position, spawnPoint.rotation);
                spawnedTrees.Add(tree);
                
                TreeHealth treeHealth = tree.GetComponent<TreeHealth>();
                if (treeHealth != null)
                {
                    trackedTrees.Add(treeHealth);
                }
                else
                {
                    Debug.LogWarning("[MISSION] Spawned tree missing TreeHealth component!");
                }
            }
        }
        
        Debug.Log("[MISSION] Spawned " + spawnedTrees.Count + " trees");
    }
    
    void ClearSpawnedTrees()
    {
        foreach (GameObject tree in spawnedTrees)
        {
            if (tree != null)
            {
                Destroy(tree);
            }
        }
        spawnedTrees.Clear();
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
        
        // Handle interaction
        if (!missionActive && !missionCompleted && playerInRange)
        {
            if (Input.GetKeyDown(interactionKey))
            {
                StartMission();
            }
        }
        
        // Update mission progress
        if (missionActive && !missionCompleted && !missionFailed)
        {
            UpdateMission();
        }
    }
    
    void UpdateMission()
    {
        // Update timers
        if (timeLimit > 0)
        {
            missionTimer += Time.deltaTime;
            if (missionTimer >= timeLimit)
            {
                FailMission("Time's up!");
                return;
            }
        }
        
        // Clean up dead enemies
        spawnedEnemies.RemoveAll(enemy => enemy == null);
        
        // Check objectives based on mission type
        switch (missionType)
        {
            case MissionType.ProtectTrees:
                CheckTreeProtectionObjective();
                break;
                
            case MissionType.RescueAnimals:
                CheckAnimalRescueObjective();
                break;
        }
    }
    
    void CheckTreeProtectionObjective()
    {
        // Check if too many trees destroyed
        int destroyedCount = 0;
        foreach (TreeHealth tree in trackedTrees)
        {
            if (tree == null || tree.IsDestroyed())
            {
                destroyedCount++;
            }
        }
        
        treesDestroyed = destroyedCount;
        
        if (treesDestroyed >= allowedTreeLosses)
        {
            FailMission("Too many trees destroyed! (" + treesDestroyed + " trees lost)");
            return;
        }
        
        // Check if killed enough enemies
        if (currentKillCount >= enemiesToKill)
        {
            CompleteMission();
        }
    }
    
    void CheckAnimalRescueObjective()
    {
        // To be implemented
        Debug.LogWarning("[MISSION] Rescue Animals mission not yet implemented!");
        
        // Placeholder: Complete when killed enough enemies
        if (currentKillCount >= enemiesToKill)
        {
            CompleteMission();
        }
    }
    
    public void StartMission()
    {
        if (missionActive) return;
        
        // Reset mission state (allow replay)
        missionActive = true;
        missionCompleted = false;
        missionFailed = false;
        missionTimer = 0f;
        survivalTimer = 0f;
        currentKillCount = 0;
        currentWave = 0;
        treesDestroyed = 0;
        animalsRescued = 0;
        
        Debug.Log("[MISSION] Started: " + missionName + " (Type: " + missionType + ")");
        
        // Setup tree protection if mission type is ProtectTrees
        if (missionType == MissionType.ProtectTrees)
        {
            SetupTreeProtection();
        }
        
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
        
        // Start spawning enemies
        StartCoroutine(SpawnWaves());
        
        OnMissionStart?.Invoke();
    }
    
    IEnumerator SpawnWaves()
    {
        while (missionActive && (totalWaves == 0 || currentWave < totalWaves))
        {
            currentWave++;
            Debug.Log("[MISSION] Wave " + currentWave + " starting!");
            
            // Spawn enemies for this wave
            SpawnWave(enemiesPerWave);
            
            // Wait for wave delay
            yield return new WaitForSeconds(waveDelay);
            
            // For non-infinite waves, wait for all enemies to die before next wave
            if (totalWaves > 0)
            {
                while (spawnedEnemies.Count > 0)
                {
                    spawnedEnemies.RemoveAll(enemy => enemy == null);
                    yield return new WaitForSeconds(0.5f);
                }
            }
        }
    }
    
    void SpawnWave(int enemyCount)
    {
        for (int i = 0; i < enemyCount; i++)
        {
            GameObject prefabToSpawn = null;
            
            // For Protect Trees mission: 50% lumberjacks, 50% enemies
            // For other missions: 70% enemies, 30% lumberjacks
            float lumberjackChance = (missionType == MissionType.ProtectTrees) ? 0.5f : 0.3f;
            
            bool spawnLumberjack = Random.value < lumberjackChance && missionLumberjackPrefabs != null && missionLumberjackPrefabs.Length > 0;
            
            if (spawnLumberjack)
            {
                prefabToSpawn = missionLumberjackPrefabs[Random.Range(0, missionLumberjackPrefabs.Length)];
            }
            else if (missionEnemyPrefabs != null && missionEnemyPrefabs.Length > 0)
            {
                prefabToSpawn = missionEnemyPrefabs[Random.Range(0, missionEnemyPrefabs.Length)];
            }
            
            if (prefabToSpawn != null)
            {
                // Pick random spawn point
                Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
                
                // Spawn enemy
                GameObject enemy = Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);
                spawnedEnemies.Add(enemy);
                
                // Listen for enemy death
                EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
                if (enemyAI != null)
                {
                    StartCoroutine(WaitForEnemyDeath(enemy, enemyAI));
                }
                
                LumberjackAI lumberjackAI = enemy.GetComponent<LumberjackAI>();
                if (lumberjackAI != null)
                {
                    StartCoroutine(WaitForLumberjackDeath(enemy, lumberjackAI));
                }
            }
        }
    }
    
    IEnumerator WaitForEnemyDeath(GameObject enemy, EnemyAI enemyAI)
    {
        while (enemy != null && enemyAI != null && !enemyAI.IsDead())
        {
            yield return new WaitForSeconds(0.5f);
        }
        
        // Enemy died
        if (missionActive)
        {
            OnEnemyKilled();
        }
    }
    
    IEnumerator WaitForLumberjackDeath(GameObject lumberjack, LumberjackAI lumberjackAI)
    {
        while (lumberjack != null && lumberjackAI != null && !lumberjackAI.IsDead())
        {
            yield return new WaitForSeconds(0.5f);
        }
        
        // Lumberjack died
        if (missionActive)
        {
            OnEnemyKilled();
        }
    }
    
    void OnEnemyKilled()
    {
        currentKillCount++;
        Debug.Log("[MISSION] Enemy killed: " + currentKillCount + "/" + enemiesToKill);
        
        OnObjectiveProgress?.Invoke(currentKillCount, enemiesToKill);
    }
    
    public void CompleteMission()
    {
        if (missionCompleted || missionFailed) return;
        
        missionActive = false;
        missionCompleted = true;
        
        Debug.Log("[MISSION] Completed: " + missionName);
        
        // Play sound
        if (audioSource != null && missionCompleteSound != null)
        {
            audioSource.PlayOneShot(missionCompleteSound);
        }
        
        // Stop spawning
        StopAllCoroutines();
        
        // Clear remaining enemies
        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy);
            }
        }
        spawnedEnemies.Clear();
        
        // Clear spawned trees
        ClearSpawnedTrees();
        
        // Give rewards
        GiveRewards();
        
        // Show completion UI
        if (missionUI != null)
        {
            missionUI.ShowCompleted();
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
        
        Debug.Log("[MISSION] Failed: " + reason);
        
        // Play sound
        if (audioSource != null && missionFailSound != null)
        {
            audioSource.PlayOneShot(missionFailSound);
        }
        
        // Stop spawning
        StopAllCoroutines();
        
        // Clear enemies
        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy);
            }
        }
        spawnedEnemies.Clear();
        
        // Clear spawned trees
        ClearSpawnedTrees();
        
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
        
        // Add gold/exp (implement based on your game's economy system)
        Debug.Log("[MISSION] Rewards: " + goldReward + " gold, " + expReward + " exp");
    }
    
    // Public methods for external control
    public void ForceCompleteMission() => CompleteMission();
    public void ForceFailMission(string reason) => FailMission(reason);
    public bool IsMissionActive() => missionActive;
    public bool IsMissionCompleted() => missionCompleted;
    public int GetKillCount() => currentKillCount;
    public int GetCurrentWave() => currentWave;
    public float GetTimeRemaining() => timeLimit > 0 ? timeLimit - missionTimer : 0f;
    public int GetTreesDestroyed() => treesDestroyed;
    public int GetAnimalsRescued() => animalsRescued;
    
    // GUI for interaction prompt
    void OnGUI()
    {
        if (!showInteractionPrompt || missionActive || missionCompleted) return;
        if (!playerInRange || player == null) return;
        
        // Draw interaction prompt
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f);
        
        if (screenPos.z > 0)
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 20;
            style.normal.textColor = Color.yellow;
            style.alignment = TextAnchor.MiddleCenter;
            
            GUI.Label(new Rect(screenPos.x - 100, Screen.height - screenPos.y - 50, 200, 30), 
                      "Press [" + interactionKey + "] to start mission", style);
            
            style.fontSize = 16;
            style.normal.textColor = Color.white;
            GUI.Label(new Rect(screenPos.x - 150, Screen.height - screenPos.y - 20, 300, 30), 
                      missionName, style);
        }
    }
    
    // Gizmos
    void OnDrawGizmosSelected()
    {
        // Draw interaction range
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
        
        // Draw spawn points
        if (spawnPoints != null)
        {
            Gizmos.color = Color.red;
            foreach (Transform spawnPoint in spawnPoints)
            {
                if (spawnPoint != null)
                {
                    Gizmos.DrawWireSphere(spawnPoint.position, 1f);
                    Gizmos.DrawLine(transform.position, spawnPoint.position);
                }
            }
        }
        
        // Draw tree spawn points
        if (treeSpawnPoints != null)
        {
            Gizmos.color = Color.blue;
            foreach (Transform treeSpawnPoint in treeSpawnPoints)
            {
                if (treeSpawnPoint != null)
                {
                    Gizmos.DrawWireSphere(treeSpawnPoint.position, 1f);
                    Gizmos.DrawLine(transform.position, treeSpawnPoint.position);
                }
            }
        }
    }
}
