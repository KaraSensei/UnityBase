using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Управляет flow урока 8:
/// - поражение при смерти игрока;
/// - победа при достижении выхода после активации encounter;
/// - показ lose/win экранов.
/// </summary>
public class GameLoopFlowController : MonoBehaviour
{
    [Header("Lose UI (обязательно из сцены/префаба)")]
    [SerializeField] private GameObject losePanel;
    [SerializeField] private Button loseRestartButton;
    [SerializeField] private Button loseMenuButton;

    [Header("Win UI (обязательно из сцены/префаба)")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private Button winMenuButton;
    [SerializeField] private Button winNextWaveButton;

    [Header("Win Condition")]
    [Tooltip("Точка выхода. Если не задана, будет использован Transform объекта exitActivationObjectOverride.")]
    [SerializeField] private Transform exitPointOverride;

    [Tooltip("Объект выхода, который становится активным после завершения encounter.")]
    [SerializeField] private GameObject exitActivationObjectOverride;

    [SerializeField, Min(0.5f)]
    [Tooltip("Дистанция, на которой считаем, что игрок дошёл до выхода.")]
    private float exitReachDistance = 2f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private PlayerStats playerStats;
    private Transform playerTransform;
    private Transform exitPoint;
    private GameObject exitActivationObject;

    private bool flowFinished;
    private bool initialized;
    private bool isSubscribedToDeath;
    private bool isUiSetupValid;

    /// <summary>
    /// Заглушка под следующий урок: внешний код сможет подписаться на запрос следующей волны.
    /// </summary>
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

        if (!CanCheckWinCondition())
            return;

        if (!IsExitUnlocked())
            return;

        if (playerTransform == null)
            TryResolvePlayer();

        if (playerTransform == null || exitPoint == null)
            return;

        float sqrDistance = (playerTransform.position - exitPoint.position).sqrMagnitude;
        float sqrReachDistance = exitReachDistance * exitReachDistance;
        if (sqrDistance > sqrReachDistance)
            return;

        TriggerWin();
    }

    private void InitializeIfNeeded()
    {
        if (initialized)
            return;

        TryResolvePlayer();
        ResolveExitTarget();
        isUiSetupValid = ValidateUiSetup();
        HideAllScreens();

        initialized = true;
    }

    private void TryResolvePlayer()
    {
        if (playerStats == null)
            playerStats = FindFirstObjectByType<PlayerStats>();

        if (playerStats != null)
            playerTransform = playerStats.transform;
    }

    private void ResolveExitTarget()
    {
        exitPoint = exitPointOverride;
        exitActivationObject = exitActivationObjectOverride;

        if (exitPoint == null && exitActivationObject != null)
            exitPoint = exitActivationObject.transform;

        if (showDebugLogs)
        {
            string exitName = exitActivationObject != null ? exitActivationObject.name : "null";
            Debug.Log($"{name}: выход для win flow = {exitName}", this);
        }
    }

    private bool ValidateUiSetup()
    {
        bool isValid = true;

        if (losePanel == null)
        {
            Debug.LogError($"{name}: losePanel не назначен. Назначьте панель из Canvas сцены.", this);
            isValid = false;
        }

        if (loseRestartButton == null)
        {
            Debug.LogError($"{name}: loseRestartButton не назначен. Назначьте кнопку из Lose панели.", this);
            isValid = false;
        }

        if (loseMenuButton == null)
        {
            Debug.LogError($"{name}: loseMenuButton не назначен. Назначьте кнопку из Lose панели.", this);
            isValid = false;
        }

        if (winPanel == null)
        {
            Debug.LogError($"{name}: winPanel не назначен. Назначьте панель из Canvas сцены.", this);
            isValid = false;
        }

        if (winMenuButton == null)
        {
            Debug.LogError($"{name}: winMenuButton не назначен. Назначьте кнопку из Win панели.", this);
            isValid = false;
        }

        if (winNextWaveButton == null)
        {
            Debug.LogError($"{name}: winNextWaveButton не назначен. Назначьте кнопку 'Next Wave' из Win панели.", this);
            isValid = false;
        }

        if (exitActivationObjectOverride == null)
        {
            Debug.LogError($"{name}: exitActivationObjectOverride не назначен. Нужен объект выхода, активируемый после encounter.", this);
            isValid = false;
        }

        if (exitPoint == null)
        {
            Debug.LogError($"{name}: точка выхода не определена. Назначьте exitPointOverride или exitActivationObjectOverride.", this);
            isValid = false;
        }

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

    private bool IsExitUnlocked()
    {
        if (exitActivationObject == null)
            return false;

        return exitActivationObject.activeInHierarchy;
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
        HidePausePanelIfOpen();

        if (GameManager.Instance != null)
            GameManager.Instance.EnterLoseState();

        if (isUiSetupValid && losePanel != null)
            losePanel.SetActive(true);
        else
            Debug.LogWarning($"{name}: lose state сработал, но UI не настроен в сцене.", this);

        if (showDebugLogs)
            Debug.Log($"{name}: показан Lose Screen.", this);
    }

    private void TriggerWin()
    {
        if (flowFinished)
            return;

        flowFinished = true;
        HidePausePanelIfOpen();

        if (GameManager.Instance != null)
            GameManager.Instance.EnterWinState();

        if (isUiSetupValid && winPanel != null)
            winPanel.SetActive(true);
        else
            Debug.LogWarning($"{name}: win state сработал, но UI не настроен в сцене.", this);

        if (showDebugLogs)
            Debug.Log($"{name}: показан Win Screen.", this);
    }

    private static void HidePausePanelIfOpen()
    {
        GameObject pausePanel = GameObject.Find("Pause");
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

        Debug.Log($"{name}: Next Wave нажата. Реализация перехода к следующей волне будет добавлена позже.", this);
    }
}
