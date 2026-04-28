using UnityEngine;
using UnityEngine.UI;

/*
 * PauseController
 * Назначение: управление pause UI и реакцией на pause/cancel ввод.
 * Что делает:
 *  - слушает события паузы/возобновления из EventBus;
 *  - открывает/закрывает pausePanel;
 *  - обрабатывает кнопки Resume/Main Menu;
 *  - обрабатывает input-действия Pause/Cancel через InputManager.
 * Связи: EventBus, InputManager, GameManager.
 * Паттерны: event-driven UI controller.
 */
public class PauseController : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button buttonResume;
    [SerializeField] private Button buttonMainMenu;

    /// <summary>
    /// Подписывается на события при включении объекта.
    /// </summary>
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
    }

    /// <summary>
    /// Отписывается от событий при выключении объекта.
    /// </summary>
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
    }

    /// <summary>
    /// Привязывает UI-кнопки pause меню.
    /// </summary>
    private void Start()
    {
        if (buttonResume != null)
            buttonResume.onClick.AddListener(OnResumeClicked);

        if (buttonMainMenu != null)
            buttonMainMenu.onClick.AddListener(OnMainMenuClicked);
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
