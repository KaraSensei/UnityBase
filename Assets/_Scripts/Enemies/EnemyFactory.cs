using UnityEngine;

/// <summary>
/// Фабрика для создания врагов на основе EnemyData.
/// Поддерживает два режима:
/// 1) Через EnemyPool (если он передан).
/// 2) Через обычный Instantiate (если pool == null).
/// </summary>
public static class EnemyFactory
{
    /// <summary>
    /// Создаёт врага из EnemyData в нужной позиции/повороте.
    /// </summary>
    public static EnemyBase CreateEnemy(EnemyPool pool, EnemyData data, Vector3 position, Quaternion rotation)
    {
        if (data == null)
        {
            Debug.LogError("EnemyFactory.CreateEnemy: EnemyData не может быть null!");
            return null;
        }

        if (data.prefab == null)
        {
            Debug.LogError($"EnemyFactory.CreateEnemy: у {data.enemyName} не назначен префаб!");
            return null;
        }

        GameObject enemyObject = pool != null
            ? pool.Get(data.prefab, position, rotation)
            : Object.Instantiate(data.prefab, position, rotation);

        if (enemyObject == null)
            return null;

        EnemyStats stats = enemyObject.GetComponent<EnemyStats>();
        if (stats == null)
        {
            Debug.LogWarning($"EnemyFactory.CreateEnemy: у префаба {data.prefab.name} нет EnemyStats. Добавляю...");
            stats = enemyObject.AddComponent<EnemyStats>();
        }
        stats.Setup(data);

        if (pool != null)
            pool.RegisterEnemy(stats);

        EnemyBase enemy = enemyObject.GetComponent<EnemyBase>();
        if (enemy == null)
        {
            Debug.LogWarning($"EnemyFactory.CreateEnemy: у префаба {data.prefab.name} нет EnemyBase. Добавляю...");
            enemy = enemyObject.AddComponent<EnemyBase>();
        }

        return enemy;
    }

    public static EnemyBase CreateEnemy(EnemyPool pool, EnemyData data, Vector3 position)
    {
        return CreateEnemy(pool, data, position, Quaternion.identity);
    }

    public static EnemyBase CreateEnemy(EnemyPool pool, EnemyData data)
    {
        return CreateEnemy(pool, data, Vector3.zero, Quaternion.identity);
    }

    public static EnemyBase CreateEnemy(EnemyData data, Vector3 position, Quaternion rotation)
    {
        return CreateEnemy(null, data, position, rotation);
    }

    public static EnemyBase CreateEnemy(EnemyData data, Vector3 position)
    {
        return CreateEnemy(null, data, position, Quaternion.identity);
    }
}
