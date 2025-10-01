using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using System.Collections;

[DisallowMultipleComponent]
public class FirstPersonLook_m : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The transform that rotates left-right (usually player body)")]
    public Transform playerBody;
    [Tooltip("Camera transform (this.transform for camera)")]
    public Transform cameraTransform;

    [Header("Look Settings")]
    [Range(0.01f, 10f)] public float sensitivity = 0.15f;   // base sensitivity multiplier
    [Range(0.01f, 0.5f)] public float smoothing = 0.08f;     // lower = snappier, higher = smoother
    public bool invertY = false;
    public float minVertical = -85f;
    public float maxVertical = 85f;

    [Header("Gyro (optional)")]
    public bool enableGyro = false;
    [Tooltip("How much gyro affects camera (0 = ignore)")]
    public float gyroWeight = 0.4f;

    [Header("Recoil")]
    [Tooltip("How fast recoil effect decays back to zero")]
    public float recoilDecay = 8f;
    Vector2 recoilOffset = Vector2.zero;

    // internal state
    Vector2 currentLook = Vector2.zero;      // accumulated yaw(x) / pitch(y)
    Vector2 smoothVel = Vector2.zero;        // used by SmoothDamp
    Vector2 lookInput = Vector2.zero;        // raw input each frame (pixels/frame or stick)
    Vector2 tempGyro = Vector2.zero;

    // small helper to scale mouse delta -> angle delta: tested multiplier
    const float PIXEL_TO_ANGLE = 0.02f;

    void Reset()
    {
        if (cameraTransform == null) cameraTransform = transform;
        if (playerBody == null && transform.parent != null) playerBody = transform.parent;
    }

    void Start()
    {
        if (cameraTransform == null)
            cameraTransform = transform;

        if (playerBody == null && transform.parent != null)
            playerBody = transform.parent;

        if (enableGyro)
            TryEnableGyro();
    }

    void Update()
    {
        // 1) read gyro if enabled (legacy gyro for simplicity)
        if (enableGyro)
            ApplyGyroDelta();

        // 2) merge inputs: lookInput + gyro (gyro weighted)
        Vector2 weighted = lookInput;
        weighted += tempGyro * gyroWeight;

        // convert pixel delta (from Mouse/Touch) into angle using PIXEL_TO_ANGLE and sensitivity
        Vector2 targetDelta = weighted * sensitivity * PIXEL_TO_ANGLE;

        // apply smoothing (SmoothDamp feels nicer than Lerp for camera)
        Vector2 frameDelta = Vector2.SmoothDamp(Vector2.zero, targetDelta, ref smoothVel, smoothing);

        // accumulate into current look (x = yaw, y = pitch)
        currentLook.x += frameDelta.x;
        currentLook.y += frameDelta.y;

        // add recoil offset (recoilOffset is in degrees)
        currentLook += recoilOffset * Time.deltaTime;

        // clamp vertical (pitch)
        currentLook.y = Mathf.Clamp(currentLook.y, minVertical, maxVertical);

        // apply rotations
        // camera pitch uses -currentLook.y (so moving finger up looks up)
        float pitch = -currentLook.y * (invertY ? -1f : 1f);
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        // yaw applied to player body (rotate around Y)
        float yaw = currentLook.x;
        playerBody.localRotation = Quaternion.Euler(0f, yaw, 0f);

        // decay recoil towards zero smoothly
        recoilOffset = Vector2.Lerp(recoilOffset, Vector2.zero, Time.deltaTime * recoilDecay);

        // reset temp gyro after used (we already applied its effect)
        tempGyro = Vector2.zero;

        // clear lookInput if not using continuous input source (touch supplies delta each drag)
        // (If using joystick/right stick you may want to keep it as it is)
        // We'll not clear here because InputSystem's pointer delta will be zero when idle,
        // and TouchLookArea will pass zero on pointer up.
    }

    // -----------------------------
    // New Input System callback (for mouse/gamepad pointer)
    // Configure your InputAction "Look" to use "<Pointer>/delta" and "<Gamepad>/rightStick"
    // and set PlayerInput behavior to "Send Messages" (so it calls "OnLook")
    // -----------------------------
#if ENABLE_INPUT_SYSTEM
    // Signature required for Send Messages: void OnLook(InputValue value) or using CallbackContext
    public void OnLook(InputAction.CallbackContext ctx)
    {
        // For pointer: delta pixels/frame; for gamepad: normalized stick (-1..1)
        Vector2 v = ctx.ReadValue<Vector2>();
        // If it is gamepad right stick, scale differently (sticks are already small)
        // Heuristic: if magnitude <= 1.5 assume stick, else pointer delta
        if (v.magnitude <= 1.5f)
        {
            // treat as stick -> scale up to angle-per-frame
            lookInput = v * 6.0f; // tuning factor for sticks; tweak if needed
        }
        else
        {
            // treat as pointer delta (mouse/touch) -> direct use
            lookInput = v;
        }
    }
#endif

    // -----------------------------
    // Methods to be called from UI Touch Area (recommended for mobile)
    // TouchLookArea will send delta values (screen-space pixels) while dragging
    // -----------------------------
    /// <summary>Set look delta from a touch drag (pixels delta) - call every drag event</summary>
    public void SetTouchLookDelta(Vector2 pixelDelta)
    {
        lookInput = pixelDelta;
    }

    /// <summary>Call when touch look stops (pointer up) — zeros the input</summary>
    public void ClearTouchLook()
    {
        lookInput = Vector2.zero;
    }

    // -----------------------------
    // Recoil API - call this from WeaponController when firing
    // recoil = degrees to add (x = yaw, y = pitch upward positive)
    // -----------------------------
    public void AddRecoil(Vector2 recoil)
    {
        // add immediate impulse; positive y will kick camera upward (increase pitch)
        recoilOffset += recoil;
    }

    // -----------------------------
    // Gyro helpers (simple approach)
    // -----------------------------
    void TryEnableGyro()
    {
#if ENABLE_INPUT_SYSTEM
        // New Input System has Gyroscope via UnityEngine.InputSystem.Gyroscope
        // but for simplicity we fallback to legacy Input.gyro if available.
#endif
        if (SystemInfo.supportsGyroscope)
        {
            Input.gyro.enabled = true;
        }
        else
        {
            enableGyro = false;
        }
    }

    void ApplyGyroDelta()
    {
        if (!SystemInfo.supportsGyroscope) return;
        // use attitude delta approximation: rotationRateUnbiased is angular velocity (rad/s)
        Vector3 rot = Input.gyro.rotationRateUnbiased; // x,y,z in rad/s approx
        // We only want small influence; convert to degrees/frame
        Vector2 degPerFrame = new Vector2(rot.y, rot.x) * Mathf.Rad2Deg * Time.deltaTime;
        // careful sign to make device tilt intuitive
        tempGyro = degPerFrame;
    }

    // -----------------------------
    // Editor convenience: allow mouse drag area using Unity UI TouchLookArea or use PlayerInput
    // -----------------------------
    void OnEnable()
    {
        // ensure cursor locked in editor for mouse testing
#if UNITY_EDITOR
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
#endif
    }

    void OnDisable()
    {
#if UNITY_EDITOR
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
#endif
    }
}
