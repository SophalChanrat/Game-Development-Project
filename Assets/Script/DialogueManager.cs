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

    [Header("Dialogue Content")]
    private List<string> dialogueLines = new List<string>
    {
        "Guardian… thank you for answering my call. Our forest is in danger.",
        "Goblins are cutting trees to steal the Heart Crystals hidden inside them.",
        "You must protect the forest. Stop the goblins and free the trapped animals.",
        "Collect Nature Orbs by defeating enemies and saving wildlife. Use them to restore parts of the forest.",
        "I believe in you, young guardian. Go, and keep the forest alive."
    };

    [SerializeField] private List<Sprite> backgroundImages = new List<Sprite>();
    
    [Header("Typing Effect")]
    [Tooltip("Enable typewriter effect")]
    public bool useTypingEffect = true;
    
    [Tooltip("Characters per second")]
    public float typingSpeed = 50f;

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
        
        // Hide dialogue canvas at start
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
        
        isDialogueActive = true;
        currentDialogueIndex = 0;
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
        if (currentDialogueIndex < dialogueLines.Count)
        {
            ChangeBackground(currentDialogueIndex);
            
            if (useTypingEffect)
            {
                StartCoroutine(TypeText(dialogueLines[currentDialogueIndex]));
            }
            else
            {
                dialogueText.text = dialogueLines[currentDialogueIndex];
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
        if (currentDialogueIndex < dialogueLines.Count)
        {
            dialogueText.text = dialogueLines[currentDialogueIndex];
        }
        isTyping = false;
    }

    private void NextDialogue()
    {
        currentDialogueIndex++;

        if (currentDialogueIndex < dialogueLines.Count)
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
        dialogueCanvas.gameObject.SetActive(false);
        currentDialogueIndex = 0;
        
        StopAllCoroutines();
        
        Debug.Log("[DIALOGUE] Ended dialogue");
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
    
    // GUI for interaction prompt (similar to MissionSystem)
    void OnGUI()
    {
        if (!showInteractionPrompt || isDialogueActive) return;
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
                      "Press [F] to talk", style);
            
            style.fontSize = 16;
            style.normal.textColor = Color.white;
            GUI.Label(new Rect(screenPos.x - 150, Screen.height - screenPos.y - 20, 300, 30), 
                      npcName, style);
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