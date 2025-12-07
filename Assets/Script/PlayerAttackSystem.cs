using System.Collections;
using UnityEngine;

public class PlayerAttackSystem : MonoBehaviour
{
    [Header("Attack Settings")]
    public GameObject slashEffectPrefab; // Drag your slash effect prefab here
    public Transform attackSpawnPoint; // Where the slash spawns (in front of player)
    public float slashSpeed = 20f; // Increased from 15 for faster travel
    public float slashLifetime = 2f; // Increased from 1 to give more range
    public float slashDistance = 10f;
    
    [Header("Attack Properties")]
    public float attackCooldown = 0.5f;
    public int attackDamage = 10;
    private float lastAttackTime = -999f;
    
    [Header("References")]
    private Animator animator;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    
    void Awake()
    {
        animator = GetComponent<Animator>();
        
        // Create attack spawn point if it doesn't exist
        if (attackSpawnPoint == null)
        {
            GameObject spawnPoint = new GameObject("AttackSpawnPoint");
            spawnPoint.transform.SetParent(transform);
            spawnPoint.transform.localPosition = new Vector3(0, 1f, 1.5f); // Slightly further in front
            attackSpawnPoint = spawnPoint.transform;
        }
    }
    
    public void PerformAttack()
    {
        if (Time.time < lastAttackTime + attackCooldown) return;
        
        lastAttackTime = Time.time;
        
        if (debugMode)
            Debug.Log($"[PlayerAttackSystem] Performing attack - Damage: {attackDamage}, Speed: {slashSpeed}");
        
        // Play animation
        if (animator != null)
        {
            animator.SetTrigger("attack");
        }
        
        // Spawn slash effect
        SpawnSlashEffect();
    }
    
    private void SpawnSlashEffect()
    {
        if (slashEffectPrefab == null)
        {
            Debug.LogWarning("[PlayerAttackSystem] Slash Effect Prefab is not assigned!");
            return;
        }
        
        // Spawn at attack point, facing player's forward direction
        Vector3 spawnPos = attackSpawnPoint.position;
        Quaternion spawnRot = attackSpawnPoint.rotation;
        
        if (debugMode)
            Debug.Log($"[PlayerAttackSystem] Spawning slash at {spawnPos}, facing {transform.forward}");
        
        GameObject slash = Instantiate(slashEffectPrefab, spawnPos, spawnRot);
        
        // Add projectile behavior to slash
        SlashProjectile projectile = slash.GetComponent<SlashProjectile>();
        if (projectile == null)
        {
            projectile = slash.AddComponent<SlashProjectile>();
        }
        
        projectile.Initialize(transform.forward, slashSpeed, slashLifetime, attackDamage);
    }
    
    public bool CanAttack()
    {
        return Time.time >= lastAttackTime + attackCooldown;
    }
    
    public float GetAttackCooldownProgress()
    {
        float timeSinceAttack = Time.time - lastAttackTime;
        return Mathf.Clamp01(timeSinceAttack / attackCooldown);
    }
}
