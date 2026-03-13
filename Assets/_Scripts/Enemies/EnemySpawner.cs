using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Спавнер врагов: периодически создаёт врагов в точках спавна.
/// Использует EnemyFactory для создания врагов.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Пул")]
    [Tooltip("Ссылка на EnemyPool, который лежит на сцене.")]
    public EnemyPool pool;

    [Header("Опыт за убийство")]
    [Tooltip("Компонент, который слушает смерть врагов и даёт опыт игроку.")]
    public EnemyDeathRewarder deathRewarder;

    [Header("Точки спавна")]
    [Tooltip("Массив точек, где могут появляться враги.")]
    public Transform[] spawnPoints;

    [Header("Типы врагов (оба мили)")]
    [Tooltip("Данные обычного врага.")]
    public EnemyData normalEnemyData;

    [Tooltip("Данные сильного врага (медленнее, но сильнее).")]
    public EnemyData strongEnemyData;

    [Header("Настройки спавна")]
    [Min(0.1f)]
    [Tooltip("Интервал между спавнами (в секундах).")]
    public float spawnInterval = 5f;

    [Min(0)]
    [Tooltip("Максимум обычных врагов одновременно на сцене.")]
    public int maxNormalEnemies = 10;

    [Min(0)]
    [Tooltip("Максимум сильных врагов одновременно на сцене.")]
    public int maxStrongEnemies = 5;

    [Tooltip("Начинать ли спавн автоматически при старте.")]
    public bool spawnOnStart = true;

    [Header("Отладка")]
    [Tooltip("Показывать ли логи спавна в консоли.")]
    public bool showDebugLogs = true;

    private bool isSpawning = false;
    private Coroutine spawnCoroutine;
    // Отдельно храним врагов по типам, чтобы удобно проверять лимиты.
    private List<EnemyBase> activeNormalEnemies = new List<EnemyBase>();
    private List<EnemyBase> activeStrongEnemies = new List<EnemyBase>();

    private void Start()
    {
        // Валидация данных
        if (pool == null)
        {
            Debug.LogWarning($"{name}: EnemyPool не назначен! Спавн не будет работать.");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning($"{name}: нет точек спавна! Спавн не будет работать.");
            return;
        }

        if (normalEnemyData == null && strongEnemyData == null)
        {
            Debug.LogWarning($"{name}: нет типов врагов для спавна! Спавн не будет работать.");
            return;
        }

        // Прогреваем пул заранее.
        // Это значит: создаём нужное количество врагов заранее и держим их выключенными.
        if (normalEnemyData != null && normalEnemyData.prefab != null)
        {
            pool.Warmup(normalEnemyData.prefab, maxNormalEnemies);
        }
        if (strongEnemyData != null && strongEnemyData.prefab != null)
        {
            pool.Warmup(strongEnemyData.prefab, maxStrongEnemies);
        }

        // Запуск спавна при старте, если включено
        if (spawnOnStart)
        {
            StartSpawning();
        }
    }

    /// <summary>
    /// Запускает периодический спавн врагов.
    /// </summary>
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

    /// <summary>
    /// Останавливает периодический спавн врагов.
    /// </summary>
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

    /// <summary>
    /// Корутина для периодического спавна врагов.
    /// </summary>
    private IEnumerator SpawnCoroutine()
    {
        while (isSpawning)
        {
            yield return new WaitForSeconds(spawnInterval);

            // Чистим списки от выключенных (возвращённых в пул) объектов.
            CleanupInactiveEnemies();

            // Создаём врага
            EnemyBase enemy = SpawnEnemy();
            if (enemy != null)
            {
                // Добавляем в нужный список по типу.
                if (enemy.GetComponent<EnemyStats>() != null && enemy.GetComponent<EnemyStats>().EnemyData == normalEnemyData)
                {
                    activeNormalEnemies.Add(enemy);
                }
                else
                {
                    activeStrongEnemies.Add(enemy);
                }
            }
        }
    }

    /// <summary>
    /// Создаёт одного врага в случайной точке спавна.
    /// </summary>
    public EnemyBase SpawnEnemy()
    {
        // Выбираем случайную точку спавна
        if (spawnPoints.Length == 0)
        {
            Debug.LogError($"{name}: нет точек спавна!");
            return null;
        }

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        // Сначала чистим списки, чтобы лимиты считались правильно.
        CleanupInactiveEnemies();

        // Проверяем лимиты по каждому типу.
        bool canSpawnNormal = normalEnemyData != null && activeNormalEnemies.Count < maxNormalEnemies;
        bool canSpawnStrong = strongEnemyData != null && activeStrongEnemies.Count < maxStrongEnemies;

        if (!canSpawnNormal && !canSpawnStrong)
        {
            if (showDebugLogs)
                Debug.Log($"{name}: достигнуты лимиты врагов. Пропускаем спавн.");
            return null;
        }

        // Выбираем, кого спавнить.
        // Простой вариант: если доступны оба — выбираем случайно.
        EnemyData enemyData;
        if (canSpawnNormal && canSpawnStrong)
        {
            enemyData = Random.value < 0.7f ? normalEnemyData : strongEnemyData; // 70% обычных, 30% сильных
        }
        else
        {
            enemyData = canSpawnNormal ? normalEnemyData : strongEnemyData;
        }

        // Создаём врага через фабрику
        EnemyBase enemy = EnemyFactory.CreateEnemy(pool, enemyData, spawnPoint.position, spawnPoint.rotation);

        // Регистрируем врага в системе наград за смерть (опыт игроку).
        if (enemy != null && deathRewarder != null)
        {
            EnemyStats stats = enemy.GetComponent<EnemyStats>();
            if (stats != null)
            {
                deathRewarder.RegisterEnemy(stats);
            }
        }

        if (enemy != null && showDebugLogs)
        {
            Debug.Log($"{name}: создан враг {enemyData.enemyName} в точке {spawnPoint.name}");
        }

        return enemy;
    }

    /// <summary>
    /// Очищает списки от выключенных (неактивных) объектов.
    /// В пуле враги обычно не Destroy, а SetActive(false).
    /// </summary>
    private void CleanupInactiveEnemies()
    {
        activeNormalEnemies.RemoveAll(enemy => enemy == null || !enemy.gameObject.activeInHierarchy);
        activeStrongEnemies.RemoveAll(enemy => enemy == null || !enemy.gameObject.activeInHierarchy);
    }

    /// <summary>
    /// Получить текущее количество живых врагов.
    /// </summary>
    public int GetCurrentEnemyCount()
    {
        CleanupInactiveEnemies();
        return activeNormalEnemies.Count + activeStrongEnemies.Count;
    }

    private void OnDestroy()
    {
        // Останавливаем корутину при уничтожении объекта
        StopSpawning();
    }

    private void OnDrawGizmosSelected()
    {
        // Рисуем точки спавна в редакторе
        if (spawnPoints == null)
            return;

        Gizmos.color = Color.green;
        foreach (Transform spawnPoint in spawnPoints)
        {
            if (spawnPoint != null)
            {
                Gizmos.DrawWireSphere(spawnPoint.position, 0.5f);
                Gizmos.DrawLine(spawnPoint.position, spawnPoint.position + Vector3.up * 2f);
            }
        }
    }
}