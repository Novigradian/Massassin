using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class MaskController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed;

    [Header("Throw")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float throwSpeed;
    [SerializeField] private float throwRecoveryTime;
    [SerializeField] private float arriveDistance;

    private Rigidbody rb;
    private PlayerInputActions input;
    private Vector2 moveInput;
    private bool canMove;
    private bool isThrowing;
    private Vector3 throwTarget;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
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
    }

    void OnDisable()
    {
        input.Player.Throw.performed -= _ => TryThrow();
        input.Player.Disable();
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
    }

    void TryThrow()
    {
        if (!canMove || isThrowing) return;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
            return;

        throwTarget = hit.point;
        throwTarget.y = transform.position.y;

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
}
