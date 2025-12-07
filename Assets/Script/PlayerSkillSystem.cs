using System;
using UnityEngine;

[Serializable]
public class Skill
{
    public string skillName;
    public GameObject skillEffectPrefab;
    public float cooldown;
    public int damage = 20; // Skill damage
    public float range = 50f; // Skill range (converted to lifetime)
    public float speed = 25f; // Projectile speed
    
    [HideInInspector]
    public float lastUsedTime = -999f;
    
    public bool IsOnCooldown()
    {
        return Time.time < lastUsedTime + cooldown;
    }
    
    public float GetCooldownRemaining()
    {
        float remaining = (lastUsedTime + cooldown) - Time.time;
        return Mathf.Max(0, remaining);
    }
    
    public float GetCooldownProgress()
    {
        if (!IsOnCooldown()) return 1f;
        float elapsed = Time.time - lastUsedTime;
        return Mathf.Clamp01(elapsed / cooldown);
    }
    
    // Calculate lifetime from range and speed
    public float GetLifetime()
    {
        return range / speed;
    }
}

public class PlayerSkillSystem : MonoBehaviour
{
    [Header("Skill 1 - Fire Blast (Projectile)")]
    public Skill skill1 = new Skill 
    { 
        skillName = "Fire Blast", 
        cooldown = 3f,
        damage = 25,
        range = 50f,
        speed = 25f
    };
    
    [Header("Skill 2 - Ice Shield (Shield)")]
    public Skill skill2 = new Skill 
    { 
        skillName = "Ice Shield", 
        cooldown = 8f,
        damage = 10, // Damage on contact
        range = 3f, // Shield radius
        speed = 0f // Shield doesn't move
    };
    
    [Header("Skill 3 - Thunder Strike (AOE)")]
    public Skill skill3 = new Skill 
    { 
        skillName = "Thunder Strike", 
        cooldown = 12f,
        damage = 40,
        range = 10f, // AOE distance from player
        speed = 0f // Instant cast
    };
    
    [Header("Skill Spawn Settings")]
    public Transform skillSpawnPoint;
    
    [Header("Shield Settings")]
    public float shieldDuration = 5f;
    
    [Header("AOE Settings")]
    public float aoeRadius = 5f;
    public float aoeDuration = 2f;
    
    [Header("Temporary Testing (Remove when Input System is setup)")]
    public bool enableKeyboardTesting = true;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    
    private Animator animator;
    
    void Awake()
    {
        animator = GetComponent<Animator>();
        
        // Create skill spawn point if it doesn't exist
        if (skillSpawnPoint == null)
        {
            GameObject spawnPoint = new GameObject("SkillSpawnPoint");
            spawnPoint.transform.SetParent(transform);
            spawnPoint.transform.localPosition = new Vector3(0, 1.5f, 1.5f);
            skillSpawnPoint = spawnPoint.transform;
        }
        
        if (debugMode)
            Debug.Log("[PlayerSkillSystem] Initialized! Skills ready.");
    }
    
