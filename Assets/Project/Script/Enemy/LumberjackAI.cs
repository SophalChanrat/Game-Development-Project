using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Lumberjack enemy AI - targets and attacks trees instead of the player
/// Can also fight back if attacked
/// </summary>
public class LumberjackAI : MonoBehaviour
{
    [Header("Components")]
    public NavMeshAgent agent;
    public Animator animator;

    [Header("Health")]
    public float health = 100f;

    [Header("Tree Detection")]
    [Tooltip("Range to detect trees")]
    public float treeDetectionRange = 15f;

    [Tooltip("Range to start attacking tree (must be close!)")]
    public float treeAttackRange = 1.5f;

    [Tooltip("Which layers contain trees")]
    public LayerMask treeLayer;

    [Header("Combat")]
    [Tooltip("Damage dealt to trees per attack (ignored, uses chop system)")]
    public float treeDamage = 15f;

    [Tooltip("Time between attacks on trees")]
    public float attackCooldown = 2f;

    [Tooltip("Time in attack animation when damage is dealt")]
    public float attackHitTiming = 0.5f;

    [Header("Self Defense")]
    [Tooltip("If true, lumberjack will fight back when attacked")]
    public bool canFightBack = true;

    [Tooltip("Damage dealt to player if fighting back")]
    public float playerDamage = 20f;

    [Tooltip("Range to attack player")]
    public float playerAttackRange = 2f;

    [Tooltip("How long to chase player before returning to trees")]
    public float chaseTimeout = 10f;

    [Header("Movement")]
    public float moveSpeed = 3.5f;
    public float stoppingDistance = 0f;

    [Header("Patrol (when no trees nearby)")]
    [Tooltip("Scan for player when no trees found")]
    public bool scanForPlayer = true;

    [Tooltip("Range to detect player when no trees")]
    public float playerDetectionRange = 20f;

    [Tooltip("Wander around if no trees or player found")]
    public bool shouldWander = true;
    public float wanderRadius = 10f;
    public float wanderWaitTime = 3f;

    [Header("Debug")]
    [Tooltip("Show detailed debug logs")]
    public bool showDebugLogs = true;

    // State machine
    private enum State { Idle, SeekingTree, ChoppingTree, ChasingPlayer, AttackingPlayer, Dead }
    private State currentState = State.Idle;

    // Target references
    private TreeHealth currentTargetTree;
    private Transform player;
    private float nextAttackTime = 0f;
    private bool isAttacking = false;
    private bool isDead = false;

    // Chase timer
    private float chaseTimer = 0f;
    private bool hasNotifiedMusicManager = false; // Track if we've notified music manager

    // Debug timer
    private float debugTimer = 0f;

    // Wander timer
    private float wanderTimer = 0f;
    private bool isWandering = false;

    // Animation parameter names
    private const string ANIM_SPEED = "Speed";
    private const string ANIM_ATTACK = "Attack";
    private const string ANIM_DEATH = "Death";

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        // Validate NavMeshAgent
        if (agent == null)
        {
            enabled = false;
            return;
        }

        // Configure NavMeshAgent - LET IT HANDLE ROTATION WHILE MOVING!
        agent.speed = moveSpeed;
        agent.stoppingDistance = stoppingDistance;
        agent.updateRotation = true;  // NavMesh handles rotation while moving
        agent.autoBraking = true;
        agent.autoRepath = true;
        agent.angularSpeed = 120f; // Smooth rotation speed


