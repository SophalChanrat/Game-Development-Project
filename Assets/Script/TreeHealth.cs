using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Chop-based system for destructible trees
/// Tree falls after a set number of chops (no health bar)
/// </summary>
public class TreeHealth : MonoBehaviour
{
    [Header("Chop System")]
    [SerializeField] private int chopsRequired = 5;
    [SerializeField] private int currentChops = 0;
    
    [Header("Visual Feedback")]
    [Tooltip("Particle effect when tree takes a chop (e.g., wood chips)")]
    public GameObject chopParticles;
    
    [Tooltip("Shake intensity when chopped")]
    public float shakeIntensity = 0.1f;
    
    [Tooltip("Shake duration")]
    public float shakeDuration = 0.2f;
    
    [Tooltip("Prefab to spawn when tree is destroyed (logs)")]
    public GameObject destroyedTreePrefab;
    
    [Header("Collapse Settings")]
    [Tooltip("How the tree collapses when destroyed")]
    public CollapseMode collapseMode = CollapseMode.FallOver;
    
    [Tooltip("Direction for tree to fall (leave 0,0,0 for random)")]
    public Vector3 fallDirection = Vector3.zero;
    
    [Tooltip("How long until tree is destroyed after collapse")]
    public float destroyDelay = 5f;
    
    [Header("Audio")]
    [Tooltip("Sound when tree takes damage")]
    public AudioClip chopSound;
    
    [Tooltip("Sound when tree falls (final chop)")]
    public AudioClip fallSound;
    
    private AudioSource audioSource;
    private bool isDestroyed = false;
    private Rigidbody rb;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool isShaking = false;
    
    [Header("Events")]
    public UnityEvent<int, int> OnChopped; // current chops, total chops needed
    public UnityEvent OnDestroyed;
    
    public enum CollapseMode
    {
        Instant,        // Tree disappears immediately
        FallOver,       // Tree falls over using physics (most realistic)
        Sink,           // Tree sinks into ground
        Explode         // Tree breaks apart
    }

    void Awake()
    {
        currentChops = 0;
        audioSource = GetComponent<AudioSource>();
        
        // Add AudioSource if not present
        if (audioSource == null && (chopSound != null || fallSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        
        // Get or add Rigidbody for physics collapse
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; // Start as kinematic
        }
        
        // Store original transform
        originalPosition = transform.position;
        originalRotation = transform.rotation;
    }

    /// <summary>
    /// Apply a chop to the tree (replaces TakeDamage)
    /// </summary>
    public void TakeDamage(float damage)
    {
        // Convert damage to chop count (ignore damage value, just count chops)
        Chop();
    }
    
    /// <summary>
    /// Apply a chop to the tree
    /// </summary>
    public void Chop()
    {
        if (isDestroyed) return;

        currentChops++;
        
        Debug.Log($"{gameObject.name} chopped! {currentChops}/{chopsRequired}");

        // Spawn chop particles
        if (chopParticles != null)
        {
            Vector3 particlePos = transform.position + Vector3.up * 1f;
            Instantiate(chopParticles, particlePos, Quaternion.identity);
        }
        
        // Play chop sound
        if (audioSource != null && chopSound != null)
        {
            audioSource.PlayOneShot(chopSound);
        }
        
        // Shake tree
        if (!isShaking)
        {
            StartCoroutine(ShakeTree());
        }
        
        OnChopped?.Invoke(currentChops, chopsRequired);

        // Check if tree should fall
        if (currentChops >= chopsRequired)
        {
            DestroyTree();
        }
    }

    /// <summary>
    /// Shake tree when chopped
    /// </summary>
    System.Collections.IEnumerator ShakeTree()
    {
        isShaking = true;
        float elapsed = 0f;
        
        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeIntensity;
            float z = Random.Range(-1f, 1f) * shakeIntensity;
            
            transform.position = originalPosition + new Vector3(x, 0, z);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Reset position
        transform.position = originalPosition;
        isShaking = false;
    }

    /// <summary>
    /// Handle tree destruction
    /// </summary>
    void DestroyTree()
    {
        if (isDestroyed) return;
        
        isDestroyed = true;
        Debug.Log($"{gameObject.name} has been chopped down!");
        
        // Play fall sound
        if (audioSource != null && fallSound != null)
        {
            audioSource.PlayOneShot(fallSound);
        }
        
        OnDestroyed?.Invoke();
        
        // Execute collapse mode
        switch (collapseMode)
        {
            case CollapseMode.Instant:
                CollapseInstant();
                break;
            case CollapseMode.FallOver:
                CollapseFallOver();
                break;
            case CollapseMode.Sink:
                CollapseSink();
                break;
            case CollapseMode.Explode:
                CollapseExplode();
                break;
        }
    }

    /// <summary>
    /// Instant collapse - tree disappears and spawns loot
    /// </summary>
    void CollapseInstant()
    {
        // Spawn destroyed tree prefab (logs, etc)
        if (destroyedTreePrefab != null)
        {
            Instantiate(destroyedTreePrefab, transform.position, transform.rotation);
        }
        
        Destroy(gameObject);
    }

    /// <summary>
    /// Physics-based fall over
    /// </summary>
    void CollapseFallOver()
    {
        // Disable collider to prevent blocking
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }
        
        // Enable physics
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            
            // Apply fall force
            Vector3 fallDir = fallDirection;
            if (fallDir == Vector3.zero)
            {
                // Random fall direction
                fallDir = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
            }
            
            // Apply torque to make it fall over
            Vector3 torqueDir = Vector3.Cross(Vector3.up, fallDir);
            rb.AddTorque(torqueDir * 50f, ForceMode.Impulse);
        }
        else
        {
            // No rigidbody, add one
            rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = true;
            
            Vector3 fallDir = fallDirection;
            if (fallDir == Vector3.zero)
            {
                fallDir = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
            }
            
            Vector3 torqueDir = Vector3.Cross(Vector3.up, fallDir);
            rb.AddTorque(torqueDir * 50f, ForceMode.Impulse);
        }
        
