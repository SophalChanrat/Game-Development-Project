using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement3D : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 6f;
    public float turnSmoothTime = 0.1f; // How long it takes to turn
    public float speedSmoothTime = 0.1f; // How long it takes to reach max speed
    private bool isMoving;
    public float jumpForce = 6f;
    private bool isJumping = false; // Prevents jump spam
    public float jumpCooldown = 0.5f; // Time after landing before can jump again

    [Header("References")]
    public Transform camTransform; // Drag your Main Camera here
    public float groundDistance = 0.3f;
    public LayerMask groundMask;

    private Rigidbody rb;
    private Vector2 moveInput;
    private Vector3 currentVelocity; // Ref variable for SmoothDamp
    private Vector3 smoothVelocityXZ;
    private float turnSmoothVelocity; // Ref variable for rotation
    private bool isGrounded;
    private bool wasGrounded;
    Animator animator;
    
    [Header("Dash Settings")]
    public float dashForce = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    private bool isDashing = false;
    private float lastDashTime = -999f;

    [Header("Attack Settings")]
    public float attackCooldown = 0.5f;
    private float lastAttackTime = -999f;
    
    private PlayerAttackSystem attackSystem;
    private PlayerSkillSystem skillSystem;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        attackSystem = GetComponent<PlayerAttackSystem>();
        skillSystem = GetComponent<PlayerSkillSystem>();

        // Safety check: if camera isn't assigned, use the main one
        if (camTransform == null && Camera.main != null)
        {
            camTransform = Camera.main.transform;
        }

        // Optional: Freeze Rotation prevents physics objects from tipping the player over
        rb.freezeRotation = true;
    }

    private void FixedUpdate()
    {
        wasGrounded = isGrounded;
        CheckGround();
        
        // Reset jump flag when landing
        if (isGrounded && !wasGrounded)
        {
            StartCoroutine(ResetJumpAfterDelay());
        }
        
        MovePlayer();
    }

    private IEnumerator ResetJumpAfterDelay()
    {
        yield return new WaitForSeconds(jumpCooldown);
        isJumping = false;
    }

    private void MovePlayer()
    {
        if (isDashing) return;
        if (moveInput.magnitude < 0.1f)
        {
            // Only slow down XZ, do NOT affect Y
            Vector3 stopXZ = Vector3.SmoothDamp(new Vector3(rb.velocity.x, 0, rb.velocity.z),
                                                Vector3.zero,
                                                ref smoothVelocityXZ,
                                                speedSmoothTime);

            rb.velocity = new Vector3(stopXZ.x, rb.velocity.y, stopXZ.z);
            return;
        }

        float targetAngle = Mathf.Atan2(moveInput.x, moveInput.y) * Mathf.Rad2Deg + camTransform.eulerAngles.y;
        float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
        transform.rotation = Quaternion.Euler(0f, angle, 0f);

        Vector3 moveDir = Quaternion.Euler(0, targetAngle, 0) * Vector3.forward;
        Vector3 targetXZ = moveDir.normalized * moveSpeed;

        // Smooth only XZ (DON'T TOUCH Y)
        Vector3 newXZ = Vector3.SmoothDamp(new Vector3(rb.velocity.x, 0, rb.velocity.z),
                                           targetXZ,
                                           ref smoothVelocityXZ,
                                           speedSmoothTime);

        rb.velocity = new Vector3(newXZ.x, rb.velocity.y, newXZ.z);
    }

    // ---------------- INPUT EVENTS ---------------- //

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
        isMoving = moveInput != Vector2.zero;

        if (animator != null)
        {
            animator.SetBool("isMoving", isMoving);
        }
    }
    
    private void CheckGround()
    {
        // Check if grounded using a small sphere at player's feet
        Vector3 origin = transform.position;
        
        // Try with layer mask first
        if (groundMask.value != 0)
        {
            isGrounded = Physics.CheckSphere(origin, groundDistance, groundMask);
        }
        else
        {
            // Fallback: check without layer mask (checks all layers)
            isGrounded = Physics.Raycast(origin, Vector3.down, groundDistance + 0.1f);
        }
        
        // Update animator
        if (animator != null)
        {
            animator.SetBool("isGrounded", isGrounded);
        }
    }
    
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started && isGrounded && !isJumping)
        {
            isJumping = true;
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z); // Reset vertical velocity before jump
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            
            if (animator != null)
            {
                animator.SetTrigger("isJump");
            }
        }
    }
    
    public void OnDash(InputAction.CallbackContext context)
    {
        if (!context.started) return;

        // Can only dash on ground OR in air? (your choice)
        if (Time.time < lastDashTime + dashCooldown) return;
        if (isDashing) return;

        StartCoroutine(Dash());
    }
    
    private IEnumerator Dash()
    {
        isDashing = true;
        lastDashTime = Time.time;
        
        Vector3 dashDir;

        if (moveInput.sqrMagnitude > 0.1f)
        {
            // Convert WASD to world direction using camera
            float targetAngle = Mathf.Atan2(moveInput.x, moveInput.y) * Mathf.Rad2Deg
                                + camTransform.eulerAngles.y;

            dashDir = Quaternion.Euler(0, targetAngle, 0) * Vector3.forward;
        }
        else
        {
            // If player is not moving -> dash forward
            dashDir = transform.forward;
        }

        dashDir.Normalize();

        // Apply dash force
        rb.velocity = new Vector3(dashDir.x * dashForce, rb.velocity.y, dashDir.z * dashForce);
        
        // Wait for dash duration
        yield return new WaitForSeconds(dashDuration);
        
        isDashing = false;
    }
    
    public void OnAttack(InputAction.CallbackContext context)
    {
        if (!context.started) return;

        // Use the attack system if available
        if (attackSystem != null)
        {
            attackSystem.PerformAttack();
        }
        else
        {
            // Fallback to old behavior
            if (Time.time < lastAttackTime + attackCooldown) return;
            lastAttackTime = Time.time;

            if (animator != null)
                animator.SetTrigger("attack");
        }
    }
    
    // Skill Input Actions
    public void OnSkill1(InputAction.CallbackContext context)
    {
        if (!context.started) return;
        
        if (skillSystem != null)
        {
            skillSystem.UseSkill(skillSystem.skill1, 1);
        }
    }
    
    public void OnSkill2(InputAction.CallbackContext context)
    {
        if (!context.started) return;
        
        if (skillSystem != null)
        {
            skillSystem.UseSkill(skillSystem.skill2, 2);
        }
    }
    
    public void OnSkill3(InputAction.CallbackContext context)
    {
        if (!context.started) return;
        
        if (skillSystem != null)
        {
            skillSystem.UseSkill(skillSystem.skill3, 3);
        }
    }
}