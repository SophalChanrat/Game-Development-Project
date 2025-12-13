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
    
    private ParticleSystem particleSys;
    private List<ParticleCollisionEvent> collisionEvents;
    private Dictionary<GameObject, float> lastHitTimes;

    void Awake()
    {
        particleSys = GetComponent<ParticleSystem>();
        collisionEvents = new List<ParticleCollisionEvent>();
        lastHitTimes = new Dictionary<GameObject, float>();
        
        // Ensure collision is enabled on the particle system
        var collision = particleSys.collision;
        collision.enabled = true;
        collision.type = ParticleSystemCollisionType.World;
        collision.mode = ParticleSystemCollisionMode.Collision3D;
        collision.sendCollisionMessages = true;
        
        // Optional: Configure collision quality
        collision.collidesWith = damageableLayers;
        collision.maxCollisionShapes = 256;
    }

    void OnParticleCollision(GameObject other)
    {
        // Check if we can damage this target
        if (!CanDamageTarget(other))
            return;

        // Get collision events
        int numCollisionEvents = particleSys.GetCollisionEvents(other, collisionEvents);
        
        // Check if enough time has passed since last hit (prevents spam damage)
        if (preventMultipleHits)
        {
            if (lastHitTimes.ContainsKey(other))
            {
                if (Time.time - lastHitTimes[other] < hitCooldown)
                    return;
            }
        }

        // Try to damage the target
        EnemyAI enemy = other.GetComponent<EnemyAI>();
        if (enemy != null)
        {
            // Deal damage based on number of particles that hit
            float totalDamage = damagePerHit * numCollisionEvents;
            enemy.TakeDamage((int)totalDamage);
            
            // Update last hit time
            if (preventMultipleHits)
            {
                lastHitTimes[other] = Time.time;
            }
        }
    }

    bool CanDamageTarget(GameObject target)
    {
        // Check if target is on a damageable layer
        int targetLayer = target.layer;
        return ((1 << targetLayer) & damageableLayers) != 0;
    }

    void OnDestroy()
    {
        // Clear the dictionary to prevent memory leaks
        if (lastHitTimes != null)
        {
            lastHitTimes.Clear();
        }
    }
}
