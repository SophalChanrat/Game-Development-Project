using UnityEngine;

/// <summary>
/// Component that deals damage when hitting objects with HealthSystem
/// Attach this to projectiles, enemy attacks, or trigger zones
/// </summary>
public class DamageDealer : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private bool destroyOnHit = true;
    [SerializeField] private string targetTag = ""; // Leave empty to damage anything with health
    
    [Header("Team System")]
    [SerializeField] private bool useTeamSystem = true;
    public enum Team { Player, Enemy, Neutral }
    [SerializeField] private Team team = Team.Player;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    private void OnTriggerEnter(Collider other)
    {
        if (debugMode)
            Debug.Log($"[DamageDealer] Trigger enter with: {other.gameObject.name}, Tag: {other.tag}, Layer: {LayerMask.LayerToName(other.gameObject.layer)}");
        
        HandleCollision(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (debugMode)
            Debug.Log($"[DamageDealer] Collision enter with: {collision.gameObject.name}, Tag: {collision.gameObject.tag}");
        
        HandleCollision(collision.gameObject);
    }

    private void HandleCollision(GameObject hitObject)
    {
        if (debugMode)
            Debug.Log($"[DamageDealer] HandleCollision called for: {hitObject.name}");
        
        // Check tag if specified
        if (!string.IsNullOrEmpty(targetTag) && !hitObject.CompareTag(targetTag))
        {
            if (debugMode)
                Debug.Log($"[DamageDealer] Tag mismatch. Expected: '{targetTag}', Got: '{hitObject.tag}' - SKIPPING");
            return;
        }

        // Team-based damage prevention
        if (useTeamSystem)
        {
            if (debugMode)
                Debug.Log($"[DamageDealer] Team System enabled. My team: {team}");
            
            DamageDealer otherDealer = hitObject.GetComponent<DamageDealer>();
            if (otherDealer != null && otherDealer.team == this.team)
            {
                if (debugMode)
                    Debug.Log($"[DamageDealer] Same team detected ({team}), no damage - SKIPPING");
                return; // Don't damage same team
            }
            
            // Check if we should damage based on team
            if (team == Team.Player)
            {
                if (debugMode)
                    Debug.Log($"[DamageDealer] Player team - looking for EnemyHealth on {hitObject.name}");
                
                // Player projectile should only damage enemies
                EnemyHealth enemyHealth = hitObject.GetComponent<EnemyHealth>();
                if (enemyHealth != null)
                {
                    if (debugMode)
                        Debug.Log($"[DamageDealer] EnemyHealth found! Dealing {damage} damage to {hitObject.name}");
                    
                    enemyHealth.TakeDamage(damage);
                    
                    if (debugMode)
                        Debug.Log($"[DamageDealer] Damage dealt successfully. Enemy health now: {enemyHealth.GetCurrentHealth()}/{enemyHealth.GetMaxHealth()}");
                    
                    if (destroyOnHit)
                    {
                        if (debugMode)
                            Debug.Log($"[DamageDealer] Destroying projectile on hit");
                        Destroy(gameObject);
                    }
                    return;
                }
                else
                {
                    if (debugMode)
                        Debug.LogWarning($"[DamageDealer] No EnemyHealth component found on {hitObject.name}!");
                }
            }
            else if (team == Team.Enemy)
            {
                if (debugMode)
                    Debug.Log($"[DamageDealer] Enemy team - looking for PlayerHealth on {hitObject.name}");
                
                // Enemy projectile should only damage player
                PlayerHealth playerHealth = hitObject.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damage);
                    if (debugMode)
                        Debug.Log($"[DamageDealer] Enemy dealt {damage} damage to {hitObject.name}");
                    
                    if (destroyOnHit)
                    {
                        Destroy(gameObject);
                    }
                    return;
                }
                else
                {
                    if (debugMode)
                        Debug.LogWarning($"[DamageDealer] No PlayerHealth component found on {hitObject.name}!");
                }
            }
        }

        // Fallback: Try generic HealthSystem
        if (debugMode)
            Debug.Log($"[DamageDealer] Trying fallback HealthSystem on {hitObject.name}");
        
        HealthSystem health = hitObject.GetComponent<HealthSystem>();
        if (health != null)
        {
            health.TakeDamage(damage);
            if (debugMode)
                Debug.Log($"[DamageDealer] Dealt {damage} damage to {hitObject.name} via HealthSystem");
            
            if (destroyOnHit)
            {
                Destroy(gameObject);
            }
        }
        else
        {
            if (debugMode)
                Debug.LogWarning($"[DamageDealer] No health component found on {hitObject.name} - NO DAMAGE DEALT");
        }
    }

    public void SetDamage(float newDamage)
    {
        damage = newDamage;
    }

    public void SetTargetTag(string tag)
    {
        targetTag = tag;
    }

    public void SetTeam(Team newTeam)
    {
        team = newTeam;
    }
}
