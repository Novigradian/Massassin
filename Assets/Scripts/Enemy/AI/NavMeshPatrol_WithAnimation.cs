using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using Unity.VisualScripting;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyVision))]
[RequireComponent(typeof(Animator))]
public class NavMeshPatrol_WithAnimation : MonoBehaviour
{
    // --- New Enum for Combat Style ---
    public enum CombatType { Melee, Ranged }

    [Header("Combat Settings")]
    [SerializeField] private CombatType combatType;
    [SerializeField] private float attackRange = 2f; // Distance to stop and attack
    [SerializeField] private float timeBetweenAttacks = 1.5f;
    [SerializeField] private float projectileSpawnDelay = 0.2f; // 攻击动画后子弹发射延迟
    [SerializeField] private GameObject projectilePrefab; // Only for Ranged
    [SerializeField] private Transform firePoint; // Where projectiles spawn

    [Header("References")]
    [SerializeField] private Transform[] patrolPoints;
    private EnemyVision vision;
    private NavMeshAgent agent;
    [SerializeField] private Animator animator;

    [Header("Animation Settings")]
    [SerializeField] private bool useRootMotion = true;
    [SerializeField] private string speedParameterName = "Speed"; // Animator参数名称
    [SerializeField] private float animationSpeedMultiplier = 1f; // 动画速度倍增器

    [Header("Patrol Settings")]
    [SerializeField] private float patrolSpeed = 3.5f;
    [SerializeField] private float waitTime = 1f;
    [SerializeField] private float turnSpeed = 90f;
    [SerializeField] private float lookAngle = 60f;

    [Header("Chase Settings")]
    [SerializeField] private float chaseSpeed = 6f;

    // --- State Machine ---
    private enum State { Patrolling, Chasing, Searching, Attacking }
    private State currentState;

    // --- Internal Tracking ---
    private int currentPointIndex = 0;
    private bool isWaiting = false;
    private Coroutine activeLookCoroutine;
    private Vector3 lastKnownPosition;
    private float attackTimer = 0f; // Counts down to next attack

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        vision = GetComponent<EnemyVision>();
        animator = GetComponent<Animator>();

        // 配置NavMeshAgent使用Root Motion
        if (useRootMotion)
        {
            agent.updatePosition = false; // 禁用NavMeshAgent的位置更新
            agent.updateRotation = true;  // 保留旋转控制
        }

