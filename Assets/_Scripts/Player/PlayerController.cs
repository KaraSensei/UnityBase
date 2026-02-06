using UnityEngine;

/// <summary>
/// Player movement controller for 3D third-person.
/// Uses InputManager to read input.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Player stats component (health, move speed, jump force, etc.).")]
    public PlayerStats playerStats;

    [Tooltip("Camera transform used as reference for movement (usually main camera or Cinemachine virtual camera).")]
    public Transform cameraTransform;

    [Tooltip("Root transform of the visual model (rotates to face camera).")]
    public Transform visualRoot;

    [Header("Movement & Physics")]
    [Tooltip("Gravity value (negative).")]
    public float gravity = -9.81f;

    [Tooltip("Small downward velocity to keep the character grounded.")]
    public float groundedGravity = -2f;

    [Tooltip("Speed multiplier when sprinting.")]
    public float sprintMultiplier = 1.5f;

    private CharacterController characterController;
    private Vector3 verticalVelocity;
    private bool isGrounded;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        if (InputManager.Instance == null)
            return;

        HandleMovement();
        HandleJump();

        // reset one?frame button flags in InputManager
        InputManager.Instance.ResetButtonFlags();
    }

    private void HandleMovement()
    {
        Vector2 moveInput = InputManager.Instance.MoveInput;
        Vector3 moveDirection = Vector3.zero;

        // Movement relative to camera:
        // W/S ? move forward/back along camera forward,
        // A/D ? move left/right along camera right (strafe).
        if (moveInput.sqrMagnitude > 0.001f && cameraTransform != null)
        {
            Vector3 forward = cameraTransform.forward;
            forward.y = 0f;
            forward.Normalize();

            Vector3 right = cameraTransform.right;
            right.y = 0f;
            right.Normalize();

            moveDirection = forward * moveInput.y + right * moveInput.x;
            moveDirection.Normalize();
        }

        float speed = 5f;
        float rotationSpeed = 720f;

        if (playerStats != null && playerStats.playerData != null)
        {
            speed = playerStats.playerData.moveSpeed;
            rotationSpeed = playerStats.playerData.rotationSpeed;
        }

        if (InputManager.Instance.IsSprintHeld())
        {
            speed *= sprintMultiplier;
        }

        Vector3 horizontalVelocity = moveDirection * speed;

        isGrounded = characterController.isGrounded;

        if (isGrounded && verticalVelocity.y < 0f)
        {
            verticalVelocity.y = groundedGravity;
        }

        verticalVelocity.y += gravity * Time.deltaTime;

        Vector3 velocity = horizontalVelocity + verticalVelocity;

        characterController.Move(velocity * Time.deltaTime);

        // Strafing-style rotation:
        // visual model always faces camera forward on XZ plane,
        // movement can be forward/back/strafe relative to camera.
        if (cameraTransform != null && visualRoot != null)
        {
            Vector3 cameraForward = cameraTransform.forward;
            cameraForward.y = 0f;
            cameraForward.Normalize();

            if (cameraForward.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(cameraForward);
                visualRoot.rotation = Quaternion.Slerp(
                    visualRoot.rotation,
                    targetRotation,
                    rotationSpeed * Mathf.Deg2Rad * Time.deltaTime
                );
            }
        }
    }

    private void HandleJump()
    {
        if (!isGrounded)
            return;

        if (InputManager.Instance.IsJumpPressed())
        {
            float jumpForce = 5f;

            if (playerStats != null && playerStats.playerData != null)
            {
                jumpForce = playerStats.playerData.jumpForce;
            }

            verticalVelocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        }
    }
}