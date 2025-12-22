using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class AnimalMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2.5f;
    public float turnSpeed = 6f;
    public float wanderRadius = 6f;
    public float changeDirectionTime = 3f;

    [Header("Ground")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.3f;
    public float gravity = -9.81f;

    private CharacterController controller;
    private Vector3 moveDirection;
    private Vector3 velocity;
    private float directionTimer;
    private bool isTrapped = true;

    private Animator animator;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        ChooseNewDirection();
    }

    void Update()
    {
        if (isTrapped)
            return;

        ApplyGravity();
        Wander();
        Move();
    }

    public void Release()
    {
        isTrapped = false;
        ChooseNewDirection();
    }

    void Wander()
    {
        directionTimer -= Time.deltaTime;

        if (directionTimer <= 0f)
            ChooseNewDirection();
    }

    void ChooseNewDirection()
    {
        Vector2 random = Random.insideUnitCircle * wanderRadius;
        moveDirection = new Vector3(random.x, 0, random.y).normalized;
        directionTimer = changeDirectionTime;
    }

    void Move()
    {
        if (moveDirection.sqrMagnitude < 0.01f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            turnSpeed * Time.deltaTime
        );
        animator.SetFloat("Speed", moveSpeed);
        Vector3 motion = moveDirection * moveSpeed;
        motion.y = velocity.y;
        controller.Move(motion * Time.deltaTime);
    }

    void ApplyGravity()
    {
        bool grounded = Physics.Raycast(
            transform.position + Vector3.up * 0.1f,
            Vector3.down,
            groundCheckDistance,
            groundLayer
        );

        if (grounded && velocity.y < 0)
            velocity.y = -2f;
        else
            velocity.y += gravity * Time.deltaTime;
    }
}
