/*
 * GameManager
 * Назначение: центральный менеджер состояния игры (меню, игра, пауза).
 * Что делает: управляет состоянием GameState, временем (Time.timeScale), загрузкой сцен и переключением ввода.
 * Связи: использует SceneLoader, InputManager, EventBus, SceneNames; Singleton через статическое свойство Instance.
 * Паттерны: Singleton, простая машина состояний (state machine), Facade над SceneLoader и EventBus.
 */

using UnityEngine;

public enum GameState
{
    Menu,
    Playing,
    Paused,
    Lost,
    Won,
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    /// <summary>
    /// Текущее состояние игры (меню / игра / пауза).
    /// </summary>
    public GameState CurrentState { get; private set; } = GameState.Menu;

    /// <summary>
    /// Инициализация Singleton и закрепление объекта между сценами.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Запускает игру из меню: переключает состояние, сбрасывает время, загружает игровую сцену и включает ввод игрока.
    /// </summary>
    public void StartGame()
    {
        RestartGameScene();
    }

    /// <summary>
    /// Возврат в главное меню: переключает состояние, сбрасывает скорость времени, загружает сцену меню и включает UI‑ввод.
    /// </summary>
    public void GoToMenu()
    {
        CurrentState = GameState.Menu;
        Time.timeScale = 1f;
        SceneLoader.Instance.Load(SceneNames.MainMenu);
        Debug.Log("Go to Main Menu");
        if (InputManager.Instance != null)
            InputManager.Instance.EnableUIInput();
    }

    /// <summary>
    /// Ставит игру на паузу из состояния Playing:
    /// останавливает время через Time.timeScale и оповещает слушателей через EventBus.
    /// </summary>
    public void Pause()
    {
        if (CurrentState != GameState.Playing)
            return;

        CurrentState = GameState.Paused;
        Time.timeScale = 0f; // простой вариант паузы
        EventBus.Instance.RaiseGamePaused();
        Debug.Log("Game paused");
    }

    /// <summary>
    /// Снимает паузу из состояния Paused:
    /// возвращает Time.timeScale к 1 и оповещает слушателей через EventBus.
    /// </summary>
    public void Resume()
    {
        if (CurrentState != GameState.Paused)
            return;

        CurrentState = GameState.Playing;
        Time.timeScale = 1f;
        EventBus.Instance.RaiseGameResumed();
        Debug.Log("Game resumed");
    }

    /// <summary>
    /// Перезапускает игровую сцену через Loading и переводит игру в состояние Playing.
    /// Используется для "New Game" и "Restart" с lose-экрана.
    /// </summary>
    public void RestartGameScene()
    {
        CurrentState = GameState.Playing;
        Time.timeScale = 1f;
        SceneLoader.Instance.LoadWithLoading(SceneNames.GameScene);
        Debug.Log("Game scene restart requested");
        if (InputManager.Instance != null)
            InputManager.Instance.EnablePlayerInput();
    }

    /// <summary>
    /// Переводит игру в состояние поражения и включает UI-ввод.
    /// </summary>
    public void EnterLoseState()
    {
        if (CurrentState != GameState.Playing)
            return;

        CurrentState = GameState.Lost;
        Time.timeScale = 0f;
        if (InputManager.Instance != null)
            InputManager.Instance.EnableUIInput();
        Debug.Log("Game lost");
    }

    /// <summary>
    /// Переводит игру в состояние победы и включает UI-ввод.
    /// </summary>
    public void EnterWinState()
    {
        if (CurrentState != GameState.Playing)
            return;

        CurrentState = GameState.Won;
        Time.timeScale = 0f;
        if (InputManager.Instance != null)
            InputManager.Instance.EnableUIInput();
        Debug.Log("Game won");
    }
}
