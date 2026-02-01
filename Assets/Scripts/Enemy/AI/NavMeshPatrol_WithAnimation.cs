using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyVision))]
public class NavMeshPatrol_WithAnimation : MonoBehaviour
{
    static readonly int _moving = Animator.StringToHash("Moving");
    static readonly int _leftTurn = Animator.StringToHash("LeftTurn");
    static readonly int _rightTurn = Animator.StringToHash("RightTurn");
    static readonly int _attack = Animator.StringToHash("Attack");

    // --- New Enum for Combat Style ---
    public enum CombatType { Melee, Ranged }

    [Header("Combat Settings")]
    [SerializeField] private CombatType combatType;
    [SerializeField] private float attackRange = 2f; // Distance to stop and attack
    [SerializeField] private float timeBetweenAttacks = 1.5f;
    [SerializeField] private GameObject projectilePrefab; // Only for Ranged
    [SerializeField] private Transform firePoint; // Where projectiles spawn

    [Header("References")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private Animator animator;
    private EnemyVision vision;
    private NavMeshAgent agent;

    [Header("Patrol Settings")]
    [SerializeField] private float patrolSpeed = 3.5f;
    [SerializeField] private float waitTime = 1f; // when looking around
    [SerializeField] private float turnSpeed = 90f;
    [SerializeField] private float lookAngle = 60f;

    [Header("Chase Settings")]
    [SerializeField] private float chaseSpeed = 6f;

    // --- State Machine ---
    private enum State { Patrolling, Chasing, Searching, Attacking }
    private State currentState;

    // --- Internal Tracking ---
    private int currentPointIndex;
    private bool isWaiting;
    private Coroutine activeLookCoroutine;
    private Vector3 lastKnownPosition;
    private float attackTimer; // Counts down to next attack

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        vision = GetComponent<EnemyVision>();

        if (animator == null)
            animator = GetComponent<Animator>();

        currentState = State.Patrolling;
        agent.speed = patrolSpeed;
        agent.isStopped = false;

        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            currentPointIndex = 0;
            agent.SetDestination(patrolPoints[0].position);
            if (animator != null) animator.SetBool(_moving, true);
        }
    }

    void Update()
    {
        if (attackTimer > 0) attackTimer -= Time.deltaTime;

        CheckVision();

        // Keep movement animation in sync with movement unless attacking or waiting
        if (animator != null)
        {
            bool isMovingNow = !isWaiting && !agent.isStopped && agent.velocity.sqrMagnitude > 0.01f && currentState != State.Attacking;
            animator.SetBool(_moving, isMovingNow);
        }

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
        if (vision != null && vision.visibleTargets.Count > 0)
        {
            Transform target = vision.visibleTargets[0];
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
        if (animator != null) animator.SetBool(_moving, true);
    }

    void StartAttacking()
    {
        if (currentState == State.Attacking) return;

        currentState = State.Attacking;
        agent.isStopped = true; // Stop moving to shoot/hit
        StopLooking();
        if (animator != null) animator.SetBool(_moving, false);
    }

    void StartSearching()
    {
        if (currentState == State.Searching) return;

        currentState = State.Searching;
        agent.isStopped = false;
        agent.speed = chaseSpeed;
        agent.SetDestination(lastKnownPosition);
        if (animator != null) animator.SetBool(_moving, true);
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
        if (animator != null) animator.SetBool(_moving, true);
    }

    void StopLooking()
    {
        if (activeLookCoroutine != null) StopCoroutine(activeLookCoroutine);
        isWaiting = false;
    }

    // --- BEHAVIORS ---

    void ChaseUpdate()
    {
        if (vision != null && vision.visibleTargets.Count > 0)
        {
            agent.SetDestination(vision.visibleTargets[0].position);
        }
    }

    void AttackUpdate()
    {
        if (vision != null && vision.visibleTargets.Count > 0)
        {
            Transform target = vision.visibleTargets[0];

            // 1. Calculate direction to target
            Vector3 dirToTarget = (target.position - transform.position).normalized;
            dirToTarget.y = 0; // Keep it flat so they don't look up/down

            // 2. Rotate to face the target (faster rotation to aim)
            Quaternion lookRot = Quaternion.LookRotation(dirToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 10f);

            // 3. Check Distance (Existing logic)
            float distance = Vector3.Distance(transform.position, target.position);
            if (distance > attackRange)
            {
                StartChase();
                return;
            }

            // 4. Check Angle before shooting
            float angleToTarget = Vector3.Angle(transform.forward, dirToTarget);

            // Only shoot if we are facing the player (within 10 degrees)
            if (angleToTarget < 10f)
            {
                if (attackTimer <= 0)
                {
                    PerformAttack();
                    attackTimer = timeBetweenAttacks;
                }
            }
        }
    }

    void PerformAttack()
    {
        if (animator != null)
        {
            animator.SetTrigger(_attack);
        }

        if (combatType == CombatType.Melee)
        {
            Debug.Log("Melee Attack! (Add damage handling on animation event)");
            // Example: apply damage in animation event callback
        }
        else // Ranged
        {
            Debug.Log("Pew Pew! (Shooting)");
            if (projectilePrefab != null && firePoint != null)
            {
                Instantiate(projectilePrefab, firePoint.position, transform.rotation);
            }
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
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            activeLookCoroutine = StartCoroutine(LookAroundRoutine(false));
        }
    }

    IEnumerator LookAroundRoutine(bool isSearching)
    {
        isWaiting = true;
        if (animator != null) animator.SetBool(_moving, false);

        Quaternion originalRotation = transform.rotation;

        Quaternion lookLeft = originalRotation * Quaternion.Euler(0, -lookAngle, 0);
        Quaternion lookRight = originalRotation * Quaternion.Euler(0, lookAngle, 0);

        // Trigger left turn animation and rotate toward that direction
        if (animator != null) animator.SetTrigger(_leftTurn);
        yield return StartCoroutine(RotateToTarget(lookLeft));
        yield return new WaitForSeconds(GetCurrentAnimLength() + waitTime);

        // Trigger right turn animation and rotate toward that direction
        if (animator != null) animator.SetTrigger(_rightTurn);
        yield return StartCoroutine(RotateToTarget(lookRight));
        yield return new WaitForSeconds(GetCurrentAnimLength() + waitTime);

        // Trigger left turn to return to center (re-using left trigger as in original)
        if (animator != null) animator.SetTrigger(_leftTurn);
        yield return StartCoroutine(RotateToTarget(originalRotation));
        yield return new WaitForSeconds(GetCurrentAnimLength() + waitTime);

        isWaiting = false;

        if (isSearching) ReturnToPatrol();
        else GoToNextPoint();
    }

    // Helper to safely get current animation state's length
    float GetCurrentAnimLength()
    {
        if (animator == null) return 0f;
        var state = animator.GetCurrentAnimatorStateInfo(0);
        return state.length;
    }

    // A helper function to smoothly rotate the enemy
    IEnumerator RotateToTarget(Quaternion targetRotation)
    {
        while (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
        {
            // RotateTowards ensures we move at a constant speed
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime
            );

            // Wait for the next frame
            yield return null;
        }
    }

    void GoToNextPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;
        currentPointIndex = (currentPointIndex + 1) % patrolPoints.Length;
        agent.SetDestination(patrolPoints[currentPointIndex].position);
        if (animator != null) animator.SetBool(_moving, true);
    }
}