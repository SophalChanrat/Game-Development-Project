using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Canvas dialogueCanvas;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private Button skipButton;
    [SerializeField] private GameObject pressEPrompt;
    [SerializeField] private Image backgroundImage;
    
    [Header("Interaction Settings")]
    [Tooltip("Distance player must be to interact")]
    public float interactionDistance = 3f;
    
    [Tooltip("Show prompt above NPC")]
    public bool showInteractionPrompt = true;
    
    [Tooltip("NPC name to display")]
    public string npcName = "Forest Guardian";

    [Header("Dialogue Content - Initial")]
    [SerializeField] private List<string> initialDialogueLines = new List<string>
    {
        "Guardian… thank you for answering my call. Our forest is in danger.",
        "Goblins are cutting trees to steal the Heart Crystals hidden inside them.",
        "You must protect the forest. Stop the goblins and free the trapped animals.",
        "Collect Nature Orbs by defeating enemies and saving wildlife. Use them to restore parts of the forest.",
        "I believe in you, young guardian. Go, and keep the forest alive."
    };
    
    [Header("Dialogue Content - After Missions Complete")]
    [SerializeField] private List<string> completionDialogueLines = new List<string>
    {
        "You have done it, Guardian! The forest is saved!",
        "The trees are protected and the animals are free once more.",
        "The Heart Crystals are safe, and nature flourishes again.",
        "You have proven yourself a true protector of the forest.",
        "Thank you, brave Guardian. The forest spirits will remember your deeds forever!"
    };

    [SerializeField] private List<Sprite> backgroundImages = new List<Sprite>();
    
    [Header("Typing Effect")]
    [Tooltip("Enable typewriter effect")]
    public bool useTypingEffect = true;
    
    [Tooltip("Characters per second")]
    public float typingSpeed = 50f;
    
    [Header("Mission Tracking")]
    [Tooltip("Has the player talked to the Tree Spirit?")]
    public static bool hasTalkedToTreeSpirit = false;
    
    [Tooltip("Have all missions been completed?")]
    public static bool allMissionsCompleted = false;
    
    [Tooltip("Has player received the completion dialogue?")]
    public static bool hasReceivedCompletionDialogue = false;
    
    [Header("Return Marker Settings")]
    [Tooltip("Show marker when all missions complete")]
    public bool showReturnMarker = true;
    
    [Tooltip("Marker height above NPC")]
    public float markerHeight = 3f;
    
    [Tooltip("Marker bob speed")]
    public float markerBobSpeed = 2f;
    
    [Tooltip("Marker bob amount")]
    public float markerBobAmount = 0.3f;
    
    [Header("Events")]
    [Tooltip("Event when dialogue completes and missions unlock")]
    public UnityEngine.Events.UnityEvent OnMissionsUnlocked;
    
    [Tooltip("Event when all missions complete and player talks again")]
    public UnityEngine.Events.UnityEvent OnAllMissionsCompleteTalk;

    // Current dialogue being used
    private List<string> currentDialogueLines;
    
    private int currentDialogueIndex = 0;
    private bool isDialogueActive = false;
    private bool isTyping = false;
    private Transform player;
    private bool playerInRange = false;
    private PlayerInput playerInput;

    private void Start()
    {
        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerInput = playerObj.GetComponent<PlayerInput>();
        }
        
        // Set initial dialogue
        currentDialogueLines = initialDialogueLines;
        
        // Hide dialogue canvas at start
        if (dialogueCanvas != null)
            dialogueCanvas.gameObject.SetActive(false);
        
        if (pressEPrompt != null)
        {
            pressEPrompt.SetActive(false);
        }

        // Setup skip button
        if (skipButton != null)
        {
            skipButton.onClick.AddListener(SkipDialogue);
        }
        
        Debug.Log("[DIALOGUE] DialogueManager initialized. Missions unlocked: " + hasTalkedToTreeSpirit);
    }

    private void Update()
    {
        if (player == null) return;
        
        // Check player distance for interaction
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        playerInRange = distanceToPlayer <= interactionDistance;
        
        // Show/hide prompt based on distance
        if (pressEPrompt != null)
        {
            pressEPrompt.SetActive(playerInRange && !isDialogueActive);
        }
        
        // Check if all missions are completed
        CheckAllMissionsCompleted();
    }
    
    void CheckAllMissionsCompleted()
    {
        if (allMissionsCompleted) return; // Already marked complete
        if (!hasTalkedToTreeSpirit) return; // Missions not even started
        
        // Check tree protection mission
        MissionSystem treeMission = FindObjectOfType<MissionSystem>();
        bool treeComplete = (treeMission == null || treeMission.IsMissionCompleted());
        
        // Check animal rescue mission
        AnimalRescueMission rescueMission = FindObjectOfType<AnimalRescueMission>();
        bool rescueComplete = (rescueMission == null || rescueMission.IsMissionCompleted());
        
        // If both complete, mark all missions done
        if (treeComplete && rescueComplete)
        {
            allMissionsCompleted = true;
            currentDialogueLines = completionDialogueLines;
            Debug.Log("[DIALOGUE] ========================================");
            Debug.Log("[DIALOGUE] ALL MISSIONS COMPLETED! Return to Tree Spirit!");
            Debug.Log("[DIALOGUE] ========================================");
        }
    }

    // Called from PlayerInput via UnityEvents or direct call
    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!context.started) return;
        
        // Start dialogue if in range and not active
        if (!isDialogueActive && playerInRange)
        {
            StartDialogue();
        }
    }
    
    // Called from PlayerInput for advancing dialogue
    public void OnContinue(InputAction.CallbackContext context)
    {
        if (!context.started) return;
        
        if (isDialogueActive)
        {
            if (isTyping)
            {
                // Skip typing animation
                StopAllCoroutines();
                CompleteCurrentLine();
            }
            else
            {
                NextDialogue();
            }
        }
    }
    
    // Alternative: Mouse click to advance (can be called from UI button or Input System)
    public void OnMouseClick(InputAction.CallbackContext context)
    {
        if (!context.started) return;
        
        if (isDialogueActive)
        {
            if (isTyping)
            {
                StopAllCoroutines();
                CompleteCurrentLine();
            }
            else
            {
                NextDialogue();
            }
        }
    }

    public void StartDialogue()
    {
        if (isDialogueActive) return;
        
        // Switch to completion dialogue if all missions done
        if (allMissionsCompleted && !hasReceivedCompletionDialogue)
        {
            currentDialogueLines = completionDialogueLines;
        }
        
        isDialogueActive = true;
        currentDialogueIndex = 0;
        
        if (dialogueCanvas != null)
            dialogueCanvas.gameObject.SetActive(true);
        
        if (pressEPrompt != null)
        {
            pressEPrompt.SetActive(false);
        }
        
        DisplayCurrentDialogue();
        
        Debug.Log("[DIALOGUE] Started dialogue");
    }

    private void DisplayCurrentDialogue()
    {
        if (currentDialogueIndex < currentDialogueLines.Count)
        {
            ChangeBackground(currentDialogueIndex);
            
            if (useTypingEffect)
            {
                StartCoroutine(TypeText(currentDialogueLines[currentDialogueIndex]));
            }
            else
            {
                dialogueText.text = currentDialogueLines[currentDialogueIndex];
            }
        }
    }
    
    IEnumerator TypeText(string text)
    {
        isTyping = true;
        dialogueText.text = "";
        
        foreach (char c in text)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(1f / typingSpeed);
        }
        
        isTyping = false;
    }
    
    void CompleteCurrentLine()
    {
        if (currentDialogueIndex < currentDialogueLines.Count)
        {
            dialogueText.text = currentDialogueLines[currentDialogueIndex];
        }
        isTyping = false;
    }

    private void NextDialogue()
    {
        currentDialogueIndex++;

        if (currentDialogueIndex < currentDialogueLines.Count)
        {
            DisplayCurrentDialogue();
        }
        else
        {
            EndDialogue();
        }
    }

    private void SkipDialogue()
    {
        EndDialogue();
    }

    private void EndDialogue()
    {
        isDialogueActive = false;
        isTyping = false;
        
        if (dialogueCanvas != null)
            dialogueCanvas.gameObject.SetActive(false);
        
        currentDialogueIndex = 0;
        
        StopAllCoroutines();
        
        // First time talking - unlock missions
        if (!hasTalkedToTreeSpirit)
        {
            hasTalkedToTreeSpirit = true;
            Debug.Log("[DIALOGUE] ========================================");
            Debug.Log("[DIALOGUE] MISSIONS UNLOCKED! Player has talked to Tree Spirit!");
            Debug.Log("[DIALOGUE] ========================================");
            
            // Invoke event to notify missions
            OnMissionsUnlocked?.Invoke();
            
            // Notify all missions that they are now available
            NotifyMissionsUnlocked();
        }
        // All missions complete - completion dialogue finished
        else if (allMissionsCompleted && !hasReceivedCompletionDialogue)
        {
            hasReceivedCompletionDialogue = true;
            Debug.Log("[DIALOGUE] ========================================");
            Debug.Log("[DIALOGUE] GAME COMPLETE! Player finished all missions!");
            Debug.Log("[DIALOGUE] ========================================");
            
            // Invoke completion event
            OnAllMissionsCompleteTalk?.Invoke();
        }
        
        Debug.Log("[DIALOGUE] Ended dialogue");
    }
    
    private void NotifyMissionsUnlocked()
    {
        // Notify tree protection mission
        MissionSystem treeMission = FindObjectOfType<MissionSystem>();
        if (treeMission != null)
        {
            treeMission.OnMissionsUnlockedByDialogue();
        }
        
        // Notify animal rescue mission
        AnimalRescueMission rescueMission = FindObjectOfType<AnimalRescueMission>();
        if (rescueMission != null)
        {
            rescueMission.OnMissionsUnlockedByDialogue();
        }
    }

    public void ShowPressEPrompt()
    {
        if (pressEPrompt != null)
        {
            pressEPrompt.SetActive(true);
        }
    }

    public void HidePressEPrompt()
    {
        if (pressEPrompt != null)
        {
            pressEPrompt.SetActive(false);
        }
    }

    private void ChangeBackground(int dialogueIndex)
    {
        if (backgroundImages.Count > 0 && dialogueIndex < backgroundImages.Count)
        {
            if (backgroundImages[dialogueIndex] != null && backgroundImage != null)
            {
                backgroundImage.sprite = backgroundImages[dialogueIndex];
                Debug.Log("[DIALOGUE] Background changed to image " + dialogueIndex);
            }
        }
    }
    
    // Public getters
    public bool IsDialogueActive() => isDialogueActive;
    public bool IsPlayerInRange() => playerInRange;
    public static bool AreMissionsUnlocked() => hasTalkedToTreeSpirit;
    public static bool AreAllMissionsComplete() => allMissionsCompleted;
    public static bool HasReceivedCompletion() => hasReceivedCompletionDialogue;
    
    // GUI for interaction prompt AND return marker
    void OnGUI()
    {
        if (player == null) return;
        
        // Show return marker when all missions complete but haven't talked yet
        if (showReturnMarker && allMissionsCompleted && !hasReceivedCompletionDialogue && !isDialogueActive)
        {
            DrawReturnMarker();
        }
        
        if (!showInteractionPrompt || isDialogueActive) return;
        if (!playerInRange) return;
        
        // Draw interaction prompt
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f);
        
        if (screenPos.z > 0)
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 20;
            style.normal.textColor = Color.yellow;
            style.alignment = TextAnchor.MiddleCenter;
            
            GUI.Label(new Rect(screenPos.x - 100, Screen.height - screenPos.y - 50, 200, 30), 
                      "Press [F] to talk", style);
            
            style.fontSize = 16;
            style.normal.textColor = Color.white;
            GUI.Label(new Rect(screenPos.x - 150, Screen.height - screenPos.y - 20, 300, 30), 
                      npcName, style);
        }
    }
    
    void DrawReturnMarker()
    {
        // Calculate bobbing position
        float bob = Mathf.Sin(Time.time * markerBobSpeed) * markerBobAmount;
        Vector3 markerWorldPos = transform.position + Vector3.up * (markerHeight + bob);
        
        Vector3 screenPos = Camera.main.WorldToScreenPoint(markerWorldPos);
        
        if (screenPos.z > 0)
        {
            GUIStyle markerStyle = new GUIStyle();
            markerStyle.fontSize = 30;
            markerStyle.normal.textColor = Color.yellow;
            markerStyle.alignment = TextAnchor.MiddleCenter;
            
            // Draw question mark
            GUI.Label(new Rect(screenPos.x - 15, Screen.height - screenPos.y - 15, 30, 30), "?", markerStyle);
            
            // Draw text below marker
            markerStyle.fontSize = 14;
            markerStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(screenPos.x - 100, Screen.height - screenPos.y + 15, 200, 20), 
                      "Return to " + npcName, markerStyle);
            
            // Draw "Missions Complete!" text
            markerStyle.fontSize = 12;
            markerStyle.normal.textColor = Color.green;
            GUI.Label(new Rect(screenPos.x - 100, Screen.height - screenPos.y + 35, 200, 20), 
                      "All Missions Complete!", markerStyle);
        }
    }
    
    // Gizmos for debugging (similar to MissionSystem)
    void OnDrawGizmosSelected()
    {
        // Draw interaction range
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}