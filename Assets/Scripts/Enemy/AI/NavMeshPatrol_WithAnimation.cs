using UnityEngine.AI;
using UnityEngine;
using System.Collections;

public class NavMeshPatrol_WithAnimation : MonoBehaviour
{
    static readonly int _moving = Animator.StringToHash("Moving");
    static readonly int _leftTurn = Animator.StringToHash("LeftTurn");
    static readonly int _rightTurn = Animator.StringToHash("RightTurn");

    [Header("Patrol Settings")]
    [SerializeField] private Transform[] patrolPoints;

    [Header("Look Around Settings")]
    [SerializeField] private float waitTime = 0.2f; // These are serialized don't look at these values  
    [SerializeField] private float turnSpeed = 90f;
    [SerializeField] private float lookAngle = 60f; 

    [SerializeField] private Animator animator;
    
    private NavMeshAgent agent;
    private int currentPointIndex = 0;
    private bool isWaiting = false;
    

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            agent.SetDestination(patrolPoints[0].position);
        }
    }

    void Update()
    {
        if (isWaiting) return;

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            StartCoroutine(LookAroundRoutine());
        }
    }

    IEnumerator LookAroundRoutine()
    {
        isWaiting = true;
        animator.SetBool( _moving, false);

        Quaternion originalRotation = transform.rotation;

        Quaternion lookLeft = originalRotation * Quaternion.Euler(0, -lookAngle, 0);
        Quaternion lookRight = originalRotation * Quaternion.Euler(0, lookAngle, 0);

        // yield return StartCoroutine(RotateToTarget(lookLeft));
        animator.SetTrigger(_leftTurn);
        yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length + waitTime);
        // yield return new WaitForSeconds(waitTime);

        // yield return StartCoroutine(RotateToTarget(lookRight));
        animator.SetTrigger(_rightTurn);
        yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length + waitTime);
        // yield return new WaitForSeconds(waitTime);

        animator.SetTrigger(_leftTurn);
        // yield return StartCoroutine(RotateToTarget(originalRotation));
        yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length + waitTime);

        GoToNextPoint();
        
        isWaiting = false;
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
        if (patrolPoints.Length == 0) return;
        currentPointIndex = (currentPointIndex + 1) % patrolPoints.Length;
        agent.SetDestination(patrolPoints[currentPointIndex].position);
        animator.SetBool(_moving, true);
    }
}