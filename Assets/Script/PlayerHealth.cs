using UnityEngine;

/// <summary>
/// Player-specific health system with additional features
/// </summary>
public class PlayerHealth : HealthSystem
{
    [Header("Player Specific")]
    [SerializeField] private bool enableHealthRegeneration = false;
    [SerializeField] private float healthRegenRate = 5f; // HP per second
    [SerializeField] private float regenDelay = 3f; // Delay after taking damage
    
    private float timeSinceLastDamage = 0f;
    private PlayerMovement3D playerMovement;

    protected override void Awake()
    {
        base.Awake();
        playerMovement = GetComponent<PlayerMovement3D>();
    }

    protected override void Update()
    {
        base.Update();

        // Handle health regeneration
        if (enableHealthRegeneration && !isDead)
        {
            timeSinceLastDamage += Time.deltaTime;
            
            if (timeSinceLastDamage >= regenDelay && currentHealth < maxHealth)
            {
                Heal(healthRegenRate * Time.deltaTime);
            }
        }
    }

    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage);
        timeSinceLastDamage = 0f;
        
        // Optional: Add camera shake or screen effect here
        Debug.Log($"Player took {damage} damage! Health: {currentHealth}/{maxHealth}");
    }

    protected override void Die()
    {
        base.Die();
        
        Debug.Log("Player has died!");
        
        // Disable player movement
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }
        
        // You can add respawn logic, game over screen, etc. here
        // Example: Invoke("Respawn", 3f);
    }

    private void Respawn()
    {
        isDead = false;
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }
        
        Debug.Log("Player respawned!");
    }

    // Public method to trigger respawn from external scripts
    public void TriggerRespawn()
    {
        Respawn();
    }
}
