using UnityEngine;
using UnityEngine.UI;

/*
 * MainMenuController
 * Назначение: связывает кнопки главного меню с игровым потоком и окном настроек.
 * Роль в игре: запускает игру, открывает отдельную панель настроек, завершает приложение.
 * Связи: GameManager, SettingsPanelController, кнопки UI главного меню.
 * Как используется: вешается в MainMenu-сцене, ссылки задаются в Inspector.
 * Идеи расширения:
 * - Добавить подтверждение перед выходом из приложения.
 * - Добавить звуковые эффекты на клики кнопок.
 * Практические советы:
 * - Для teacher repo лучше явные инспекторные связи, без автопоиска/автодобавления.
 * - Если Settings не открывается, сначала проверьте ссылку settingsPanelController.
 */
public class MainMenuController : MonoBehaviour
{
    [Header("Кнопки главного меню")]
    [Tooltip("Кнопка начала новой игры.")]
    [SerializeField] private Button buttonNewGame;

    [Tooltip("Кнопка открытия окна настроек.")]
    [SerializeField] private Button buttonSettings;

    [Tooltip("Кнопка выхода из приложения.")]
    [SerializeField] private Button buttonExit;

    [Header("Окно настроек")]
    [Tooltip("Контроллер отдельной панели настроек в MainMenu.")]
    [SerializeField] private SettingsPanelController settingsPanelController;

    private void Start()
    {
        ValidateReferences();
    }

    private void OnEnable()
    {
        if (buttonNewGame != null)
            buttonNewGame.onClick.AddListener(HandleNewGameClicked);

        if (buttonExit != null)
            buttonExit.onClick.AddListener(HandleExitClicked);

        if (buttonSettings != null)
            buttonSettings.onClick.AddListener(HandleSettingsClicked);
    }

    private void OnDisable()
    {
        if (buttonNewGame != null)
            buttonNewGame.onClick.RemoveListener(HandleNewGameClicked);

        if (buttonExit != null)
            buttonExit.onClick.RemoveListener(HandleExitClicked);

        if (buttonSettings != null)
            buttonSettings.onClick.RemoveListener(HandleSettingsClicked);
    }

    private void ValidateReferences()
    {
        if (buttonNewGame == null)
            Debug.LogError($"{name}: buttonNewGame не назначен.", this);

        if (buttonExit == null)
            Debug.LogError($"{name}: buttonExit не назначен.", this);

        if (buttonSettings == null)
            Debug.LogWarning($"{name}: buttonSettings не назначен.", this);

        if (settingsPanelController == null)
            Debug.LogError($"{name}: settingsPanelController не назначен в Inspector.", this);
    }

    private void HandleNewGameClicked()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError($"{name}: GameManager.Instance == null. Нельзя начать игру.", this);
            return;
        }

        GameManager.Instance.StartGame();
    }

    private static void HandleExitClicked()
    {
        Application.Quit();
    }

    /// <summary>
    /// Контракт: открывает отдельную панель настроек из главного меню.
    /// Почему так: MainMenu использует оконный сценарий, отдельно от встроенных настроек Pause.
    /// Как дебажить: если окно не открылось, проверьте settingsPanelController и ссылку settingsPanel внутри него.
    /// </summary>
    private void HandleSettingsClicked()
    {
        if (settingsPanelController == null)
        {
            Debug.LogError($"{name}: settingsPanelController отсутствует. Назначьте ссылку в Inspector.", this);
            return;
        }

        settingsPanelController.OpenPanel();
    }
}
