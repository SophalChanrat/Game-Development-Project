using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("How much damage each particle deals on collision")]
    public float damagePerHit = 10f;
    
    [Tooltip("Which layers can be damaged by particles")]
    public LayerMask damageableLayers;
    
    [Header("Damage Limits")]
    [Tooltip("Prevent same enemy from being hit multiple times rapidly")]
    public bool preventMultipleHits = true;
    
    [Tooltip("Cooldown between hits on same enemy (seconds)")]
    public float hitCooldown = 0.1f;
    
    [Tooltip("Maximum hits per enemy (0 = unlimited)")]
    public int maxHitsPerEnemy = 0;
    
    [Header("Debug")]
    public bool showDebugMessages = true;
    
    private ParticleSystem particleSys;
    private List<ParticleCollisionEvent> collisionEvents;
    private Dictionary<GameObject, float> lastHitTimes;
    private Dictionary<GameObject, int> hitCounts;

    void Awake()
    {
        particleSys = GetComponent<ParticleSystem>();
        collisionEvents = new List<ParticleCollisionEvent>();
        lastHitTimes = new Dictionary<GameObject, float>();
        hitCounts = new Dictionary<GameObject, int>();
        
        // Ensure collision is enabled on the particle system
        var collision = particleSys.collision;
        collision.enabled = true;
        collision.type = ParticleSystemCollisionType.World;
        collision.mode = ParticleSystemCollisionMode.Collision3D;
        collision.sendCollisionMessages = true;
        
        // Configure collision quality
        if (damageableLayers != 0)
        {
            collision.collidesWith = damageableLayers;
        }
        collision.maxCollisionShapes = 256;
        
        if (showDebugMessages)
        {
            Debug.Log($"<color=magenta>[PARTICLE DAMAGE]</color> Initialized on {gameObject.name} - Damage: {damagePerHit}");
        }
    }

    void OnParticleCollision(GameObject other)
    {
        // Check if we can damage this target
        if (!CanDamageTarget(other))
            return;

        // Get collision events
        int numCollisionEvents = particleSys.GetCollisionEvents(other, collisionEvents);
        
        // Check hit cooldown
        if (preventMultipleHits)
        {
            if (lastHitTimes.ContainsKey(other))
            {
                if (Time.time - lastHitTimes[other] < hitCooldown)
                    return;
            }
        }
        
        // Check max hits per enemy
        if (maxHitsPerEnemy > 0)
        {
            if (hitCounts.ContainsKey(other) && hitCounts[other] >= maxHitsPerEnemy)
                return;
        }

        // Calculate total damage based on number of particle hits
        float totalDamage = damagePerHit * Mathf.Max(1, numCollisionEvents);
        
        bool didDamage = false;

        // Try to damage regular enemy
        EnemyAI enemy = other.GetComponent<EnemyAI>();
        if (enemy != null)
        {
            enemy.TakeDamage((int)totalDamage);
            didDamage = true;
        }
        
        // Try to damage lumberjack
        if (!didDamage)
        {
            LumberjackAI lumberjack = other.GetComponent<LumberjackAI>();
            if (lumberjack != null)
            {
                lumberjack.TakeDamage((int)totalDamage);
                didDamage = true;
            }
        }
        
        if (didDamage)
        {
            // Update last hit time
            if (preventMultipleHits)
            {
                lastHitTimes[other] = Time.time;
            }
            
            // Update hit count
            if (maxHitsPerEnemy > 0)
            {
                if (!hitCounts.ContainsKey(other))
                    hitCounts[other] = 0;
                hitCounts[other]++;
            }
            
            if (showDebugMessages)
            {
                Debug.Log($"<color=red>[PARTICLE HIT]</color> {other.name} took {totalDamage} damage from {numCollisionEvents} particle(s)!");
            }
        }
    }

    bool CanDamageTarget(GameObject target)
    {
        // If no layers specified, allow all
        if (damageableLayers == 0)
            return true;
            
        // Check if target is on a damageable layer
        int targetLayer = target.layer;
        return ((1 << targetLayer) & damageableLayers) != 0;
    }

    void OnDestroy()
    {
        // Clear dictionaries to prevent memory leaks
        if (lastHitTimes != null)
            lastHitTimes.Clear();
        if (hitCounts != null)
            hitCounts.Clear();
    }
    
    /// <summary>
    /// Reset hit tracking (call this if you want enemies to be damageable again)
    /// </summary>
    public void ResetHitTracking()
    {
        lastHitTimes.Clear();
        hitCounts.Clear();
    }
}
