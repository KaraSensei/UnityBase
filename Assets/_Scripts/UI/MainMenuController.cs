using UnityEngine;
using UnityEngine.UI;

/*
 * MainMenuController
 * Назначение: контроллер UI главного меню.
 * Что делает: подключает кнопки "New Game" и "Exit" к действиям приложения.
 * Связи: использует GameManager для старта игры и Application.Quit для выхода.
 * Паттерны: UI-controller (тонкий слой между кнопками и core-логикой).
 */
public class MainMenuController : MonoBehaviour
{
    [SerializeField] private Button buttonNewGame;
    [SerializeField] private Button buttonExit;

    /// <summary>
    /// Подключает обработчики кнопок.
    /// Добавлены null-проверки, чтобы ошибка в инспекторе не приводила к silent-fail.
    /// </summary>
    private void Start()
    {
        if (buttonNewGame == null)
        {
            Debug.LogError($"{name}: buttonNewGame is not assigned.", this);
        }
        else
        {
            buttonNewGame.onClick.AddListener(HandleNewGameClicked);
        }

        if (buttonExit == null)
        {
            Debug.LogError($"{name}: buttonExit is not assigned.", this);
        }
        else
        {
            buttonExit.onClick.AddListener(HandleExitClicked);
        }
    }

    private void OnDestroy()
    {
        if (buttonNewGame != null)
            buttonNewGame.onClick.RemoveListener(HandleNewGameClicked);

        if (buttonExit != null)
            buttonExit.onClick.RemoveListener(HandleExitClicked);
    }

    /// <summary>
    /// Обработчик старта новой игры.
    /// </summary>
    private void HandleNewGameClicked()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError($"{name}: GameManager.Instance is null. Cannot start game.", this);
            return;
        }

        GameManager.Instance.StartGame();
    }

    /// <summary>
    /// Обработчик выхода из приложения.
    /// </summary>
    private static void HandleExitClicked()
    {
        Application.Quit();
    }
}
