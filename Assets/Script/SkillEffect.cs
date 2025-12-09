using UnityEngine;
public class SkillEffect : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("How much damage this skill deals")]
    public float damage = 50f;
    
    [Tooltip("Radius to detect enemies (red sphere in Scene view)")]
    public float radius = 3f;
    
    [Tooltip("Which layers to damage (select Enemy layer)")]
    public LayerMask targetLayers;
    
    [Header("Timing")]
    [Tooltip("Deal damage immediately when skill spawns")]
    public bool dealDamageOnSpawn = true;
    
    [Tooltip("Deal damage continuously over time")]
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
        if (dealDamageOnSpawn)
        {
            DealDamage();
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
                DealDamage();
                damageTimer = 0f;
            }
        }
    }
    public void DealDamage()
    {
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

    void OnDrawGizmosSelected()
    {
        // Draw semi-transparent sphere
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawSphere(transform.position, radius);
        
        // Draw wireframe
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
