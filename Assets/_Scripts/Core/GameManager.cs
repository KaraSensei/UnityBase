using UnityEngine;
using UnityEngine.SceneManagement;

/*
 * GameManager
 * Назначение: центральный менеджер состояния игры и игрового flow.
 * Что управляет:
 *  - текущим состоянием игры (Menu/Playing/Paused/Lost/Won)
 *  - timeScale при паузе/lose/win
 *  - загрузкой уровней через SceneLoader
 *  - простым progression по последовательности уровней (LevelSequenceData)
 *
 * Почему так:
 *  - Для урока 9 нужен замкнутый loop с переходом между уровнями.
 *  - Самый простой учебный вариант: хранить порядок уровней в ScriptableObject
 *    и переключать сцену кнопкой "Next" на win экране.
 */
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

    [Header("Level Sequence")]
    [Tooltip("Путь в Resources до LevelSequenceData без расширения .asset.")]
    [SerializeField] private string levelSequenceResourcePath = "Levels/LevelSequence_Default";
    [SerializeField] private LevelSequenceData levelSequenceOverride;

    /// <summary>
    /// Текущее состояние игры.
    /// </summary>
    public GameState CurrentState { get; private set; } = GameState.Menu;

    /// <summary>
    /// Индекс текущего уровня из LevelSequenceData.
    /// -1 означает "не определён или fallback режим".
    /// </summary>
    public int CurrentLevelIndex => currentLevelIndex;

    private LevelSequenceData levelSequenceData;
    private int currentLevelIndex = -1;

    /// <summary>
    /// Инициализирует singleton и подгружает конфиг последовательности уровней.
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

        LoadLevelSequenceData();
    }

    /// <summary>
    /// Запускает новую игру из меню.
    /// Логика: берём 0-й уровень из sequence, если sequence не найден - fallback на GameScene.
    /// </summary>
    public void StartGame()
    {
        currentLevelIndex = 0;

        if (!TryGetLevelSceneName(currentLevelIndex, out string firstLevelScene))
        {
            firstLevelScene = SceneNames.GameScene;
            currentLevelIndex = -1;
        }

        LoadGameplayScene(firstLevelScene);
    }

    /// <summary>
    /// Возвращает игрока в главное меню.
    /// </summary>
    public void GoToMenu()
    {
        CurrentState = GameState.Menu;
        Time.timeScale = 1f;
        SceneLoader.Instance.Load(SceneNames.MainMenu);

        if (InputManager.Instance != null)
            InputManager.Instance.EnableUIInput();
    }

    /// <summary>
    /// Ставит игру на паузу и публикует событие в EventBus.
    /// </summary>
    public void Pause()
    {
        if (CurrentState != GameState.Playing)
            return;

        CurrentState = GameState.Paused;
        Time.timeScale = 0f;
        EventBus.Instance.RaiseGamePaused();
    }

    /// <summary>
    /// Снимает паузу и публикует событие в EventBus.
    /// </summary>
    public void Resume()
    {
        if (CurrentState != GameState.Paused)
            return;

        CurrentState = GameState.Playing;
        Time.timeScale = 1f;
        EventBus.Instance.RaiseGameResumed();
    }

    /// <summary>
    /// Перезапускает текущий игровой уровень.
    /// Метод сохранён для обратной совместимости со старыми кнопками/скриптами.
    /// </summary>
    public void RestartGameScene()
    {
        string sceneToReload = ResolveCurrentGameplayScene();
        LoadGameplayScene(sceneToReload);
    }

    /// <summary>
    /// Пытается загрузить следующий уровень из sequence.
    /// Возвращает true, если уровень найден и загрузка запущена.
    /// Возвращает false, если текущий уровень последний.
    /// </summary>
    public bool TryLoadNextLevel()
    {
        UpdateCurrentLevelIndexFromActiveScene();

        int nextLevelIndex = currentLevelIndex + 1;
        if (!TryGetLevelSceneName(nextLevelIndex, out string nextScene))
            return false;

        currentLevelIndex = nextLevelIndex;
        LoadGameplayScene(nextScene);
        return true;
    }

    /// <summary>
    /// Переводит игру в состояние поражения.
    /// </summary>
    public void EnterLoseState()
    {
        if (CurrentState != GameState.Playing)
            return;

        CurrentState = GameState.Lost;
        Time.timeScale = 0f;

        if (InputManager.Instance != null)
            InputManager.Instance.EnableUIInput();
    }

    /// <summary>
    /// Переводит игру в состояние победы.
    /// </summary>
    public void EnterWinState()
    {
        if (CurrentState != GameState.Playing)
            return;

        CurrentState = GameState.Won;
        Time.timeScale = 0f;

        if (InputManager.Instance != null)
            InputManager.Instance.EnableUIInput();
    }

    /// <summary>
    /// Загружает sequence asset из Resources.
    /// Если не найден, система всё равно работает в fallback режиме через GameScene.
    /// </summary>
    private void LoadLevelSequenceData()
    {
        if (levelSequenceOverride != null)
        {
            levelSequenceData = levelSequenceOverride;
            return;
        }

#if UNITY_EDITOR
        // В Editor приоритет у учебного ассета в _ScriptableObjects,
        // чтобы брались именно те данные, которые вы редактируете вручную.
        if (TryLoadLevelSequenceFromEditorAssetPath())
            return;
#endif

        if (string.IsNullOrWhiteSpace(levelSequenceResourcePath))
        {
#if UNITY_EDITOR
            TryLoadLevelSequenceFromEditorAssetPath();
#endif
            return;
        }

        levelSequenceData = Resources.Load<LevelSequenceData>(levelSequenceResourcePath);

#if UNITY_EDITOR
        if (levelSequenceData == null)
            TryLoadLevelSequenceFromEditorAssetPath();
#endif

        if (levelSequenceData == null)
        {
            Debug.LogWarning(
                $"GameManager: LevelSequenceData not found at Resources/{levelSequenceResourcePath}. " +
                "Fallback to SceneNames.GameScene will be used.");
        }
    }

