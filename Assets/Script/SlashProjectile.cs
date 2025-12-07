using UnityEngine;

/// <summary>
/// Slash projectile that moves forward and damages enemies
/// Uses DamageDealer component for collision handling
/// </summary>
public class SlashProjectile : MonoBehaviour
{
    private Vector3 direction;
    private float speed;
    private float maxDistance;
    private float damage;
    private Vector3 startPosition;
    private bool isInitialized = false;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    
    public void Initialize(Vector3 dir, float spd, float lifetime, int dmg)
    {
        direction = dir.normalized;
        speed = spd;
        // Convert lifetime to max distance
        maxDistance = speed * lifetime;
        damage = dmg;
        startPosition = transform.position;
        isInitialized = true;
        
        if (debugMode)
            Debug.Log($"[SlashProjectile] Initializing - Damage: {damage}, Speed: {speed}, MaxDistance: {maxDistance}");
        
        // Ensure we have a collider set as trigger
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            // Add a box collider if none exists
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(1.5f, 1.5f, 2f); // Larger collider for better hit detection
            if (debugMode)
                Debug.Log("[SlashProjectile] Added BoxCollider as trigger");
        }
        else
        {
            col.isTrigger = true;
            if (debugMode)
                Debug.Log($"[SlashProjectile] Using existing {col.GetType().Name} as trigger");
        }
        
        // Ensure rigidbody for physics detection
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true; // Kinematic so it's not affected by gravity
            rb.useGravity = false;
            if (debugMode)
                Debug.Log("[SlashProjectile] Added Rigidbody (kinematic)");
        }
        
        // Configure DamageDealer component
        DamageDealer damageDealer = GetComponent<DamageDealer>();
        if (damageDealer == null)
        {
            damageDealer = gameObject.AddComponent<DamageDealer>();
            if (debugMode)
                Debug.Log("[SlashProjectile] Added DamageDealer component");
        }
        
        damageDealer.SetDamage(damage);
        damageDealer.SetTeam(DamageDealer.Team.Player);
        damageDealer.SetTargetTag(""); // Remove tag requirement - let team system handle it
        
        if (debugMode)
        {
            Debug.Log($"[SlashProjectile] DamageDealer configured - Damage: {damage}, Team: Player");
            Debug.Log($"[SlashProjectile] Spawned at: {startPosition}, Direction: {direction}");
        }
    }
    
    void Update()
    {
        if (!isInitialized) return;
        
        // Move the slash forward
        transform.position += direction * speed * Time.deltaTime;
        
        // Check if traveled max distance
        float distanceTraveled = Vector3.Distance(startPosition, transform.position);
        if (distanceTraveled >= maxDistance)
        {
            if (debugMode)
                Debug.Log($"[SlashProjectile] Max distance reached ({distanceTraveled:F2}/{maxDistance:F2}), destroying");
            Destroy(gameObject);
        }
    }
    
    // Let DamageDealer handle collision - it will destroy the projectile automatically
    
    void OnDestroy()
    {
        if (debugMode && isInitialized)
        {
            float distanceTraveled = Vector3.Distance(startPosition, transform.position);
            Debug.Log($"[SlashProjectile] Destroyed after traveling {distanceTraveled:F2} units");
        }
    }
}
