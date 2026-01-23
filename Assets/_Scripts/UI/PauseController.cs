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
    }

    private void OnDisable()
    {
        // Отписываемся от событий при выключении объекта (ВАЖНО для предотвращения утечек памяти!)
        if (EventBus.Instance != null)
        {
            EventBus.Instance.OnGamePaused -= ShowPausePanel;
            EventBus.Instance.OnGameResumed -= HidePausePanel;
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

    private void Update()
    {
        // Проверяем нажатие Esc
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    private void TogglePause()
    {
        // Проверяем, что менеджеры созданы
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameManager не создан! Запустите игру через Bootstrap.");
            return;
        }

        // Просто вызываем методы GameManager
        // EventBus автоматически покажет/скроет панель
        if (GameManager.Instance.CurrentState == GameState.Playing)
        {
            GameManager.Instance.Pause(); // вызовет EventBus.Instance.RaiseGamePaused()
        }
        else if (GameManager.Instance.CurrentState == GameState.Paused)
        {
            GameManager.Instance.Resume(); // вызовет EventBus.Instance.RaiseGameResumed()
        }
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
}
