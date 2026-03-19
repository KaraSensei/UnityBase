using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Базовый спавнер врагов для этапа 7:
/// - работает со списком EnemyData;
/// - ограничивает общее число активных врагов;
/// - опционально использует EnemyPool и EnemyDeathRewarder.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Опциональные системы")]
    [Tooltip("Если назначен, враги будут создаваться через пул. Если нет, через Instantiate.")]
    public EnemyPool pool;

    [Tooltip("Если назначен, спавнер регистрирует врагов для выдачи опыта.")]
    public EnemyDeathRewarder deathRewarder;

    [Header("Точки спавна")]
    [Tooltip("Массив точек, где могут появляться враги.")]
    public Transform[] spawnPoints;

    [Header("Типы врагов")]
    [Tooltip("Список EnemyData, из которого выбирается случайный враг.")]
    public EnemyData[] enemyDataList;

    [Header("Настройки спавна")]
    [Min(0.1f)]
    [Tooltip("Интервал между спавнами (в секундах).")]
    public float spawnInterval = 5f;

    [Min(0)]
    [Tooltip("Максимальное количество активных врагов на сцене.")]
    public int maxEnemies = 10;

    [Tooltip("Начинать ли спавн автоматически при старте.")]
    public bool spawnOnStart = true;

    [Header("Отладка")]
    [Tooltip("Показывать ли логи в консоли.")]
    public bool showDebugLogs = true;

    private bool isSpawning;
    private Coroutine spawnCoroutine;
    private readonly List<EnemyBase> activeEnemies = new List<EnemyBase>();

    private void Start()
    {
        if (!ValidateSetup())
            return;

        WarmupPoolIfNeeded();

        if (spawnOnStart)
            StartSpawning();
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
            return;

        isSpawning = false;
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        if (showDebugLogs)
            Debug.Log($"{name}: спавн врагов остановлен.");
    }

    public EnemyBase SpawnEnemy()
    {
        if (!HasSpawnData())
            return null;

        CleanupInactiveEnemies();
        if (activeEnemies.Count >= maxEnemies)
        {
            if (showDebugLogs)
                Debug.Log($"{name}: достигнут лимит врагов. Пропускаем спавн.");
            return null;
        }

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        EnemyData selectedData = PickRandomEnemyData();
        if (selectedData == null)
            return null;

        EnemyBase enemy = pool != null
            ? EnemyFactory.CreateEnemy(pool, selectedData, spawnPoint.position, spawnPoint.rotation)
            : EnemyFactory.CreateEnemy(selectedData, spawnPoint.position, spawnPoint.rotation);

        if (enemy == null)
            return null;

        activeEnemies.Add(enemy);

        if (deathRewarder != null)
        {
            EnemyStats stats = enemy.GetComponent<EnemyStats>();
            if (stats != null)
                deathRewarder.RegisterEnemy(stats);
        }

        if (showDebugLogs)
            Debug.Log($"{name}: создан враг {selectedData.enemyName} в точке {spawnPoint.name}");

        return enemy;
    }

    public int GetCurrentEnemyCount()
    {
        CleanupInactiveEnemies();
        return activeEnemies.Count;
    }

    private IEnumerator SpawnCoroutine()
    {
        while (isSpawning)
        {
            yield return new WaitForSeconds(spawnInterval);
            SpawnEnemy();
        }
    }

    private bool ValidateSetup()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning($"{name}: нет точек спавна. Спавн отключён.");
            return false;
        }

        if (!HasSpawnData())
        {
            Debug.LogWarning($"{name}: enemyDataList пустой или без валидных EnemyData. Спавн отключён.");
            return false;
        }

        return true;
    }

    private bool HasSpawnData()
    {
        if (enemyDataList == null || enemyDataList.Length == 0)
            return false;

        for (int i = 0; i < enemyDataList.Length; i++)
        {
            EnemyData data = enemyDataList[i];
            if (data != null && data.prefab != null)
                return true;
        }

        return false;
    }

    private EnemyData PickRandomEnemyData()
    {
        List<EnemyData> valid = null;

        for (int i = 0; i < enemyDataList.Length; i++)
        {
            EnemyData data = enemyDataList[i];
            if (data == null || data.prefab == null)
                continue;

            if (valid == null)
                valid = new List<EnemyData>();

            valid.Add(data);
        }

        if (valid == null || valid.Count == 0)
        {
            if (showDebugLogs)
                Debug.LogWarning($"{name}: нет валидных EnemyData с назначенным prefab.");
            return null;
        }

        return valid[Random.Range(0, valid.Count)];
    }

    private void WarmupPoolIfNeeded()
    {
        if (pool == null || enemyDataList == null)
            return;

        for (int i = 0; i < enemyDataList.Length; i++)
        {
            EnemyData data = enemyDataList[i];
            if (data == null || data.prefab == null)
                continue;

            pool.Warmup(data.prefab, maxEnemies);
        }
    }

    private void CleanupInactiveEnemies()
    {
        activeEnemies.RemoveAll(enemy => enemy == null || !enemy.gameObject.activeInHierarchy);
    }

    private void OnDestroy()
    {
        StopSpawning();
    }

    private void OnDrawGizmosSelected()
    {
        if (spawnPoints == null)
            return;

        Gizmos.color = Color.green;
        foreach (Transform spawnPoint in spawnPoints)
        {
            if (spawnPoint == null)
                continue;

            Gizmos.DrawWireSphere(spawnPoint.position, 0.5f);
            Gizmos.DrawLine(spawnPoint.position, spawnPoint.position + Vector3.up * 2f);
        }
    }
}
