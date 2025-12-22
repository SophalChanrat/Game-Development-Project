using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays mission progress and objectives to player
/// </summary>
public class MissionUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI missionNameText;
    public TextMeshProUGUI objectiveText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI waveText;
    public TextMeshProUGUI treeStatusText;
    public TextMeshProUGUI rescueStatusText;
    public Image progressBar;
    public GameObject completedPanel;
    public GameObject failedPanel;
    public GameObject missionProgressPanel;
    public GameObject bgprotect;
    public GameObject bganimal;
    
    [Header("Mission Reference")]
    public MissionSystem mission;
    public AnimalRescueMission rescueMission;

    [Header("Settings")]
    public float completePanelDisplayTime = 3f;
    public float failedPanelDisplayTime = 3f;
    
    [Header("Debug")]
    public bool showDebugLogs = false;
    
    void Start()
    {
        if (mission == null)
        {
            mission = FindObjectOfType<MissionSystem>();
            rescueMission = FindObjectOfType<AnimalRescueMission>();
        }
        
        // Hide all panels initially
        if (completedPanel != null) completedPanel.SetActive(false);
        if (failedPanel != null) failedPanel.SetActive(false);
        if (missionProgressPanel != null) missionProgressPanel.SetActive(false);
        
        // Clear all text to prevent showing placeholder values
        ClearAllText();
        
        if (showDebugLogs)
        {
            Debug.Log("[MISSION UI] Initialized. Mission found: " + (mission != null));
        }
    }
    
    void ClearAllText()
    {
        // Clear all text fields so placeholder text doesn't show
        if (missionNameText != null) missionNameText.text = "";
        if (objectiveText != null) objectiveText.text = "";
        if (timerText != null) timerText.text = "";
        if (waveText != null) waveText.text = "";
        if (treeStatusText != null) treeStatusText.text = "";
        if (rescueStatusText != null) rescueStatusText.text = "";
        if (progressBar != null) progressBar.fillAmount = 0f;
    }
    
    void Update()
    {
        // Show/hide progress panel based on mission state
        if (missionProgressPanel != null)
        {   
            bool shouldShow =
                (mission != null && mission.IsMissionActive()) ||
                (rescueMission != null && rescueMission.IsMissionActive());
            if (missionProgressPanel.activeSelf != shouldShow)
            {
                missionProgressPanel.SetActive(shouldShow);
                
                if (showDebugLogs)
                {
                    Debug.Log("[MISSION UI] Progress panel " + (shouldShow ? "shown" : "hidden"));
                }
            }
        }
        
        // Update mission info if active
        if (mission != null && mission.IsMissionActive())
        {
            UpdateTreeMissionInfo();
        }
        else if (rescueMission != null && rescueMission.IsMissionActive())
        {
            UpdateRescueMissionInfo();
        }
    }
    
    void UpdateTreeMissionInfo()
    {
        bganimal?.SetActive(false);
        bgprotect?.SetActive(true);
        // Update mission name
        if (missionNameText != null)
        {
            missionNameText.text = mission.missionName;
        }
        
        // Update objective
        if (objectiveText != null)
        {
            int kills = mission.GetKillCount();
            int total = mission.enemiesToKill;
            objectiveText.text = "Enemies: " + kills + " / " + total;
        }
        
        // Update tree status
        if (treeStatusText != null)
        {
            int treesLost = mission.GetTreesDestroyed();
            treeStatusText.text = "Trees Lost: " + treesLost + " / " + mission.allowedTreeLosses;
            treeStatusText.gameObject.SetActive(true);
        }
        
        // Hide rescue status
        if (rescueStatusText != null)
        {
            rescueStatusText.gameObject.SetActive(false);
        }
        
        // Update timer
        UpdateTimer(mission.timeLimit, mission.GetTimeRemaining());
        
        // Update wave
        UpdateWave(mission.totalWaves, mission.GetCurrentWave());
        
        // Update progress bar
        if (progressBar != null && mission.enemiesToKill > 0)
        {
            progressBar.fillAmount = (float)mission.GetKillCount() / mission.enemiesToKill;
        }
    }
    
    void UpdateRescueMissionInfo()
    {
        bganimal?.SetActive(true);
        bgprotect?.SetActive(false);
        // Update mission name
        if (missionNameText != null)
        {
            missionNameText.text = rescueMission.missionName;
        }
        
        // Update objective - show animals rescued
        if (objectiveText != null)
        {
            int rescued = rescueMission.GetAnimalsRescued();
            int total = rescueMission.GetAnimalsToRescue();
            objectiveText.text = "Animals Rescued: " + rescued + " / " + total;
        }
        
        // Show rescue status
        if (rescueStatusText != null)
        {
            int rescued = rescueMission.GetAnimalsRescued();
            int total = rescueMission.GetAnimalsToRescue();
            rescueStatusText.text = "Progress: " + rescued + " / " + total;
            rescueStatusText.gameObject.SetActive(true);
        }
        
        // Hide tree status
        if (treeStatusText != null)
        {
            treeStatusText.gameObject.SetActive(false);
        }
        
        // Update timer
        UpdateTimer(rescueMission.timeLimit, rescueMission.GetTimeRemaining());
        
        // Hide wave text (rescue mission doesn't show waves)
        if (waveText != null)
        {
            waveText.gameObject.SetActive(false);
        }
        
        // Update progress bar
        if (progressBar != null && rescueMission.GetAnimalsToRescue() > 0)
        {
            progressBar.fillAmount = (float)rescueMission.GetAnimalsRescued() / rescueMission.GetAnimalsToRescue();
        }
    }
    
    void UpdateTimer(float timeLimit, float timeRemaining)
    {
        if (timerText != null)
        {
            if (timeLimit > 0)
            {
                int minutes = Mathf.FloorToInt(timeRemaining / 60f);
                int seconds = Mathf.FloorToInt(timeRemaining % 60f);
                timerText.text = string.Format("Time: {0:00}:{1:00}", minutes, seconds);
                timerText.gameObject.SetActive(true);
            }
            else
            {
                timerText.gameObject.SetActive(false);
            }
        }
    }
    
    void UpdateWave(int totalWaves, int currentWave)
    {
        if (waveText != null)
        {
            if (totalWaves > 0)
            {
                waveText.text = "Wave: " + currentWave + " / " + totalWaves;
                waveText.gameObject.SetActive(true);
            }
            else
            {
                waveText.gameObject.SetActive(false);
            }
        }
    }
    
    public void ShowCompleted()
    {
        if (showDebugLogs)
        {
            Debug.Log("[MISSION UI] Showing completed panel");
        }
        
        // Hide progress panel
        if (missionProgressPanel != null)
        {
            missionProgressPanel.SetActive(false);
        }
        
        // Show completed panel
        if (completedPanel != null)
        {
            completedPanel.SetActive(true);
            
            // Auto-hide after delay
            StartCoroutine(HidePanelAfterDelay(completedPanel, completePanelDisplayTime));
        }
    }
    
    public void ShowFailed()
    {
        if (showDebugLogs)
        {
            Debug.Log("[MISSION UI] Showing failed panel");
        }
        
        // Hide progress panel
        if (missionProgressPanel != null)
        {
            missionProgressPanel.SetActive(false);
        }
        
        // Show failed panel
        if (failedPanel != null)
        {
            failedPanel.SetActive(true);
            
            // Auto-hide after delay
            StartCoroutine(HidePanelAfterDelay(failedPanel, failedPanelDisplayTime));
        }
    }
    
    System.Collections.IEnumerator HidePanelAfterDelay(GameObject panel, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }
    
    // Manual show/hide methods for external control
    public void ShowMissionUI()
    {
        if (missionProgressPanel != null)
        {
            missionProgressPanel.SetActive(true);
        }
    }
    
    public void HideMissionUI()
    {
        if (missionProgressPanel != null)
        {
            missionProgressPanel.SetActive(false);
        }
        if (completedPanel != null)
        {
            completedPanel.SetActive(false);
        }
        if (failedPanel != null)
        {
            failedPanel.SetActive(false);
        }
        
        // Clear text when hiding
        ClearAllText();
    }
}
