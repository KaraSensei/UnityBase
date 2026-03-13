# Урок 7.3: EnemyFactory — паттерн Factory для создания врагов

---

## 0. Теория: паттерн Factory

### 0.1. Что такое паттерн Factory

**Factory (Фабрика)** — это порождающий паттерн проектирования, который предоставляет интерфейс для создания объектов без указания их конкретных классов.

**Проблема, которую решает Factory:**

Без Factory:
- Код создания врагов разбросан по разным местам (`EnemySpawner`, боевые события, квесты).
- При изменении логики создания нужно править код во многих местах.
- Сложно добавить дополнительную логику при создании (инициализация, настройка компонентов).

С Factory:
- Вся логика создания врагов централизована в одном месте.
- Легко изменить способ создания без изменения кода, который использует врагов.
- Можно добавить валидацию, дополнительные правила создания и другие улучшения.

### 0.2. Как работает Factory в нашем случае

`EnemyFactory` будет:

1. Принимать `EnemyData` как параметр.
2. Получать объект врага из **EnemyPool** (пула объектов) по префабу `EnemyData.prefab`.
3. Настраивать компонент `EnemyStats` на созданном объекте:
   - вызывать `Setup(data)` (назначает данные и инициализирует здоровье).
4. Убеждаться, что на объекте есть `EnemyBase`.
5. Возвращать созданный объект типа `EnemyBase`.

**Преимущества:**

- `EnemySpawner` не знает, как создавать врагов — он просто вызывает `EnemyFactory.CreateEnemy(data)`.
- Пул объектов уже встроен: мы меняем только пул/фабрику, а спавнер остаётся простым.
- Легко добавить логирование, статистику создания врагов и т.п.

### 0.3. Статический класс vs Singleton

Для `EnemyFactory` можно использовать два подхода:

1. **Статический класс** — все методы статические, не нужен экземпляр.
2. **Singleton** — один экземпляр на всю игру, можно использовать нестатические методы.

В этом уроке мы используем **статический класс** для простоты, но можно реализовать и Singleton, если понадобится состояние фабрики.

---

## 1. Цели урока

- **Техническая цель**:
  - создать простой `EnemyPool` для переиспользования врагов;
  - создать статический класс `EnemyFactory` с методом создания врагов;
  - реализовать логику получения врага из пула и настройки его компонентов.
- **Обучающая цель**:
  - показать паттерн Factory на практике;
  - показать Object Pool на понятном примере;
  - продемонстрировать, как централизация логики создания упрощает код.

После урока у тебя будет фабрика, которую можно использовать в `EnemySpawner` и других системах для создания врагов.

---

## 2. Подготовка

Перед началом убедись, что:

1. Уроки 7.1 и 7.2 выполнены:
   - есть класс `EnemyData` и несколько ассетов;
   - есть классы `EnemyStats` и `EnemyBase` и префаб врага как минимум с этими двумя компонентами (или фабрика добавит их сама).
2. Префаб врага назначен в `EnemyData.prefab` (хотя бы для одного ассета).

---

## 3. Проектирование EnemyFactory

### 3.1. Зачем нужен EnemyPool

Если мы часто создаём и удаляем врагов через `Instantiate` и `Destroy`, игра может подлагивать.

**Object Pool (пул объектов)** — это простой приём:

- заранее создаём несколько врагов и держим их выключенными (`SetActive(false)`);
- когда нужен враг — берём готового из пула и включаем (`SetActive(true)`);
- когда враг "умирает" — выключаем и возвращаем обратно в пул.

Так во время игры почти не происходит тяжёлых операций создания/удаления объектов.

### 3.2. Что должна делать фабрика

`EnemyFactory` должна:

- Создавать врага из `EnemyData`.
- Настраивать компоненты `EnemyStats` и `EnemyBase` на созданном объекте.
- Возвращать готовый к использованию объект.
- Обрабатывать ошибки (отсутствие данных, префаба и т.п.).

### 3.3. Методы пула и фабрики

**Методы пула:**

- `Warmup(prefab, count)` — заранее создать `count` выключенных объектов и положить в пул.
- `Get(prefab, position, rotation)` — получить объект для спавна.
- `Release(instance)` — вернуть объект в пул.

Основной метод:

- `EnemyBase CreateEnemy(EnemyPool pool, EnemyData data, Vector3 position, Quaternion rotation)` — создание врага в указанной позиции.

Дополнительные методы (опционально):

- `EnemyBase CreateEnemy(EnemyPool pool, EnemyData data, Vector3 position)` — создание с поворотом по умолчанию.
- `EnemyBase CreateEnemy(EnemyPool pool, EnemyData data)` — создание в позиции (0, 0, 0).

