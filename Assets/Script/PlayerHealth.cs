using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;

    // Health bar UI - Image based
    public Image healthBarFill;      // The fill image (health)
    public Image healthBarFrame;     // Optional frame/border image
    
    
    // Optional: Smooth health bar animation
    public bool smoothHealthBar = true;
    public float healthBarLerpSpeed = 5f;
    private float targetFillAmount;

    // Damage feedback
    public float invincibilityDuration = 0.5f;
    private bool isInvincible = false;

    void Start()
    {
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

        if (healthBarFrame == null)
        {
            Debug.LogWarning("Health Bar Frame Image is not assigned on Player (optional)!");
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
        // Ignore damage if invincible
        if (isInvincible)
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
            // Grant brief invincibility after taking damage
            StartCoroutine(InvincibilityCoroutine());
        }
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
        Debug.Log("Player has died! Game Over!");
        
        // Disable player movement
        CharacterController movement = GetComponent<CharacterController>();
        if (movement != null)
        {
            movement.enabled = false;
        }

        // Optional: Respawn after delay
        StartCoroutine(RespawnCoroutine());
    }

    private IEnumerator RespawnCoroutine()
    {
        yield return new WaitForSeconds(1f);

        // Reset health
        currentHealth = maxHealth;
        UpdateHealthUI();

        // Reset position
        CharacterController controller = GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = false;
            transform.position = new Vector3(0f, 0f, 0f);
            transform.rotation = Quaternion.identity;
            controller.enabled = true;
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
}