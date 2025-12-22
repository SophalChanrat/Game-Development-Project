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
    
    [Header("Cast Animation")]
    public bool useTwoStateAnimation = true;
    public float castDuration = 0.5f; // Wind-up time before effect spawns
    public string castAnimationTrigger = "castSkill";
    public string releaseAnimationTrigger = "releaseSkill";
    
    [Header("Visual Effect")]
    public GameObject skillPrefab;
    public Vector3 spawnOffset = Vector3.zero;
    public float prefabLifetime = 3f;
    
    [Header("Cast Range")]
    public bool useTargetedCast = true; // Enable/disable targeted casting
    public float castRange = 10f; // Maximum distance skill can be cast
    public LayerMask groundLayer; // Layer to raycast against for targeting
    public bool faceTargetOnCast = true; // Should player face the target location?
    public bool preferLockedTarget = true; // Prioritize locked enemy as target
    
    [Header("Settings")]
    public bool destroyOnCast = true;
    public bool lockMovementDuringCast = true;
    
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
    public bool useSharedAnimation = true; // All skills use same animation
    public Transform skillSpawnPoint; // Optional: if null, uses player position
    public Animator animator;
    
    [Header("Targeting")]
    public GameObject targetIndicatorPrefab; // Visual indicator for target location
    private GameObject targetIndicatorInstance;
    private Vector3 currentTargetPosition;
    private bool hasValidTarget = false;
    
    [Header("References")]
    private PlayerMovement3D playerMovement;
    private CinemachineLockOn lockOnSystem;
    private bool isCasting = false;
    private Camera mainCamera;

    void Awake()
    {
        // Get references
        animator = GetComponent<Animator>();
        playerMovement = GetComponent<PlayerMovement3D>();
        lockOnSystem = GetComponent<CinemachineLockOn>();
        mainCamera = Camera.main;
        
        // Initialize skill names if not set
        if (string.IsNullOrEmpty(skill1.skillName)) skill1.skillName = "Skill 1";
        if (string.IsNullOrEmpty(skill2.skillName)) skill2.skillName = "Skill 2";
        if (string.IsNullOrEmpty(skill3.skillName)) skill3.skillName = "Skill 3";
        
        // Set hotkey references
        skill1.hotkey = KeyCode.E;
        skill2.hotkey = KeyCode.Q;
        skill3.hotkey = KeyCode.R;
        
        if (useSharedAnimation)
        {
            // All skills use the same animation triggers
            skill1.castAnimationTrigger = "castSkill";
            skill2.castAnimationTrigger = "castSkill";
            skill3.castAnimationTrigger = "castSkill";
            
            skill1.releaseAnimationTrigger = "releaseSkill";
            skill2.releaseAnimationTrigger = "releaseSkill";
            skill3.releaseAnimationTrigger = "releaseSkill";
        }
        else
        {
            // Each skill has unique animation triggers
            if (string.IsNullOrEmpty(skill1.castAnimationTrigger)) skill1.castAnimationTrigger = "castSkill1";
            if (string.IsNullOrEmpty(skill2.castAnimationTrigger)) skill2.castAnimationTrigger = "castSkill2";
            if (string.IsNullOrEmpty(skill3.castAnimationTrigger)) skill3.castAnimationTrigger = "castSkill3";
            
            if (string.IsNullOrEmpty(skill1.releaseAnimationTrigger)) skill1.releaseAnimationTrigger = "releaseSkill1";
            if (string.IsNullOrEmpty(skill2.releaseAnimationTrigger)) skill2.releaseAnimationTrigger = "releaseSkill2";
            if (string.IsNullOrEmpty(skill3.releaseAnimationTrigger)) skill3.releaseAnimationTrigger = "releaseSkill3";
        }
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
        if (context.started && !isCasting)
        {
            if (GetTargetPosition(skill1, out Vector3 targetPos))
            {
                currentTargetPosition = targetPos;
                hasValidTarget = true;
                StartCoroutine(CastSkillCoroutine(skill1));
            }
        }
    }

    public void OnSkill2(InputAction.CallbackContext context)
    {
        if (context.started && !isCasting)
        {
            if (GetTargetPosition(skill2, out Vector3 targetPos))
            {
                currentTargetPosition = targetPos;
                hasValidTarget = true;
                StartCoroutine(CastSkillCoroutine(skill2));
            }
        }
    }

    public void OnSkill3(InputAction.CallbackContext context)
    {
        if (context.started && !isCasting)
        {
            if (GetTargetPosition(skill3, out Vector3 targetPos))
            {
                currentTargetPosition = targetPos;
                hasValidTarget = true;
                StartCoroutine(CastSkillCoroutine(skill3));
            }
        }
    }
    
    /// <summary>
    /// Gets the target position for skill casting using raycast or locked target
    /// </summary>
    private bool GetTargetPosition(Skill skill, out Vector3 targetPosition)
    {
        targetPosition = transform.position;
        
        // If not using targeted cast, use player position
        if (!skill.useTargetedCast)
        {
            return true;
        }
        
        // PRIORITY 1: Check if player is locked onto an enemy
        if (skill.preferLockedTarget && lockOnSystem != null && lockOnSystem.IsLockedOn())
        {
            Transform lockedTarget = lockOnSystem.GetCurrentTarget();
            
            if (lockedTarget != null)
            {
                // Check if locked target is within cast range
                float distanceToTarget = Vector3.Distance(transform.position, lockedTarget.position);
                
                if (distanceToTarget <= skill.castRange)
                {
                    // Cast at the locked enemy's position
                    targetPosition = lockedTarget.position;
                    Debug.Log($"<color=cyan>[TARGET]</color> Casting {skill.skillName} on locked target: {lockedTarget.name}");
                    return true;
                }
                else
                {
                    // Enemy is locked but out of range
                    Debug.Log($"<color=yellow>[TARGET]</color> Locked enemy is out of range ({distanceToTarget:F1}m / {skill.castRange}m). Using ground target.");
                    // Fall through to ground targeting
                }
            }
        }
        
        // PRIORITY 2: Use ground targeting (raycast from camera)
        Ray ray = mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, 100f, skill.groundLayer))
        {
            Vector3 potentialTarget = hit.point;
            
            // Check if target is within cast range
            float distanceToTarget = Vector3.Distance(transform.position, potentialTarget);
            
            if (distanceToTarget <= skill.castRange)
            {
                targetPosition = potentialTarget;
                return true;
            }
            else
            {
                // Clamp to max range in the direction of the target
                Vector3 directionToTarget = (potentialTarget - transform.position).normalized;
                targetPosition = transform.position + directionToTarget * skill.castRange;
                return true;
            }
        }
        else
        {
            // If raycast doesn't hit, cast at max range in forward direction
            targetPosition = transform.position + transform.forward * skill.castRange;
            return true;
        }
    }

    IEnumerator CastSkillCoroutine(Skill skill)
    {
        // Check if skill is ready
        if (!skill.IsReady())
        {
            float remainingTime = skill.cooldownTimer;
            if (showCooldownMessages)
            {
                Debug.Log($"<color=yellow>[COOLDOWN]</color> {skill.skillName} is on cooldown! {remainingTime:F1}s remaining");
            }
            yield break;
        }

        // Mark as casting
        isCasting = true;
        
        // Face target location if enabled
        if (skill.faceTargetOnCast && hasValidTarget && skill.useTargetedCast)
        {
            Vector3 directionToTarget = (currentTargetPosition - transform.position);
            directionToTarget.y = 0; // Keep on horizontal plane
            
            if (directionToTarget.magnitude > 0.1f)
            {
                transform.rotation = Quaternion.LookRotation(directionToTarget);
            }
        }

        // Lock movement if enabled
        if (skill.lockMovementDuringCast && playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        if (skill.useTwoStateAnimation)
        {
            // === STATE 1: CAST/WIND-UP ===
            if (animator != null)
            {
                // Reset movement animations
                animator.SetBool("isMoving", false);
                animator.SetBool("walkBack", false);
                animator.SetBool("walkLeft", false);
                animator.SetBool("walkRight", false);
                
                // Trigger cast animation
                animator.SetTrigger(skill.castAnimationTrigger);
            }

            Debug.Log($"<color=cyan>[CASTING]</color> {skill.skillName} - Wind-up started");

            // Wait for cast duration (wind-up time)
            yield return new WaitForSeconds(skill.castDuration);

            // === STATE 2: RELEASE ===
            if (animator != null)
            {
                animator.SetTrigger(skill.releaseAnimationTrigger);
            }

            Debug.Log($"<color=green>[RELEASE]</color> {skill.skillName} - Effect spawned!");

            // Spawn skill effect at release point
            if (skill.skillPrefab != null)
            {
                SpawnSkillEffect(skill);
            }
        }
        else
        {
            // Single state animation (instant cast)
            if (animator != null)
            {
                animator.SetTrigger(skill.castAnimationTrigger);
            }

            // Spawn effect immediately
            if (skill.skillPrefab != null)
            {
                SpawnSkillEffect(skill);
            }

            Debug.Log($"<color=green>[CAST]</color> {skill.skillName} used!");
        }

        // Start cooldown
        skill.StartCooldown();

        // Small delay before re-enabling movement (let release animation play a bit)
        yield return new WaitForSeconds(0.2f);

        // Re-enable movement
        if (skill.lockMovementDuringCast && playerMovement != null)
        {
            playerMovement.enabled = true;
        }

        hasValidTarget = false;
        isCasting = false;
    }

    void SpawnSkillEffect(Skill skill)
    {
        // Determine spawn position
        Vector3 spawnPosition;
        Quaternion spawnRotation;

        if (skill.useTargetedCast && hasValidTarget)
        {
            // Spawn at target position
            spawnPosition = currentTargetPosition + skill.spawnOffset;
            
            // Calculate rotation to face from player to target
            Vector3 direction = (currentTargetPosition - transform.position);
            direction.y = 0;
            
            if (direction.magnitude > 0.1f)
            {
                spawnRotation = Quaternion.LookRotation(direction);
            }
            else
            {
                spawnRotation = transform.rotation;
            }
        }
        else if (skillSpawnPoint != null)
        {
            // Use spawn point
            spawnPosition = skillSpawnPoint.position + skill.spawnOffset;
            spawnRotation = skillSpawnPoint.rotation;
        }
        else
        {
            // Use player position
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
    public bool IsSkill1Ready() => skill1.IsReady() && !isCasting;
    public bool IsSkill2Ready() => skill2.IsReady() && !isCasting;
    public bool IsSkill3Ready() => skill3.IsReady() && !isCasting;

    public float GetSkill1Cooldown() => skill1.cooldownTimer;
    public float GetSkill2Cooldown() => skill2.cooldownTimer;
    public float GetSkill3Cooldown() => skill3.cooldownTimer;

    public float GetSkill1CooldownPercentage() => skill1.GetCooldownPercentage();
    public float GetSkill2CooldownPercentage() => skill2.GetCooldownPercentage();
    public float GetSkill3CooldownPercentage() => skill3.GetCooldownPercentage();
    
    public bool IsCasting() => isCasting;
    
    // Visualize cast ranges in editor
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        
        // Draw skill 1 range
        if (skill1.useTargetedCast)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, skill1.castRange);
        }
        
        // Draw skill 2 range
        if (skill2.useTargetedCast)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, skill2.castRange);
        }
        
        // Draw skill 3 range
        if (skill3.useTargetedCast)
        {
            Gizmos.color = new Color(0f, 0f, 1f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, skill3.castRange);
        }
        
        // Draw current target position
        if (hasValidTarget)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(currentTargetPosition, 0.5f);
            Gizmos.DrawLine(transform.position, currentTargetPosition);
        }
        
        // Draw locked target if active
        if (lockOnSystem != null && lockOnSystem.IsLockedOn())
        {
            Transform lockedTarget = lockOnSystem.GetCurrentTarget();
            if (lockedTarget != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(lockedTarget.position, 0.75f);
                Gizmos.DrawLine(transform.position, lockedTarget.position);
            }
        }
    }
}
