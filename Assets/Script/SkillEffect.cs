using UnityEngine;
public class SkillEffect : MonoBehaviour
{
    [Header("Damage Mode")]
    [Tooltip("Use particle collision for damage (recommended for falling particles)")]
    public bool useParticleDamage = true;
    
    [Header("Legacy Radius Damage (Optional)")]
    [Tooltip("Enable radius-based damage (old method)")]
    public bool useLegacyRadiusDamage = false;
    
    [Tooltip("How much damage this skill deals (legacy mode)")]
    public float damage = 50f;
    
    [Tooltip("Radius to detect enemies (red sphere in Scene view)")]
    public float radius = 3f;
    
    [Tooltip("Which layers to damage (select Enemy layer)")]
    public LayerMask targetLayers;
    
    [Header("Timing")]
    [Tooltip("Deal damage immediately when skill spawns (legacy mode)")]
    public bool dealDamageOnSpawn = false;
    
    [Tooltip("Deal damage continuously over time (legacy mode)")]
    public bool dealDamageContinuously = false;
    
    [Tooltip("How often to deal damage if continuous (seconds)")]
    public float damageInterval = 0.5f;
    
    private float damageTimer = 0f;
    
    [Header("Visual Effects (Optional)")]
    [Tooltip("Make the skill rotate over time")]
    public bool rotateOverTime = false;
    
    [Tooltip("Rotation speed (degrees per second)")]
    public Vector3 rotationSpeed = new Vector3(0, 100, 0);

    void Start()
    {
        // Only use legacy damage if explicitly enabled
        if (useLegacyRadiusDamage && dealDamageOnSpawn)
        {
            DealDamage();
        }
        
        // If using particle damage, ensure ParticleDamage component exists
        if (useParticleDamage)
        {
            ParticleSystem ps = GetComponentInChildren<ParticleSystem>();
            if (ps != null)
            {
                ParticleDamage particleDamage = ps.GetComponent<ParticleDamage>();
                if (particleDamage == null)
                {
                    Debug.LogWarning($"[SKILL EFFECT] {gameObject.name} is set to use particle damage but has no ParticleDamage component!");
                }
            }
        }
    }

    void Update()
    {
        // Rotation effect
        if (rotateOverTime)
        {
            transform.Rotate(rotationSpeed * Time.deltaTime);
        }

        // Continuous damage (legacy mode only)
        if (useLegacyRadiusDamage && dealDamageContinuously)
        {
            damageTimer += Time.deltaTime;
            if (damageTimer >= damageInterval)
            {
                DealDamage();
                damageTimer = 0f;
            }
        }
    }

    /// <summary>
    /// Legacy: Finds and damages all enemies in radius
    /// </summary>
    public void DealDamage()
    {
        if (!useLegacyRadiusDamage)
            return;
            
        // Find all colliders in radius on target layers
        Collider[] hits = Physics.OverlapSphere(transform.position, radius, targetLayers);

        foreach (Collider hit in hits)
        {
            // Try to damage enemy
            EnemyAI enemy = hit.GetComponent<EnemyAI>();
            if (enemy != null)
            {
                enemy.TakeDamage((int)damage);
            }
        }
    }

    /// <summary>
    /// Visualize damage radius in Scene view (red sphere) - Legacy mode only
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (!useLegacyRadiusDamage)
            return;
            
        // Draw semi-transparent sphere
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawSphere(transform.position, radius);
        
        // Draw wireframe
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
