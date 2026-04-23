using System;
using UnityEngine;
using UnityEngine.UI;

public class GameLoopFlowController : MonoBehaviour
{
    [Header("Lose UI (scene canvas or prefab)")]
    [SerializeField] private GameObject losePanel;
    [SerializeField] private Button loseRestartButton;
    [SerializeField] private Button loseMenuButton;

    [Header("Win UI (scene canvas or prefab)")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private Button winMenuButton;
    [SerializeField] private Button winNextWaveButton;

    [Header("Shared UI")]
    [SerializeField] private GameObject pausePanel;

    [Header("Win Condition")]
    [Tooltip("Exit object that becomes active after encounter completion.")]
    [SerializeField] private GameObject exitActivationObjectOverride;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private PlayerStats playerStats;

    private bool flowFinished;
    private bool initialized;
    private bool isSubscribedToDeath;
    private bool isUiSetupValid;

    public event Action OnNextWaveRequested;

    private void OnEnable()
    {
        InitializeIfNeeded();
        TrySubscribeToPlayerDeath();
        BindButtons();
    }

    private void OnDisable()
    {
        UnsubscribeFromPlayerDeath();
        UnbindButtons();
    }

    private void Update()
    {
        InitializeIfNeeded();
        TrySubscribeToPlayerDeath();
    }

    public void RequestWinFromExit()
    {
        if (!CanCheckWinCondition())
            return;

        if (exitActivationObjectOverride != null && !exitActivationObjectOverride.activeInHierarchy)
            return;

        TriggerWin();
    }

    private void InitializeIfNeeded()
    {
        if (initialized)
            return;

        TryResolvePlayer();
        isUiSetupValid = ValidateUiSetup();
        HideAllScreens();

        initialized = true;
    }

    private void TryResolvePlayer()
    {
        if (playerStats == null)
            playerStats = FindFirstObjectByType<PlayerStats>();
    }

    private bool ValidateUiSetup()
    {
        bool isValid = true;

        if (losePanel == null)
        {
            Debug.LogError($"{name}: losePanel is not assigned.", this);
            isValid = false;
        }

        if (loseRestartButton == null)
        {
            Debug.LogError($"{name}: loseRestartButton is not assigned.", this);
            isValid = false;
        }

        if (loseMenuButton == null)
        {
            Debug.LogError($"{name}: loseMenuButton is not assigned.", this);
            isValid = false;
        }

        if (winPanel == null)
        {
            Debug.LogError($"{name}: winPanel is not assigned.", this);
            isValid = false;
        }

        if (winMenuButton == null)
        {
            Debug.LogError($"{name}: winMenuButton is not assigned.", this);
            isValid = false;
        }

        if (winNextWaveButton == null)
        {
            Debug.LogError($"{name}: winNextWaveButton is not assigned.", this);
            isValid = false;
        }

        if (pausePanel == null && showDebugLogs)
            Debug.LogWarning($"{name}: pausePanel is not assigned. Pause UI will not be hidden on lose/win.", this);

        if (exitActivationObjectOverride == null && showDebugLogs)
            Debug.LogWarning($"{name}: exitActivationObjectOverride is not assigned. Win can still be requested by trigger.", this);

        return isValid;
    }

    private void BindButtons()
    {
        UnbindButtons();

        if (loseRestartButton != null)
            loseRestartButton.onClick.AddListener(HandleLoseRestartClicked);

        if (loseMenuButton != null)
            loseMenuButton.onClick.AddListener(HandleMenuClicked);

        if (winMenuButton != null)
            winMenuButton.onClick.AddListener(HandleMenuClicked);

        if (winNextWaveButton != null)
            winNextWaveButton.onClick.AddListener(HandleWinNextWaveClicked);
    }

    private void UnbindButtons()
    {
        if (loseRestartButton != null)
            loseRestartButton.onClick.RemoveListener(HandleLoseRestartClicked);

        if (loseMenuButton != null)
            loseMenuButton.onClick.RemoveListener(HandleMenuClicked);

        if (winMenuButton != null)
            winMenuButton.onClick.RemoveListener(HandleMenuClicked);

        if (winNextWaveButton != null)
            winNextWaveButton.onClick.RemoveListener(HandleWinNextWaveClicked);
    }

    private void TrySubscribeToPlayerDeath()
    {
        if (isSubscribedToDeath)
            return;

        if (playerStats == null)
            TryResolvePlayer();

        if (playerStats == null)
            return;

        playerStats.OnDeath += HandlePlayerDeath;
        isSubscribedToDeath = true;
    }

    private void UnsubscribeFromPlayerDeath()
    {
        if (!isSubscribedToDeath || playerStats == null)
            return;

        playerStats.OnDeath -= HandlePlayerDeath;
        isSubscribedToDeath = false;
    }

    private bool CanCheckWinCondition()
    {
        if (flowFinished)
            return false;

        if (GameManager.Instance == null)
            return false;

        return GameManager.Instance.CurrentState == GameState.Playing;
    }

    private void HandlePlayerDeath()
    {
        TriggerLose();
    }

    private void TriggerLose()
    {
        if (flowFinished)
            return;

        flowFinished = true;
        HidePausePanelIfAssigned();

        if (GameManager.Instance != null)
            GameManager.Instance.EnterLoseState();

        if (isUiSetupValid && losePanel != null)
            losePanel.SetActive(true);
        else
            Debug.LogWarning($"{name}: lose state triggered, but lose UI is not fully configured.", this);

        if (showDebugLogs)
            Debug.Log($"{name}: lose screen shown.", this);
    }

    private void TriggerWin()
    {
        if (flowFinished)
            return;

        flowFinished = true;
        HidePausePanelIfAssigned();

        if (GameManager.Instance != null)
            GameManager.Instance.EnterWinState();

        if (isUiSetupValid && winPanel != null)
            winPanel.SetActive(true);
        else
            Debug.LogWarning($"{name}: win state triggered, but win UI is not fully configured.", this);

        if (showDebugLogs)
            Debug.Log($"{name}: win screen shown.", this);
    }

    private void HidePausePanelIfAssigned()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    private void HideAllScreens()
    {
        if (losePanel != null)
            losePanel.SetActive(false);

        if (winPanel != null)
            winPanel.SetActive(false);
    }

    private void HandleLoseRestartClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RestartGameScene();
    }

    private void HandleMenuClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.GoToMenu();
    }

    private void HandleWinNextWaveClicked()
    {
        if (OnNextWaveRequested != null)
        {
            OnNextWaveRequested.Invoke();
            return;
        }

        Debug.Log($"{name}: Next Wave clicked. Implementation will be added in a later lesson.", this);
    }
}
