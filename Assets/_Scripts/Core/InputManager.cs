using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    public InputActionAsset inputActions;

    // Action Maps
    private InputActionMap playerActionMap;
    private InputActionMap uiActionMap;

    // Player Actions
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction zoomAction;
    private InputAction jumpAction;
    private InputAction attackAction;
    private InputAction interactAction;
    private InputAction sprintAction;
    private InputAction crouchAction;
    private InputAction pauseAction;
    private InputAction cancelAction;

    // ÃÃ¥ÃªÃ³Ã¹Ã¨Ã¥ Ã§Ã­Ã Ã·Ã¥Ã­Ã¨Ã¿ (ÃªÃ½Ã¸Ã¨Ã°Ã³Ã¥Ã¬ Ã¤Ã«Ã¿ Ã¡Ã»Ã±Ã²Ã°Ã®Ã£Ã® Ã¤Ã®Ã±Ã²Ã³Ã¯Ã )
    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public float ZoomInput { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool AttackPressed { get; private set; }
    public bool InteractPressed { get; private set; }
    public bool SprintHeld { get; private set; }
    public bool CrouchHeld { get; private set; }

    // ÃÃ®Ã¡Ã»Ã²Ã¨Ã¿ Ã¤Ã«Ã¿ ÃªÃ­Ã®Ã¯Ã®Ãª (Ã¢Ã»Ã§Ã»Ã¢Ã Ã¾Ã²Ã±Ã¿ Ã®Ã¤Ã¨Ã­ Ã°Ã Ã§ Ã¯Ã°Ã¨ Ã­Ã Ã¦Ã Ã²Ã¨Ã¨)
    public System.Action OnJumpPressed;
    public System.Action OnAttackPressed;
    public System.Action OnInteractPressed;
    public System.Action OnPausePressed;
    public System.Action OnCancelPressed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        InitializeInputSystem();
    }

    private void InitializeInputSystem()
    {
        if (inputActions == null)
        {
            Debug.LogError("InputManager: Input Actions Asset Ã­Ã¥ Ã­Ã Ã§Ã­Ã Ã·Ã¥Ã­!");
            return;
        }

        // ÃÃ®Ã«Ã³Ã·Ã Ã¥Ã¬ Action Maps
        playerActionMap = inputActions.FindActionMap("Player");
        uiActionMap = inputActions.FindActionMap("UI");

        if (playerActionMap == null)
        {
            Debug.LogError("InputManager: Action Map 'Player' Ã­Ã¥ Ã­Ã Ã©Ã¤Ã¥Ã­!");
            return;
        }

        // ÃÃ®Ã«Ã³Ã·Ã Ã¥Ã¬ Actions Ã¨Ã§ Player Action Map
        moveAction = playerActionMap.FindAction("Move");
        lookAction = playerActionMap.FindAction("Look");
        zoomAction = playerActionMap.FindAction("Zoom");
        jumpAction = playerActionMap.FindAction("Jump");
        attackAction = playerActionMap.FindAction("Attack");
        interactAction = playerActionMap.FindAction("Interact");
        sprintAction = playerActionMap.FindAction("Sprint");
        crouchAction = playerActionMap.FindAction("Crouch");
        pauseAction = playerActionMap.FindAction("Pause");
        if (uiActionMap != null)
            cancelAction = uiActionMap.FindAction("Cancel");

        // ÃÃ®Ã¤Ã¯Ã¨Ã±Ã»Ã¢Ã Ã¥Ã¬Ã±Ã¿ Ã­Ã  Ã±Ã®Ã¡Ã»Ã²Ã¨Ã¿ ÃªÃ­Ã®Ã¯Ã®Ãª
        if (jumpAction != null)
            jumpAction.performed += OnJumpPerformed;
        if (attackAction != null)
            attackAction.performed += OnAttackPerformed;
        if (interactAction != null)
            interactAction.performed += OnInteractPerformed;
        if (pauseAction != null)
            pauseAction.performed += OnPausePerformed;
        if (cancelAction != null)
            cancelAction.performed += OnCancelPerformed;

        // ÃÃªÃ«Ã¾Ã·Ã Ã¥Ã¬ Player Action Map Ã¯Ã® Ã³Ã¬Ã®Ã«Ã·Ã Ã­Ã¨Ã¾
        EnablePlayerInput();
    }

    private void OnEnable()
    {
        // ÃÃªÃ«Ã¾Ã·Ã Ã¥Ã¬ Input Actions Ã¯Ã°Ã¨ Ã¢ÃªÃ«Ã¾Ã·Ã¥Ã­Ã¨Ã¨ Ã®Ã¡ÃºÃ¥ÃªÃ²Ã 
        if (inputActions != null)
            inputActions.Enable();

        // ÃÃ®Ã¤Ã¯Ã¨Ã±Ã»Ã¢Ã Ã¥Ã¬Ã±Ã¿ Ã­Ã  Ã±Ã®Ã¡Ã»Ã²Ã¨Ã¿ Ã¯Ã Ã³Ã§Ã» Ã·Ã¥Ã°Ã¥Ã§ EventBus
        if (EventBus.Instance != null)
        {
            EventBus.Instance.OnGamePaused += HandleGamePaused;
            EventBus.Instance.OnGameResumed += HandleGameResumed;
        } 
    }

    private void OnDisable()
    {
        // ÃÃ»ÃªÃ«Ã¾Ã·Ã Ã¥Ã¬ Input Actions Ã¯Ã°Ã¨ Ã¢Ã»ÃªÃ«Ã¾Ã·Ã¥Ã­Ã¨Ã¨ Ã®Ã¡ÃºÃ¥ÃªÃ²Ã 
        if (inputActions != null)
            inputActions.Disable();

        // ÃÃ²Ã¯Ã¨Ã±Ã»Ã¢Ã Ã¥Ã¬Ã±Ã¿ Ã®Ã² Ã±Ã®Ã¡Ã»Ã²Ã¨Ã© Ã¯Ã Ã³Ã§Ã» (ÃÃÃÃÃ Ã¤Ã«Ã¿ Ã¯Ã°Ã¥Ã¤Ã®Ã²Ã¢Ã°Ã Ã¹Ã¥Ã­Ã¨Ã¿ Ã³Ã²Ã¥Ã·Ã¥Ãª Ã¯Ã Ã¬Ã¿Ã²Ã¨!)
        if (EventBus.Instance != null)
        {
            EventBus.Instance.OnGamePaused -= HandleGamePaused;
            EventBus.Instance.OnGameResumed -= HandleGameResumed;
        }
    }

    private void OnDestroy()
    {
        // ÃÃ²Ã¯Ã¨Ã±Ã»Ã¢Ã Ã¥Ã¬Ã±Ã¿ Ã®Ã² Ã±Ã®Ã¡Ã»Ã²Ã¨Ã©
        if (jumpAction != null)
            jumpAction.performed -= OnJumpPerformed;
        if (attackAction != null)
            attackAction.performed -= OnAttackPerformed;
        if (interactAction != null)
            interactAction.performed -= OnInteractPerformed;
        if (pauseAction != null)
            pauseAction.performed -= OnPausePerformed;
        if (cancelAction != null)
            cancelAction.performed -= OnCancelPerformed;
    }

    private void Update()
    {
        // ÃÃ¡Ã­Ã®Ã¢Ã«Ã¿Ã¥Ã¬ Ã§Ã­Ã Ã·Ã¥Ã­Ã¨Ã¿ Ã¢Ã¢Ã®Ã¤Ã  ÃªÃ Ã¦Ã¤Ã»Ã© ÃªÃ Ã¤Ã°
        UpdateInputValues();
    }

    private void UpdateInputValues()
    {
        // ÃÃ¨Ã²Ã Ã¥Ã¬ Ã²Ã¥ÃªÃ³Ã¹Ã¨Ã¥ Ã§Ã­Ã Ã·Ã¥Ã­Ã¨Ã¿ Ã¤Ã¥Ã©Ã±Ã²Ã¢Ã¨Ã©
        MoveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        LookInput = lookAction != null ? lookAction.ReadValue<Vector2>() : Vector2.zero;
        ZoomInput = zoomAction != null ? zoomAction.ReadValue<Vector2>().y : 0f;
        SprintHeld = sprintAction != null && sprintAction.IsPressed();
        CrouchHeld = crouchAction != null && crouchAction.IsPressed();

        // ÃÃ«Ã¿ ÃªÃ­Ã®Ã¯Ã®Ãª Ã¨Ã±Ã¯Ã®Ã«Ã¼Ã§Ã³Ã¥Ã¬ Ã±Ã®Ã¡Ã»Ã²Ã¨Ã¿ (OnJumpPerformed Ã¨ Ã².Ã¤.)
        // ÃÃ® Ã²Ã ÃªÃ¦Ã¥ Ã¬Ã®Ã¦Ã­Ã® Ã¯Ã°Ã®Ã¢Ã¥Ã°Ã¨Ã²Ã¼ Ã·Ã¥Ã°Ã¥Ã§ IsPressed() Ã¤Ã«Ã¿ Ã³Ã¤Ã¥Ã°Ã¦Ã Ã­Ã¨Ã¿
    }

    // ÃÃ¡Ã°Ã Ã¡Ã®Ã²Ã·Ã¨ÃªÃ¨ Ã±Ã®Ã¡Ã»Ã²Ã¨Ã© ÃªÃ­Ã®Ã¯Ã®Ãª
    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        JumpPressed = true;
        OnJumpPressed?.Invoke();
    }

    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        AttackPressed = true;
        OnAttackPressed?.Invoke();
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        InteractPressed = true;
        OnInteractPressed?.Invoke();
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        OnPausePressed?.Invoke();
    }

    private void OnCancelPerformed(InputAction.CallbackContext context)
    {
        OnCancelPressed?.Invoke();
    }


    // ÃÃ¥Ã²Ã®Ã¤Ã» Ã¤Ã«Ã¿ Ã±Ã¡Ã°Ã®Ã±Ã  Ã´Ã«Ã Ã£Ã®Ã¢ (Ã¢Ã»Ã§Ã»Ã¢Ã Ã¾Ã²Ã±Ã¿ Ã¢ ÃªÃ®Ã­Ã¶Ã¥ ÃªÃ Ã¤Ã°Ã )
    public void ResetButtonFlags()
    {
        JumpPressed = false;
        AttackPressed = false;
        InteractPressed = false;
    }

    // ÃÃ¥Ã²Ã®Ã¤Ã» Ã¤Ã«Ã¿ Ã¯Ã¥Ã°Ã¥ÃªÃ«Ã¾Ã·Ã¥Ã­Ã¨Ã¿ Action Maps
    public void EnablePlayerInput()
    {
        if (playerActionMap != null)
            playerActionMap.Enable();
        if (uiActionMap != null)
            uiActionMap.Disable();
    }

    public void EnableUIInput()
    {
        if (playerActionMap != null)
            playerActionMap.Disable();
        if (uiActionMap != null)
            uiActionMap.Enable();
    }

    // ÃÃ¡Ã°Ã Ã¡Ã®Ã²Ã·Ã¨ÃªÃ¨ Ã±Ã®Ã¡Ã»Ã²Ã¨Ã© Ã¯Ã Ã³Ã§Ã» (Ã¨Ã­Ã²Ã¥Ã£Ã°Ã Ã¶Ã¨Ã¿ Ã± EventBus)
    private void HandleGamePaused()
    {
        // ÃÃ°Ã¨ Ã¯Ã Ã³Ã§Ã¥ Ã®Ã²ÃªÃ«Ã¾Ã·Ã Ã¥Ã¬ Player Action Map
        // ÃÃ²Ã® Ã Ã¢Ã²Ã®Ã¬Ã Ã²Ã¨Ã·Ã¥Ã±ÃªÃ¨ Ã¡Ã«Ã®ÃªÃ¨Ã°Ã³Ã¥Ã² Ã¢Ã±Ã¥ Ã¤Ã¥Ã©Ã±Ã²Ã¢Ã¨Ã¿ Ã³Ã¯Ã°Ã Ã¢Ã«Ã¥Ã­Ã¨Ã¿ Ã¨Ã£Ã°Ã®ÃªÃ®Ã¬
        if (playerActionMap != null)
            playerActionMap.Disable();

        Debug.Log("InputManager: Player input disabled (game paused)");
    }

    private void HandleGameResumed()
    {
        // ÃÃ°Ã¨ Ã¢Ã®Ã§Ã®Ã¡Ã­Ã®Ã¢Ã«Ã¥Ã­Ã¨Ã¨ Ã¨Ã£Ã°Ã» Ã¢ÃªÃ«Ã¾Ã·Ã Ã¥Ã¬ Player Action Map Ã®Ã¡Ã°Ã Ã²Ã­Ã®
        if (playerActionMap != null)
            playerActionMap.Enable();

        Debug.Log("InputManager: Player input enabled (game resumed)");
    }

    // ÃÃ³Ã¡Ã«Ã¨Ã·Ã­Ã»Ã¥ Ã¬Ã¥Ã²Ã®Ã¤Ã» Ã¤Ã«Ã¿ Ã¯Ã®Ã«Ã³Ã·Ã¥Ã­Ã¨Ã¿ Ã¢Ã¢Ã®Ã¤Ã  (Ã Ã«Ã¼Ã²Ã¥Ã°Ã­Ã Ã²Ã¨Ã¢Ã  Ã±Ã¢Ã®Ã©Ã±Ã²Ã¢Ã Ã¬)
    public Vector2 GetMoveInput()
    {
        return MoveInput;
    }

    public Vector2 GetLookInput()
    {
        return LookInput;
    }

    public float GetZoomInput()
    {
        return ZoomInput;
    }

    public bool IsJumpPressed()
    {
        return JumpPressed;
    }

    public bool IsAttackPressed()
    {
        return AttackPressed;
    }

    public bool IsInteractPressed()
    {
        return InteractPressed;
    }

    public bool IsSprintHeld()
    {
        return SprintHeld;
    }

    public bool IsCrouchHeld()
    {
        return CrouchHeld;
    }
}
