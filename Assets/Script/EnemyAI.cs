using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// AI Controller for Goblin enemy with detection, chase, and attack behaviors
/// Requires: NavMeshAgent, EnemyHealth components
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyHealth))]
public class EnemyAI : MonoBehaviour
{
    private enum AIState { Idle, Patrol, Chase, Attack, Dead }
    [SerializeField] private AIState currentState = AIState.Idle;

    [Header("Detection Settings")]
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float loseTargetRange = 15f; // Stop chasing if player gets too far
    [SerializeField] private LayerMask detectionLayers;
    [SerializeField] private bool drawGizmos = true;

    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float rotationSpeed = 5f;

    [Header("Attack Settings")]
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private GameObject attackHitbox; // Optional: Enable/disable attack hitbox
    
    [Header("Patrol Settings (Optional)")]
    [SerializeField] private bool enablePatrol = false;
    [SerializeField] private float patrolRadius = 10f;
    [SerializeField] private float patrolWaitTime = 2f;
    private Vector3 patrolStartPosition;
    private float patrolWaitTimer = 0f;

    [Header("References")]
    [SerializeField] private Transform player;
    private NavMeshAgent agent;
    private EnemyHealth enemyHealth;
    private Animator animator;
    
    private float lastAttackTime = -999f;
    private bool isAttacking = false;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        enemyHealth = GetComponent<EnemyHealth>();
        animator = GetComponent<Animator>();
        
        patrolStartPosition = transform.position;
        
        // Setup NavMeshAgent
        agent.speed = walkSpeed;
        agent.stoppingDistance = attackRange * 0.8f;

        // Find player if not assigned
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }

        // Subscribe to death event
        if (enemyHealth != null)
        {
            enemyHealth.OnDeath.AddListener(OnDeath);
        }
    }

    private void Update()
    {
        if (enemyHealth.IsDead() || currentState == AIState.Dead)
        {
            return;
        }

        // Update AI based on current state
        switch (currentState)
        {
            case AIState.Idle:
                UpdateIdleState();
                break;
            case AIState.Patrol:
                UpdatePatrolState();
                break;
            case AIState.Chase:
                UpdateChaseState();
                break;
            case AIState.Attack:
                UpdateAttackState();
                break;
        }

        // Update animator
        UpdateAnimator();
    }

    private void UpdateIdleState()
    {
        // Check for player in detection range
        if (CanSeePlayer())
        {
            ChangeState(AIState.Chase);
            return;
        }

        // Switch to patrol if enabled
        if (enablePatrol)
        {
            patrolWaitTimer += Time.deltaTime;
            if (patrolWaitTimer >= patrolWaitTime)
            {
                ChangeState(AIState.Patrol);
                patrolWaitTimer = 0f;
            }
        }
    }

    private void UpdatePatrolState()
    {
        // Check for player
        if (CanSeePlayer())
        {
            ChangeState(AIState.Chase);
            return;
        }

        // Check if reached destination
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            ChangeState(AIState.Idle);
            return;
        }

        // If no destination, set a new patrol point
        if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
        {
            SetRandomPatrolPoint();
        }
    }

    private void UpdateChaseState()
    {
        if (player == null)
        {
            ChangeState(AIState.Idle);
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Check if player is too far
        if (distanceToPlayer > loseTargetRange)
        {
            ChangeState(enablePatrol ? AIState.Patrol : AIState.Idle);
            return;
        }

        // Check if in attack range
        if (distanceToPlayer <= attackRange)
        {
            ChangeState(AIState.Attack);
            return;
        }

        // Chase the player
        agent.SetDestination(player.position);
    }

    private void UpdateAttackState()
    {
        if (player == null)
        {
            ChangeState(AIState.Idle);
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Check if player moved out of attack range
        if (distanceToPlayer > attackRange * 1.2f)
        {
            ChangeState(AIState.Chase);
            return;
        }

        // Stop moving
        agent.ResetPath();

        // Look at player
        Vector3 lookDirection = (player.position - transform.position).normalized;
        lookDirection.y = 0; // Keep rotation on Y axis only
        if (lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }

        // Attack if cooldown is ready
        if (Time.time >= lastAttackTime + attackCooldown && !isAttacking)
        {
            PerformAttack();
        }
    }

    private void ChangeState(AIState newState)
    {
        if (currentState == newState) return;

        // Exit current state
        switch (currentState)
        {
            case AIState.Attack:
                isAttacking = false;
                break;
        }

        // Enter new state
        currentState = newState;

        switch (newState)
        {
            case AIState.Idle:
                agent.ResetPath();
                agent.speed = walkSpeed;
                break;
            case AIState.Patrol:
                agent.speed = walkSpeed;
                SetRandomPatrolPoint();
                break;
            case AIState.Chase:
                agent.speed = chaseSpeed;
                break;
            case AIState.Attack:
                agent.ResetPath();
                break;
        }

        Debug.Log($"{gameObject.name} changed state to: {newState}");
    }

    private bool CanSeePlayer()
    {
        if (player == null) return false;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        if (distanceToPlayer <= detectionRange)
        {
            // Optional: Add line of sight check using raycast
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            
            if (Physics.Raycast(transform.position + Vector3.up, directionToPlayer, out RaycastHit hit, detectionRange, detectionLayers))
            {
                if (hit.transform == player)
                {
                    return true;
                }
            }
            
            // If no layer mask, just use distance
            if (detectionLayers.value == 0)
            {
                return true;
            }
        }

        return false;
    }

    private void SetRandomPatrolPoint()
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += patrolStartPosition;
        randomDirection.y = transform.position.y; // Keep same height

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    private void PerformAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        // Trigger attack animation
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        // The actual damage dealing will be called from animation event or timer
        Invoke(nameof(DealDamage), 0.5f); // Delay to sync with animation
    }

    // Called during attack animation or after delay
    private void DealDamage()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        if (distanceToPlayer <= attackRange)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
                Debug.Log($"{gameObject.name} damaged player for {attackDamage}!");
            }
        }

        isAttacking = false;
    }

    private void OnDeath()
    {
        currentState = AIState.Dead;
        agent.enabled = false;
        
        if (animator != null)
        {
            animator.SetTrigger("Death");
        }
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        // Update movement speed for blend tree
        float speed = agent.velocity.magnitude;
        animator.SetFloat("Speed", speed);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        // Detection range (yellow)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Attack range (red)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Lose target range (gray)
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, loseTargetRange);

        // Patrol radius (blue)
        if (enablePatrol)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(patrolStartPosition, patrolRadius);
        }

        // Line to player
        if (player != null)
        {
            Gizmos.color = CanSeePlayer() ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position + Vector3.up, player.position + Vector3.up);
        }
    }
}
