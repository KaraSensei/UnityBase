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
    private PlayerRuntimeState pendingPlayerRuntimeState;

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
        SceneManager.sceneLoaded += HandleSceneLoaded;

        LoadLevelSequenceData();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    /// <summary>
    /// Запускает новую игру из меню.
    /// Логика: берём 0-й уровень из sequence, если sequence не найден - fallback на GameScene.
    /// </summary>
    public void StartGame()
    {
        pendingPlayerRuntimeState = null;
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
        pendingPlayerRuntimeState = null;
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
        pendingPlayerRuntimeState = null;
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

        pendingPlayerRuntimeState = CaptureCurrentPlayerRuntimeState();
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

    /// <summary>
    /// Hook Unity на загрузку сцены.
    /// Если есть отложенное runtime-состояние, применяет его только в gameplay-сцене.
    /// </summary>
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (pendingPlayerRuntimeState == null)
            return;

        if (!IsGameplayScene(scene.name))
            return;

        ApplyPendingPlayerRuntimeState();
    }

    /// <summary>
    /// Проверяет, что сцена относится к gameplay.
    /// Нужна для защиты: не переносить состояние в Bootstrap/MainMenu/Loading.
    /// </summary>
    private bool IsGameplayScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return false;

        return sceneName != SceneNames.Bootstrap
               && sceneName != SceneNames.MainMenu
               && sceneName != SceneNames.Loading;
    }

    /// <summary>
    /// Снимает runtime-состояние текущего игрока перед переходом на следующий уровень.
    /// Важно: это перенос в памяти, а не сохранение на диск.
    /// </summary>
    private PlayerRuntimeState CaptureCurrentPlayerRuntimeState()
    {
        if (!TryResolvePlayerSystems(out PlayerStats playerStats, out PlayerProgression playerProgression, out WeaponManager weaponManager))
        {
            Debug.LogWarning(
                "GameManager: не удалось собрать runtime-состояние игрока. " +
                "Проверьте, что на сцене есть Player с компонентами PlayerStats, PlayerProgression и WeaponManager.",
                this);
            return null;
        }

        float healthToTransfer = playerStats.CurrentHealth;
        if (healthToTransfer <= 0f)
        {
            healthToTransfer = playerStats.playerData != null
                ? Mathf.Clamp(playerStats.playerData.maxHealth, 1f, float.MaxValue)
                : 1f;

            Debug.LogWarning(
                "GameManager: при переносе на следующий уровень обнаружен HP <= 0. " +
                $"Применяем безопасное значение HP={healthToTransfer}.",
                this);
        }

        return new PlayerRuntimeState
        {
            Health = healthToTransfer,
            Mana = playerStats.CurrentMana,
            Level = playerProgression.CurrentLevel,
            Experience = playerProgression.CurrentExperience,
            WeaponSlotIndex = weaponManager.CurrentWeaponSlotIndex
        };
    }

    /// <summary>
    /// Применяет ранее сохранённое runtime-состояние к новому экземпляру игрока в следующей сцене.
    /// </summary>
    private void ApplyPendingPlayerRuntimeState()
    {
        PlayerRuntimeState stateToApply = pendingPlayerRuntimeState;
        pendingPlayerRuntimeState = null;

        if (stateToApply == null)
            return;

        if (!TryResolvePlayerSystems(out PlayerStats playerStats, out PlayerProgression playerProgression, out WeaponManager weaponManager))
        {
            Debug.LogWarning(
                "GameManager: не удалось применить runtime-состояние в новой сцене. " +
                "Проверьте, что на объекте игрока присутствуют PlayerStats, PlayerProgression и WeaponManager.",
                this);
            return;
        }

        playerProgression.ApplyRuntimeState(stateToApply.Level, stateToApply.Experience);
        playerStats.ApplyRuntimeState(stateToApply.Health, stateToApply.Mana);
        weaponManager.ApplyRuntimeState(stateToApply.WeaponSlotIndex);
    }

    /// <summary>
    /// Ищет необходимые компоненты игрока для runtime-переноса.
    /// При дубликатах выбирает первый найденный и пишет предупреждение.
    /// </summary>
    private bool TryResolvePlayerSystems(
        out PlayerStats playerStats,
        out PlayerProgression playerProgression,
        out WeaponManager weaponManager)
    {
        playerStats = FindSingleOrFirst<PlayerStats>("PlayerStats");

        if (playerStats != null)
        {
            playerProgression = playerStats.GetComponent<PlayerProgression>();
            weaponManager = playerStats.GetComponent<WeaponManager>();
        }
        else
        {
            playerProgression = null;
            weaponManager = null;
        }

        if (playerProgression == null)
            playerProgression = FindSingleOrFirst<PlayerProgression>("PlayerProgression");

        if (weaponManager == null)
            weaponManager = FindSingleOrFirst<WeaponManager>("WeaponManager");

        if (playerStats != null && playerProgression != null && weaponManager != null)
            return true;

        string missing = string.Empty;
        if (playerStats == null)
            missing += "PlayerStats ";
        if (playerProgression == null)
            missing += "PlayerProgression ";
        if (weaponManager == null)
            missing += "WeaponManager ";

        Debug.LogWarning(
            $"GameManager: не найдены компоненты для runtime-переноса: {missing}. " +
            "Проверьте prefab игрока и ссылки на новой gameplay-сцене.",
            this);

        return false;
    }

    /// <summary>
    /// Ищет компонент на сцене:
    /// если найден один — возвращает его,
    /// если найдено несколько — предупреждает и берёт первый.
    /// Такой подход помогает быстро найти ошибку в сцене.
    /// </summary>
    private T FindSingleOrFirst<T>(string componentName) where T : Object
    {
        T[] found = FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (found == null || found.Length == 0)
            return null;

        if (found.Length > 1)
        {
            Debug.LogWarning(
                $"GameManager: найдено несколько компонентов {componentName}. " +
                $"Будет использован первый: {found[0].name}. " +
                "Для предсказуемого поведения оставьте на сцене один объект игрока.",
                this);
        }

        return found[0];
    }

    /// <summary>
    /// Временное состояние игрока для перехода ТОЛЬКО на следующий уровень.
    /// Это runtime-перенос в рамках одного запуска, не save/load на диск.
    /// sealed означает, что от этого класса нельзя наследоваться.
    /// Здесь это просто "контейнер данных" для одного сценария, без дочерних классов.
    /// </summary>
    private sealed class PlayerRuntimeState
    {
        public float Health;
        public float Mana;
        public int Level;
        public float Experience;
        public int WeaponSlotIndex;
    }
}
