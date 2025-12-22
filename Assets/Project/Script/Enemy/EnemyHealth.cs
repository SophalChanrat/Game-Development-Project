using UnityEngine;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour
{
    public Slider healthBar;
    public float maxHealth = 100f;
    private EnemyAI enemyAI;

    void Start()
    {
        enemyAI = GetComponent<EnemyAI>();

        if (enemyAI == null || healthBar == null)
            return;

        // If health is 0, use the backup maxHealth value
        if (enemyAI.health <= 0)
        {
            enemyAI.health = maxHealth;
        }

        // Initialize health bar
        healthBar.minValue = 0f;
        healthBar.maxValue = enemyAI.health;
        healthBar.value = enemyAI.health;
        healthBar.gameObject.SetActive(true);
    }

    void Update()
    {
        // Update health bar every frame
        if (healthBar != null && enemyAI != null)
        {
            healthBar.value = enemyAI.health;
        }
    }
}
