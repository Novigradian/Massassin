using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollowThirdPerson : CameraFollow
{
    [Tooltip("Default distance behind the target")]
    public float distance = 4f;
    [Tooltip("How fast the camera rotates to look at the target")]
    public float rotationSpeed = 6f;
    [Tooltip("How fast scroll wheel zooms")]
    public float scrollSpeed = 2f;
    [Tooltip("Pitch (vertical) limits when rotating with the mouse")]
    public Vector2 pitchLimits = new Vector2(-20f, 80f);

    float yaw;
    float pitch;
    float currentDistance;

    private PlayerInputActions input;

    void Awake()
    {
        input = new PlayerInputActions();
    }

    void Start()
    {
        currentDistance = distance;
        if (target)
        {
            yaw = target.eulerAngles.y;
            pitch = 10f;
            // Snap initially so the camera doesn't interpolate from origin
            SnapToTarget();
        }
    }

    void OnEnable()
    {
        input?.Player.Enable();
    }

    void OnDisable()
    {
        input?.Player.Disable();
    }

    void OnDestroy()
    {
        input?.Dispose();
        input = null;
    }

    void LateUpdate()
    {
        if (!target) return;

        // Use the new Input System's Mouse device if available, otherwise fall back to legacy Input
        bool rightPressed;
        Vector2 mouseDelta;
        float scroll;

        if (Mouse.current != null)
        {
            rightPressed = Mouse.current.rightButton.isPressed;
            mouseDelta = Mouse.current.delta.ReadValue();
            scroll = Mouse.current.scroll.ReadValue().y;
        }
        else
        {
            rightPressed = Input.GetMouseButton(1);
            mouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            scroll = Input.GetAxis("Mouse ScrollWheel");
        }

        // Rotate camera when right mouse button is held
        if (rightPressed)
        {
            // mouseDelta is in pixels for the new system; scale down to match previous feel
            float scale = (Mouse.current != null) ? 0.02f : 1f;
            yaw += mouseDelta.x * rotationSpeed * scale;
            pitch -= mouseDelta.y * rotationSpeed * scale;
            pitch = Mathf.Clamp(pitch, pitchLimits.x, pitchLimits.y);
        }

        // Zoom with scroll wheel
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            currentDistance = Mathf.Clamp(currentDistance - scroll * scrollSpeed, 1f, 12f);
        }

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 targetOffset = new Vector3(0f, height, 0f);
        Vector3 desiredPosition = target.position + rotation * new Vector3(0f, 0f, -currentDistance) + targetOffset;

        // Smoothly move and rotate the camera
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
        Quaternion lookRotation = Quaternion.LookRotation(target.position + targetOffset - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Immediately snaps camera to the target using current settings.
    /// </summary>
    public void SnapToTarget()
    {
        if (!target) return;
        currentDistance = distance;
        yaw = target.eulerAngles.y;
        pitch = Mathf.Clamp(pitch, pitchLimits.x, pitchLimits.y);
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 targetOffset = new Vector3(0f, height, 0f);
        transform.position = target.position + rotation * new Vector3(0f, 0f, -currentDistance) + targetOffset;
        transform.LookAt(target.position + targetOffset);
    }
}
