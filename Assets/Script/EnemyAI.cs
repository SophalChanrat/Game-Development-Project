using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("Components")]
    public NavMeshAgent agent;
    public Animator animator;
    private Transform player;

    [Header("Health")]
    public float health = 100f;

    [Header("Detection")]
    public float detectionRange = 10f;
    public float attackRange = 2.5f;

    [Header("Combat")]
    public float attackDamage = 30f;
    public float attackCooldown = 2f;
    [Tooltip("Time in attack animation when damage is dealt")]
    public float attackHitTiming = 0.5f;
    private float nextAttackTime = 0f;
    private bool isAttacking = false;
    private bool isDead = false;

    [Header("Movement")]
    public float chaseSpeed = 3.5f;
    public float stoppingDistance = 2f;


    // States
    private enum State { Idle, Chasing, Attacking, Dead }
    private State currentState = State.Idle;

    // Animation parameter names
    private const string ANIM_SPEED = "Speed";
    private const string ANIM_ATTACK = "Attack";
    private const string ANIM_DEATH = "Death";

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // Find player
        GameObject playerObj = GameObject.Find("PlayerObj");
        if (playerObj == null)
            playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
            playerObj = GameObject.Find("Player");

        if (playerObj != null)
            player = playerObj.transform;

        // Ensure health is set
        if (health <= 0)
            health = 100f;

        // Configure NavMeshAgent
        if (agent != null)
        {
            agent.speed = chaseSpeed;
            agent.stoppingDistance = stoppingDistance;
            agent.updatePosition = true;
            agent.updateRotation = false;
            agent.baseOffset = 0f;
        }

        // Disable root motion on animator to prevent floating
        if (animator != null)
        {
            animator.applyRootMotion = false;
        }
    }

    void Start()
    {
        currentState = State.Idle;
        isDead = false;
    }

    void Update()
    {
        if (isDead || player == null)
            return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // State machine
        switch (currentState)
        {
            case State.Idle:
                CheckForPlayer(distanceToPlayer);
                break;

            case State.Chasing:
                ChasePlayer(distanceToPlayer);
                break;

            case State.Attacking:
                AttackPlayer(distanceToPlayer);
                break;
        }

        UpdateAnimator();
    }

    void CheckForPlayer(float distance)
    {
        if (distance <= detectionRange)
        {
            currentState = State.Chasing;
        }
    }

    void ChasePlayer(float distance)
    {
        if (distance > detectionRange * 1.5f)
        {
            currentState = State.Idle;
            if (agent != null && agent.enabled)
                agent.SetDestination(transform.position);
            return;
        }

        // Move towards player
        if (agent != null && agent.enabled)
        {
            agent.SetDestination(player.position);
        }

        // Look at player
        Vector3 lookDirection = player.position - transform.position;
        lookDirection.y = 0;
        if (lookDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5f * Time.deltaTime);
        }

        // Switch to attack if close enough
        if (distance <= attackRange)
        {
            currentState = State.Attacking;
            if (agent != null && agent.enabled)
                agent.SetDestination(transform.position);
        }
    }

    void AttackPlayer(float distance)
    {
        // If player moves away, chase again
        if (distance > attackRange * 1.5f)
        {
            currentState = State.Chasing;
            return;
        }

        // Look at player
        Vector3 lookDirection = player.position - transform.position;
        lookDirection.y = 0;
        if (lookDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
        }

        // Attack if cooldown is ready
        if (Time.time >= nextAttackTime && !isAttacking)
        {
            StartCoroutine(PerformAttack());
        }
    }

    IEnumerator PerformAttack()
    {
        isAttacking = true;

        if (animator != null)
        {
            animator.SetTrigger(ANIM_ATTACK);
        }

        yield return new WaitForSeconds(attackHitTiming);

        if (!isDead)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            if (distanceToPlayer <= attackRange)
            {
                PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(attackDamage);
                }
            }
        }

        yield return new WaitForSeconds(1f - attackHitTiming);

        nextAttackTime = Time.time + attackCooldown;
        isAttacking = false;
    }

    public void OnAttackHit()
    {
        if (isDead || player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer <= attackRange)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }
        }
    }

    void UpdateAnimator()
    {
        if (animator == null || isDead)
            return;

        float speed = 0f;

        if (agent != null && agent.enabled)
        {
            speed = agent.velocity.magnitude / agent.speed;
            speed = Mathf.Clamp01(speed);
        }

        if (currentState == State.Attacking)
        {
            speed = 0f;
        }

        animator.SetFloat(ANIM_SPEED, speed);
    }

    public void TakeDamage(int damage)
    {
        if (isDead)
            return;

        health -= damage;

        if (currentState == State.Idle)
        {
            currentState = State.Chasing;
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

        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        StopAllCoroutines();

        if (animator != null)
        {
            animator.SetFloat(ANIM_SPEED, 0);
            animator.ResetTrigger(ANIM_ATTACK);
            animator.SetTrigger(ANIM_DEATH);
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        Destroy(gameObject, 3f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
