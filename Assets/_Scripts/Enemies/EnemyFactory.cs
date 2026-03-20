/*
 * EnemyFactory
 * Назначение: фабрика создания врагов для advanced/reference-пути.
 * Что делает: централизует создание EnemyBase по EnemyData.
 * Связи: используется advanced-веткой (pool/factory), не обязателен для simple-спавна.
 * Паттерны: Factory Method.
 */

using UnityEngine;

/// <summary>
/// Фабрика создания врагов для advanced-ветки.
/// </summary>
public static class EnemyFactory
{
    /// <summary>
    /// Создаёт врага по EnemyData через Instantiate.
    /// </summary>
    public static EnemyBase CreateEnemy(EnemyData data, Vector3 position, Quaternion rotation)
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

        GameObject enemyObject = Object.Instantiate(data.prefab, position, rotation);
        EnemyBase enemy = enemyObject.GetComponent<EnemyBase>();

        if (enemy == null)
        {
            // Fail fast: ошибки сборки префаба не должны скрываться в runtime.
            Debug.LogError($"EnemyFactory.CreateEnemy: у префаба {data.prefab.name} отсутствует EnemyBase.");
            Object.Destroy(enemyObject);
            return null;
        }

        enemy.Setup(data);
        return enemy;
    }

    public static EnemyBase CreateEnemy(EnemyData data, Vector3 position)
    {
        return CreateEnemy(data, position, Quaternion.identity);
    }

    /// <summary>
    /// Совместимость со старым API: pool параметр пока не влияет на simple-фабричный путь.
    /// </summary>
    public static EnemyBase CreateEnemy(EnemyPool pool, EnemyData data, Vector3 position, Quaternion rotation)
    {
        return CreateEnemy(data, position, rotation);
    }

    public static EnemyBase CreateEnemy(EnemyPool pool, EnemyData data, Vector3 position)
    {
        return CreateEnemy(data, position, Quaternion.identity);
    }

    public static EnemyBase CreateEnemy(EnemyPool pool, EnemyData data)
    {
        return CreateEnemy(data, Vector3.zero, Quaternion.identity);
    }
}
