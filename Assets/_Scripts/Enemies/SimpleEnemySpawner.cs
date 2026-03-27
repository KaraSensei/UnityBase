/*
 * SimpleEnemySpawner
 * Назначение: учебный спавнер врагов через Instantiate для simple-ветки.
 * Что делает: создаёт врагов в случайных spawn points, держит лимит активных и управляет циклом спавна.
 * Связи: использует EnemyData/EnemyBase, при наличии назначает цель через PlayerController.
 * Паттерны: Composition, Fail Fast, Local Validation.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Упрощённый спавнер врагов для базового обучения без pooling/factory.
/// </summary>
public class SimpleEnemySpawner : MonoBehaviour
{
    [Header("Тип врага")]
    [Tooltip("Данные врага, которого будем спавнить.")]
    [SerializeField] private EnemyData enemyData;

    [Header("Точки спавна")]
    [Tooltip("Массив точек, где могут появляться враги.")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Настройки спавна")]
    [Min(0.1f)]
    [Tooltip("Интервал между спавнами (в секундах).")]
    [SerializeField] private float spawnInterval = 5f;

    [Min(0)]
    [Tooltip("Максимальное количество одновременно активных врагов.")]
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
        {
            if (showDebugLogs)
                Debug.LogWarning($"{name}: спавн уже запущен.", this);
            return;
        }

        isSpawning = true;
        spawnCoroutine = StartCoroutine(SpawnCoroutine());

        if (showDebugLogs)
            Debug.Log($"{name}: спавн врагов запущен.", this);
    }

    public void StopSpawning()
    {
        if (!isSpawning)
        {
            if (showDebugLogs)
                Debug.LogWarning($"{name}: спавн не был запущен.", this);
            return;
        }

        isSpawning = false;
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        if (showDebugLogs)
            Debug.Log($"{name}: спавн врагов остановлен.", this);
    }

    public EnemyBase SpawnEnemy()
    {
        if (!ValidateSetup())
            return null;

        if (playerTarget == null)
            ResolvePlayerTarget();

        CleanupInactiveEnemies();
        if (activeEnemies.Count >= maxEnemies)
        {
            if (showDebugLogs)
                Debug.Log($"{name}: достигнут лимит врагов. Пропускаем спавн.", this);
            return null;
        }

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        if (spawnPoint == null)
        {
            Debug.LogWarning($"{name}: одна из spawn points не назначена.", this);
            return null;
        }

        GameObject enemyObject = Instantiate(enemyData.prefab, spawnPoint.position, spawnPoint.rotation);
        EnemyBase enemy = enemyObject.GetComponent<EnemyBase>();
        if (enemy == null)
        {
            Debug.LogError($"{name}: на префабе {enemyData.prefab.name} отсутствует EnemyBase.", this);
            Destroy(enemyObject);
            return null;
        }

        enemy.Setup(enemyData);
        if (playerTarget != null)
            enemy.SetTarget(playerTarget);

        activeEnemies.Add(enemy);

        if (showDebugLogs)
            Debug.Log($"{name}: создан враг {enemyData.enemyName} в точке {spawnPoint.name}.", this);

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

    private bool ValidateSetup()
    {
        if (enemyData == null || enemyData.prefab == null)
        {
            Debug.LogWarning($"{name}: не назначены EnemyData или prefab.", this);
            return false;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning($"{name}: нет точек спавна.", this);
            return false;
        }

        return true;
    }

    private void ResolvePlayerTarget()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        playerTarget = player != null ? player.transform : null;
    }

    private void CleanupInactiveEnemies()
    {
        activeEnemies.RemoveAll(enemy => enemy == null || !enemy.gameObject.activeInHierarchy);
    }

    private void OnDestroy()
    {
        StopSpawning();
    }
}
