using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Упрощённый пул врагов для обучения.
/// Работает только с одним типом врага (один префаб) и одной очередью.
/// </summary>
public class SimpleEnemyPool : MonoBehaviour
{
    [Header("Настройки пула")]
    [Tooltip("Префаб врага, который будет переиспользоваться.")]
    public GameObject enemyPrefab;

    [Tooltip("Сколько экземпляров создать заранее.")]
    public int initialCount = 5;

    private readonly Queue<GameObject> pool = new Queue<GameObject>();

    private void Awake()
    {
        Warmup();
    }

    /// <summary>
    /// Заранее создаёт несколько экземпляров врага и складывает их в очередь.
    /// </summary>
    public void Warmup()
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning($"{name}: SimpleEnemyPool — не назначен enemyPrefab.");
            return;
        }

        for (int i = 0; i < initialCount; i++)
        {
            GameObject instance = Instantiate(enemyPrefab, transform);
            instance.SetActive(false);
            pool.Enqueue(instance);
        }
    }

    /// <summary>
    /// Получить врага из пула (или создать нового, если очередь пуста).
    /// </summary>
    public GameObject Get(Vector3 position, Quaternion rotation)
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning($"{name}: SimpleEnemyPool — не назначен enemyPrefab.");
            return null;
        }

        GameObject instance;

        if (pool.Count > 0)
        {
            instance = pool.Dequeue();
        }
        else
        {
            instance = Instantiate(enemyPrefab, transform);
        }

        instance.transform.SetPositionAndRotation(position, rotation);
        instance.SetActive(true);

        return instance;
    }

    /// <summary>
    /// Вернуть врага обратно в пул.
    /// </summary>
    public void Release(GameObject instance)
    {
        if (instance == null)
            return;

        instance.SetActive(false);
        pool.Enqueue(instance);
    }

    /// <summary>
    /// Подписывает врага на событие смерти, чтобы автоматически возвращать его в пул.
    /// </summary>
    public void RegisterEnemy(EnemyStats stats)
    {
        if (stats == null)
            return;

        stats.OnDied += HandleEnemyDied;
    }

    private void HandleEnemyDied(EnemyStats stats)
    {
        if (stats == null)
            return;

        stats.OnDied -= HandleEnemyDied;
        Release(stats.gameObject);
    }
}

