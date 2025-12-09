using UnityEngine;
using UnityEngine.Events;

public class HealthSystem : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] protected float maxHealth = 100f;
    [SerializeField] protected float currentHealth;
    
    [Header("Events")]
    public UnityEvent<float, float> OnHealthChanged; // current, max
    public UnityEvent OnDeath;
    public UnityEvent<float> OnDamaged; // damage amount
    public UnityEvent<float> OnHealed; // heal amount
    
    [Header("Invincibility")]
    [SerializeField] protected bool canTakeDamage = true;
    [SerializeField] protected float invincibilityDuration = 0.5f;
    protected float invincibilityTimer = 0f;
    
    protected bool isDead = false;

    protected virtual void Awake()
    {
        currentHealth = maxHealth;
    }

    protected virtual void Update()
    {
        // Handle invincibility timer
        if (invincibilityTimer > 0)
        {
            invincibilityTimer -= Time.deltaTime;
            if (invincibilityTimer <= 0)
            {
                canTakeDamage = true;
            }
        }
    }

    public virtual void TakeDamage(float damage)
    {
        if (isDead || !canTakeDamage) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        OnDamaged?.Invoke(damage);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // Start invincibility frames
        canTakeDamage = false;
        invincibilityTimer = invincibilityDuration;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public virtual void Heal(float amount)
    {
        if (isDead) return;

        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        
        OnHealed?.Invoke(amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    protected virtual void Die()
    {
        if (isDead) return;
        
        isDead = true;
        OnDeath?.Invoke();
    }

    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public float GetHealthPercentage() => maxHealth > 0 ? currentHealth / maxHealth : 0f;
    public bool IsDead() => isDead;
    public bool IsInvincible() => !canTakeDamage;
}
