using UnityEngine;
/*
 * MainMenuController
 * Назначение: контроллер UI главного меню.
 * Что делает: подключает кнопки к действиям игры (старт новой игры, выход).
 * Связи: вызывает методы `GameManager` (переход в игровой процесс) и системный `Application.Quit()`.
 * Паттерны: MVC‑подобный подход в Unity UI (контроллер/презентер над сценой меню).
 */
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private Button buttonNewGame;
    [SerializeField] private Button buttonExit;

/// <summary>
/// Привязывает обработчики кликов к кнопкам меню.
/// </summary>


    private void Start()
    {
        // Подключаем кнопки через код (связь UI → GameManager).
        buttonNewGame.onClick.AddListener(() => GameManager.Instance.StartGame());
        buttonExit.onClick.AddListener(() => Application.Quit());
    }
}