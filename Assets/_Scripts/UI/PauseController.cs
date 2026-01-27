using UnityEngine;
using UnityEngine.UI;

public class PauseController : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button buttonResume;
    [SerializeField] private Button buttonMainMenu;

    private void OnEnable()
    {
        // Подписываемся на события EventBus при включении объекта
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

    private void OnDisable()
    {
        // Отписываемся от событий при выключении объекта (ВАЖНО для предотвращения утечек памяти!)
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

    private void Start()
    {
        // Подключаем кнопки (Start вызывается после всех Awake, поэтому менеджеры уже созданы)
        if (buttonResume != null)
            buttonResume.onClick.AddListener(OnResumeClicked);
        if (buttonMainMenu != null)
            buttonMainMenu.onClick.AddListener(OnMainMenuClicked);
    }

    // Эти методы вызываются автоматически через EventBus
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

    // Обработчики кнопок
    private void OnResumeClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.Resume(); // вызовет EventBus, который скроет панель
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
