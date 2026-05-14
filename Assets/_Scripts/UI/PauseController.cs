using UnityEngine;
using UnityEngine.UI;

/*
 * PauseController
 * Назначение: управляет показом/скрытием паузы и действиями Resume/Main Menu.
 * Роль в игре: обслуживает только pause-flow, без логики панели настроек.
 * Связи: EventBus (события паузы), InputManager (клавиши), GameManager (смена состояния), UI-кнопки.
 * Как используется: вешается в игровой сцене, ссылки на pausePanel и кнопки назначаются в Inspector.
 * Идеи расширения:
 * - Добавить установку фокуса на Resume при открытии паузы.
 * - Добавить анимацию появления/скрытия панели.
 * Практические советы:
 * - Если пауза не открывается с клавиши, проверьте InputManager.Instance и его подписки.
 * - Если кнопки молчат, сначала проверьте назначение buttonResume/buttonMainMenu в Inspector.
 */
public class PauseController : MonoBehaviour
{
    [Header("UI паузы")]
    [Tooltip("Корневой объект панели паузы.")]
    [SerializeField] private GameObject pausePanel;

    [Tooltip("Кнопка продолжения игры.")]
    [SerializeField] private Button buttonResume;

    [Tooltip("Кнопка возврата в главное меню.")]
    [SerializeField] private Button buttonMainMenu;

    /// <summary>
    /// Входные условия: gameplay-сцена только создала UI-объекты.
    /// Шаги: сразу скрыть pausePanel, чтобы active state из prefab или scene не переносил паузу между уровнями.
    /// Типичные поломки: панель была сохранена активной в prefab или scene, поэтому появлялась при загрузке уровня.
    /// Что проверить: active state pausePanel в Inspector и Console на ошибки ссылок.
    /// </summary>
    private void Awake()
    {
        HidePausePanel();
    }

    private void Start()
    {
        ValidateReferences();
        HidePausePanel();
    }

    private void OnEnable()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.OnGamePaused += ShowPausePanel;
            EventBus.Instance.OnGameResumed += HidePausePanel;
        }

        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnPausePressed += HandlePausePressed;
            InputManager.Instance.OnCancelPressed += HandleCancelPressed;
        }

        if (buttonResume != null)
            buttonResume.onClick.AddListener(OnResumeClicked);

        if (buttonMainMenu != null)
            buttonMainMenu.onClick.AddListener(OnMainMenuClicked);
    }

    private void OnDisable()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.OnGamePaused -= ShowPausePanel;
            EventBus.Instance.OnGameResumed -= HidePausePanel;
        }

        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnPausePressed -= HandlePausePressed;
            InputManager.Instance.OnCancelPressed -= HandleCancelPressed;
        }

        if (buttonResume != null)
            buttonResume.onClick.RemoveListener(OnResumeClicked);

        if (buttonMainMenu != null)
            buttonMainMenu.onClick.RemoveListener(OnMainMenuClicked);
    }

    private void ValidateReferences()
    {
        if (pausePanel == null)
            Debug.LogError($"{name}: pausePanel не назначен.", this);

        if (buttonResume == null)
            Debug.LogWarning($"{name}: buttonResume не назначен.", this);

        if (buttonMainMenu == null)
            Debug.LogWarning($"{name}: buttonMainMenu не назначен.", this);
    }

    private void ShowPausePanel()
    {
        if (pausePanel != null)
            pausePanel.SetActive(true);
    }

    private void HidePausePanel()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    private void OnResumeClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.Resume();
    }

    private void OnMainMenuClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.GoToMenu();
    }

    private void HandlePausePressed()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
            GameManager.Instance.Pause();
    }

    private void HandleCancelPressed()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Paused)
            GameManager.Instance.Resume();
    }
}
