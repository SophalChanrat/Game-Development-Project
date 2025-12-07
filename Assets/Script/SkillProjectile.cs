using UnityEngine;

/// <summary>
/// Skill projectile that moves forward and damages enemies
/// Uses DamageDealer component for collision handling
/// Similar to SlashProjectile but for skills
/// </summary>
public class SkillProjectile : MonoBehaviour
{
    private Vector3 direction;
    private float speed;
    private float maxDistance;
    private float damage;
    private Vector3 startPosition;
    private bool isInitialized = false;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    
    public void Initialize(Vector3 dir, float spd, float lifetime, int dmg = 15)
    {
        direction = dir.normalized;
        speed = spd;
        // Convert lifetime to max distance
        maxDistance = speed * lifetime;
        damage = dmg;
        startPosition = transform.position;
        isInitialized = true;
        
        if (debugMode)
            Debug.Log($"[SkillProjectile] Initializing - Damage: {damage}, Speed: {speed}, MaxDistance: {maxDistance}");
        
        // Ensure we have a collider set as trigger
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            // Add a sphere collider for skills (better for magic effects)
            SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = 0.5f;
            if (debugMode)
                Debug.Log("[SkillProjectile] Added SphereCollider as trigger");
        }
        else
        {
            col.isTrigger = true;
            if (debugMode)
                Debug.Log($"[SkillProjectile] Using existing {col.GetType().Name} as trigger");
        }
        
        // Ensure rigidbody for physics detection
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            if (debugMode)
                Debug.Log("[SkillProjectile] Added Rigidbody (kinematic)");
        }
        
        // Configure DamageDealer component
        DamageDealer damageDealer = GetComponent<DamageDealer>();
        if (damageDealer == null)
        {
            damageDealer = gameObject.AddComponent<DamageDealer>();
            if (debugMode)
                Debug.Log("[SkillProjectile] Added DamageDealer component");
        }
        
        damageDealer.SetDamage(damage);
        damageDealer.SetTeam(DamageDealer.Team.Player);
        damageDealer.SetTargetTag(""); // Let team system handle targeting
        
        if (debugMode)
        {
            Debug.Log($"[SkillProjectile] DamageDealer configured - Damage: {damage}, Team: Player");
            Debug.Log($"[SkillProjectile] Spawned at: {startPosition}, Direction: {direction}");
        }
    }
    
    void Update()
    {
        if (!isInitialized) return;
        
        // Move the projectile forward
        transform.position += direction * speed * Time.deltaTime;
        
        // Check if traveled max distance
        float distanceTraveled = Vector3.Distance(startPosition, transform.position);
        if (distanceTraveled >= maxDistance)
        {
            if (debugMode)
                Debug.Log($"[SkillProjectile] Max distance reached ({distanceTraveled:F2}/{maxDistance:F2}), destroying");
            Destroy(gameObject);
        }
    }
    
    // Let DamageDealer handle collision - it will destroy the projectile automatically
    
    void OnDestroy()
    {
        if (debugMode && isInitialized)
        {
            float distanceTraveled = Vector3.Distance(startPosition, transform.position);
            Debug.Log($"[SkillProjectile] Destroyed after traveling {distanceTraveled:F2} units");
        }
    }
}
