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

    public event Action OnNextWaveRequested;

    private void Awake()
    {
        ResolvePlayerStats();
        ValidateReferences();
        HideAllScreens();
    }

    private void OnEnable()
    {
        SubscribeToPlayerDeath();
        BindButtons();
    }

    private void OnDisable()
    {
        UnsubscribeFromPlayerDeath();
        UnbindButtons();
    }

    public void RequestWinFromExit()
    {
        if (!CanTriggerWin())
            return;

        if (exitActivationObjectOverride != null && !exitActivationObjectOverride.activeInHierarchy)
            return;

        TriggerWin();
    }

    private void ResolvePlayerStats()
    {
        if (playerStats == null)
            playerStats = FindFirstObjectByType<PlayerStats>();
    }

    private void ValidateReferences()
    {
        if (losePanel == null)
            Debug.LogError($"{name}: losePanel is not assigned.", this);

        if (loseRestartButton == null)
            Debug.LogError($"{name}: loseRestartButton is not assigned.", this);

        if (loseMenuButton == null)
            Debug.LogError($"{name}: loseMenuButton is not assigned.", this);

        if (winPanel == null)
            Debug.LogError($"{name}: winPanel is not assigned.", this);

        if (winMenuButton == null)
            Debug.LogError($"{name}: winMenuButton is not assigned.", this);

        if (winNextWaveButton == null)
            Debug.LogError($"{name}: winNextWaveButton is not assigned.", this);

        if (pausePanel == null && showDebugLogs)
            Debug.LogWarning($"{name}: pausePanel is not assigned. Pause UI will not be hidden on lose/win.", this);

        if (exitActivationObjectOverride == null && showDebugLogs)
            Debug.LogWarning($"{name}: exitActivationObjectOverride is not assigned. Win can still be requested by trigger.", this);
    }

    private void BindButtons()
    {
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

    private void SubscribeToPlayerDeath()
    {
        if (playerStats == null)
            ResolvePlayerStats();

        if (playerStats == null)
        {
            if (showDebugLogs)
                Debug.LogWarning($"{name}: PlayerStats not found. Lose on death is disabled.", this);

            return;
        }

        playerStats.OnDeath += HandlePlayerDeath;
    }

    private void UnsubscribeFromPlayerDeath()
    {
        if (playerStats != null)
            playerStats.OnDeath -= HandlePlayerDeath;
    }

    private bool CanTriggerWin()
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

        if (losePanel != null)
            losePanel.SetActive(true);
        else
            Debug.LogWarning($"{name}: lose state triggered, but losePanel is not assigned.", this);

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

        if (winPanel != null)
            winPanel.SetActive(true);
        else
            Debug.LogWarning($"{name}: win state triggered, but winPanel is not assigned.", this);

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
