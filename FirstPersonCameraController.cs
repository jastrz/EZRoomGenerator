// FirstPersonCameraController.cs
// Attach to your Camera. If a CharacterController exists on a parent object (recommended),
// this script will move that CharacterController. Otherwise it will move the Camera transform directly.
//
// Features:
// - WSAD movement
// - Mouse look (lock cursor)
// - Gravity + simple grounding (works with CharacterController if present)
// - Head-bob for step simulation
// - Footstep sounds (randomized from an array) played while moving
// - Toggle flashlight with 'F' (expects a Light component on a child named "Flashlight" or assigned in inspector)

using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class FirstPersonCameraController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 4f;
    public float runSpeed = 6f;
    public KeyCode runKey = KeyCode.LeftShift;
    public float gravity = -9.81f;
    public bool useCharacterControllerOnParent = true; // attempt to use parent CharacterController if available

    [Header("Mouse Look")]
    public float mouseSensitivity = 2.0f;
    public float pitchMin = -85f;
    public float pitchMax = 85f;
    public bool lockCursorOnStart = true;

    [Header("Head Bob / Steps")]
    public float headBobFrequency = 10f;
    public float headBobAmplitude = 0.03f;
    public float stepInterval = 0.5f; // seconds between footsteps at normal walking pace
    public AudioClip[] footstepClips;
    public float footstepVolume = 0.7f;

    [Header("Flashlight")]
    public KeyCode flashlightKey = KeyCode.F;
    public Light flashlight; // assign in inspector or the script will try to find a child Light named "Flashlight"

    // Internal
    AudioSource audioSource;
    Transform camTransform;
    Vector3 originalCamLocalPos;
    CharacterController parentController;
    Transform parentTransform; // used for yaw rotation when using CharacterController
    float yaw;
    float pitch;

    // Movement internals
    Vector3 verticalVelocity = Vector3.zero;
    float stepTimer = 0f;
    float nextStepTime = 0f;
    float bobTimer = 0f;
    bool wasGrounded = true;

    void Start()
    {
        camTransform = transform;
        originalCamLocalPos = camTransform.localPosition;
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;

        // Cursor
        if (lockCursorOnStart)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Find CharacterController on parent (recommended setup: camera under a capsule with CharacterController)
        if (useCharacterControllerOnParent)
        {
            parentController = GetComponentInParent<CharacterController>();
            if (parentController != null)
            {
                parentTransform = parentController.transform;
            }
        }

        // If no CharacterController found, move camera directly and use camera's parent for yaw if it exists
        if (parentController == null)
        {
            if (transform.parent != null) parentTransform = transform.parent;
            else parentTransform = transform;
        }

        // Initialize rotation
        yaw = parentTransform.eulerAngles.y;
        pitch = camTransform.localEulerAngles.x;

        // Try to auto-assign flashlight if not set
        if (flashlight == null)
        {
            var childLight = transform.Find("Flashlight");
            if (childLight != null)
            {
                flashlight = childLight.GetComponent<Light>();
            }
            else
            {
                // search any Light in children
                flashlight = GetComponentInChildren<Light>();
            }
        }

        // Start step timing
        stepTimer = 0f;
        nextStepTime = stepInterval;
    }

    void Update()
    {
        HandleMouseLook();
        HandleMovement();
        HandleFlashlightToggle();
        HandleHeadBobAndFootsteps();
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        // apply yaw to parent (so movement follows view) and pitch to camera local rotation
        parentTransform.rotation = Quaternion.Euler(0f, yaw, 0f);
        camTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void HandleMovement()
    {
        // Input
        float inputX = Input.GetAxisRaw("Horizontal"); // A/D
        float inputZ = Input.GetAxisRaw("Vertical");   // W/S

        Vector3 moveDir = Vector3.zero;
        if (inputX != 0f || inputZ != 0f)
        {
            // Movement relative to yaw (parentTransform forward/right)
            Vector3 forward = parentTransform.forward;
            Vector3 right = parentTransform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            moveDir = (right * inputX + forward * inputZ).normalized;
        }

        // Speed
        float speed = Input.GetKey(runKey) ? runSpeed : walkSpeed;
        Vector3 horizontalVelocity = moveDir * speed;

        // Gravity / grounding
        if (parentController != null)
        {
            // Using CharacterController provides grounding and collisions
            if (parentController.isGrounded && verticalVelocity.y < 0f)
            {
                verticalVelocity.y = -1f; // small negative to keep grounded
            }
            verticalVelocity.y += gravity * Time.deltaTime;

            Vector3 finalVelocity = horizontalVelocity + new Vector3(0f, verticalVelocity.y, 0f);
            parentController.Move(finalVelocity * Time.deltaTime);
        }
        else
        {
            // Move the camera transform directly (less ideal for collisions)
            if (IsGroundedSimple())
            {
                if (verticalVelocity.y < 0f) verticalVelocity.y = -1f;
            }
            verticalVelocity.y += gravity * Time.deltaTime;
            Vector3 finalVelocity = horizontalVelocity + new Vector3(0f, verticalVelocity.y, 0f);
            transform.position += finalVelocity * Time.deltaTime;
        }
    }

    // Simple raycast ground check used when no CharacterController is present
    bool IsGroundedSimple()
    {
        float checkDistance = 0.1f;
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 1f + checkDistance))
        {
            return true;
        }
        return false;
    }

    void HandleFlashlightToggle()
    {
        if (Input.GetKeyDown(flashlightKey) && flashlight != null)
        {
            flashlight.enabled = !flashlight.enabled;
        }
    }

    void HandleHeadBobAndFootsteps()
    {
        bool isMoving = (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.01f || Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.01f);
        bool isGrounded = parentController != null ? parentController.isGrounded : IsGroundedSimple();

        // Head bob - only when moving and grounded
        if (isMoving && isGrounded)
        {
            float currentSpeedFactor = Input.GetKey(runKey) ? 1.5f : 1.0f;
            bobTimer += Time.deltaTime * headBobFrequency * currentSpeedFactor;
            float bobAmount = Mathf.Sin(bobTimer) * headBobAmplitude * currentSpeedFactor;
            camTransform.localPosition = originalCamLocalPos + new Vector3(0f, bobAmount, 0f);
        }
        else
        {
            // Smoothly return to original position when not bobbing
            bobTimer = 0f;
            camTransform.localPosition = Vector3.Lerp(camTransform.localPosition, originalCamLocalPos, Time.deltaTime * 6f);
        }

        // Footstep sounds - use a simple interval timer
        if (isMoving && isGrounded && footstepClips != null && footstepClips.Length > 0)
        {
            float speed = Input.GetKey(runKey) ? runSpeed : walkSpeed;
            // scale step interval by speed (faster = less time between steps)
            float scaledInterval = Mathf.Max(0.08f, stepInterval / (speed / walkSpeed));

            stepTimer += Time.deltaTime;
            if (stepTimer >= scaledInterval)
            {
                PlayFootstep();
                stepTimer = 0f;
            }
        }
        else
        {
            // reset timer when not moving
            stepTimer = 0f;
        }
    }

    void PlayFootstep()
    {
        if (footstepClips == null || footstepClips.Length == 0 || audioSource == null) return;
        int index = Random.Range(0, footstepClips.Length);
        audioSource.PlayOneShot(footstepClips[index], footstepVolume);
    }

    // Public helpers (useful from other scripts)
    public void SetFlashlightEnabled(bool enabled)
    {
        if (flashlight != null) flashlight.enabled = enabled;
    }

    public bool IsFlashlightOn()
    {
        return flashlight != null && flashlight.enabled;
    }
}
