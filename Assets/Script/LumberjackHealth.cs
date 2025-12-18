using UnityEngine;
using UnityEngine.UI;

public class LumberjackHealth : MonoBehaviour
{
    public Slider healthBar;
    public float maxHealth = 100f;
    private LumberjackAI lumberjackAI;

    void Start()
    {
        lumberjackAI = GetComponent<LumberjackAI>();

        if (lumberjackAI == null)
        {
            Debug.LogError("[LUMBERJACK HEALTH] " + gameObject.name + " is missing LumberjackAI component!");
            return;
        }
        
        if (healthBar == null)
        {
            Debug.LogError("[LUMBERJACK HEALTH] " + gameObject.name + " health bar UI is not assigned in Inspector!");
            return;
        }

        // If health is 0, use the backup maxHealth value
        if (lumberjackAI.health <= 0)
        {
            lumberjackAI.health = maxHealth;
        }

        // Initialize health bar
        healthBar.minValue = 0f;
        healthBar.maxValue = lumberjackAI.health;
        healthBar.value = lumberjackAI.health;
        healthBar.gameObject.SetActive(true);
        
        Debug.Log("[LUMBERJACK HEALTH] " + gameObject.name + " initialized - Health: " + healthBar.value + "/" + healthBar.maxValue);
    }

    void Update()
    {
        // Update health bar every frame
        if (healthBar != null && lumberjackAI != null)
        {
            healthBar.value = lumberjackAI.health;
        }
    }
}
