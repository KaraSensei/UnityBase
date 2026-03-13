using UnityEngine;

/// <summary>
/// Фабрика для создания врагов на основе EnemyData.
/// Настраивает EnemyStats и EnemyBase и скрывает детали создания.
/// </summary>
public static class EnemyFactory
{
    /// <summary>
    /// Создаёт врага из EnemyData в нужной позиции, используя пул объектов.
    /// </summary>
    public static EnemyBase CreateEnemy(EnemyPool pool, EnemyData data, Vector3 position, Quaternion rotation)
    {
        // Без пула мы не сможем переиспользовать врагов.
        if (pool == null)
        {
            Debug.LogError("EnemyFactory.CreateEnemy: pool не может быть null!");
            return null;
        }

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

        // Берём объект из пула по префабу.
        GameObject enemyObject = pool.Get(data.prefab, position, rotation);
        if (enemyObject == null)
            return null;

        // Гарантируем наличие EnemyStats
        EnemyStats stats = enemyObject.GetComponent<EnemyStats>();
        if (stats == null)
        {
            Debug.LogWarning($"EnemyFactory.CreateEnemy: у префаба {data.prefab.name} нет EnemyStats. Добавляю...");
            stats = enemyObject.AddComponent<EnemyStats>();
        }

        // Единая точка инициализации: назначаем данные и подготавливаем состояние.
        stats.Setup(data);

        // Регистрируем врага в пуле, чтобы при смерти он автоматически возвращался в очередь.
        pool.RegisterEnemy(stats);

        // Гарантируем наличие EnemyBase
        EnemyBase enemy = enemyObject.GetComponent<EnemyBase>();
        if (enemy == null)
        {
            Debug.LogWarning($"EnemyFactory.CreateEnemy: у префаба {data.prefab.name} нет EnemyBase. Добавляю...");
            enemy = enemyObject.AddComponent<EnemyBase>();
        }

        Debug.Log($"EnemyFactory: создан враг {data.enemyName} в позиции {position}");

        return enemy;
    }

    /// <summary>
    /// Создаёт врага с поворотом по умолчанию.
    /// </summary>
    public static EnemyBase CreateEnemy(EnemyPool pool, EnemyData data, Vector3 position)
    {
        return CreateEnemy(pool, data, position, Quaternion.identity);
    }

    /// <summary>
    /// Создаёт врага в позиции (0,0,0).
    /// </summary>
    public static EnemyBase CreateEnemy(EnemyPool pool, EnemyData data)
    {
        return CreateEnemy(pool, data, Vector3.zero, Quaternion.identity);
    }
}