using UnityEngine;
using UnityEngine.InputSystem;

public class BootstrapManager : MonoBehaviour
{
    // Защита от повторной инициализации, если Bootstrap загрузится повторно
    private static bool _initialized;

    private void Awake()
    {
        if (_initialized)
        {
            Destroy(gameObject);
            return;
        }

        _initialized = true;
        DontDestroyOnLoad(gameObject);

        // Создаём/поднимаем основные менеджеры
        CreateGameManager();
        CreateSceneLoader();
        CreateEventBus();
        CreateInputManager();

        // Переходим в главное меню
        SceneLoader.Instance.Load(SceneNames.MainMenu);
    }

    private static void CreateGameManager()
    {
        GameManager existing = FindFirstObjectByType<GameManager>();
        if (existing != null)
        {
            DontDestroyOnLoad(existing.gameObject);
            return;
        }

        GameObject go = new GameObject("GameManager");
        go.AddComponent<GameManager>();
        DontDestroyOnLoad(go);
    }

    private static void CreateSceneLoader()
    {
        SceneLoader existing = FindFirstObjectByType<SceneLoader>();
        if (existing != null)
        {
            DontDestroyOnLoad(existing.gameObject);
            return;
        }

        GameObject go = new GameObject("SceneLoader");
        go.AddComponent<SceneLoader>();
        DontDestroyOnLoad(go);
    }

    private static void CreateEventBus()
    {
        EventBus existing = FindFirstObjectByType<EventBus>();
        if (existing != null)
        {
            DontDestroyOnLoad(existing.gameObject);
            return;
        }

        GameObject go = new GameObject("EventBus");
        go.AddComponent<EventBus>();
        DontDestroyOnLoad(go);
    }
    private static void CreateInputManager()
    {
        InputManager existing = FindFirstObjectByType<InputManager>();
        if (existing != null)
        {
            DontDestroyOnLoad(existing.gameObject);
            return;
        }

        GameObject go = new GameObject("InputManager");
        InputManager inputManager = go.AddComponent<InputManager>();

        // Загружаем из Resources — работает и в редакторе, и в билде
        inputManager.inputActions = Resources.Load<InputActionAsset>("InputSystem_Actions");

        if (inputManager.inputActions == null)
        {
            Debug.LogError("InputManager: Не удалось загрузить InputSystem_Actions! " +
                "Убедитесь, что файл InputSystem_Actions.inputactions лежит в папке Assets/Resources/");
        }

        DontDestroyOnLoad(go);
    }
}

