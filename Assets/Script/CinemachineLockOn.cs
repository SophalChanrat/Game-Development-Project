using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// God of War 2018 style lock-on system
/// Camera stays in normal mode - Player rotates to face target
/// Movement becomes strafe-based around the enemy
/// </summary>
public class CinemachineLockOn : MonoBehaviour
{
    [Header("Lock-On Settings")]
    [Tooltip("Maximum distance to detect enemies")]
    public float lockOnRange = 15f;

    [Tooltip("Layer mask for enemies")]
    public LayerMask enemyLayer;

    [Tooltip("Field of view angle for initial lock-on")]
    public float lockOnFOV = 90f;

    [Tooltip("How fast player rotates to face target")]
    public float rotationSpeed = 10f;

    [Header("Camera Settings (Optional)")]
    [Tooltip("Gently recenter camera toward target")]
    public bool recenterCamera = true;

    [Tooltip("Camera recenter speed (subtle)")]
    public float cameraRecenterSpeed = 2f;

    [Tooltip("Main camera transform")]
    public Transform cameraTransform;

    [Header("UI Indicator")]
    [Tooltip("Prefab to show over locked enemy")]
    public GameObject lockOnIndicatorPrefab;

    [Tooltip("Offset from enemy for indicator")]
    public Vector3 indicatorOffset = new Vector3(0, 2, 0);

    // Private variables
    private Transform currentTarget;
    private GameObject lockOnIndicatorInstance;
    private List<Transform> nearbyEnemies = new List<Transform>();
    private bool isLocked = false;

    void Start()
    {
        // Auto-find camera if not assigned
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        if (isLocked && currentTarget != null)
        {
            // Validate target
            if (!IsTargetValid(currentTarget))
            {
                UnlockTarget();
                return;
            }

            // Rotate player to face target
            RotateTowardsTarget();

            // Optional: Gently recenter camera
            if (recenterCamera && cameraTransform != null)
            {
                RecenterCamera();
            }

            // Update indicator
            UpdateLockOnIndicator();
        }
    }

