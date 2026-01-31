using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class MaskController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed;

    private Rigidbody rb;
    private PlayerInputActions input;
    private Vector2 moveInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        input = new PlayerInputActions();
    }

    void OnEnable()
    {
        input.Player.Enable();
        input.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        input.Player.Move.canceled += _ => moveInput = Vector2.zero;
    }

    void OnDisable()
    {
        input.Player.Disable();
    }

    void FixedUpdate()
    {
        Vector3 velocity = new Vector3(moveInput.x, 0f, moveInput.y);

        if (velocity.sqrMagnitude > 1f)
            velocity.Normalize();

        rb.linearVelocity = velocity * moveSpeed;
    }
}