        // Spawn loot after delay
        if (destroyedTreePrefab != null)
        {
            Invoke(nameof(SpawnLoot), destroyDelay * 0.5f);
        }
        
        // Destroy after delay
        Destroy(gameObject, destroyDelay);
    }

    /// <summary>
    /// Sink into ground
    /// </summary>
    void CollapseSink()
    {
        StartCoroutine(SinkCoroutine());
    }

    System.Collections.IEnumerator SinkCoroutine()
    {
        float elapsed = 0f;
        float duration = 2f;
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos - Vector3.up * 5f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
            yield return null;
        }
        
        // Spawn loot
        if (destroyedTreePrefab != null)
        {
            Instantiate(destroyedTreePrefab, startPos, transform.rotation);
        }
        
        Destroy(gameObject);
    }

    /// <summary>
    /// Explode into pieces
    /// </summary>
    void CollapseExplode()
    {
        // Spawn loot
        if (destroyedTreePrefab != null)
        {
            GameObject loot = Instantiate(destroyedTreePrefab, transform.position, transform.rotation);
            
            // If loot has rigidbodies, add explosion force
            Rigidbody[] pieces = loot.GetComponentsInChildren<Rigidbody>();
            foreach (Rigidbody piece in pieces)
            {
                piece.AddExplosionForce(500f, transform.position, 5f);
            }
        }
        
        Destroy(gameObject);
    }

    void SpawnLoot()
    {
        if (destroyedTreePrefab != null)
        {
            Instantiate(destroyedTreePrefab, transform.position, transform.rotation);
        }
    }

    // Public getters
    public int GetCurrentChops() => currentChops;
    public int GetChopsRequired() => chopsRequired;
    public float GetChopPercentage() => chopsRequired > 0 ? (float)currentChops / chopsRequired : 0f;
    public bool IsDestroyed() => isDestroyed;

    // Gizmo for debugging
    void OnDrawGizmosSelected()
    {
        if (fallDirection != Vector3.zero)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position + Vector3.up, fallDirection.normalized * 3f);
        }
        
        // Show chop count
        if (Application.isPlaying)
        {
            UnityEditor.Handles.Label(transform.position + Vector3.up * 3f, $"Chops: {currentChops}/{chopsRequired}");
        }
    }
}
