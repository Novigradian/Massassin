using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class MaskController : MonoBehaviour
{
    //public GameObject mainCamera;
    
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

        
        
    }

    void OnEnable()
    {
        input.Player.Enable();
        input.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        input.Player.Move.canceled += _ => moveInput = Vector2.zero;
        input.Player.Throw.performed += _ => TryThrow();
        input.Player.Melee.performed += _ => TryMeleePossess();
    }

    void OnDisable()
    {
        input.Player.Throw.performed -= _ => TryThrow();
        input.Player.Disable();
        input.Player.Melee.performed -= _ => TryMeleePossess();
    }

    void FixedUpdate()
    {
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
    }

    void TryThrow()
    {
        if (!canMove || isThrowing) return;

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
                target.position = controlledTarget.position;
                target.rotation = controlledTarget.rotation;
            }
        }
        
        if (target.TryGetComponent<Possessable>(out Possessable possessable))
        {
            possessable.isPossessed = true;
            possessable.maskController = this;
            possessable.ToggleMask(true);

            controlledTarget = target;

            rb = target.GetComponent<Rigidbody>();

            cameraFollow.target = controlledTarget;

            GameManager.Instance.OnPossessionChanged(controlledTarget);
        }
    }

    void FaceMousePosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
            return;

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

    public void TryPossessTarget(Collider other)
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

}
