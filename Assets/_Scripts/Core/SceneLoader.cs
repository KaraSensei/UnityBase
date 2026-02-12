/*
 * SceneLoader
 * Назначение: единая точка для загрузки сцен.
 * Что делает: синхронно и асинхронно загружает сцены Unity, логирует прогресс при асинхронной загрузке.
 * Связи: используется GameManager и BootstrapManager при переходах между сценами.
 * Паттерны: Singleton, Facade над UnityEngine.SceneManagement.
 */
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    // Инициализирует Singleton SceneLoader и делает объект переживающим смену сцен.
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
    /// Асинхронно загружает сцену по имени.
    /// Подходит для простых переходов, когда не нужен прогресс загрузки.
    /// </summary>
    public void Load(string sceneName)
    {
        Debug.Log($"Loading scene: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Запускает асинхронную загрузку сцены.
    /// Можно расширить для показа экрана загрузки/прогресса.
    /// </summary>
    public void LoadAsync(string sceneName)
    {
        StartCoroutine(LoadSceneAsyncCoroutine(sceneName));
    }

    private IEnumerator LoadSceneAsyncCoroutine(string sceneName)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        // Здесь можно обновлять прогрессбар/индикатор загрузки
        while (!operation.isDone)
        {
            Debug.Log($"Loading progress: {operation.progress}");
            yield return null;
        }

        Debug.Log("Scene loaded");
    }
}