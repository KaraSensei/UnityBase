/*
 * EnemySpawner
 * Назначение: простой спавнер врагов для student/simple-ветки.
 * Что делает: создаёт врагов через Instantiate по точкам спавна и ограничивает число активных врагов.
 * Связи: использует EnemyData/EnemyBase; может быть запущен внешней encounter-системой.
 * Паттерны: Composition, Local Validation, Fail Fast.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Простой спавнер для уроков без pooling/factory.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Точки спавна")]
    [Tooltip("Набор точек, в которых могут появляться враги.")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Типы врагов")]
    [Tooltip("Список конфигов врагов для случайного спавна.")]
    [SerializeField] private EnemyData[] enemyDataList;

    [Header("Настройки спавна")]
    [Tooltip("Интервал между попытками спавна.")]
    [Min(0.1f)]
    [SerializeField] private float spawnInterval = 5f;

    [Tooltip("Лимит одновременно активных врагов.")]
    [Min(0)]
    [SerializeField] private int maxEnemies = 10;

    [Tooltip("Запускать ли спавн автоматически при старте.")]
    [SerializeField] private bool spawnOnStart = true;

    [Header("Отладка")]
    [Tooltip("Показывать подробные логи спавнера.")]
    [SerializeField] private bool showDebugLogs = true;

    private bool isSpawning;
    private Coroutine spawnCoroutine;
    private Transform playerTarget;
    private readonly List<EnemyBase> activeEnemies = new List<EnemyBase>();

    /// <summary>
    /// Текущее число активных врагов после очистки невалидных ссылок.
    /// </summary>
    public int CurrentEnemyCount
    {
        get
        {
            CleanupInactiveEnemies();
            return activeEnemies.Count;
        }
    }

    private void Start()
    {
        ResolvePlayerTarget();

        if (!ValidateSetup())
            return;

        if (spawnOnStart)
            StartSpawning();
    }

    public void StartSpawning()
    {
        if (isSpawning)
            return;

        isSpawning = true;
        spawnCoroutine = StartCoroutine(SpawnCoroutine());
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
    }

    public EnemyBase SpawnEnemy()
    {
        if (!HasSpawnData())
            return null;

        CleanupInactiveEnemies();
        if (activeEnemies.Count >= maxEnemies)
        {
            if (showDebugLogs)
                Debug.Log($"{name}: достигнут лимит врагов.");
            return null;
        }

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        EnemyData selectedData = PickRandomEnemyData();
        if (spawnPoint == null || selectedData == null || selectedData.prefab == null)
            return null;

        // В simple-ветке спавн выполняется напрямую через Instantiate.
        GameObject enemyObject = Instantiate(selectedData.prefab, spawnPoint.position, spawnPoint.rotation);
        EnemyBase enemy = enemyObject.GetComponent<EnemyBase>();

        if (enemy == null)
        {
            Debug.LogError($"{name}: у префаба {selectedData.prefab.name} отсутствует EnemyBase.", this);
            Destroy(enemyObject);
            return null;
        }

        enemy.Setup(selectedData);
        enemy.SetTarget(playerTarget);
        activeEnemies.Add(enemy);

        if (showDebugLogs)
            Debug.Log($"{name}: создан враг {selectedData.enemyName} в точке {spawnPoint.name}");

        return enemy;
    }

    private IEnumerator SpawnCoroutine()
    {
        while (isSpawning)
        {
            yield return new WaitForSeconds(spawnInterval);
            SpawnEnemy();
        }
    }

    private void ResolvePlayerTarget()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        playerTarget = player != null ? player.transform : null;
    }

    private bool ValidateSetup()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning($"{name}: нет точек спавна. Спавн отключён.", this);
            return false;
        }

        if (!HasSpawnData())
        {
            Debug.LogWarning($"{name}: enemyDataList пустой или без валидных EnemyData.", this);
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
        List<EnemyData> validData = null;
        for (int i = 0; i < enemyDataList.Length; i++)
        {
            EnemyData data = enemyDataList[i];
            if (data == null || data.prefab == null)
                continue;

            if (validData == null)
                validData = new List<EnemyData>();

            validData.Add(data);
        }

        if (validData == null || validData.Count == 0)
            return null;

        return validData[Random.Range(0, validData.Count)];
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
