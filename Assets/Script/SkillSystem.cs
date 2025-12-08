using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class Skill
{
    [Header("Skill Info")]
    public string skillName = "Skill";
    public KeyCode hotkey; // For reference only (using Input System)
    
    [Header("Cooldown")]
    public float cooldownTime = 5f;
    [HideInInspector] public float cooldownTimer = 0f;
    
    [Header("Visual Effect")]
    public GameObject skillPrefab;
    public Vector3 spawnOffset = Vector3.zero;
    public float prefabLifetime = 3f;
    
    [Header("Settings")]
    public bool destroyOnCast = true;
    
    public bool IsReady()
    {
        return cooldownTimer <= 0f;
    }
    
    public float GetCooldownPercentage()
    {
        return 1f - (cooldownTimer / cooldownTime);
    }
    
    public void StartCooldown()
    {
        cooldownTimer = cooldownTime;
    }
    
    public void UpdateCooldown()
    {
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
        }
    }
}

public class SkillSystem : MonoBehaviour
{
    [Header("Skills")]
    public Skill skill1;
    public Skill skill2;
    public Skill skill3;
    
    [Header("Settings")]
    public bool showCooldownMessages = true;
    public Transform skillSpawnPoint; // Optional: if null, uses player position

    void Awake()
    {
        // Initialize skill names if not set
        if (string.IsNullOrEmpty(skill1.skillName)) skill1.skillName = "Skill 1";
        if (string.IsNullOrEmpty(skill2.skillName)) skill2.skillName = "Skill 2";
        if (string.IsNullOrEmpty(skill3.skillName)) skill3.skillName = "Skill 3";
        
        // Set hotkey references
        skill1.hotkey = KeyCode.E;
        skill2.hotkey = KeyCode.Q;
        skill3.hotkey = KeyCode.R;
    }

    void Update()
    {
        // Update all cooldowns
        skill1.UpdateCooldown();
        skill2.UpdateCooldown();
        skill3.UpdateCooldown();
    }

    // Input System callbacks
    public void OnSkill1(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            CastSkill(skill1);
        }
    }

    public void OnSkill2(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            CastSkill(skill2);
        }
    }

    public void OnSkill3(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            CastSkill(skill3);
        }
    }

    void CastSkill(Skill skill)
    {
        if (!skill.IsReady())
        {
            float remainingTime = skill.cooldownTimer;
            if (showCooldownMessages)
            {
                Debug.Log($"<color=yellow>[COOLDOWN]</color> {skill.skillName} is on cooldown! {remainingTime:F1}s remaining");
            }
            return;
        }

        // Spawn skill visual effect
        if (skill.skillPrefab != null)
        {
            SpawnSkillEffect(skill);
        }

        // Start cooldown
        skill.StartCooldown();
    }

    void SpawnSkillEffect(Skill skill)
    {
        // Determine spawn position
        Vector3 spawnPosition;
        Quaternion spawnRotation;

        if (skillSpawnPoint != null)
        {
            spawnPosition = skillSpawnPoint.position + skill.spawnOffset;
            spawnRotation = skillSpawnPoint.rotation;
        }
        else
        {
            spawnPosition = transform.position + skill.spawnOffset;
            spawnRotation = transform.rotation;
        }

        // Instantiate the skill prefab
        GameObject skillInstance = Instantiate(skill.skillPrefab, spawnPosition, spawnRotation);

        // Destroy after lifetime if enabled
        if (skill.destroyOnCast)
        {
            Destroy(skillInstance, skill.prefabLifetime);
        }
    }

    // Public methods to check skill status (for UI)
    public bool IsSkill1Ready() => skill1.IsReady();
    public bool IsSkill2Ready() => skill2.IsReady();
    public bool IsSkill3Ready() => skill3.IsReady();

    public float GetSkill1Cooldown() => skill1.cooldownTimer;
    public float GetSkill2Cooldown() => skill2.cooldownTimer;
    public float GetSkill3Cooldown() => skill3.cooldownTimer;

    public float GetSkill1CooldownPercentage() => skill1.GetCooldownPercentage();
    public float GetSkill2CooldownPercentage() => skill2.GetCooldownPercentage();
    public float GetSkill3CooldownPercentage() => skill3.GetCooldownPercentage();
}