#if UNITY_EDITOR
    private bool TryLoadLevelSequenceFromEditorAssetPath()
    {
        const string editorAssetPath = "Assets/_ScriptableObjects/Levels/LevelSequenceData.asset";
        levelSequenceData = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelSequenceData>(editorAssetPath);
        return levelSequenceData != null;
    }
#endif

    /// <summary>
    /// Единая точка перехода в игровую сцену:
    /// переводит state в Playing, возвращает timeScale и включает player input.
    /// </summary>
    private void LoadGameplayScene(string sceneName)
    {
        CurrentState = GameState.Playing;
        Time.timeScale = 1f;
        SceneLoader.Instance.LoadWithLoading(sceneName);

        if (InputManager.Instance != null)
            InputManager.Instance.EnablePlayerInput();
    }

    /// <summary>
    /// Безопасный доступ к имени сцены уровня из sequence.
    /// </summary>
    private bool TryGetLevelSceneName(int levelIndex, out string sceneName)
    {
        sceneName = null;

        if (levelSequenceData == null)
            return false;

        return levelSequenceData.TryGetLevelSceneName(levelIndex, out sceneName);
    }

    /// <summary>
    /// Синхронизирует currentLevelIndex с реально активной сценой.
    /// Нужен перед вычислением "следующего" уровня.
    /// </summary>
    private void UpdateCurrentLevelIndexFromActiveScene()
    {
        if (levelSequenceData == null)
            return;

        string activeSceneName = SceneManager.GetActiveScene().name;
        int sceneIndex = levelSequenceData.FindLevelIndex(activeSceneName);
        if (sceneIndex >= 0)
            currentLevelIndex = sceneIndex;
    }

    /// <summary>
    /// Определяет, какую сцену перезапускать на Restart:
    /// 1) текущую сцену из sequence
    /// 2) если sequence не знает сцену - активную сцену
    /// 3) если активна MainMenu - fallback на GameScene
    /// </summary>
    private string ResolveCurrentGameplayScene()
    {
        UpdateCurrentLevelIndexFromActiveScene();

        if (TryGetLevelSceneName(currentLevelIndex, out string sceneName))
            return sceneName;

        return SceneManager.GetActiveScene().name == SceneNames.MainMenu
            ? SceneNames.GameScene
            : SceneManager.GetActiveScene().name;
    }
}
