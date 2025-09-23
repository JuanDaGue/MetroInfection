using UnityEngine;
using UnityEngine.InputSystem;
#if ENABLE_INPUT_SYSTEM && UNITY_2019_1_OR_NEWER
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
#endif

[RequireComponent(typeof(CharacterController))]
public class FirstPersonMovement_InputSystem : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float walkSpeed = 3.5f;
    [SerializeField] float runSpeed = 6.5f;
    [SerializeField] float rotationSmoothTime = 0.12f;
    [SerializeField] float deadzone = 0.1f;

    [Header("Look")]
    [SerializeField] Transform cameraTransform;    // the camera that looks around
    [SerializeField] float lookSensitivity = 1.5f; // general multiplier
    [SerializeField] float mobileLookSensitivity = 0.8f; // optional separate sensitivity for touch
    [SerializeField] float maxPitch = 85f;         // clamp up/down
    [SerializeField] bool invertY = false;

    [Header("Jump / Gravity")]
    [SerializeField] float gravity = -9.81f;
    [SerializeField] float jumpHeight = 1.3f;
    [SerializeField] float groundedYOffset = 0.05f;

    [Header("References")]
    [SerializeField] Animator animator; // optional

    CharacterController controller;

    // Inputs
    Vector2 moveInput = Vector2.zero;
    Vector2 lookInput = Vector2.zero; // delta look
    bool sprintPressed = false;
    bool jumpPressed = false;
    bool crouchPressed = false;

    // look state
    float yaw = 0f;
    float pitch = 0f;
    float rotationVelocity = 0f;

    // physics
    float verticalVelocity = 0f;

    // mobile joystick hook (optional)
    public bool useMobileLook = true; // set true for touch look
    public bool lockCursorOnStart = true;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null) Debug.LogError("CharacterController required.");

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        // initialize yaw/pitch from current rotation
        yaw = transform.eulerAngles.y;
        if (cameraTransform != null)
            pitch = cameraTransform.localEulerAngles.x;
    }

    void OnEnable()
    {
        #if ENABLE_INPUT_SYSTEM && UNITY_2019_1_OR_NEWER
        // Enhanced touch optional support (improves touch behaviour)
        EnhancedTouchSupport.Enable();
        #endif

        if (lockCursorOnStart)
            Cursor.lockState = CursorLockMode.Locked;
    }

    void OnDisable()
    {
        #if ENABLE_INPUT_SYSTEM && UNITY_2019_1_OR_NEWER
        EnhancedTouchSupport.Disable();
        #endif
    }

    void Update()
    {
        // --- movement input processing (deadzone)
        Vector2 input = moveInput;
        if (input.magnitude < deadzone) input = Vector2.zero;

        // Camera-relative movement directions (flattened)
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = forward * input.y + right * input.x;

        // Determine target speed
        float targetSpeed = sprintPressed ? runSpeed : walkSpeed;

        Vector3 horizontalVelocity = Vector3.zero;
        if (moveDirection.sqrMagnitude > 0f)
        {
            horizontalVelocity = moveDirection.normalized * targetSpeed;

            // Rotate player to face movement direction (smooth)
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,
                1f - Mathf.Exp(-rotationSmoothTime * Time.deltaTime));
        }

        // --- gravity & jumping
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -0.5f; // stick to ground
            if (jumpPressed)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                jumpPressed = false;
            }
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        // compose final velocity and move
        Vector3 finalVelocity = horizontalVelocity;
        finalVelocity.y = verticalVelocity;
        controller.Move(finalVelocity * Time.deltaTime);

        // --- animator update
        if (animator != null)
        {
            Vector3 flatVel = new Vector3(controller.velocity.x, 0f, controller.velocity.z);
            animator.SetFloat("speed", flatVel.magnitude);
            animator.SetBool("isGrounded", controller.isGrounded);
        }

        // --- look handling
        HandleLook();
    }

    void HandleLook()
    {
        if (lookInput.sqrMagnitude != 0f)
        {
            // decide sensitivity based on device (touch vs mouse/gamepad)
            bool isTouch = Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed; // quick test
            float sens = isTouch ? mobileLookSensitivity : lookSensitivity;
            float y = lookInput.y * (invertY ? 1f : -1f) * sens;
            float x = lookInput.x * sens;

            yaw += x;
            pitch += y;

            pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);

            // apply camera rotation (camera local pitch, player yaw)
            transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            if (cameraTransform != null)
            {
                cameraTransform.localEulerAngles = new Vector3(pitch, 0f, 0f);
            }

            // after applying, zero look input unless it's persistent (for touch you might use delta)
            // We keep it; input is consumed by the frame provider (callback context).
            // If using pointer delta you should feed delta each frame.
        }
    }

    // -------------------------
    // Input System callbacks (PlayerInput -> Send Messages or hook via events)
    // Example: wire PlayerInput component to "Invoke Unity Events" OR use "Send Messages"
    // -------------------------

    // Move (Vector2)
    public void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }

    // Look (Vector2). On desktop with mouse: use Pointer/delta. On gamepad: right stick. On touch: use a touch-drag region that writes delta values.
    public void OnLook(InputAction.CallbackContext ctx)
    {
        // For delta style devices (mouse/gamepad), this will be a delta per frame; for stick it's value.
        lookInput = ctx.ReadValue<Vector2>();
    }

    public void OnSprint(InputAction.CallbackContext ctx)
    {
        if (ctx.phase == InputActionPhase.Performed)
            sprintPressed = true;
        else if (ctx.phase == InputActionPhase.Canceled)
            sprintPressed = false;
    }

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (ctx.phase == InputActionPhase.Performed)
            jumpPressed = true;
    }

    public void OnCrouch(InputAction.CallbackContext ctx)
    {
        if (ctx.phase == InputActionPhase.Performed)
            crouchPressed = true;
        else if (ctx.phase == InputActionPhase.Canceled)
            crouchPressed = false;
    }

    // Public hooks for mobile UI: allow a joystick UI to call these
    public void SetMoveInput(Vector2 v) => moveInput = v;
    public void SetLookInput(Vector2 v) => lookInput = v;
}
