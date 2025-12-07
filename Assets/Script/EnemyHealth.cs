using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Enemy-specific health system with health bar UI support
/// Inherits from HealthSystem and integrates with EnemyAI
/// </summary>
public class EnemyHealth : HealthSystem
{
    [Header("Health Bar UI (Optional)")]
    [SerializeField] private Slider healthBarSlider;
    [SerializeField] private bool hideHealthBarWhenFull = true;
    [SerializeField] private bool hideHealthBarWhenDead = true;
    [SerializeField] private Canvas healthBarCanvas;

    [Header("Death Settings")]
    [SerializeField] private float destroyDelay = 3f;
    [SerializeField] private bool dropLoot = false;
    [SerializeField] private GameObject[] lootPrefabs;

    private EnemyAI enemyAI;
    private Animator animator;

    protected override void Awake()
    {
        base.Awake();
        
        enemyAI = GetComponent<EnemyAI>();
        animator = GetComponent<Animator>();

        // Initialize health bar if present
        if (healthBarSlider != null)
        {
            healthBarSlider.minValue = 0f;
            healthBarSlider.maxValue = maxHealth;
            healthBarSlider.value = currentHealth;

            // Hide health bar initially if at full health
            if (hideHealthBarWhenFull && currentHealth >= maxHealth)
            {
                SetHealthBarVisible(false);
            }
            else
            {
                SetHealthBarVisible(true);
            }
        }

        // Subscribe to health events
        OnHealthChanged.AddListener(UpdateHealthBar);
    }

    public override void TakeDamage(float damage)
    {
        if (isDead) return;

        base.TakeDamage(damage);

        // Show health bar when damaged
        if (healthBarSlider != null && hideHealthBarWhenFull)
        {
            SetHealthBarVisible(true);
        }

        // Play hurt animation if available
        if (animator != null && !isDead)
        {
            animator.SetTrigger("Hurt");
        }

        Debug.Log($"{gameObject.name} took {damage} damage! Health: {currentHealth}/{maxHealth}");
    }

    protected override void Die()
    {
        if (isDead) return;

        base.Die();

        Debug.Log($"{gameObject.name} has died!");

        // Hide or show death health bar based on settings
        if (hideHealthBarWhenDead && healthBarSlider != null)
        {
            SetHealthBarVisible(false);
        }

        // Trigger death animation
        if (animator != null)
        {
            animator.SetTrigger("Death");
        }

        // Drop loot if enabled
        if (dropLoot && lootPrefabs.Length > 0)
        {
            DropLoot();
        }

        // Destroy after delay
        Destroy(gameObject, destroyDelay);
    }

    private void UpdateHealthBar(float current, float max)
    {
        if (healthBarSlider == null) return;

        healthBarSlider.value = current;

        // Auto-hide when full if enabled
        if (hideHealthBarWhenFull && current >= max)
        {
            SetHealthBarVisible(false);
        }
    }

    private void SetHealthBarVisible(bool visible)
    {
        if (healthBarSlider != null)
        {
            healthBarSlider.gameObject.SetActive(visible);
        }

        if (healthBarCanvas != null)
        {
            healthBarCanvas.gameObject.SetActive(visible);
        }
    }

    private void DropLoot()
    {
        if (lootPrefabs.Length == 0) return;

        // Pick random loot item
        GameObject lootPrefab = lootPrefabs[Random.Range(0, lootPrefabs.Length)];
        
        if (lootPrefab != null)
        {
            Vector3 dropPosition = transform.position + Vector3.up * 0.5f;
            Instantiate(lootPrefab, dropPosition, Quaternion.identity);
        }
    }

    // Optional: Make health bar face camera
    private void LateUpdate()
    {
        if (healthBarCanvas != null && Camera.main != null)
        {
            healthBarCanvas.transform.LookAt(Camera.main.transform);
            healthBarCanvas.transform.Rotate(0, 180, 0); // Flip to face camera correctly
        }
    }

    // Public methods for external access
    public void SetMaxHealth(float newMaxHealth)
    {
        maxHealth = newMaxHealth;
        currentHealth = maxHealth;
        
        if (healthBarSlider != null)
        {
            healthBarSlider.maxValue = maxHealth;
            healthBarSlider.value = currentHealth;
        }
        
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
