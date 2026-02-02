using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MaskController : MonoBehaviour
{
    //public GameObject mainCamera;
    public static MaskController Instance { get; private set; }
    
    [Header("Targets")]
    public Transform controlledTarget;
    [SerializeField] private Transform maskTarget;
    
    [Header("Movement")]
    [SerializeField] private float moveSpeed;

    [Header("Melee")]
    [SerializeField] private float meleeRadius = 2.5f;
    [SerializeField] private LayerMask possessableLayer;

    [Header("Throw")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float throwSpeed;
    [SerializeField] private float throwRecoveryTime;
    [SerializeField] private float arriveDistance;
    [SerializeField] private float throwPossessIgnoreTime = 0.15f;
    private float throwStartTime;

    [Header("ThrowMinion")]
    [SerializeField] private MinionMask minionPrefab;
    [SerializeField] private float minionThrowGap=1.5f;
    public int maxMinions = 3;
    
    public List<MinionMask> activeMinions;

    private Rigidbody rb;
    private PlayerInputActions input;
    private Vector2 moveInput;
    private bool canMove;
    private bool isThrowing;
    private Vector3 throwTarget;

    private CameraFollow cameraFollow;
    
    

    void Awake()
    {
        cameraFollow = Camera.main.GetComponent<CameraFollow>();
        PossessTarget(maskTarget);
        
        input = new PlayerInputActions();

        canMove = true;
        isThrowing = false;

        activeMinions = new List<MinionMask>(99);

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        Canvas.Instance.UpdateMaskCount(maxMinions);
    }

    void Start()
    {
        if (GameManager.Instance.maxMinion >= 0)
        {
            maxMinions = GameManager.Instance.maxMinion;
            Canvas.Instance.UpdateMaskCount(maxMinions);
        }
    }

    void OnEnable()
    {
        input.Player.Enable();
        input.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        input.Player.Move.canceled += _ => moveInput = Vector2.zero;
        //input.Player.Throw.performed += _ => TryThrow();
        input.Player.Throw.performed += _ => TryThrowMinion();
        input.Player.Melee.performed += _ => TryMeleePossess();
        //input.Player.Recall.performed += _ => RecallAllMinions();
    }

    void OnDisable()
    {
        //input.Player.Throw.performed -= _ => TryThrow();
        input.Player.Throw.performed -= _ => TryThrowMinion();
        input.Player.Disable();
        input.Player.Melee.performed -= _ => TryMeleePossess();
        //input.Player.Recall.performed -= _ => RecallAllMinions();
    }

    void FixedUpdate()
    {
        if (GameManager.Instance.levelEnded) return;
        
        if (isThrowing)
        {
            HandleThrowMovement();
            return;
        }

        //MOVEMENT
        if (!canMove) return;
        
        Vector3 velocity = new Vector3(moveInput.x, 0f, moveInput.y);

        if (velocity.sqrMagnitude > 1f)
            velocity.Normalize();

        rb.linearVelocity = velocity * moveSpeed;

        if (canMove && !isThrowing)
        {
            FaceMousePosition();
        }

        rb.angularVelocity = Vector3.zero;
    }

    void TryThrowMinion()
    {
        /*//Debug.Log($"Minions: {activeMinions.Count} / {maxMinions}");

        if (activeMinions.Count >= maxMinions) return;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
            return;

        MinionMask minion = Instantiate(
            minionPrefab,
            controlledTarget.position,
            Quaternion.identity
        );

        minion.Launch(hit.point);
        activeMinions.Add(minion);*/

        if (activeMinions.Count >= maxMinions) return;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane groundPlane = new Plane(Vector3.up, controlledTarget.position);

        if (!groundPlane.Raycast(ray, out float enter))
            return;

        Vector3 hitPoint = ray.GetPoint(enter);
        Vector3 direction = hitPoint - controlledTarget.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
            return;

        MinionMask minion = Instantiate(
            minionPrefab,
            new Vector3 (controlledTarget.position.x, 1.68f, controlledTarget.position.z)
            + controlledTarget.forward * minionThrowGap,
            Quaternion.LookRotation(direction)
        );

        minion.Launch(direction);
        activeMinions.Add(minion);

        Canvas.Instance.UpdateMaskCount(maxMinions - activeMinions.Count);
    }
    

    /*void RecallAllMinions()
    {
        foreach (var minion in activeMinions)
        {
            if (minion != null)
                minion.Recall(controlledTarget.position);
        }

        activeMinions.Clear();
    }*/

    public void PickUpMinion(GameObject minion)
    {
        if (activeMinions.Contains(minion.GetComponent<MinionMask>()))
        {
            activeMinions.Remove(minion.GetComponent<MinionMask>());
        }
        else
        {
            maxMinions++; //picked up new minion, increase max count
        }

        Canvas.Instance.UpdateMaskCount(maxMinions - activeMinions.Count);
        
        Destroy(minion);
    }

    void TryThrow()
    {
        if (!canMove || isThrowing || controlledTarget == maskTarget) return;

        throwStartTime = Time.time;

        if (controlledTarget != maskTarget)
        {
            PossessTarget(maskTarget);
        }

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
            return;

        throwTarget = hit.point;
        throwTarget.y = rb.position.y;

        isThrowing = true;
        canMove = false;
        moveInput = Vector2.zero;
    }

    void HandleThrowMovement()
    {
        Vector3 toTarget = throwTarget - rb.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude <= arriveDistance * arriveDistance)
        {
            // HARD STOP (this is the important part)
            rb.linearVelocity = Vector3.zero;
            rb.position = throwTarget;

            isThrowing = false;

            StartCoroutine(ThrowRecovery());
            return;
        }

        Vector3 direction = toTarget.normalized;
        rb.linearVelocity = direction * throwSpeed;
    }

    IEnumerator ThrowRecovery()
    {
        yield return new WaitForSeconds(throwRecoveryTime);
        canMove = true;
    }

    private void PossessTarget(Transform target)
    {
        
        //clean up previous target
        if (controlledTarget != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            
            controlledTarget.GetComponent<Possessable>().isPossessed = false;

            if (controlledTarget == maskTarget && target != maskTarget)
            {
                maskTarget.gameObject.SetActive(false);
                //maskTarget.position = maskTarget.position - Vector3.down * 100f; //move mask out of sight
            }
            else
            {
                controlledTarget.GetComponent<Possessable>().ToggleMask(false);
            }

            if (target == maskTarget) //switch from controlled enemy back to mask
            {
                maskTarget.gameObject.SetActive(true);
                target.position = controlledTarget.position + Vector3.up * 1f;
                target.rotation = controlledTarget.rotation;
            }

            SetEnemyControlState(controlledTarget, false);
        }
        
        //handle current target
        if (target.TryGetComponent<Possessable>(out Possessable possessable))
        {
            possessable.isPossessed = true;
            possessable.maskController = this;
            possessable.ToggleMask(true);

            controlledTarget = target;

            rb = target.GetComponent<Rigidbody>();

            cameraFollow.target = controlledTarget;

            GameManager.Instance.OnPossessionChanged(controlledTarget);

            SetEnemyControlState(target, true);
        }
    }

    private void SetEnemyControlState(Transform enemy, bool isPlayerControlled)
    {
        if (!enemy) return;

        if (enemy.TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var agent))
        {
            if (isPlayerControlled)
            {
                // AI → Player
                if (agent.enabled)
                {
                    agent.ResetPath();   // ✅ clear stale path
                    agent.enabled = false;
                }
            }
            else
            {
                // Player → AI
                agent.enabled = true;
                agent.isStopped = false;
            }
        }


        if (enemy.TryGetComponent<Rigidbody>(out var enemyRb))
        {
            enemyRb.isKinematic = !isPlayerControlled;
        }

        if (enemy.TryGetComponent<NavMeshPatrol>(out var patrol))
        {
            patrol.enabled = !isPlayerControlled;
            //if (!isPlayerControlled) patrol.RestartPatrol();
        }

        if (enemy.TryGetComponent<NavMeshPatrol_WithAnimation>(out var patrolWithAnim))
        {
            patrolWithAnim.enabled = !isPlayerControlled;
            //if (!isPlayerControlled) patrolWithAnim.RestartPatrol();
        }
            

        if (enemy.TryGetComponent<EnemyVision>(out var vision))
            vision.enabled = !isPlayerControlled;
        
        if (enemy.TryGetComponent<BoxCollider>(out var boxCollider))
            boxCollider.isTrigger = !isPlayerControlled;
    }

    void FaceMousePosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        //Debug.Log("f22n");

        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
            return;

        //Debug.Log("facing mouse position");

        Vector3 lookDir = hit.point - rb.position;
        lookDir.y = 0f;

        if (lookDir.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(lookDir, Vector3.up);
        Quaternion smoothed = Quaternion.Slerp(
            rb.rotation,
            targetRotation,
            20f * Time.fixedDeltaTime
        );

        rb.MoveRotation(smoothed);
    }

    public void TryPossessTarget(GameObject other)
    {
        if (isThrowing)
        {
            if (Time.time - throwStartTime < throwPossessIgnoreTime) return;
            
            if ((other.transform.CompareTag("Enemy") || other.transform.CompareTag("Mask") || other.transform.CompareTag("TargetEnemy")) && other.transform != controlledTarget)
            {
                Debug.Log("throwing hit enemy");
                
                isThrowing = false;
                canMove = true;
                
                //StartCoroutine(ThrowRecovery());

                PossessTarget(other.transform);
            }
        }
    }

    void TryMeleePossess()
    {
        if (isThrowing) return;
        if (controlledTarget == null) return;

        Vector3 origin = rb.position;

        Collider[] hits = Physics.OverlapSphere(
            origin,
            meleeRadius,
            possessableLayer
        );

        Transform closestTarget = null;
        float closestDistSq = float.MaxValue;

        foreach (Collider hit in hits)
        {
            Transform candidate = hit.transform;

            // Skip self
            if (candidate == controlledTarget)
                continue;

            // Must be possessable
            if (!candidate.TryGetComponent<Possessable>(out var possessable))
                continue;

            float distSq = (candidate.position - origin).sqrMagnitude;

            if (distSq < closestDistSq)
            {
                closestDistSq = distSq;
                closestTarget = candidate;
            }
        }

        if (closestTarget != null)
        {
            PossessTarget(closestTarget);
        }
    }

    public void PossessedDie()
    {
        //possessed target dies, switch back to mask
        GameObject deadPossessed = controlledTarget.gameObject;
        PossessTarget(maskTarget);
        Destroy(deadPossessed);
    }

    public void HandleMelee()
    {
        
    }

}
