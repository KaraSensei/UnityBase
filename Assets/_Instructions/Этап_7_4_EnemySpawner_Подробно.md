# Урок 7.4: EnemySpawner — спавнер врагов

---

## 0. Теория: система спавна врагов

### 0.1. Зачем нужен EnemySpawner

`EnemySpawner` — это компонент, который отвечает за:

- **Периодическое создание врагов** в определённых точках сцены.
- **Управление количеством врагов** (максимальное число одновременно существующих врагов).
- **Выбор типа врага** для спавна (случайный или по правилам).
- **Контроль спавна** (включение/выключение, задержки).

Без спавнера:
- Враги нужно было бы расставлять вручную в редакторе.
- Невозможно было бы создавать врагов динамически во время игры.
- Сложно управлять балансом количества врагов.

### 0.2. Как работает EnemySpawner

`EnemySpawner` использует:

- **Точки спавна** (`Transform[]`) — места, где могут появляться враги.
- **Список типов врагов** (`EnemyData[]`) — какие враги могут быть созданы.
- **EnemyFactory** — для создания врагов (из урока 7.3). Внутри фабрика сама настраивает `EnemyStats` и `EnemyBase`.
- **Корутины или таймеры** — для периодического спавна.

**Логика работы:**

1. При старте (или по сигналу) начинается цикл спавна.
2. Каждые N секунд (`spawnInterval`):
   - проверяем лимиты врагов (для каждого типа отдельно);
   - выбираем случайную точку спавна;
   - выбираем, какого врага спавнить (обычного или усиленного);
   - вызываем `EnemyFactory.CreateEnemy()`.
3. Можно остановить спавн в любой момент.

### 0.3. Важно: мы используем Object Pool

В этом проекте враги **не уничтожаются** через `Destroy`, а **возвращаются в пул** (см. уроки 7.2–7.3).
Поэтому спавнер должен считать активных врагов не по `null`, а по тому, активен ли объект в сцене (`activeInHierarchy`).

---

## 1. Цели урока

- **Техническая цель**:
  - создать компонент `EnemySpawner` для периодического создания врагов;
  - реализовать логику выбора точки спавна и типа врага;
  - интегрировать с `EnemyFactory` для создания врагов.
- **Обучающая цель**:
  - показать, как несколько систем (`EnemyData`, `EnemyStats`, `EnemyBase`, `EnemyFactory`, `EnemySpawner`) работают вместе;
  - продемонстрировать использование корутин для периодических действий.

После урока у тебя будет рабочая система спавна врагов, которая создаёт их автоматически во время игры.

---

## 2. Подготовка

Перед началом убедись, что:

1. Уроки 7.1–7.3 выполнены:
   - есть `EnemyData` и несколько ассетов;
   - есть префаб врага с компонентами `EnemyStats` и `EnemyBase` (или фабрика будет их добавлять);
   - есть `EnemyFactory`.
2. Префабы врагов назначены в `EnemyData.prefab` для всех используемых ассетов.

---

## 3. Проектирование EnemySpawner

### 3.1. Что должен делать спавнер

`EnemySpawner` должен:

- Хранить список точек спавна.
- Хранить список типов врагов (`EnemyData`).
- Периодически создавать врагов в случайных точках.
- Ограничивать максимальное количество врагов.
- Предоставлять методы для запуска/остановки спавна.

### 3.2. Поля и методы

**Поля:**

- `Transform[] spawnPoints` — точки спавна.
- `EnemyData normalEnemyData` — данные обычного врага (мили).
- `EnemyData strongEnemyData` — данные более сильного врага (тоже мили).
- `float spawnInterval` — интервал между спавнами (в секундах).
- `int maxNormalEnemies` — лимит обычных врагов.
- `int maxStrongEnemies` — лимит сильных врагов.
- `bool spawnOnStart` — спавнить ли при старте.
- `bool isSpawning` — флаг активного спавна.

**Методы:**

- `void StartSpawning()` — запуск спавна.
- `void StopSpawning()` — остановка спавна.
- `EnemyBase SpawnEnemy()` — создание одного врага (внутренний метод).
- `IEnumerator SpawnCoroutine()` — корутина для периодического спавна.

---

## 4. Создание скрипта EnemySpawner

### 4.1. Шаги в Unity

