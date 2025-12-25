using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class CreditManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The ScrollRect component for the credits")]
    [SerializeField] private ScrollRect creditScrollView;
    
    [Tooltip("The content RectTransform that contains all credit elements")]
    [SerializeField] private RectTransform content;
    
    [Header("Scroll Settings")]
    [Tooltip("Speed of the credit scroll (0-1 per second)")]
    [SerializeField] private float scrollSpeed = 0.05f;
    
    [Tooltip("Delay before credits start scrolling")]
    [SerializeField] private float startDelay = 1f;
    
    [Header("Scene Settings")]
    [Tooltip("Name of the menu scene to load")]
    [SerializeField] private string menuSceneName = "MenuScene";
    
    [Tooltip("Delay after credits end before returning to menu")]
    [SerializeField] private float endDelay = 2f;
    
    [Header("Skip Settings")]
    [Tooltip("Allow player to skip credits")]
    [SerializeField] private bool allowSkip = true;
    
    [Tooltip("Show skip prompt text")]
    [SerializeField] private bool showSkipPrompt = true;
    
    [Tooltip("Optional skip button UI element")]
    [SerializeField] private Button skipButton;
    
    // Private variables
    private bool isScrolling = false;
    private bool creditsEnded = false;
    private float currentScrollPosition = 1f; // Start at top (1 = top, 0 = bottom)
    private float startTimer = 0f;
    private bool isInitialized = false;
    
    private void Awake()
    {
        // Ensure proper state when credits scene loads
        InitializeCreditScene();
    }
    
    private void Start()
    {
        // Initialize scroll position to top
        if (creditScrollView != null)
        {
            creditScrollView.verticalNormalizedPosition = 1f;
            currentScrollPosition = 1f;
        }
        
        // Setup skip button if assigned
        if (skipButton != null)
        {
            skipButton.onClick.AddListener(SkipCredits);
        }
        
        startTimer = startDelay;
        isInitialized = true;
        
        Debug.Log("[CREDITS] Credit Manager initialized");
    }
    
    private void InitializeCreditScene()
    {
        // CRITICAL: Ensure time is running (in case game was paused)
        Time.timeScale = 1f;
        
        // CRITICAL: Unlock and show cursor so player can click
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        Debug.Log("[CREDITS] Credit scene initialized - Cursor unlocked, TimeScale reset to 1");
    }
    
    private void Update()
    {
        // Safety check - ensure cursor stays unlocked in credits
        if (Cursor.lockState != CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        // Handle start delay
        if (startTimer > 0f)
        {
            startTimer -= Time.deltaTime;
            if (startTimer <= 0f)
            {
                isScrolling = true;
                Debug.Log("[CREDITS] Credits started scrolling");
            }
            return;
        }
        
        // Check for skip input (mouse click, touch, or keyboard)
        if (allowSkip && !creditsEnded)
        {
            // Check for any input to skip
            if (Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
            {
                // Small delay to prevent accidental skip on scene load
                if (isInitialized && Time.timeSinceLevelLoad > 0.5f)
                {
                    SkipCredits();
                }
            }
        }
        
        // Scroll credits
        if (isScrolling && !creditsEnded)
        {
            ScrollCredits();
        }
    }
    
    private void ScrollCredits()
    {
        if (creditScrollView == null) return;
        
        // Decrease scroll position (moving from top to bottom)
        currentScrollPosition -= scrollSpeed * Time.deltaTime;
        
        // Apply scroll position
        creditScrollView.verticalNormalizedPosition = Mathf.Clamp01(currentScrollPosition);
        
        // Check if credits have ended (reached bottom)
        if (currentScrollPosition <= 0f)
        {
            OnCreditsEnded();
        }
    }
    
    private void OnCreditsEnded()
    {
        if (creditsEnded) return;
        
        creditsEnded = true;
        isScrolling = false;
        
        Debug.Log("[CREDITS] Credits ended, returning to menu in " + endDelay + " seconds");
        
        // Return to menu after delay
        Invoke(nameof(ReturnToMenu), endDelay);
    }
    
    public void SkipCredits()
    {
        if (creditsEnded) return;
        
        Debug.Log("[CREDITS] Credits skipped by player");
        
        creditsEnded = true;
        isScrolling = false;
        
        // Cancel any pending invokes
        CancelInvoke();
        
        // Return to menu immediately
        ReturnToMenu();
    }
    
    private void ReturnToMenu()
    {
        Debug.Log("[CREDITS] Loading menu scene: " + menuSceneName);
        
        // Reset any static variables if needed
        ResetGameState();
        
        // Ensure cursor is visible for menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Ensure time scale is normal
        Time.timeScale = 1f;
        
        // Load menu scene
        if (!string.IsNullOrEmpty(menuSceneName))
        {
            SceneManager.LoadScene(menuSceneName);
        }
        else
        {
            Debug.LogWarning("[CREDITS] Menu scene name not set!");
        }
    }
    
    private void ResetGameState()
    {
        // Reset dialogue manager static variables for new game
        DialogueManager.hasTalkedToTreeSpirit = false;
        DialogueManager.allMissionsCompleted = false;
        DialogueManager.hasReceivedCompletionDialogue = false;
        
        Debug.Log("[CREDITS] Game state reset for new game");
    }
    
    // Called from Input System if using new input
    public void OnSkip(InputAction.CallbackContext context)
    {
        if (!context.started) return;
        
        if (allowSkip && !creditsEnded)
        {
            SkipCredits();
        }
    }
    
    // GUI for skip prompt
    private void OnGUI()
    {
        if (!showSkipPrompt || creditsEnded) return;
        
        GUIStyle style = new GUIStyle();
        style.fontSize = 16;
        style.normal.textColor = new Color(1f, 1f, 1f, 0.7f);
        style.alignment = TextAnchor.MiddleCenter;
        
        // Draw skip prompt at bottom of screen
        GUI.Label(new Rect(Screen.width / 2 - 150, Screen.height - 50, 300, 30), 
                  "Click anywhere or press any key to skip", style);
    }
    
    private void OnEnable()
    {
        // Ensure proper state when enabled
        InitializeCreditScene();
    }
    
    private void OnDisable()
    {
        // Clean up
        CancelInvoke();
    }
    
    // Public methods for external control
    public void PauseCredits()
    {
        isScrolling = false;
    }
    
    public void ResumeCredits()
    {
        if (!creditsEnded)
        {
            isScrolling = true;
        }
    }
    
    public void SetScrollSpeed(float speed)
    {
        scrollSpeed = speed;
    }
    
    public bool AreCreditsEnded() => creditsEnded;
    public bool IsScrolling() => isScrolling;
}
