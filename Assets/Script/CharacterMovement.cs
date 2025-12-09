using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerMovement3D : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 6f;
    public float turnSmoothTime = 0.1f;
    public float speedSmoothTime = 0.1f;
    private bool isMoving;
    public float jumpHeight = 6f;
    public float gravity = -9.81f;

    [Header("References")]
    public Transform camTransform;
    public Transform groundCheck;
    public float groundDistance = 0.2f;
    public LayerMask groundMask;

    [Header("Lock On")]
    private CinemachineLockOn lockOnScript;

    private CharacterController controller;
    private Vector2 moveInput;
    private Vector3 velocity;
    private float currentSpeed;
    private float speedSmoothVelocity;
    private float turnSmoothVelocity;
    private bool isGrounded;
    Animator animator;
    
    [Header("Dash Settings")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    private bool isDashing = false;
    private float lastDashTime = -999f;
    private Vector3 dashDirection;

    [Header("Attack Settings")]
    public int attackDamage = 25;
    public float attackRange = 2f;
    public float attackCooldown = 0.5f;
    public LayerMask enemyLayer;
    private bool isAttacking = false;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        lockOnScript = GetComponent<CinemachineLockOn>();

        if (camTransform == null && Camera.main != null)
        {
            camTransform = Camera.main.transform;
        }

        // Create ground check position if it doesn't exist
        if (groundCheck == null)
        {
            GameObject groundCheckObj = new GameObject("GroundCheck");
            groundCheck = groundCheckObj.transform;
            groundCheck.parent = transform;
            groundCheck.localPosition = new Vector3(0, 0, 0);
        }
    }
    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        CheckGround();
        ApplyGravity();
        
        // Only allow movement if not attacking
        if (!isAttacking)
        {
            MovePlayer();
        }
        else
        {
            FaceLockedTargetIfAvailable();
        }
    }

    private void CheckGround()
    {
        // Use both built-in and sphere check for more reliable ground detection
        bool controllerGrounded = controller.isGrounded;
        bool sphereGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        
        isGrounded = controllerGrounded || sphereGrounded;
    }

    private void ApplyGravity()
    {
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small negative value to keep player grounded
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }
    }

    private void MovePlayer()
    {
        Vector3 moveDirection = Vector3.zero;

        if (isDashing)
        {
            moveDirection = dashDirection * dashSpeed;
            moveDirection.y = velocity.y;
            controller.Move(moveDirection * Time.deltaTime);
            return;
        }

        if (moveInput.magnitude >= 0.1f)
        {
            // Check if locked on - use strafe movement
            if (lockOnScript != null && lockOnScript.IsLockedOn())
            {
                Transform target = lockOnScript.GetCurrentTarget();
                if (target != null)
                {
                    // Strafe movement relative to target
                    Vector3 toTarget = (target.position - transform.position).normalized;
                    toTarget.y = 0;

                    // Right vector perpendicular to target direction
                    Vector3 right = Vector3.Cross(Vector3.up, toTarget).normalized;

                    // Calculate strafe direction
                    // Forward/Back = toward/away from target
                    // Left/Right = strafe around target
                    Vector3 moveDir = toTarget * moveInput.y + right * moveInput.x;

                    float targetSpeed = moveSpeed * moveInput.magnitude;
                    currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedSmoothVelocity, speedSmoothTime);

                    moveDirection = moveDir.normalized * currentSpeed;

                    // Set animator parameters for locked-on movement
                    if (animator != null)
                    {
                        // Check movement direction
                        bool isWalkingBack = moveInput.y < -0.1f;
                        bool isWalkingLeft = moveInput.x < -0.1f;
                        bool isWalkingRight = moveInput.x > 0.1f;
                        
                        animator.SetBool("walkBack", isWalkingBack);
                        animator.SetBool("walkLeft", isWalkingLeft);
                        animator.SetBool("walkRight", isWalkingRight);
                        animator.SetBool("isMoving", true);
                    }

                    // Player rotation is handled by lock-on system, don't override it here
                }
            }
            else
            {
                // Normal camera-relative movement (not locked on)
                Vector3 cameraForward = camTransform.forward;
                Vector3 cameraRight = camTransform.right;
                cameraForward.y = 0;
                cameraRight.y = 0;
                cameraForward.Normalize();
                cameraRight.Normalize();

                Vector3 moveDir = cameraForward * moveInput.y + cameraRight * moveInput.x;

                if (moveDir.magnitude >= 0.1f)
                {
                    // Calculate target rotation based on movement direction
                    float targetAngle = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;

                    // Smoothly rotate player to face movement direction
                    float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
                    transform.rotation = Quaternion.Euler(0f, angle, 0f);

                    float targetSpeed = moveSpeed * moveInput.magnitude;
                    currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedSmoothVelocity, speedSmoothTime);

                    moveDirection = moveDir.normalized * currentSpeed;
                    
                    // When not locked on, always walk forward (player rotates to face direction)
                    if (animator != null)
                    {
                        animator.SetBool("walkBack", false);
                        animator.SetBool("walkLeft", false);
                        animator.SetBool("walkRight", false);
                    }
                }
            }
        }
        else
        {
            currentSpeed = Mathf.SmoothDamp(currentSpeed, 0f, ref speedSmoothVelocity, speedSmoothTime);
            
            // Reset animator when not moving
            if (animator != null)
            {
                animator.SetBool("walkBack", false);
                animator.SetBool("walkLeft", false);
                animator.SetBool("walkRight", false);
            }
        }

        moveDirection.y = velocity.y;
        controller.Move(moveDirection * Time.deltaTime);
    }
    private void FaceLockedTargetIfAvailable()
    {
        if (lockOnScript != null && lockOnScript.IsLockedOn())
        {
            Transform target = lockOnScript.GetCurrentTarget();
            if (target != null)
            {
                // Calculate direction to target
                Vector3 direction = target.position - transform.position;
                direction.y = 0; // Keep rotation on horizontal plane

                if (direction.magnitude > 0.1f)
                {
                    // Smoothly rotate to face target
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
                }
            }
        }
    }

    // ---------------- INPUT EVENTS ---------------- //

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
        isMoving = moveInput != Vector2.zero;

        if (animator != null && !isAttacking)
        {
            animator.SetBool("isMoving", isMoving);
        }
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started && isGrounded && !isAttacking)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * 2f * -gravity);
            if (animator != null)
            {
                animator.SetTrigger("isJump");
            }
        }
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (!context.started) return;
        if (isAttacking) return;
        if (Time.time < lastDashTime + dashCooldown) return;
        if (isDashing) return;

        StartCoroutine(Dash());
    }

    private IEnumerator Dash()
    {
        isDashing = true;
        lastDashTime = Time.time;

        if (moveInput.sqrMagnitude > 0.1f)
        {
            // Dash in the direction relative to camera
            Vector3 cameraForward = camTransform.forward;
            Vector3 cameraRight = camTransform.right;
            cameraForward.y = 0;
            cameraRight.y = 0;
            cameraForward.Normalize();
            cameraRight.Normalize();

            dashDirection = (cameraForward * moveInput.y + cameraRight * moveInput.x).normalized;
        }
        else
        {
            // If not moving, dash in the direction player is facing
            dashDirection = transform.forward;
        }

        dashDirection.y = 0;
        dashDirection.Normalize();

        yield return new WaitForSeconds(dashDuration);
        
        isDashing = false;
    }
    
    public void OnAttack(InputAction.CallbackContext context)
    {
        if (!context.started) return;
        if (isAttacking) return;

        Attack();
    }

    void Attack()
    {
        isAttacking = true;
        if(lockOnScript != null && lockOnScript.IsLockedOn())
        {
            Transform target = lockOnScript.GetCurrentTarget();
            if (target != null)
            {
                Vector3 direction = target.position - transform.position;
                direction.y = 0; // Keep rotation on horizontal plane
                if(direction.magnitude > 0.1f){
                    transform.rotation = Quaternion.LookRotation(direction);
                }
            }
        }

        if (animator != null)
        {
            animator.SetTrigger("attack");
            animator.SetBool("isMoving", false); // Stop movement animation
        }

        // Wait for attack animation to finish
        StartCoroutine(AttackCoroutine());
    }

    IEnumerator AttackCoroutine()
    {
        // Wait a bit for the attack animation to reach the hit point
        yield return new WaitForSeconds(0.3f);
        
        // Detect and damage enemies in range
        DealDamageToEnemies();
        
        // Wait for rest of attack animation to finish
        yield return new WaitForSeconds(attackCooldown - 0.3f);

        isAttacking = false;
    }

    void DealDamageToEnemies()
    {
        // Find all colliders in attack range on enemy layer
        Collider[] hitEnemies = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);

        foreach (Collider enemy in hitEnemies)
        {
            EnemyAI enemyAi = enemy.GetComponent<EnemyAI>();
            if (enemyAi != null)
            {
                enemyAi.TakeDamage(attackDamage);
            }
        }
    }

    // Visual debugging
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }

        // Visualize attack range
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}