1. В окне `Project` перейди в `Assets/_Scripts/Enemies/`.
2. ПКМ → `Create` → `C# Script`.
3. Назови скрипт **`EnemySpawner`**.
4. Открой его в редакторе.

### 4.2. Реализация EnemySpawner

Замените содержимое файла на следующий код:

```csharp
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
```

Разбор ключевых моментов:

- Используется **корутина** (`IEnumerator SpawnCoroutine()`) для периодического спавна без блокировки основного потока.
- Мы ведём два списка активных врагов: обычные и сильные, чтобы проверять лимиты (10 и 5).
- Метод `CleanupInactiveEnemies()` удаляет из списков выключенных врагов (в пуле враги обычно выключаются, а не уничтожаются).
- В `OnDrawGizmosSelected()` визуализируются точки спавна в редакторе.

### 4.3. Новые конструкции этого урока

В коде `EnemySpawner` появляются несколько важных конструкций, с которыми ты можешь столкнуться впервые:

- **Корутины (`IEnumerator`, `StartCoroutine`, `yield return`)** — позволяют выполнять действия с паузами, не останавливая игру.  
  - Метод `SpawnCoroutine` возвращает `IEnumerator`, а строка `yield return new WaitForSeconds(spawnInterval);` говорит: «подождать N секунд и потом продолжить цикл».
- **Списки (`List<EnemyBase>`)** — коллекция, в которой можно динамически добавлять и удалять элементы.  
  - Мы добавляем созданных врагов в `activeNormalEnemies` или `activeStrongEnemies`.
  - Метод `CleanupInactiveEnemies()` удаляет из списков тех, кто уже выключен (`activeInHierarchy == false`).
- **`Random.Range`** — выбор случайного числа в диапазоне.  
  - Вызов `Random.Range(0, spawnPoints.Length)` выбирает случайную точку спавна.
  - Для выбора типа врага мы используем `Random.value < 0.7f` (пример: 70% обычных, 30% сильных).
- **`OnDrawGizmosSelected` и `Gizmos`** — специальные методы/класс для отрисовки вспомогательной графики **только в редакторе**.  
  - Здесь мы рисуем зелёные сферы и линии, чтобы в Scene View было видно, где находятся точки спавна.

---

### 4.4. EnemyDeathRewarder — выдаём опыт за убийство врага

Чтобы связать смерть врага с опытом игрока, создадим простой компонент‑“слушатель”.

1. В папке `Assets/_Scripts/Enemies/` создай скрипт `EnemyDeathRewarder`.
2. Замени содержимое на такой код:

```csharp
using UnityEngine;

/// <summary>
/// Слушает смерть врагов (EnemyStats.OnDied)
/// и передаёт опыт в PlayerProgression.
/// </summary>
public class EnemyDeathRewarder : MonoBehaviour
{
    [Header("Ссылки")]
    [Tooltip("Компонент прогрессии игрока, куда будем добавлять опыт.")]
    public PlayerProgression playerProgression;

    /// <summary>
    /// Регистрирует врага: подписывается на его событие смерти.
    /// </summary>
    public void RegisterEnemy(EnemyStats stats)
    {
        if (stats == null)
            return;

        // Подписываемся на событие смерти конкретного врага.
        stats.OnDied += HandleEnemyDied;
    }

    /// <summary>
    /// Обработчик смерти врага.
    /// Отдаёт игроку опыт за этого врага.
    /// </summary>
    private void HandleEnemyDied(EnemyStats stats)
    {
        if (stats == null)
            return;

        // Очень важно отписаться, чтобы не копить "лишние" подписки.
        stats.OnDied -= HandleEnemyDied;

        if (playerProgression == null)
        {
            Debug.LogWarning("EnemyDeathRewarder: PlayerProgression не назначен.", this);
            return;
        }

        float reward = stats.ExperienceReward;
        if (reward > 0f)
        {
            // Добавляем опыт игроку.
            playerProgression.AddExperience(reward);
        }
    }
}
```

Этот класс делает только одну вещь:

- слушает событие `EnemyStats.OnDied`;
- один раз добавляет опыт игроку через `PlayerProgression.AddExperience`.

---

## 5. Настройка EnemySpawner в сцене

### 5.1. Создание точек спавна

