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
    public Image progressBar;
    public GameObject completedPanel;
    public GameObject failedPanel;
    public GameObject missionProgressPanel;
    
    [Header("Mission Reference")]
    public MissionSystem mission;
    
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
        if (progressBar != null) progressBar.fillAmount = 0f;
    }
    
    void Update()
    {
        if (mission == null) return;
        
        // Show/hide progress panel based on mission state
        if (missionProgressPanel != null)
        {
            bool shouldShow = mission.IsMissionActive();
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
        if (mission.IsMissionActive())
        {
            UpdateMissionInfo();
        }
    }
    
    void UpdateMissionInfo()
    {
        // Update mission name
        if (missionNameText != null)
        {
            string newName = mission.missionName;
            if (missionNameText.text != newName)
            {
                missionNameText.text = newName;
                
                if (showDebugLogs)
                {
                    Debug.Log("[MISSION UI] Updated mission name: " + newName);
                }
            }
        }
        
        // Update objective
        if (objectiveText != null)
        {
            int kills = mission.GetKillCount();
            int total = mission.enemiesToKill;
            string newObjective = "Enemies: " + kills + " / " + total;
            
            if (objectiveText.text != newObjective)
            {
                objectiveText.text = newObjective;
                
                if (showDebugLogs)
                {
                    Debug.Log("[MISSION UI] Updated objective: " + newObjective);
                }
            }
        }
        
        // Update tree status for Protect Trees mission
        if (treeStatusText != null)
        {
            int treesLost = mission.GetTreesDestroyed();
            string treeStatus = "Trees Lost: " + treesLost + " / " + mission.allowedTreeLosses;
            
            if (treeStatusText.text != treeStatus)
            {
                treeStatusText.text = treeStatus;
            }
            
            // Show tree status only active mission is active
            treeStatusText.gameObject.SetActive(mission.IsMissionActive());
        }
        
        // Update timer
        if (timerText != null)
        {
            if (mission.timeLimit > 0)
            {
                float timeRemaining = mission.GetTimeRemaining();
                int minutes = Mathf.FloorToInt(timeRemaining / 60f);
                int seconds = Mathf.FloorToInt(timeRemaining % 60f);
                string newTimer = string.Format("Time: {0:00}:{1:00}", minutes, seconds);
                
                if (timerText.text != newTimer)
                {
                    timerText.text = newTimer;
                }
                
                // Make sure timer is visible
                if (!timerText.gameObject.activeSelf)
                {
                    timerText.gameObject.SetActive(true);
                }
            }
            else
            {
                // Hide timer if not used
                if (timerText.gameObject.activeSelf)
                {
                    timerText.gameObject.SetActive(false);
                }
            }
        }
        
        // Update wave
        if (waveText != null)
        {
            if (mission.totalWaves > 0)
            {
                string newWave = "Wave: " + mission.GetCurrentWave() + " / " + mission.totalWaves;
                
                if (waveText.text != newWave)
                {
                    waveText.text = newWave;
                }
                
                // Make sure wave is visible
                if (!waveText.gameObject.activeSelf)
                {
                    waveText.gameObject.SetActive(true);
                }
            }
            else
            {
                // Hide wave if not used
                if (waveText.gameObject.activeSelf)
                {
                    waveText.gameObject.SetActive(false);
                }
            }
        }
        
        // Update progress bar
        if (progressBar != null && mission.enemiesToKill > 0)
        {
            float progress = (float)mission.GetKillCount() / mission.enemiesToKill;
            progressBar.fillAmount = progress;
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