    void Update()
    {
        // Temporary keyboard testing (will be replaced by Input System)
        if (enableKeyboardTesting)
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                if (debugMode)
                    Debug.Log("[PlayerSkillSystem] Q key pressed - attempting Skill 1");
                UseSkill(skill1, 1);
            }
            
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (debugMode)
                    Debug.Log("[PlayerSkillSystem] E key pressed - attempting Skill 2");
                UseSkill(skill2, 2);
            }
            
            if (Input.GetKeyDown(KeyCode.R))
            {
                if (debugMode)
                    Debug.Log("[PlayerSkillSystem] R key pressed - attempting Skill 3");
                UseSkill(skill3, 3);
            }
        }
    }
    
    public void UseSkill(Skill skill, int skillNumber)
    {
        if (debugMode)
            Debug.Log($"[PlayerSkillSystem] UseSkill called for {skill.skillName}");
        
        // Check if skill can be used
        if (skill.IsOnCooldown())
        {
            if (debugMode)
                Debug.Log($"[PlayerSkillSystem] {skill.skillName} is on cooldown! {skill.GetCooldownRemaining():F1}s remaining");
            return;
        }
        
        // Set cooldown
        skill.lastUsedTime = Time.time;
        
        // Play animation (skip if not set up)
        if (animator != null)
        {
            animator.SetTrigger($"skill{skillNumber}");
        }
        
        // Execute skill effect
        ExecuteSkillEffect(skill, skillNumber);
        
        if (debugMode)
            Debug.Log($"[PlayerSkillSystem] Successfully used {skill.skillName}!");
    }
    
    private void ExecuteSkillEffect(Skill skill, int skillNumber)
    {
        if (skill.skillEffectPrefab == null)
        {
            Debug.LogWarning($"[PlayerSkillSystem] {skill.skillName} effect prefab is not assigned! Creating invisible projectile.");
        }
        
        if (debugMode)
            Debug.Log($"[PlayerSkillSystem] Executing effect for {skill.skillName} (Damage: {skill.damage}, Range: {skill.range})");
        
        switch (skillNumber)
        {
            case 1:
                // Fire Blast - Projectile that shoots forward
                SpawnProjectileSkill(skill);
                break;
                
            case 2:
                // Ice Shield - Effect around player
                SpawnShieldSkill(skill);
                break;
                
            case 3:
                // Thunder Strike - AOE at target location
                SpawnAOESkill(skill);
                break;
        }
    }
    
    private void SpawnProjectileSkill(Skill skill)
    {
        GameObject skillEffect;
        
        if (skill.skillEffectPrefab != null)
        {
            skillEffect = Instantiate(skill.skillEffectPrefab, skillSpawnPoint.position, skillSpawnPoint.rotation);
        }
        else
        {
            // Create invisible projectile if no prefab assigned
            skillEffect = new GameObject($"{skill.skillName}_Projectile");
            skillEffect.transform.position = skillSpawnPoint.position;
            skillEffect.transform.rotation = skillSpawnPoint.rotation;
        }
        
        // Add projectile behavior
        SkillProjectile projectile = skillEffect.GetComponent<SkillProjectile>();
        if (projectile == null)
        {
            projectile = skillEffect.AddComponent<SkillProjectile>();
        }
        
        float lifetime = skill.GetLifetime();
        projectile.Initialize(transform.forward, skill.speed, lifetime, skill.damage);
        
        if (debugMode)
            Debug.Log($"[PlayerSkillSystem] Projectile spawned - Speed: {skill.speed}, Lifetime: {lifetime:F2}s, Range: {skill.range} units");
    }
    
    private void SpawnShieldSkill(Skill skill)
    {
        GameObject shield;
        
        if (skill.skillEffectPrefab != null)
        {
            shield = Instantiate(skill.skillEffectPrefab, transform.position, Quaternion.identity);
            shield.transform.SetParent(transform);
        }
        else
        {
            // Create invisible shield if no prefab assigned
            shield = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shield.name = $"{skill.skillName}_Shield";
            shield.transform.position = transform.position;
            shield.transform.SetParent(transform);
            shield.transform.localScale = Vector3.one * skill.range * 2f;
            
            // Make it semi-transparent
            Renderer renderer = shield.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = renderer.material;
                Color color = new Color(0.5f, 0.8f, 1f, 0.3f);
                mat.color = color;
            }
        }
        
        // Add collider for damage
        SphereCollider collider = shield.GetComponent<SphereCollider>();
        if (collider == null)
        {
            collider = shield.AddComponent<SphereCollider>();
        }
        collider.isTrigger = true;
        collider.radius = skill.range;
        
        // Add rigidbody
        Rigidbody rb = shield.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = shield.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = false;
        
        // Add damage dealer
        DamageDealer damageDealer = shield.GetComponent<DamageDealer>();
        if (damageDealer == null)
        {
            damageDealer = shield.AddComponent<DamageDealer>();
        }
        damageDealer.SetDamage(skill.damage);
        damageDealer.SetTeam(DamageDealer.Team.Player);
        damageDealer.SetTargetTag("");
        
        // Destroy shield after duration
        Destroy(shield, shieldDuration);
        
        if (debugMode)
            Debug.Log($"[PlayerSkillSystem] Shield spawned - Duration: {shieldDuration}s, Radius: {skill.range}, Damage: {skill.damage}");
    }
    
    private void SpawnAOESkill(Skill skill)
    {
        // Calculate AOE position in front of player
        Vector3 aoePosition = transform.position + transform.forward * skill.range;
        aoePosition.y = transform.position.y; // Keep at player height
        
        GameObject aoe;
        
        if (skill.skillEffectPrefab != null)
        {
            aoe = Instantiate(skill.skillEffectPrefab, aoePosition, Quaternion.identity);
        }
        else
        {
            // Create visible AOE indicator if no prefab assigned
            aoe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            aoe.name = $"{skill.skillName}_AOE";
            aoe.transform.position = aoePosition;
            aoe.transform.localScale = new Vector3(aoeRadius * 2f, 0.1f, aoeRadius * 2f);
            
            // Make it yellow/electric color
            Renderer renderer = aoe.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = renderer.material;
                mat.color = new Color(1f, 1f, 0f, 0.7f);
            }
        }
        
        // Replace collider with trigger
        Collider existingCollider = aoe.GetComponent<Collider>();
        if (existingCollider != null)
        {
            Destroy(existingCollider);
        }
        
        // Add sphere collider for AOE damage
        SphereCollider sphereCollider = aoe.AddComponent<SphereCollider>();
        sphereCollider.isTrigger = true;
        sphereCollider.radius = aoeRadius;
        
        // Add rigidbody
        Rigidbody rb = aoe.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = aoe.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = false;
        
        // Add damage dealer
        DamageDealer damageDealer = aoe.GetComponent<DamageDealer>();
        if (damageDealer == null)
        {
            damageDealer = aoe.AddComponent<DamageDealer>();
        }
        damageDealer.SetDamage(skill.damage);
        damageDealer.SetTeam(DamageDealer.Team.Player);
        damageDealer.SetTargetTag("");
        
        // Destroy AOE after duration
        Destroy(aoe, aoeDuration);
        
        if (debugMode)
            Debug.Log($"[PlayerSkillSystem] AOE spawned at {aoePosition} - Duration: {aoeDuration}s, Radius: {aoeRadius}, Damage: {skill.damage}");
    }
    
    // Public methods for UI
    public Skill GetSkill(int skillNumber)
    {
        switch (skillNumber)
        {
            case 1: return skill1;
            case 2: return skill2;
            case 3: return skill3;
            default: return null;
        }
    }
}
