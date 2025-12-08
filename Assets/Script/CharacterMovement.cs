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
    public float attackCooldown = 0.5f;
    private float lastAttackTime = -999f;
    private float attackRange;
    public int attackDamage = 25;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

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

    private void Update()
    {
        CheckGround();
        ApplyGravity();
        MovePlayer();
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
            // Get camera's forward and right directions (flattened to horizontal plane)
            Vector3 cameraForward = camTransform.forward;
            Vector3 cameraRight = camTransform.right;
            cameraForward.y = 0;
            cameraRight.y = 0;
            cameraForward.Normalize();
            cameraRight.Normalize();

            // Calculate movement direction relative to camera
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
            }
        }
        else
        {
            currentSpeed = Mathf.SmoothDamp(currentSpeed, 0f, ref speedSmoothVelocity, speedSmoothTime);
        }

        moveDirection.y = velocity.y;
        controller.Move(moveDirection * Time.deltaTime);
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

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started && isGrounded)
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

        if (Time.time < lastAttackTime + attackCooldown) return;
        lastAttackTime = Time.time;

        if (animator != null)
            animator.SetTrigger("attack");
    }

    void DealDamageToEnemies()
    {
        Collider[] hitEnemies = Physics.OverlapSphere(transform.position, attackRange, groundMask);

        foreach (Collider enemy in hitEnemies)
        {
            EnemyAI enemyAi = enemy.GetComponent<EnemyAI>();
            if (enemyAi != null)
            {
                enemyAi.TakeDamage(attackDamage);
                continue;
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
    }
}