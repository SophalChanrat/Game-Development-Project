using UnityEngine;
using UnityEngine.UI;

public class RescueInteractable : MonoBehaviour
{
    [Header("Rescue Settings")]
    public float rescueDuration = 5f;

    [Header("References")]
    public AnimalMovement trappedAnimal;
    public ParticleSystem cageParticles;
    public Slider worldSlider;

    [Header("Interaction Prompt")]
    [Tooltip("Show prompt above animal")]
    public bool showInteractionPrompt = true;
    
    [Tooltip("Animal name to display")]
    public string animalName = "Trapped Animal";

    [HideInInspector] public bool playerInRange = false;

    private float progress;
    private bool isRescuing;
    private bool rescued;
    private Transform player;

    private void Start()
    {
        if (worldSlider != null)
            worldSlider.gameObject.SetActive(false);
            
        // Find player for prompt
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    private void Update()
    {
        if (!isRescuing || rescued)
            return;

        progress += Time.deltaTime;

        if (worldSlider != null)
            worldSlider.value = progress / rescueDuration;

        if (progress >= rescueDuration)
            CompleteRescue();
    }

    public void TryRescue()
    {
        if (playerInRange)
            StartRescue();
    }

    public void StartRescue()
    {
        if (rescued || isRescuing)
            return;

        Debug.Log("Rescue started.");
        isRescuing = true;
        progress = 0f;

        if (worldSlider != null)
        {
            worldSlider.value = 0f;
            worldSlider.gameObject.SetActive(true);
        }
    }

    public void CancelRescue()
    {
        if (!isRescuing)
            return;

        Debug.Log("Rescue canceled.");
        isRescuing = false;
        progress = 0f;

        if (worldSlider != null)
        {
            worldSlider.value = 0f;
            worldSlider.gameObject.SetActive(false);
        }
    }

    private void CompleteRescue()
    {
        Debug.Log("[RESCUE] *** Rescue completed! ***");
        Debug.Log("[RESCUE] GameObject: " + gameObject.name);
        Debug.Log("[RESCUE] Active before SetActive(false): " + gameObject.activeSelf);
        
        rescued = true;
        isRescuing = false;

        if (worldSlider != null)
            worldSlider.gameObject.SetActive(false);

        if (trappedAnimal != null)
            trappedAnimal.Release();

        if (cageParticles != null)
            cageParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // Disable the gameObject - this should trigger OnDisable in RescueDetector
        gameObject.SetActive(false);
        
        // This line should NOT execute because object is disabled
        // If you see this in console, something is wrong!
        Debug.Log("[RESCUE] Active after SetActive(false): " + gameObject.activeSelf);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            other.GetComponent<PlayerMovement3D>().currentRescueTarget = this;
            Debug.Log("Player entered rescue zone.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            other.GetComponent<PlayerMovement3D>().currentRescueTarget = null;
            CancelRescue();
            Debug.Log("Player left rescue zone.");
        }
    }
    
    // GUI for interaction prompt (like MissionSystem and DialogueManager)
    void OnGUI()
    {
        if (!showInteractionPrompt || rescued) return;
        if (!playerInRange || player == null) return;
        
        // Draw interaction prompt
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f);
        
        if (screenPos.z > 0)
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 20;
            style.normal.textColor = Color.yellow;
            style.alignment = TextAnchor.MiddleCenter;
            
            if (isRescuing)
            {
                // Show progress while rescuing
                int percentage = Mathf.RoundToInt((progress / rescueDuration) * 100f);
                GUI.Label(new Rect(screenPos.x - 100, Screen.height - screenPos.y - 50, 200, 30), 
                          "Rescuing... " + percentage + "%", style);
            }
            else
            {
                // Show hold prompt
                GUI.Label(new Rect(screenPos.x - 100, Screen.height - screenPos.y - 50, 200, 30), 
                          "Hold [F] to rescue", style);
            }
            
            style.fontSize = 16;
            style.normal.textColor = Color.white;
            GUI.Label(new Rect(screenPos.x - 150, Screen.height - screenPos.y - 20, 300, 30), 
                      animalName, style);
        }
    }
    
    // Gizmos for debugging
    void OnDrawGizmosSelected()
    {
        // Draw interaction range (from trigger collider)
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 2f);
        }
    }
}
