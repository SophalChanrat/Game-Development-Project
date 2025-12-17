using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;
    private Vector3 respawnPoint = new Vector3(63.47f, 3.093f, 63.09f);

    // Health bar UI - Image based
    public Image healthBarFill;
    public Image healthBarFrame;
    
    // Optional: Smooth health bar animation
    public bool smoothHealthBar = true;
    public float healthBarLerpSpeed = 5f;
    private float targetFillAmount;

    // Damage feedback
    public float invincibilityDuration = 0.5f;
    private bool isInvincible = false;
    
    // Hit reaction settings
    [Header("Hit Reaction")]
    public bool playHitAnimation = true;
    public float hitStunDuration = 0.3f;
    private bool isInHitStun = false;
    
    // Death state
    private bool isDead = false;

    public Animator animator;
    private PlayerMovement3D playerMovement;

    void Start()
    {
        animator = GetComponent<Animator>();
        playerMovement = GetComponent<PlayerMovement3D>();
        
        currentHealth = maxHealth;
        targetFillAmount = 1f;

        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = 1f;
            healthBarFill.type = Image.Type.Filled;
            healthBarFill.fillMethod = Image.FillMethod.Horizontal;
            healthBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        }
        else
        {
            Debug.LogWarning("Health Bar Fill Image is not assigned on Player!");
        }

        UpdateHealthUI();
    }

    void Update()
    {
        // Smoothly lerp the health bar fill amount
        if (smoothHealthBar && healthBarFill != null)
        {
            healthBarFill.fillAmount = Mathf.Lerp(
                healthBarFill.fillAmount, 
                targetFillAmount, 
                Time.deltaTime * healthBarLerpSpeed
            );
        }
    }

    public void TakeDamage(float damage)
    {
        // Ignore damage if invincible or dead
        if (isInvincible || isDead)
            return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log("Player took " + damage + " damage! Health: " + currentHealth + "/" + maxHealth);

        UpdateHealthUI();

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Play hit reaction
            if (playHitAnimation && !isInHitStun)
            {
                StartCoroutine(PlayHitReaction());
            }
            
            // Grant brief invincibility after taking damage
            StartCoroutine(InvincibilityCoroutine());
        }
    }

    private IEnumerator PlayHitReaction()
    {
        isInHitStun = true;
        
        // Temporarily disable player movement during hit stun
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }
        
        // Play hit animation
        if (animator != null)
        {
            // Reset movement animations
            animator.SetBool("isMoving", false);
            animator.SetBool("walkBack", false);
            animator.SetBool("walkLeft", false);
            animator.SetBool("walkRight", false);
            
            // Trigger hit animation
            animator.SetTrigger("TakeHit");
        }
        
        // Wait for hit stun duration
        yield return new WaitForSeconds(hitStunDuration);
        
        // Re-enable movement
        if (playerMovement != null && !isDead)
        {
            playerMovement.enabled = true;
        }
        
        isInHitStun = false;
    }

    private IEnumerator InvincibilityCoroutine()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibilityDuration);
        isInvincible = false;
    }

    private void UpdateHealthUI()
    {
        if (healthBarFill != null)
        {
            float fillAmount = currentHealth / maxHealth;
            
            if (smoothHealthBar)
            {
                targetFillAmount = fillAmount;
            }
            else
            {
                healthBarFill.fillAmount = fillAmount;
            }
        }
    }

    private void Die()
    {
        if (isDead) return; // Prevent multiple death calls
        
        isDead = true;
        Debug.Log("Player has died! Game Over!");
        
        // Stop any ongoing hit reactions
        StopAllCoroutines();
        
        // Disable player controls immediately
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }
        
        // Disable character controller
        CharacterController controller = GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = false;
        }

        // IMPORTANT: Reset all animator parameters to prevent animation conflicts
        if (animator != null)
        {
            animator.ResetTrigger("attack");
            animator.ResetTrigger("isJump");
            animator.ResetTrigger("TakeHit");
            animator.SetBool("isMoving", false);
            animator.SetBool("walkBack", false);
            animator.SetBool("walkLeft", false);
            animator.SetBool("walkRight", false);
            
            // Now trigger death animation
            animator.SetTrigger("Die");
        }

        // Optional: Respawn after delay
        StartCoroutine(RespawnCoroutine());
    }

    private IEnumerator RespawnCoroutine()
    {
        // Wait for death animation to finish (adjust time based on your animation length)
        yield return new WaitForSeconds(2.5f);

        // Reset death state
        isDead = false;
        isInHitStun = false;
        
        // Reset animator
        if (animator != null)
        {
            animator.ResetTrigger("Die");
            animator.ResetTrigger("TakeHit");
            animator.SetTrigger("Respawn");
        }
        
        // Reset health
        currentHealth = maxHealth;
        UpdateHealthUI();

        // Reset position
        CharacterController controller = GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = false;
            transform.position = respawnPoint;
            transform.rotation = Quaternion.identity;
            controller.enabled = true;
        }
        
        // Re-enable player controls
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }

        Debug.Log("Player respawned!");
    }

    // Public helper methods
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }
    
    public bool IsInvincible()
    {
        return isInvincible;
    }
    
    public bool IsDead()
    {
        return isDead;
    }
    
    public bool IsInHitStun()
    {
        return isInHitStun;
    }
}