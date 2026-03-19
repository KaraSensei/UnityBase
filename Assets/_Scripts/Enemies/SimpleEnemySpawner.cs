using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Упрощённый спавнер врагов для базового обучения.
/// Не использует сложный пул и словари — только Instantiate и простой список.
/// </summary>
public class SimpleEnemySpawner : MonoBehaviour
{
    [Header("Тип врага")]
    [Tooltip("Данные врага, которого будем спавнить.")]
    public EnemyData enemyData;

    [Header("Точки спавна")]
    [Tooltip("Массив точек, где могут появляться враги.")]
    public Transform[] spawnPoints;

    [Header("Настройки спавна")]
    [Min(0.1f)]
    [Tooltip("Интервал между спавнами (в секундах).")]
    public float spawnInterval = 5f;

    [Min(0)]
    [Tooltip("Максимальное количество врагов одновременно на сцене.")]
    public int maxEnemies = 10;

    [Tooltip("Начинать ли спавн автоматически при старте.")]
    public bool spawnOnStart = true;

    [Header("Отладка")]
    [Tooltip("Показывать ли логи спавна в консоли.")]
    public bool showDebugLogs = true;

    private bool isSpawning;
    private Coroutine spawnCoroutine;

    // Простой список для отслеживания созданных врагов.
    private readonly List<EnemyBase> activeEnemies = new List<EnemyBase>();

    private void Start()
    {
        if (enemyData == null || enemyData.prefab == null)
        {
            Debug.LogWarning($"{name}: SimpleEnemySpawner — не назначены EnemyData или prefab.");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning($"{name}: SimpleEnemySpawner — нет точек спавна.");
            return;
        }

        if (spawnOnStart)
        {
            StartSpawning();
        }
    }

    public void StartSpawning()
    {
        if (isSpawning)
        {
            if (showDebugLogs)
                Debug.LogWarning($"{name}: спавн уже запущен.");
            return;
        }

        isSpawning = true;
        spawnCoroutine = StartCoroutine(SpawnCoroutine());

        if (showDebugLogs)
            Debug.Log($"{name}: спавн врагов запущен.");
    }

    public void StopSpawning()
    {
        if (!isSpawning)
        {
            if (showDebugLogs)
                Debug.LogWarning($"{name}: спавн не был запущен.");
            return;
        }

        isSpawning = false;
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        if (showDebugLogs)
            Debug.Log($"{name}: спавн врагов остановлен.");
    }

    private IEnumerator SpawnCoroutine()
    {
        while (isSpawning)
        {
            yield return new WaitForSeconds(spawnInterval);

            CleanupInactiveEnemies();

            if (activeEnemies.Count >= maxEnemies)
            {
                if (showDebugLogs)
                    Debug.Log($"{name}: достигнут лимит врагов. Пропускаем спавн.");
                continue;
            }

            SpawnEnemy();
        }
    }

    public EnemyBase SpawnEnemy()
    {
        if (enemyData == null || enemyData.prefab == null)
        {
            Debug.LogWarning($"{name}: SimpleEnemySpawner — нет корректных данных врага.");
            return null;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning($"{name}: SimpleEnemySpawner — нет точек спавна.");
            return null;
        }

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        GameObject enemyObject = Instantiate(enemyData.prefab, spawnPoint.position, spawnPoint.rotation);

        EnemyStats stats = enemyObject.GetComponent<EnemyStats>();
        if (stats != null)
        {
            // Инициализируем статы из EnemyData, если это ещё не было сделано.
            stats.Setup(enemyData);
        }

        EnemyBase enemy = enemyObject.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            activeEnemies.Add(enemy);
        }

        if (showDebugLogs)
        {
            Debug.Log($"{name}: создан враг {enemyData.enemyName} в точке {spawnPoint.name}");
        }

        return enemy;
    }

    private void CleanupInactiveEnemies()
    {
        activeEnemies.RemoveAll(e => e == null || !e.gameObject.activeInHierarchy);
    }

    private void OnDestroy()
    {
        StopSpawning();
    }
}