    void RotateTowardsTarget()
    {
        // Calculate direction to target (flat on ground plane)
        Vector3 direction = currentTarget.position - transform.position;
        direction.y = 0;

        if (direction.magnitude > 0.1f)
        {
            // Smoothly rotate player to face target
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    void RecenterCamera()
    {
        // Gently rotate camera toward target (very subtle)
        Vector3 directionToTarget = currentTarget.position - cameraTransform.position;
        Quaternion lookRotation = Quaternion.LookRotation(directionToTarget);

        // Only affect Y rotation (horizontal)
        Vector3 currentEuler = cameraTransform.rotation.eulerAngles;
        Vector3 targetEuler = lookRotation.eulerAngles;

        float smoothY = Mathf.LerpAngle(currentEuler.y, targetEuler.y, cameraRecenterSpeed * Time.deltaTime);
        cameraTransform.rotation = Quaternion.Euler(currentEuler.x, smoothY, currentEuler.z);
    }

    #region Input Handlers

    public void OnLockOn(InputAction.CallbackContext context)
    {
        if (!context.started) return;

        if (isLocked)
        {
            UnlockTarget();
        }
        else
        {
            LockOnToNearestEnemy();
        }
    }

    public void OnSwitchTargetRight(InputAction.CallbackContext context)
    {
        if (!context.started || !isLocked) return;
        SwitchTarget(1);
    }

    public void OnSwitchTargetLeft(InputAction.CallbackContext context)
    {
        if (!context.started || !isLocked) return;
        SwitchTarget(-1);
    }

    #endregion

    #region Lock-On Logic

    void LockOnToNearestEnemy()
    {
        FindNearbyEnemies();

        if (nearbyEnemies.Count == 0)
        {
            Debug.Log("No enemies in range");
            return;
        }

        Transform closestEnemy = GetClosestEnemyToCameraCenter();

        if (closestEnemy != null)
        {
            SetLockedTarget(closestEnemy);
        }
        else
        {
            Debug.Log("No enemy in field of view");
        }
    }

    void SetLockedTarget(Transform target)
    {
        currentTarget = target;
        isLocked = true;

        // Create indicator
        if (lockOnIndicatorPrefab != null && lockOnIndicatorInstance == null)
        {
            lockOnIndicatorInstance = Instantiate(lockOnIndicatorPrefab);
            UpdateLockOnIndicator();
        }

        Debug.Log($"Locked onto: {target.name}");
    }

    void UnlockTarget()
    {
        currentTarget = null;
        isLocked = false;

        // Destroy indicator
        if (lockOnIndicatorInstance != null)
        {
            Destroy(lockOnIndicatorInstance);
            lockOnIndicatorInstance = null;
        }

        Debug.Log("Lock-on released");
    }

    void SwitchTarget(int direction)
    {
        FindNearbyEnemies();

        if (nearbyEnemies.Count <= 1)
        {
            Debug.Log("No other targets");
            return;
        }

        int currentIndex = nearbyEnemies.IndexOf(currentTarget);
        if (currentIndex == -1)
        {
            LockOnToNearestEnemy();
            return;
        }

        int nextIndex = (currentIndex + direction + nearbyEnemies.Count) % nearbyEnemies.Count;
        currentTarget = nearbyEnemies[nextIndex];

        UpdateLockOnIndicator();
        Debug.Log($"Switched to: {currentTarget.name}");
    }

    #endregion

    #region Helper Methods

    void FindNearbyEnemies()
    {
        nearbyEnemies.Clear();

        Collider[] colliders = Physics.OverlapSphere(transform.position, lockOnRange, enemyLayer);

        foreach (Collider col in colliders)
        {
            EnemyAI enemy = col.GetComponent<EnemyAI>();
            if (enemy != null && !enemy.IsDead())
            {
                nearbyEnemies.Add(col.transform);
            }
        }

        // Sort by distance
        nearbyEnemies.Sort((a, b) =>
        {
            float distA = Vector3.Distance(transform.position, a.position);
            float distB = Vector3.Distance(transform.position, b.position);
            return distA.CompareTo(distB);
        });
    }

    Transform GetClosestEnemyToCameraCenter()
    {
        Transform closest = null;
        float closestAngle = float.MaxValue;

        Camera mainCam = Camera.main;
        if (mainCam == null) return nearbyEnemies.FirstOrDefault();

        foreach (Transform enemy in nearbyEnemies)
        {
            Vector3 directionToEnemy = enemy.position - mainCam.transform.position;
            float angle = Vector3.Angle(mainCam.transform.forward, directionToEnemy);

            if (angle < closestAngle && angle < lockOnFOV)
            {
                closestAngle = angle;
                closest = enemy;
            }
        }

        return closest ?? nearbyEnemies.FirstOrDefault();
    }

    bool IsTargetValid(Transform target)
    {
        if (target == null) return false;

        EnemyAI enemy = target.GetComponent<EnemyAI>();
        if (enemy == null || enemy.IsDead()) return false;

        float distance = Vector3.Distance(transform.position, target.position);
        if (distance > lockOnRange * 1.2f) return false;

        return true;
    }

    void UpdateLockOnIndicator()
    {
        if (lockOnIndicatorInstance != null && currentTarget != null)
        {
            lockOnIndicatorInstance.transform.position = currentTarget.position + indicatorOffset;

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                lockOnIndicatorInstance.transform.LookAt(mainCam.transform);
            }
        }
    }

    #endregion

    #region Public Getters

    public bool IsLockedOn() => isLocked;
    public Transform GetCurrentTarget() => currentTarget;

    #endregion

    #region Gizmos

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, lockOnRange);

        if (isLocked && currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.position);
            Gizmos.DrawWireSphere(currentTarget.position, 1f);
        }
    }

    #endregion
}