using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class MobileFPSController : MonoBehaviour
{
    [Header("References")]
    public CharacterController controller;
    public Transform cameraTransform; // main camera (eye)
    public Transform groundCheck;     // optional transform placed near feet
    public LayerMask groundLayers;

    [Header("Movement")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    [Tooltip("Acceleration for blending velocity")]
    public float accel = 10f;

    [Header("Jump")]
    public float jumpHeight = 1.6f;
    public float gravity = -24f;
    public float coyoteTime = 0.12f;      // time after leaving ground where jump still works
    public float jumpBufferTime = 0.12f;  // time before landing where jump input is buffered

    [Header("Crouch")]
    public float standHeight = 0f;
    public float crouchHeight = 1.0f;
    public float crouchTransitionSpeed = 8f;
    public bool crouchToggle = false; // if true crouch toggles on button press; else hold to crouch

    [Header("Other")]
    public bool canSprint = true;

    // runtime state
    Vector2 moveInput = Vector2.zero;
    Vector3 velocity = Vector3.zero;
    float currentSpeed = 0f;
    bool jumpPressedBuffered = false;
    float lastGroundedTime = -10f;
    float lastJumpPressedTime = -10f;
    bool isCrouched = false;
    bool sprintHeld = false;
    bool performingJump = false;

    void Reset()
    {
        controller = GetComponent<CharacterController>();
        if (Camera.main) cameraTransform = Camera.main.transform;
    }

    void Awake()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        if (groundCheck == null)
        {
            // create a temporary groundCheck if none assigned
            GameObject g = new GameObject("GroundCheck");
            g.transform.parent = transform;
            g.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            groundCheck = g.transform;
        }
        currentSpeed = walkSpeed;
    }

    void Update()
    {
        // 1) Ground check
        bool isGrounded = IsGrounded();

        if (isGrounded)
        {
            lastGroundedTime = Time.time;
            performingJump = false;
        }

        // 2) Jump buffering & coyote time
        if (jumpPressedBuffered)
            lastJumpPressedTime = Time.time;

        bool canJumpNow = (Time.time - lastGroundedTime) <= coyoteTime;
        bool hasBufferedJump = (Time.time - lastJumpPressedTime) <= jumpBufferTime;

        if (hasBufferedJump && canJumpNow && !performingJump)
        {
            DoJump();
            lastJumpPressedTime = -10f;
            jumpPressedBuffered = false;
        }

        // 3) Movement speed logic (sprint/crouch)
        float targetSpeed = walkSpeed;
        if (canSprint && sprintHeld && !isCrouched) targetSpeed = runSpeed;
        if (isCrouched) targetSpeed = Mathf.Min(targetSpeed, walkSpeed * 0.6f);

        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, accel * Time.deltaTime);

        // 4) Apply movement via CharacterController
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        Vector3 horizontalVelocity = move * currentSpeed;

        // Keep existing vertical velocity (gravity/jump)
        velocity.x = horizontalVelocity.x;
        velocity.z = horizontalVelocity.z;

        // Apply gravity
        if (isGrounded && velocity.y < 0f) velocity.y = -2f; // small grounded downward force
        velocity.y += gravity * Time.deltaTime;

        controller.Move(velocity * Time.deltaTime);

        // 5) Smooth crouch height
        float desiredHeight = isCrouched ? crouchHeight : standHeight;
        if (Mathf.Abs(controller.height - desiredHeight) > 0.01f)
        {
            float newHeight = Mathf.Lerp(controller.height, desiredHeight, Time.deltaTime * crouchTransitionSpeed);
            // adjust center so controller bottom stays roughly at same position
            float heightDiff = newHeight - controller.height;
            controller.height = newHeight;
            controller.center = new Vector3(controller.center.x, controller.height / 2f, controller.center.z);
        }
    }

    bool IsGrounded()
    {
        // prefer CharacterController.isGrounded if using CharacterController
        if (controller != null)
        {
            // CharacterController.isGrounded can be slightly flaky; supplement with sphere check
            if (controller.isGrounded) return true;
        }

        // fallback: physics sphere check around groundCheck
        float radius = 0.25f;
        Vector3 origin = groundCheck != null ? groundCheck.position : transform.position + Vector3.down * 0.1f;
        return Physics.CheckSphere(origin, radius, groundLayers, QueryTriggerInteraction.Ignore);
    }

    void DoJump()
    {
        performingJump = true;
        // set vertical velocity to achieve desired jump height: v = sqrt(2 * g * h)
        velocity.y = Mathf.Sqrt(Mathf.Abs(gravity) * 2f * jumpHeight);
    }

    // -----------------------
    // Input Callbacks (New Input System)
    // -----------------------
    // Connect these methods with a PlayerInput component or call them from your InputAction callbacks.

    public void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            // buffer the jump
            jumpPressedBuffered = true;
            lastJumpPressedTime = Time.time;
        }
        else if (ctx.canceled)
        {
            // nothing special for jump release
        }
    }

    public void OnSprint(InputAction.CallbackContext ctx)
    {
        sprintHeld = ctx.ReadValueAsButton();
    }

    public void OnCrouch(InputAction.CallbackContext ctx)
    {
        bool pressed = ctx.ReadValueAsButton();
        if (crouchToggle && pressed)
        {
            isCrouched = !isCrouched;
        }
        else
        {
            isCrouched = pressed;
        }
    }

    // -----------------------
    // UI-friendly methods (for on-screen joystick/buttons)
    // -----------------------
    // Use these if you prefer to drive inputs from UI code.

    /// <summary>Directly set move input (0..1 for joystick). Call from a joystick script.</summary>
    public void SetMoveInput(Vector2 vec)
    {
        moveInput = vec;
    }

    /// <summary>Call from Jump button PointerDown</summary>
    public void OnJumpButtonDown() 
    {
        jumpPressedBuffered = true;
        lastJumpPressedTime = Time.time;
    }

    /// <summary>Call from Sprint button PointerDown (or toggle via SetSprint)</summary>
    public void SetSprint(bool isOn) 
    {
        sprintHeld = isOn;
    }

    /// <summary>Call from Crouch button PointerDown / Toggle</summary>
    public void ToggleCrouch() 
    {
        isCrouched = !isCrouched;
    }

    // Optional helper for debug UI
    public float GetCurrentSpeed() => currentSpeed;
    public bool IsCrouched() => isCrouched;
}
