using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Object Pool для врагов.
/// Хранит выключенные объекты и отдаёт их для спавна по запросу.
/// </summary>
public class EnemyPool : MonoBehaviour
{
    // Очередь выключенных объектов для каждого префаба.
    // Ключ: prefab, Значение: очередь готовых экземпляров.
    private readonly Dictionary<GameObject, Queue<GameObject>> poolByPrefab =
        new Dictionary<GameObject, Queue<GameObject>>();

    /// <summary>
    /// Заранее создаёт несколько экземпляров префаба и кладёт их в пул выключенными.
    /// </summary>
    public void Warmup(GameObject prefab, int count)
    {
        // Проверка на ошибку: без префаба пул не работает.
        if (prefab == null)
        {
            Debug.LogError("EnemyPool.Warmup: prefab не может быть null!");
            return;
        }

        // Если просим 0 — ничего не делаем.
        if (count <= 0)
            return;

        // Получаем очередь для конкретного префаба (или создаём её).
        if (!poolByPrefab.TryGetValue(prefab, out Queue<GameObject> queue))
        {
            queue = new Queue<GameObject>();
            poolByPrefab.Add(prefab, queue);
        }

        for (int i = 0; i < count; i++)
        {
            // Instantiate мы делаем заранее, чтобы во время игры было меньше лагов.
            GameObject instance = Instantiate(prefab);

            // Этот компонент хранит ссылку на исходный префаб — она нужна, чтобы вернуть объект в "правильную" очередь.
            PooledEnemy marker = instance.GetComponent<PooledEnemy>();
            if (marker == null)
            {
                marker = instance.AddComponent<PooledEnemy>();
            }
            marker.SourcePrefab = prefab;

            // Выключаем объект и кладём в очередь.
            instance.SetActive(false);
            queue.Enqueue(instance);
        }
    }

    /// <summary>
    /// Получает объект из пула (или создаёт новый, если пул пустой).
    /// </summary>
    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            Debug.LogError("EnemyPool.Get: prefab не может быть null!");
            return null;
        }

        if (!poolByPrefab.TryGetValue(prefab, out Queue<GameObject> queue))
        {
            queue = new Queue<GameObject>();
            poolByPrefab.Add(prefab, queue);
        }

        GameObject instance;
        if (queue.Count > 0)
        {
            // Берём готовый экземпляр из очереди.
            instance = queue.Dequeue();
        }
        else
        {
            // Если заранее не прогрели пул — создадим один экземпляр.
            instance = Instantiate(prefab);

            PooledEnemy marker = instance.GetComponent<PooledEnemy>();
            if (marker == null)
            {
                marker = instance.AddComponent<PooledEnemy>();
            }
            marker.SourcePrefab = prefab;
        }

        // Подготавливаем объект к появлению в мире.
        instance.transform.SetPositionAndRotation(position, rotation);
        instance.SetActive(true);

        return instance;
    }

    /// <summary>
    /// Возвращает объект обратно в пул: выключает и кладёт в очередь.
    /// </summary>
    public void Release(GameObject instance)
    {
        if (instance == null)
            return;

        PooledEnemy marker = instance.GetComponent<PooledEnemy>();
        if (marker == null || marker.SourcePrefab == null)
        {
            // Если объект не "из пула" — хотя бы выключим его, чтобы он не мешал.
            instance.SetActive(false);
            Debug.LogWarning($"EnemyPool.Release: объект {instance.name} не знает свой префаб. Проверь PooledEnemy.");
            return;
        }

        if (!poolByPrefab.TryGetValue(marker.SourcePrefab, out Queue<GameObject> queue))
        {
            queue = new Queue<GameObject>();
            poolByPrefab.Add(marker.SourcePrefab, queue);
        }

        instance.SetActive(false);
        queue.Enqueue(instance);
    }

    /// <summary>
    /// Регистрирует врага в пуле.
    /// Подписывается на его событие смерти, чтобы вернуть объект в пул.
    /// </summary>
    public void RegisterEnemy(EnemyStats stats)
    {
        if (stats == null)
            return;

        // Подписываемся на событие смерти врага.
        stats.OnDied += HandleEnemyDied;
    }

    /// <summary>
    /// Обработчик смерти врага.
    /// Отписывается от события и возвращает объект в пул.
    /// </summary>
    private void HandleEnemyDied(EnemyStats stats)
    {
        if (stats == null)
            return;

        // Важно отписаться, чтобы не держать лишние ссылки.
        stats.OnDied -= HandleEnemyDied;

        // Возвращаем GameObject врага в пул.
        Release(stats.gameObject);
    }
}

/// <summary>
/// Служебный компонент: хранит ссылку на "исходный префаб" для пула.
/// </summary>
public class PooledEnemy : MonoBehaviour
{
    // Пул сам назначает это поле при создании объекта.
    public GameObject SourcePrefab { get; set; }
}