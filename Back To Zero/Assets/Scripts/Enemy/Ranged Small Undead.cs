using System.Collections;
using System.Collections.Generic;
using Pathfinding;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class RangedSmallUndead : MonoBehaviour
{
    [Header("Targeting")]
    [SerializeField] private Transform player;
    [SerializeField] private float aggroRange = 8f;
    [SerializeField, Min(1f)] private float disengageRangeMultiplier = 3f;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float attackRange = 5f;
    [SerializeField] private float attackRangeHysteresis = 0.5f;
    [SerializeField] private float dashTriggerRange = 3f;
    [SerializeField] private bool dashRequiresLineOfSight = false;

    [Header("Combat")]
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float projectileDamage = 10f;
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private float projectileRange = 10f;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private string firePointChildName = "SpellTransform";

    [Header("Mobility")]
    [SerializeField] private DashAbility dashAbility;
    [SerializeField] private float dashCooldown = 3f;
    [SerializeField] private bool dashGrantsInvincibility = true;
    [SerializeField] private float fallbackDashPower = 8f;
    [SerializeField] private float fallbackDashDuration = 0.25f;

    [Header("Visuals")]
    [SerializeField] private TrailRenderer dashTrail;

    private Transform firePoint;
    private Vector3 startingPosition;
    private float nextAttackTime;
    private float nextDashTime;
    private bool isAggro;
    private bool isDashing;
    private bool isInAttackRange;
    private Rigidbody2D rb;
    private Health enemyHealth;
    private AIPath aiPath;
    private AIDestinationSetter destinationSetter;
    private Collider2D[] colliders;
    private readonly List<Collider2D> disabledDashColliders = new();
    private float dashPower;
    private float dashDuration;
    private Coroutine dashRoutine;
    private bool isBound;
    private float boundUntilTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        enemyHealth = GetComponent<Health>();
        aiPath = GetComponent<AIPath>();
        destinationSetter = GetComponent<AIDestinationSetter>();
        colliders = GetComponentsInChildren<Collider2D>();
        if (dashTrail == null)
        {
            dashTrail = GetComponentInChildren<TrailRenderer>();
        }

        if (destinationSetter != null)
        {
            destinationSetter.enabled = false;
        }

        if (aiPath != null)
        {
            aiPath.canMove = false;
            aiPath.enabled = false;
        }
    }

    private void Start()
    {
        startingPosition = transform.position;
        firePoint = FindChildByName(transform, firePointChildName);

        if (player == null)
        {
            TryResolvePlayerReference();
        }

        dashPower = dashAbility != null ? dashAbility.dashingPower : fallbackDashPower;
        dashDuration = dashAbility != null ? dashAbility.dashingTime : fallbackDashDuration;

        if (dashPower <= 0f)
        {
            dashPower = fallbackDashPower;
        }

        if (dashDuration <= 0f)
        {
            dashDuration = fallbackDashDuration;
        }

        if (dashAbility == null)
        {
            Debug.LogWarning($"[{nameof(RangedSmallUndead)}] DashAbility not assigned. Using fallback dash settings.");
        }
    }

    private void Update()
    {
        if (player == null)
        {
            TryResolvePlayerReference();
            if (player == null) return;
        }

        // Binding: block dash & attacks
        if (isBound)
        {
            if (Time.time >= boundUntilTime)
                OnBoundEnd();
            // Optional: keep facing player
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        float distanceFromStart = Vector3.Distance(transform.position, startingPosition);
        bool hasLineOfSight = HasLineOfSight();
        float effectiveDashRange = Mathf.Max(0.1f, dashTriggerRange);

        if (!isAggro)
        {
            if (distanceToPlayer <= aggroRange && hasLineOfSight)
            {
                isAggro = true;
            }
        }
        else
        {
            if (distanceFromStart > aggroRange * disengageRangeMultiplier)
            {
                isAggro = false;
                StopDashRoutine();
            }
            else
            {
                if (!isDashing && Time.time >= nextDashTime && distanceToPlayer <= effectiveDashRange && (!dashRequiresLineOfSight || hasLineOfSight))
                {
                    StartDashRoutine();
                }

                HandleCombat(distanceToPlayer, hasLineOfSight);
            }
        }

        if (!isAggro && isDashing)
        {
            StopDashRoutine();
        }
    }

    private void HandleCombat(float distanceToPlayer, bool hasLineOfSight)
    {
        if (isDashing) return;
        if (!hasLineOfSight) return;

        if (!isInAttackRange)
        {
            if (distanceToPlayer <= attackRange)
            {
                isInAttackRange = true;
            }
        }
        else if (distanceToPlayer > attackRange + attackRangeHysteresis)
        {
            isInAttackRange = false;
        }

        if (isInAttackRange && Time.time >= nextAttackTime)
        {
            TryFireProjectile();
        }
    }

    private void StartDashRoutine()
    {
        if (dashRoutine != null)
        {
            StopCoroutine(dashRoutine);
        }

        dashRoutine = StartCoroutine(DashRoutine());
    }

    private void TryFireProjectile()
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning($"[{nameof(RangedSmallUndead)}] No projectile prefab assigned.");
            return;
        }

        var origin = firePoint != null ? firePoint : transform;
        Vector3 direction = (player.position - origin.position).normalized;
        if (direction == Vector3.zero) direction = origin.right;

        Quaternion rotation = Quaternion.AngleAxis(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg, Vector3.forward);
        var projectile = Instantiate(projectilePrefab, origin.position, rotation);

        var projComponent = projectile.GetComponent<Projectiles>();
        if (projComponent != null)
        {
            projComponent.Initialize(projectileDamage, projectileSpeed, projectileRange, gameObject);
        }
        else
        {
            var projectileRb = projectile.GetComponent<Rigidbody2D>();
            if (projectileRb != null)
            {
                projectileRb.linearVelocity = (Vector2)direction * projectileSpeed;
            }
            Destroy(projectile, Mathf.Max(0.01f, projectileRange / Mathf.Max(0.01f, projectileSpeed)));
        }

        nextAttackTime = Time.time + attackCooldown;
    }

    private IEnumerator DashRoutine()
    {
        isDashing = true;
        nextDashTime = Time.time + dashCooldown;

        if (enemyHealth != null && dashGrantsInvincibility)
        {
            enemyHealth.isInvincible = true;
        }

        SetPhysicsCollidersEnabled(false);

        Vector2 dashDirection = Random.insideUnitCircle;
        if (dashDirection.sqrMagnitude < 0.001f)
        {
            dashDirection = Vector2.right;
        }
        dashDirection.Normalize();

        if (rb != null)
        {
            rb.linearVelocity = dashDirection * dashPower;
        }

        SetTrailEmitting(true);

        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        if (enemyHealth != null && dashGrantsInvincibility)
        {
            enemyHealth.isInvincible = false;
        }

        SetPhysicsCollidersEnabled(true);
        SetTrailEmitting(false);

        isDashing = false;
        dashRoutine = null;
    }

    private bool HasLineOfSight()
    {
        if (player == null) return false;
        Vector3 direction = (player.position - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, player.position);

        return !Physics2D.Raycast(transform.position, direction, distance, obstacleMask);
    }

    private void TryResolvePlayerReference()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    private void StopDashRoutine()
    {
        if (dashRoutine != null)
        {
            StopCoroutine(dashRoutine);
            dashRoutine = null;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        if (enemyHealth != null && dashGrantsInvincibility)
        {
            enemyHealth.isInvincible = false;
        }

        SetPhysicsCollidersEnabled(true);
        SetTrailEmitting(false);

        isDashing = false;
    }

    private void SetPhysicsCollidersEnabled(bool enabled)
    {
        if (colliders == null) return;

        if (!enabled)
        {
            disabledDashColliders.Clear();
            foreach (var col in colliders)
            {
                if (col == null || col.isTrigger || !col.enabled) continue;
                col.enabled = false;
                disabledDashColliders.Add(col);
            }
        }
        else
        {
            if (disabledDashColliders.Count == 0) return;
            foreach (var col in disabledDashColliders)
            {
                if (col != null)
                {
                    col.enabled = true;
                }
            }
            disabledDashColliders.Clear();
        }
    }

    private static Transform FindChildByName(Transform root, string childName)
    {
        if (root == null || string.IsNullOrEmpty(childName)) return null;
        foreach (var child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName) return child;
        }
        return null;
    }

    private void SetTrailEmitting(bool emitting)
    {
        if (dashTrail == null) return;
        dashTrail.emitting = emitting;
    }

    // Called by BindController via SendMessage
    public void OnBoundStart(float duration)
    {
        isBound = true;
        boundUntilTime = Time.time + duration;
        StopDashRoutine();
        if (rb) rb.linearVelocity = Vector2.zero;
        // Already frozen AIPath by BindController; ensure disabled
        if (aiPath)
        {
            aiPath.isStopped = true;
            aiPath.canMove = false;
        }
    }

    public void OnBoundEnd()
    {
        isBound = false;
        // Restore movement (unless some other effect is stopping it)
        if (aiPath)
        {
            aiPath.isStopped = false;
            aiPath.canMove = true;
        }
    }
}
