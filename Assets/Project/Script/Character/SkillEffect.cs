using UnityEngine;

public class SkillEffect : MonoBehaviour
{
    [Header("INITIAL CAST DAMAGE")]
    [Tooltip("Deal AOE damage immediately when skill spawns")]
    public bool dealDamageOnCast = true;
    
    [Tooltip("Damage dealt on initial cast")]
    public float initialDamage = 30f;
    
    [Tooltip("Radius for initial cast damage")]
    public float initialDamageRadius = 3f;
    
    [Tooltip("Which layers to damage on initial cast")]
    public LayerMask initialDamageLayers;
    
    [Header("PARTICLE DROP DAMAGE")]
    [Tooltip("Enable particle collision damage (when particles hit enemies)")]
    public bool useParticleDamage = true;
    
    [Tooltip("Damage per particle hit (set in ParticleDamage component)")]
    public float particleDamage = 10f;
    
    [Header("CONTINUOUS DAMAGE (Optional)")]
    [Tooltip("Deal damage continuously over time in radius")]
    public bool dealDamageContinuously = false;
    
    [Tooltip("Damage dealt per tick")]
    public float continuousDamage = 15f;
    
    [Tooltip("Radius for continuous damage")]
    public float continuousRadius = 2f;
    
    [Tooltip("How often to deal continuous damage (seconds)")]
    public float damageInterval = 0.5f;
    
    private float damageTimer = 0f;
    
    [Header("VISUAL EFFECTS")]
    [Tooltip("Make the skill rotate over time")]
    public bool rotateOverTime = false;
    
    [Tooltip("Rotation speed (degrees per second)")]
    public Vector3 rotationSpeed = new Vector3(0, 100, 0);
    
    [Header("DEBUG")]
    public bool showDebugMessages = true;

    void Start()
    {
        // === INITIAL CAST DAMAGE ===
        if (dealDamageOnCast)
        {
            DealInitialDamage();
        }
        
        // === SETUP PARTICLE DAMAGE ===
        if (useParticleDamage)
        {
            SetupParticleDamage();
        }
    }

    void Update()
    {
        // Rotation effect
        if (rotateOverTime)
        {
            transform.Rotate(rotationSpeed * Time.deltaTime);
        }

        // Continuous damage
        if (dealDamageContinuously)
        {
            damageTimer += Time.deltaTime;
            if (damageTimer >= damageInterval)
            {
                DealContinuousDamage();
                damageTimer = 0f;
            }
        }
    }

    /// <summary>
    /// Deal AOE damage immediately when skill spawns
    /// </summary>
    void DealInitialDamage()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, initialDamageRadius, initialDamageLayers);
        
        int enemiesHit = 0;
        
        foreach (Collider hit in hits)
        {
            // Try to damage regular enemy
            EnemyAI enemy = hit.GetComponent<EnemyAI>();
            if (enemy != null)
            {
                enemy.TakeDamage((int)initialDamage);
                enemiesHit++;
                continue;
            }
            
            // Try to damage lumberjack
            LumberjackAI lumberjack = hit.GetComponent<LumberjackAI>();
            if (lumberjack != null)
            {
                lumberjack.TakeDamage((int)initialDamage);
                enemiesHit++;
                continue;
            }
        }
        
        if (showDebugMessages)
        {
            Debug.Log($"<color=orange>[SKILL CAST]</color> Initial damage: {initialDamage} to {enemiesHit} enemies in {initialDamageRadius}m radius");
        }
    }
    
    /// <summary>
    /// Deal continuous AOE damage
    /// </summary>
    void DealContinuousDamage()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, continuousRadius, initialDamageLayers);
        
        foreach (Collider hit in hits)
        {
            // Try to damage regular enemy
            EnemyAI enemy = hit.GetComponent<EnemyAI>();
            if (enemy != null)
            {
                enemy.TakeDamage((int)continuousDamage);
                continue;
            }
            
            // Try to damage lumberjack
            LumberjackAI lumberjack = hit.GetComponent<LumberjackAI>();
            if (lumberjack != null)
            {
                lumberjack.TakeDamage((int)continuousDamage);
                continue;
            }
        }
    }
    
    /// <summary>
    /// Setup ParticleDamage component on child particle systems
    /// </summary>
    void SetupParticleDamage()
    {
        ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>();
        
        foreach (ParticleSystem ps in particleSystems)
        {
            // Add ParticleDamage if not present
            ParticleDamage pd = ps.GetComponent<ParticleDamage>();
            if (pd == null)
            {
                pd = ps.gameObject.AddComponent<ParticleDamage>();
                pd.damagePerHit = particleDamage;
                pd.damageableLayers = initialDamageLayers;
                pd.preventMultipleHits = true;
                pd.hitCooldown = 0.1f;
                
                if (showDebugMessages)
                {
                    Debug.Log($"<color=cyan>[SKILL]</color> Added ParticleDamage to {ps.name} (damage: {particleDamage})");
                }
            }
            else
            {
                // Update existing ParticleDamage settings
                pd.damagePerHit = particleDamage;
                pd.damageableLayers = initialDamageLayers;
            }
            
            // Ensure collision is enabled
            var collision = ps.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision3D;
            collision.sendCollisionMessages = true;
            collision.collidesWith = initialDamageLayers;
        }
    }

    /// <summary>
    /// Visualize damage radius in Scene view
    /// </summary>
    void OnDrawGizmosSelected()
    {
        // Initial damage radius (orange)
        if (dealDamageOnCast)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.position, initialDamageRadius);
            Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
            Gizmos.DrawWireSphere(transform.position, initialDamageRadius);
        }
        
        // Continuous damage radius (red)
        if (dealDamageContinuously)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
            Gizmos.DrawSphere(transform.position, continuousRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, continuousRadius);
        }
    }
}
