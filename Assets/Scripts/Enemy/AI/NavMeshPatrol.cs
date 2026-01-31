using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyVision))]
public class NavMeshPatrol : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform[] patrolPoints;
    private EnemyVision vision;
    private NavMeshAgent agent;

    [Header("Patrol Settings")]
    [SerializeField] private float patrolSpeed = 3.5f;
    [SerializeField] private float waitTime = 1f; 
    [SerializeField] private float turnSpeed = 90f;
    [SerializeField] private float lookAngle = 60f; 

    [Header("Chase Settings")]
    [SerializeField] private float chaseSpeed = 6f;

    // --- State Machine ---
    private enum State { Patrolling, Chasing, Searching }
    private State currentState;

    // --- Internal Tracking ---
    private int currentPointIndex = 0;
    private bool isWaiting = false; 
    private Coroutine activeLookCoroutine; 
    private Vector3 lastKnownPosition; // remembers where the player was

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        vision = GetComponent<EnemyVision>();
        
        currentState = State.Patrolling;
        agent.speed = patrolSpeed;

        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            agent.SetDestination(patrolPoints[0].position);
        }
    }

    void Update()
    {
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
        }
    }

    void CheckVision()
    {
        // 1. if we see the player...
        if (vision.visibleTargets.Count > 0)
        {
            Transform target = vision.visibleTargets[0];
            lastKnownPosition = target.position; // always update memory
            
            if (currentState != State.Chasing)
            {
                StartChase();
            }
        }
        // 2. if we lost the player...
        else if (currentState == State.Chasing)
        {
            StartSearching();
        }
    }

    void StartChase()
    {
        currentState = State.Chasing;
        agent.speed = chaseSpeed;
        StopLooking(); // Stop any animations
    }

    void StartSearching()
    {
        currentState = State.Searching;
        agent.speed = chaseSpeed; // Run to the last known spot
        agent.SetDestination(lastKnownPosition);
    }

    void ReturnToPatrol()
    {
        currentState = State.Patrolling;
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
            // Keep running to the player's exact position
            agent.SetDestination(vision.visibleTargets[0].position);
        }
    }

    void SearchUpdate()
    {
        if (isWaiting) return;

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            // We arrived at the LKP, but no player. Look around confused.
            // passing 'true' means "We are searching, so return to patrol after"
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

        // Look Left
        yield return StartCoroutine(RotateToTarget(lookLeft));
        yield return new WaitForSeconds(waitTime);

        // Look Right
        yield return StartCoroutine(RotateToTarget(lookRight));
        yield return new WaitForSeconds(waitTime);

        // Look Center
        yield return StartCoroutine(RotateToTarget(originalRotation));

        isWaiting = false;

        // DECISION: What do we do now that we finished looking?
        if (isSearching)
        {
            // We checked the LKP and found nothing. Give up.
            ReturnToPatrol();
        }
        else
        {
            // We were just patrolling. Go to next point.
            GoToNextPoint();
        }
    }

    IEnumerator RotateToTarget(Quaternion targetRotation)
    {
        while (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, 
                targetRotation, 
                turnSpeed * Time.deltaTime
            );
            yield return null;
        }
    }

    void GoToNextPoint()
    {
        currentPointIndex = (currentPointIndex + 1) % patrolPoints.Length;
        agent.SetDestination(patrolPoints[currentPointIndex].position);
    }
}