        currentState = State.Patrolling;
        agent.speed = patrolSpeed;

        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            agent.SetDestination(patrolPoints[0].position);
        }
    }

    void OnDisable()
    {
        StopAllCoroutines();
        isWaiting = false;
    }

    void OnAnimatorMove()
    {
        // 使用Root Motion时，将Animator的移动应用到NavMeshAgent
        if (useRootMotion && animator != null && agent != null)
        {
            // 应用Root Motion的位置变化
            Vector3 newPosition = animator.rootPosition;
            newPosition.y = agent.nextPosition.y; // 保持NavMesh的Y轴高度
            transform.position = newPosition;
            
            // 同步NavMeshAgent，让它知道当前位置
            agent.nextPosition = newPosition;
        }
    }

    void Update()
    {
        if (attackTimer > 0) attackTimer -= Time.deltaTime;

        // 更新Animator的速度参数
        if (useRootMotion && animator != null && agent != null)
        {
            // 使用desiredVelocity而不是velocity，这样即使位置由动画控制，速度参数也是正确的
            float currentSpeed = agent.desiredVelocity.magnitude;
            animator.SetFloat(speedParameterName, currentSpeed * animationSpeedMultiplier);
        }

        CheckVision();

        switch (currentState)
        {
            case State.Patrolling:
                PatrolUpdate();
                break;
            case State.Chasing:
                ChaseUpdate();
                break;
            case State.Searching:
                SearchUpdate();
                break;
            case State.Attacking:
                AttackUpdate();
                break;
        }
    }

    // --- DECISION MAKING ---
    void CheckVision()
    {
        // 1. If we see the player...
        if (vision.visibleTargets.Count > 0)
        {
            Transform target = vision.visibleTargets[0];

            if (target == null) return;

            lastKnownPosition = target.position;

            float distanceToTarget = Vector3.Distance(transform.position, target.position);

            // LOGIC SPLIT: MELEE VS RANGED
            if (combatType == CombatType.Melee)
            {
                // Melee: Only attack if very close
                if (distanceToTarget <= attackRange)
                {
                    StartAttacking();
                }
                else
                {
                    StartChase();
                }
            }
            else // Ranged
            {
                // Ranged: Attack if within range AND we can see them
                if (distanceToTarget <= attackRange)
                {
                    StartAttacking();
                }
                else
                {
                    StartChase();
                }
            }
        }
        // 2. If we LOST the player...
        else 
        {
            if (currentState == State.Chasing || currentState == State.Attacking)
            {
                StartSearching();
            }
        }
    }

    // --- STATE SWAPPING ---
    void StartChase()
    {
        if (currentState == State.Chasing) return; // Optimization

        currentState = State.Chasing;
        agent.isStopped = false; // Make sure we can move
        agent.speed = chaseSpeed;
        StopLooking();
    }

    void StartAttacking()
    {
        if (currentState == State.Attacking) return;

        currentState = State.Attacking;
        agent.isStopped = true; // Stop moving to shoot/hit
        StopLooking();
    }

    void StartSearching()
    {
        if (currentState == State.Searching) return;

        currentState = State.Searching;
        agent.isStopped = false;
        agent.speed = chaseSpeed;
        agent.SetDestination(lastKnownPosition);
    }

    void ReturnToPatrol()
    {
        currentState = State.Patrolling;
        agent.isStopped = false;
        agent.speed = patrolSpeed;
        if (patrolPoints.Length > 0)
        {
            agent.SetDestination(patrolPoints[currentPointIndex].position);
        }
    }

    void StopLooking()
    {
        if (activeLookCoroutine != null) StopCoroutine(activeLookCoroutine);
        isWaiting = false;
    }

    // --- BEHAVIORS ---

    void ChaseUpdate()
    {
        if (vision.visibleTargets.Count > 0)
        {
            agent.SetDestination(vision.visibleTargets[0].position);
        }
    }

    void AttackUpdate()
    {
        if (vision.visibleTargets.Count > 0)
        {
            Transform target = vision.visibleTargets[0];

            if (target == null) return;

            // 1. Calculate direction to target
            Vector3 dirToTarget = (target.position - transform.position).normalized;
            dirToTarget.y = 0; // Keep it flat so they don't look up/down

            // 2. Rotate to face the target
            // Increased speed (10f) so they aim faster
            Quaternion lookRot = Quaternion.LookRotation(dirToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 10f);

            // 3. Check Distance (Existing logic)
            float distance = Vector3.Distance(transform.position, target.position);
            if (distance > attackRange)
            {
                StartChase();
                return;
            }

            // 4. NEW: Check Angle before shooting
            // We calculate the angle between where we are looking (transform.forward)
            // and where the player is (dirToTarget).
            float angleToTarget = Vector3.Angle(transform.forward, dirToTarget);

            // Only shoot if we are facing the player (within 10 degrees)
            if (angleToTarget < 10f)
            {
                if (attackTimer <= 0)
                {
                    PerformAttack(target);
                    attackTimer = timeBetweenAttacks;
                }
            }
        }
    }

    void PerformAttack(Transform target)
    {
        // Trigger attack animation
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        if (combatType == CombatType.Melee)
        {
            Debug.Log("Melee Attack! (Add animation trigger here)");
            
            if (target.gameObject.CompareTag("Mask"))
            {
                Debug.Log("You Lose!");
                // other.GetComponent<PlayerHealth>().TakeDamage(10);

                GameManager.Instance.HandleLose();
            }

            if (target.gameObject.CompareTag("Enemy"))
            {
                Possessable p = target.gameObject.GetComponent<Possessable>();
                if (p == null) return;

                if (p.isPossessed)
                {
                    Debug.Log("Hit Possessed Enemy.");  
                    p.PossessedDie();
                }

                if (p.isMinioned)
                {
                    p.gameObject.GetComponent<MinionAI>().Die();
                }
            }

            
        }
        else // Ranged
        {
            Debug.Log("Pew Pew! (Shooting)");
            // Delay projectile spawning
            StartCoroutine(FireProjectileWithDelay(projectileSpawnDelay));
        }
    }

    IEnumerator FireProjectileWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (projectilePrefab != null && firePoint != null)
        {
            Instantiate(projectilePrefab, firePoint.position, transform.rotation);
        }
    }

    // --- SEARCH & PATROL (Existing Logic) ---

    void SearchUpdate()
    {
        if (isWaiting) return;

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            activeLookCoroutine = StartCoroutine(LookAroundRoutine(true));
        }
    }

    void PatrolUpdate()
    {
        if (isWaiting) return;
        if (patrolPoints.Length == 0) return;

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            activeLookCoroutine = StartCoroutine(LookAroundRoutine(false));
        }
    }

    IEnumerator LookAroundRoutine(bool isSearching)
    {
        isWaiting = true;
        Quaternion originalRotation = transform.rotation;
        Quaternion lookLeft = originalRotation * Quaternion.Euler(0, -lookAngle, 0);
        Quaternion lookRight = originalRotation * Quaternion.Euler(0, lookAngle, 0);

        yield return StartCoroutine(RotateToTarget(lookLeft));
        yield return new WaitForSeconds(waitTime);
        yield return StartCoroutine(RotateToTarget(lookRight));
        yield return new WaitForSeconds(waitTime);
        yield return StartCoroutine(RotateToTarget(originalRotation));

        isWaiting = false;

        if (isSearching) ReturnToPatrol();
        else GoToNextPoint();
    }

    IEnumerator RotateToTarget(Quaternion targetRotation)
    {
        while (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            yield return null;
        }
    }

    void GoToNextPoint()
    {
        currentPointIndex = (currentPointIndex + 1) % patrolPoints.Length;
        agent.SetDestination(patrolPoints[currentPointIndex].position);
    }

    public void RestartPatrol()
    {
        StopAllCoroutines();
        isWaiting = false;

        if (patrolPoints != null && patrolPoints.Length > 0)
            agent.SetDestination(patrolPoints[currentPointIndex].position);
    }
}