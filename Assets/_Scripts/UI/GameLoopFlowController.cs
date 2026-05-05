using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Связывает lose/win UI с состояниями игры и переходом на следующий уровень.
/// </summary>
public class GameLoopFlowController : MonoBehaviour
{
    [Header("Lose UI (scene canvas or prefab)")]
    [SerializeField] private GameObject losePanel;
    [SerializeField] private Button loseRestartButton;
    [SerializeField] private Button loseMenuButton;

    [Header("Win UI (scene canvas or prefab)")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private Button winMenuButton;

    [FormerlySerializedAs("winNextWaveButton")]
    [SerializeField] private Button winNextLevelButton;

    [Header("Shared UI")]
    [SerializeField] private GameObject pausePanel;

    [Header("Win Condition")]
    [Tooltip("Exit object that becomes active after encounter completion.")]
    [SerializeField] private GameObject exitActivationObjectOverride;

    [Tooltip("ID обязательного encounter для победы через выход. Если пусто, фильтр по ID отключён.")]
    [SerializeField] private string requiredEncounterIdForWin = string.Empty;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private PlayerStats playerStats;
    private bool flowFinished;
    private bool isEncounterCompletedForWin;

    // Legacy-событие оставлено для обратной совместимости старых учебных подписок.
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
        SubscribeToEncounterCompleted();
        BindButtons();
    }

    private void OnDisable()
    {
        UnsubscribeFromPlayerDeath();
        UnsubscribeFromEncounterCompleted();
        UnbindButtons();
    }

    /// <summary>
    /// Запрашивает победу от exit-триггера с проверкой условий win.
    /// </summary>
    public void RequestWinFromExit()
    {
        if (!CanTriggerWin())
            return;

        if (exitActivationObjectOverride != null && !exitActivationObjectOverride.activeInHierarchy)
            return;

        if (!CanWinByEncounterRule())
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

        if (winNextLevelButton == null)
            Debug.LogError($"{name}: winNextLevelButton is not assigned.", this);

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

        if (winNextLevelButton != null)
            winNextLevelButton.onClick.AddListener(HandleWinNextLevelClicked);
    }

    private void UnbindButtons()
    {
        if (loseRestartButton != null)
            loseRestartButton.onClick.RemoveListener(HandleLoseRestartClicked);

        if (loseMenuButton != null)
            loseMenuButton.onClick.RemoveListener(HandleMenuClicked);

        if (winMenuButton != null)
            winMenuButton.onClick.RemoveListener(HandleMenuClicked);

        if (winNextLevelButton != null)
            winNextLevelButton.onClick.RemoveListener(HandleWinNextLevelClicked);
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

    private void SubscribeToEncounterCompleted()
    {
        if (EventBus.Instance != null)
            EventBus.Instance.OnEncounterCompleted += HandleEncounterCompleted;
    }

    private void UnsubscribeFromEncounterCompleted()
    {
        if (EventBus.Instance != null)
            EventBus.Instance.OnEncounterCompleted -= HandleEncounterCompleted;
    }

    /// <summary>
    /// Отмечает completion encounter, который разрешает победу через выход.
    /// </summary>
    private void HandleEncounterCompleted(string encounterId)
    {
        if (string.IsNullOrWhiteSpace(requiredEncounterIdForWin))
        {
            isEncounterCompletedForWin = true;
            if (showDebugLogs)
                Debug.Log($"{name}: encounter '{encounterId}' завершён. Фильтр ID отключён, win-разрешение обновлено.", this);
            return;
        }

        if (string.Equals(requiredEncounterIdForWin, encounterId, StringComparison.Ordinal))
        {
            isEncounterCompletedForWin = true;
            if (showDebugLogs)
                Debug.Log($"{name}: encounter '{encounterId}' совпал с requiredEncounterIdForWin. Win-разрешение обновлено.", this);
        }
    }

    private bool CanTriggerWin()
    {
        if (flowFinished)
            return false;

        if (GameManager.Instance == null)
            return false;

        return GameManager.Instance.CurrentState == GameState.Playing;
    }

    private bool CanWinByEncounterRule()
    {
        if (string.IsNullOrWhiteSpace(requiredEncounterIdForWin))
            return true;

        if (isEncounterCompletedForWin)
            return true;

        if (showDebugLogs)
            Debug.Log($"{name}: win отклонён. Encounter '{requiredEncounterIdForWin}' ещё не завершён.", this);

        return false;
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

    /// <summary>
    /// Обработчик кнопки перехода на следующий уровень с win-экрана.
    /// </summary>
    private void HandleWinNextLevelClicked()
    {
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.TryLoadNextLevel())
                return;

            // Если следующего уровня нет, завершаем прогон и уходим в меню.
            GameManager.Instance.GoToMenu();
            return;
        }

        if (OnNextWaveRequested != null)
        {
            OnNextWaveRequested.Invoke();
            return;
        }

        Debug.LogWarning($"{name}: GameManager is missing. Next level flow cannot continue.", this);
    }
}