1. В сцене создай несколько пустых объектов (например, `SpawnPoint_1`, `SpawnPoint_2`, `SpawnPoint_3`).
2. Размести их в разных местах сцены, где должны появляться враги.

### 5.2. Создание объекта EnemySpawner

1. В сцене создай пустой объект, назови его `EnemySpawner`.
2. Добавь на него компонент `EnemySpawner`.
3. В Inspector настрои:
   - `Pool` → перетащи объект с компонентом `EnemyPool`.
   - `Death Rewarder` → перетащи объект с компонентом `EnemyDeathRewarder`.
   - `Spawn Points` → добавь все созданные точки спавна в массив.
   - `Normal Enemy Data` → ассет обычного врага (лимит 10).
   - `Strong Enemy Data` → ассет сильного врага (лимит 5).
   - `Spawn Interval` → `5` секунд.
   - `Max Normal Enemies` → `10`.
   - `Max Strong Enemies` → `5`.
   - `Spawn On Start` → `true`.
   - `Show Debug Logs` → `true` (для отладки).

### 5.3. Проверка в редакторе

- В Scene View при выборе `EnemySpawner` должны быть видны зелёные сферы в точках спавна (благодаря `OnDrawGizmosSelected()`).

---

## 6. Проверка работы

1. Запусти игру через `Bootstrap`.
2. Перейди в `GameScene` через главное меню.
3. Во время игры:
   - каждые 5 секунд должны появляться враги в случайных точках спавна;
   - в консоли должны быть логи о создании врагов;
   - враги должны двигаться к игроку (если он в радиусе обнаружения);
   - после достижения лимитов (10 обычных и 5 сильных) спавн должен приостановиться до "смерти" (возврата в пул) части врагов.

---

## 7. Расширения и улучшения

В будущем можно добавить:

- **Волны врагов:**
  - разные типы и количества в зависимости от волны;
  - увеличение сложности с каждой волной.
- **Спавн по событиям:**
  - создание врагов при определённых условиях (вход в комнату, активация триггера).
- **Зоны спавна:**
  - враги появляются только в определённых областях.

---

## 8. Мини‑проверка

Ответь на вопросы:

1. Почему используется корутина вместо `Update()` для периодического спавна?
2. Зачем нужны списки активных врагов и метод `CleanupInactiveEnemies()`?
3. Как `EnemySpawner` использует `EnemyFactory` для создания врагов?

Проверь в проекте:

- `EnemySpawner` находится в папке `Assets/_Scripts/Enemies/`.
- В сцене есть объект `EnemySpawner` с настроенными точками спавна, двумя `EnemyData`, ссылкой на `EnemyPool` и `EnemyDeathRewarder`.
- При запуске игры враги появляются периодически.

Если всё это выполнено и понятно — Этап 7 («Враги и Factory») можно считать завершённым. Далее — переход к Этапу 8 (общий интерфейс IDamageable для игрока и врагов).

---

## 9. Альтернативные реализации для разных типов игр

Система спавна врагов, построенная на `EnemySpawner` и `EnemyFactory`, легко адаптируется под другие жанры и форматы:

- **2D‑игра (платформер или top‑down):**
  - Точки спавна остаются `Transform`, но враги используют 2D‑физику (`Rigidbody2D`, `Collider2D`), а движение ограничено плоскостью (`X`/`Y`).  
  - Логику выбора позиции можно поменять на спавн по тайлам/клеткам уровня.
- **Шутер от первого лица:**
  - Спавнер может создавать врагов **за пределами поля зрения игрока** (позади, за укрытиями), а точки спавна расставляются вокруг маршрутов игрока.  
  - Можно добавить «волны» спавна, привязанные к прогрессу игрока по уровню (например, зашёл в комнату → запустить `StartSpawning()`).
- **Tower defense / стратегии / симуляторы:**
  - Вместо «врагов» спавнятся юниты, машины, жители города — принцип тот же: данные (`EnemyData`/`UnitData`), фабрика, спавнер.  
  - Спавн может быть привязан к ресурсам, времени дня, этапам сценария или заполнять город/карту случайными событиями.

Главная идея: `EnemySpawner` отвечает за «когда и где» появляются объекты, а **конкретный тип игры** определяет, *кого именно* спавнить и по каким правилам.