        // Disable root motion on animator
        if (animator != null)
        {
            animator.applyRootMotion = false;
        }
    }

    void Start()
    {
        currentState = State.Idle;

    }

    void Update()
    {
        if (isDead) return;

        // Debug info every 2 seconds
        if (showDebugLogs)
        {
            debugTimer += Time.deltaTime;
            if (debugTimer >= 2f)
            {
                debugTimer = 0f;
            }
        }

        // State machine
        switch (currentState)
        {
            case State.Idle:
                HandleIdle();
                break;
            case State.SeekingTree:
                HandleSeekingTree();
                break;
            case State.ChoppingTree:
                HandleChoppingTree();
                break;
            case State.ChasingPlayer:
                HandleChasingPlayer();
                break;
            case State.AttackingPlayer:
                HandleAttackingPlayer();
                break;
        }

        UpdateAnimator();
    }

    #region State Handlers

    void HandleIdle()
    {
        // Priority 1: Look for trees (primary job)
        TreeHealth nearestTree = FindNearestTree();

        if (nearestTree != null)
        {
            currentTargetTree = nearestTree;
            currentState = State.SeekingTree;
            isWandering = false;

            if (showDebugLogs)
            {
                Debug.Log($"[LUMBERJACK] {gameObject.name} found tree: {nearestTree.gameObject.name}, switching to SeekingTree state");
            }
            return;
        }

        // Priority 2: Scan for player if no trees nearby
        if (scanForPlayer && player != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            if (distanceToPlayer <= playerDetectionRange)
            {
                if (showDebugLogs)
                {
                    Debug.Log($"[LUMBERJACK] {gameObject.name} no trees found, detected player at {distanceToPlayer:F2}m");
                }

                currentState = State.ChasingPlayer;
                chaseTimer = 0f;
                isWandering = false;

                // Notify music manager that combat started
                if (!hasNotifiedMusicManager && MusicManager.Instance != null)
                {
                    MusicManager.Instance.OnEnemyEnterCombat();
                    hasNotifiedMusicManager = true;
                }
                return;
            }
        }

        // Priority 3: Wander around if nothing found
        if (shouldWander)
        {
            wanderTimer += Time.deltaTime;

            // Check if we need to pick a new wander destination
            if (!isWandering || (agent != null && agent.enabled && !agent.pathPending && agent.remainingDistance < 0.5f))
            {
                if (wanderTimer >= wanderWaitTime)
                {
                    PickNewWanderDestination();
                    wanderTimer = 0f;
                }
            }
        }
    }

    void HandleSeekingTree()
    {
        // Check if tree still exists and is valid
        if (currentTargetTree == null || currentTargetTree.IsDestroyed())
        {
            if (showDebugLogs)
            {
                Debug.Log($"[LUMBERJACK] {gameObject.name} lost target tree, returning to Idle");
            }

            currentTargetTree = null;
            currentState = State.Idle;
            return;
        }

        // Move toward tree - NavMesh handles rotation automatically!
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.SetDestination(currentTargetTree.transform.position);

            // Check if path is valid
            if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                if (showDebugLogs)
                {
                    Debug.LogWarning($"[LUMBERJACK] {gameObject.name} has invalid path to tree!");
                }
            }
        }

        // Check distance - use shorter distance for better positioning
        float distanceToTree = Vector3.Distance(transform.position, currentTargetTree.transform.position);

        // Switch to chopping when VERY close
        if (distanceToTree <= treeAttackRange)
        {
            // Start chopping
            currentState = State.ChoppingTree;
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.ResetPath(); // Stop moving
                agent.velocity = Vector3.zero; // Stop immediately
            }

            if (showDebugLogs)
            {
                Debug.Log($"[LUMBERJACK] {gameObject.name} reached tree (distance: {distanceToTree:F2}m), starting to chop!");
            }
        }
    }

    void HandleChoppingTree()
    {
        // Check if tree still exists
        if (currentTargetTree == null || currentTargetTree.IsDestroyed())
        {
            currentTargetTree = null;
            currentState = State.Idle;

            if (showDebugLogs)
            {
                Debug.Log($"[LUMBERJACK] {gameObject.name} tree destroyed, looking for new tree");
            }
            return;
        }

        // Manually rotate to face tree when stationary (not moving)
        LookAtTarget(currentTargetTree.transform);

        // Check if still in range - use stricter distance
        float distanceToTree = Vector3.Distance(transform.position, currentTargetTree.transform.position);

        if (distanceToTree > treeAttackRange * 1.3f)
        {
            // Too far, move closer again
            currentState = State.SeekingTree;

            if (showDebugLogs)
            {
                Debug.Log($"[LUMBERJACK] {gameObject.name} too far from tree ({distanceToTree:F2}m), moving closer");
            }
            return;
        }

        // Attack tree
        if (Time.time >= nextAttackTime && !isAttacking)
        {
            StartCoroutine(AttackTreeCoroutine());
        }
    }

    void HandleChasingPlayer()
    {
        if (player == null)
        {
            currentState = State.Idle;
            return;
        }

        // Update chase timer
        chaseTimer += Time.deltaTime;

        // Check if any trees nearby while chasing - prioritize trees over player
        TreeHealth nearestTree = FindNearestTree();
        if (nearestTree != null && chaseTimer > 2f) // Give some time to chase before switching back
        {
            if (showDebugLogs)
            {
                Debug.Log($"[LUMBERJACK] {gameObject.name} found tree while chasing player, switching back to tree");
            }
            currentTargetTree = nearestTree;
            currentState = State.SeekingTree;
            chaseTimer = 0f;

            // Notify music manager that combat ended (returning to trees)
            if (hasNotifiedMusicManager && MusicManager.Instance != null)
            {
                MusicManager.Instance.OnEnemyExitCombat();
                hasNotifiedMusicManager = false;
            }
            return;
        }

        // Timeout - return to looking for trees
        if (chaseTimer >= chaseTimeout)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[LUMBERJACK] {gameObject.name} chase timeout, returning to idle");
            }
            chaseTimer = 0f;
            currentState = State.Idle;

            // Notify music manager that combat ended (chase timeout)
            if (hasNotifiedMusicManager && MusicManager.Instance != null)
            {
                MusicManager.Instance.OnEnemyExitCombat();
                hasNotifiedMusicManager = false;
            }
            return;
        }

        // Chase player - NavMesh handles rotation automatically!
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.SetDestination(player.position);
        }

        // Check distance
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= playerAttackRange)
        {
            currentState = State.AttackingPlayer;
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.ResetPath();
            }
        }
    }

    void HandleAttackingPlayer()
    {
        if (player == null)
        {
            currentState = State.Idle;
            return;
        }

        // Manually rotate to face player when stationary
        LookAtTarget(player);

        // Check distance
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer > playerAttackRange * 1.5f)
        {
            currentState = State.ChasingPlayer;
            return;
        }

        // Attack player
        if (Time.time >= nextAttackTime && !isAttacking)
        {
            StartCoroutine(AttackPlayerCoroutine());
        }
    }

    #endregion

    #region Attack Coroutines

    IEnumerator AttackTreeCoroutine()
    {
        isAttacking = true;

        // Make sure we're facing the tree (instant snap for attack)
        if (currentTargetTree != null)
        {
            Vector3 direction = currentTargetTree.transform.position - transform.position;
            direction.y = 0;
            if (direction.magnitude > 0.1f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        // Trigger attack animation
        if (animator != null)
        {
            animator.SetTrigger(ANIM_ATTACK);
        }

        // Wait for hit timing
        yield return new WaitForSeconds(attackHitTiming);

        // Deal damage to tree (one chop)
        if (!isDead && currentTargetTree != null && !currentTargetTree.IsDestroyed())
        {
            float distance = Vector3.Distance(transform.position, currentTargetTree.transform.position);

            // Only chop if still close enough
            if (distance <= treeAttackRange * 1.2f)
            {
                currentTargetTree.Chop();
                Debug.Log($"[LUMBERJACK] {gameObject.name} chopped tree! {currentTargetTree.GetCurrentChops()}/{currentTargetTree.GetChopsRequired()}");
            }
            else
            {
                if (showDebugLogs)
                {
                    Debug.LogWarning($"[LUMBERJACK] {gameObject.name} attack missed - too far ({distance:F2}m)");
                }
            }
        }

        // Wait for animation to finish
        yield return new WaitForSeconds(1f - attackHitTiming);

        nextAttackTime = Time.time + attackCooldown;
        isAttacking = false;
    }

    IEnumerator AttackPlayerCoroutine()
    {
        isAttacking = true;

        // Trigger attack animation
        if (animator != null)
        {
            animator.SetTrigger(ANIM_ATTACK);
        }

        // Wait for hit timing
        yield return new WaitForSeconds(attackHitTiming);

        // Deal damage to player
        if (!isDead && player != null)
        {
            float distance = Vector3.Distance(transform.position, player.position);

            if (distance <= playerAttackRange)
            {
                PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(playerDamage);
                    Debug.Log($"[LUMBERJACK] {gameObject.name} attacked player for {playerDamage} damage!");
                }
            }
        }

        // Wait for animation to finish
        yield return new WaitForSeconds(1f - attackHitTiming);

        nextAttackTime = Time.time + attackCooldown;
        isAttacking = false;
    }

    #endregion

    #region Helper Methods

    TreeHealth FindNearestTree()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, treeDetectionRange, treeLayer);

        TreeHealth nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider col in colliders)
        {
            TreeHealth tree = col.GetComponent<TreeHealth>();

            if (tree != null && !tree.IsDestroyed())
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);

                if (distance < nearestDistance)
                {
                    nearest = tree;
                    nearestDistance = distance;
                }
            }
        }

        return nearest;
    }

    void PickNewWanderDestination()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        // Pick random point to wander to
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += transform.position;
        randomDirection.y = transform.position.y; // Keep same Y level

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            isWandering = true;
        }
    }

    void LookAtTarget(Transform target)
    {
        if (target == null) return;

        Vector3 direction = target.position - transform.position;
        direction.y = 0;

        if (direction.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            // Slower rotation when stationary for smoother look
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
        }
    }

    void UpdateAnimator()
    {
        if (animator == null || isDead) return;

        float speed = 0f;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            speed = agent.velocity.magnitude / agent.speed;
            speed = Mathf.Clamp01(speed);
        }

        if (currentState == State.ChoppingTree || currentState == State.AttackingPlayer)
        {
            speed = 0f;
        }

        animator.SetFloat(ANIM_SPEED, speed);
    }

    #endregion

    #region Damage & Death

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        health -= damage;

        Debug.Log($"[LUMBERJACK] {gameObject.name} took {damage} damage! Health: {health}");

        // Fight back if enabled
        if (canFightBack && player != null)
        {
            // Switch to attacking player
            currentState = State.ChasingPlayer;
            chaseTimer = 0f;
        }

        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;
        currentState = State.Dead;

        // Notify music manager that combat ended (lumberjack died)
        if (hasNotifiedMusicManager && MusicManager.Instance != null)
        {
            MusicManager.Instance.OnEnemyExitCombat();
            hasNotifiedMusicManager = false;
        }

        Debug.Log($"[LUMBERJACK] {gameObject.name} died!");

        // Stop agent
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        StopAllCoroutines();

        // Death animation
        if (animator != null)
        {
            animator.SetFloat(ANIM_SPEED, 0);
            animator.ResetTrigger(ANIM_ATTACK);
            animator.SetTrigger(ANIM_DEATH);
        }

        // Disable collider
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        // Destroy after delay
        Destroy(gameObject, 1f);
    }

    public bool IsDead()
    {
        return isDead;
    }

    #endregion

    #region Gizmos

    void OnDrawGizmosSelected()
    {
        // Tree detection range
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, treeDetectionRange);

        // Tree attack range (SMALLER - shows closer range)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, treeAttackRange);

        // Player detection range when no trees
        if (scanForPlayer)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f); // Orange
            Gizmos.DrawWireSphere(transform.position, playerDetectionRange);
        }

        // Player attack range
        if (canFightBack)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, playerAttackRange);
        }

        // Draw line to current target
        if (currentTargetTree != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, currentTargetTree.transform.position);
            Gizmos.DrawSphere(currentTargetTree.transform.position, 0.5f);

            // Show distance to target
            if (Application.isPlaying)
            {
                float distance = Vector3.Distance(transform.position, currentTargetTree.transform.position);
                UnityEditor.Handles.Label(
                    (transform.position + currentTargetTree.transform.position) / 2f,
                    $"Distance: {distance:F2}m\nChops: {currentTargetTree.GetCurrentChops()}/{currentTargetTree.GetChopsRequired()}"
                );
            }
        }

        // Show agent path
        if (Application.isPlaying && agent != null && agent.hasPath)
        {
            Gizmos.color = Color.magenta;
            Vector3[] corners = agent.path.corners;
            for (int i = 0; i < corners.Length - 1; i++)
            {
                Gizmos.DrawLine(corners[i], corners[i + 1]);
            }
        }
    }

    #endregion
}