---

## 4. Создание скрипта EnemyFactory

### 4.1. Шаги в Unity

1. В окне `Project` перейди в `Assets/_Scripts/Enemies/`.
2. ПКМ → `Create` → `C# Script`.
3. Создай два скрипта:
   - **`EnemyPool`**
   - **`EnemyFactory`**
4. Открой оба скрипта в редакторе.

### 4.2. Реализация EnemyPool

Замените содержимое файла `EnemyPool` на следующий код:

```csharp
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
```

### 4.3. Реализация EnemyFactory

Замените содержимое файла `EnemyFactory` на следующий код:

```csharp
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
```

Разбор ключевых моментов:

- Класс помечен как `static` — не нужен экземпляр, все методы статические.
- Метод `CreateEnemy` проверяет входные данные перед созданием.
- Вместо `Instantiate` мы берём объект из **EnemyPool** (`pool.Get(...)`).
- После получения объекта мы настраиваем **EnemyStats** через `Setup(data)`.
- Затем убеждаемся, что есть **EnemyBase**.
- Возвращаем `EnemyBase`, а не `GameObject` — это позволяет работать с врагом через единый интерфейс поведения.

---

## 5. Как EnemyFactory будет использоваться дальше

На следующем уроке (7.4):

- `EnemySpawner` будет вызывать:
  - `EnemyFactory.CreateEnemy(pool, enemyData, spawnPoint.position)`
  - для создания врагов в точках спавна.

В других системах:

- квесты могут создавать врагов через фабрику;
- события могут спавнить врагов через фабрику;
- все используют единый интерфейс создания и не знают, как именно настраиваются `EnemyStats` и `EnemyBase`.

### 5.1. Где ещё можно использовать Factory

Паттерн Factory полезен не только для врагов. Те же идеи можно применять и в других частях игры:

- **Оружие и снаряды:** фабрика может создавать разные типы снарядов (`Projectile`) или оружия на основе данных (`WeaponData`), добавляя нужные компоненты и настраивая урон/скорость.  
- **Предметы и лут:** фабрика для предметов инвентаря (`ItemBase`) — создание объекта по `ItemData` и настройка внешнего вида/иконки/логики использования.  
- **NPC и окна UI:** фабрика может создавать NPC определённого типа или окна интерфейса (меню, диалог, всплывающие подсказки) по данным, не разбрызгивая `Instantiate` по всему коду.  
- **Боссы и особые существа:** вместо прямого `Instantiate` в квестах/сценариях можно всегда вызывать `BossFactory.CreateBoss(data, position)`, чтобы всё специальное поведение и инициализация были собраны в одном месте.

С точки зрения SOLID:

- **SRP:** фабрика отвечает только за создание объектов, а не за их поведение.  
- **OCP:** можно добавлять новые типы врагов, снарядов, предметов и UI, меняя только фабрику и данные, а код, который вызывает Factory, остаётся прежним.

---

## 6. Тестирование EnemyFactory

Для проверки можно создать простой тестовый скрипт:

1. Создай пустой объект в сцене.
2. Добавь компонент с таким кодом:

```csharp
using UnityEngine;

public class EnemyFactoryTest : MonoBehaviour
{
    [Tooltip("Ссылка на EnemyPool, который лежит на сцене.")]
    public EnemyPool pool;
    public EnemyData testEnemyData;

    private void Start()
    {
        // Проверяем, что всё назначено в инспекторе.
        if (pool != null && testEnemyData != null)
        {
            // Создаём врага в позиции этого объекта
            EnemyBase enemy = EnemyFactory.CreateEnemy(pool, testEnemyData, transform.position);
            if (enemy != null)
            {
                Debug.Log($"Тест: враг создан успешно — {enemy.name}");
            }
        }
    }
}
```

3. В Inspector назначь `testEnemyData` (например, `Enemy_Goblin_Default`).
4. Запусти сцену — враг должен появиться в позиции объекта.

---

## 7. Мини‑проверка

Ответь на вопросы:

1. Почему `EnemyFactory` сделан статическим классом, а не обычным?
2. Какие преимущества даёт использование Factory вместо прямого `Instantiate` в `EnemySpawner`?
3. Что произойдёт, если в `EnemyData` не назначен префаб?

Проверь код:

- `EnemyFactory` находится в папке `Assets/_Scripts/Enemies/`.
- Класс помечен как `static`.
- Метод `CreateEnemy` проверяет входные данные и настраивает компоненты `EnemyStats` и `EnemyBase`.

Если всё это выполнено и понятно — можно переходить к уроку 7.4 (`EnemySpawner` — спавнер врагов).
