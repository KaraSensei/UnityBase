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
- **EnemyFactory** — для создания врагов (из урока 7.3).
- **Корутины или таймеры** — для периодического спавна.

**Логика работы:**

1. При старте (или по сигналу) начинается цикл спавна.
2. Каждые N секунд (`spawnInterval`):
   - проверяем, не превышен ли лимит врагов (`maxEnemies`);
   - выбираем случайную точку спавна;
   - выбираем случайный тип врага из списка;
   - вызываем `EnemyFactory.CreateEnemy()`.
3. Можно остановить спавн в любой момент.

### 0.3. Расширения в будущем

На следующих этапах можно добавить:

- **Волны врагов** — разные типы и количества в зависимости от волны.
- **Спавн по событиям** — создание врагов при определённых условиях.
- **Зоны спавна** — враги появляются только в определённых областях.
- **Интеграция с Object Pool** (Этап 10) — переиспользование объектов вместо создания новых.

---

## 1. Цели урока

- **Техническая цель**:
  - создать компонент `EnemySpawner` для периодического создания врагов;
  - реализовать логику выбора точки спавна и типа врага;
  - интегрировать с `EnemyFactory` для создания врагов.
- **Обучающая цель**:
  - показать, как несколько систем (`EnemyData`, `EnemyBase`, `EnemyFactory`, `EnemySpawner`) работают вместе;
  - продемонстрировать использование корутин для периодических действий.

После урока у тебя будет рабочая система спавна врагов, которая создаёт их автоматически во время игры.

---

## 2. Подготовка

Перед началом убедись, что:

1. Уроки 7.1–7.3 выполнены:
   - есть `EnemyData` и несколько ассетов;
   - есть `EnemyBase` и префаб врага;
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
- `EnemyData[] enemyDataList` — список типов врагов для спавна.
- `float spawnInterval` — интервал между спавнами (в секундах).
- `int maxEnemies` — максимальное количество врагов одновременно.
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
    [Header("Точки спавна")]
    [Tooltip("Массив точек, где могут появляться враги.")]
    public Transform[] spawnPoints;

    [Header("Типы врагов")]
    [Tooltip("Список типов врагов, которые могут быть созданы.")]
    public EnemyData[] enemyDataList;

    [Header("Настройки спавна")]
    [Min(0.1f)]
    [Tooltip("Интервал между спавнами (в секундах).")]
    public float spawnInterval = 5f;

    [Min(1)]
    [Tooltip("Максимальное количество врагов одновременно на сцене.")]
    public int maxEnemies = 10;

    [Tooltip("Начинать ли спавн автоматически при старте.")]
    public bool spawnOnStart = true;

    [Header("Отладка")]
    [Tooltip("Показывать ли логи спавна в консоли.")]
    public bool showDebugLogs = true;

    private bool isSpawning = false;
    private Coroutine spawnCoroutine;
    private List<EnemyBase> spawnedEnemies = new List<EnemyBase>();

    private void Start()
    {
        // Валидация данных
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning($"{name}: нет точек спавна! Спавн не будет работать.");
            return;
        }

        if (enemyDataList == null || enemyDataList.Length == 0)
        {
            Debug.LogWarning($"{name}: нет типов врагов для спавна! Спавн не будет работать.");
            return;
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

            // Проверяем количество врагов
            CleanupDestroyedEnemies();
            if (spawnedEnemies.Count >= maxEnemies)
            {
                if (showDebugLogs)
                    Debug.Log($"{name}: достигнут лимит врагов ({maxEnemies}). Пропускаем спавн.");
                continue;
            }

            // Создаём врага
            EnemyBase enemy = SpawnEnemy();
            if (enemy != null)
            {
                spawnedEnemies.Add(enemy);
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

        // Выбираем случайный тип врага
        if (enemyDataList.Length == 0)
        {
            Debug.LogError($"{name}: нет типов врагов!");
            return null;
        }

        EnemyData enemyData = enemyDataList[Random.Range(0, enemyDataList.Length)];

        // Создаём врага через фабрику
        EnemyBase enemy = EnemyFactory.CreateEnemy(enemyData, spawnPoint.position, spawnPoint.rotation);

        if (enemy != null && showDebugLogs)
        {
            Debug.Log($"{name}: создан враг {enemyData.enemyName} в точке {spawnPoint.name}");
        }

        return enemy;
    }

    /// <summary>
    /// Очищает список врагов от уничтоженных объектов.
    /// </summary>
    private void CleanupDestroyedEnemies()
    {
        spawnedEnemies.RemoveAll(enemy => enemy == null || enemy.gameObject == null);
    }

    /// <summary>
    /// Получить текущее количество живых врагов.
    /// </summary>
    public int GetCurrentEnemyCount()
    {
        CleanupDestroyedEnemies();
        return spawnedEnemies.Count;
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
- Список `spawnedEnemies` отслеживает созданных врагов для контроля лимита.
- Метод `CleanupDestroyedEnemies()` удаляет из списка уничтоженных врагов.
- В `OnDrawGizmosSelected()` визуализируются точки спавна в редакторе.

---

## 5. Настройка EnemySpawner в сцене

### 5.1. Создание точек спавна

1. В сцене создай несколько пустых объектов (например, `SpawnPoint_1`, `SpawnPoint_2`, `SpawnPoint_3`).
2. Размести их в разных местах сцены, где должны появляться враги.

### 5.2. Создание объекта EnemySpawner

1. В сцене создай пустой объект, назови его `EnemySpawner`.
2. Добавь на него компонент `EnemySpawner`.
3. В Inspector настрои:
   - `Spawn Points` → добавь все созданные точки спавна в массив.
   - `Enemy Data List` → добавь ассеты `EnemyData` (Goblin, Orc, Archer).
   - `Spawn Interval` → `5` секунд.
   - `Max Enemies` → `10`.
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
   - после достижения лимита (`maxEnemies`) спавн должен приостановиться до смерти части врагов.

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
- **Интеграция с Object Pool** (Этап 10):
  - переиспользование объектов вместо создания новых.

---

## 8. Мини‑проверка

Ответь на вопросы:

1. Почему используется корутина вместо `Update()` для периодического спавна?
2. Зачем нужен список `spawnedEnemies` и метод `CleanupDestroyedEnemies()`?
3. Как `EnemySpawner` использует `EnemyFactory` для создания врагов?

Проверь в проекте:

- `EnemySpawner` находится в папке `Assets/_Scripts/Enemies/`.
- В сцене есть объект `EnemySpawner` с настроенными точками спавна и списком `EnemyData`.
- При запуске игры враги появляются периодически.

Если всё это выполнено и понятно — Этап 7 («Враги и Factory») можно считать завершённым. Далее — переход к Этапу 8 (общий интерфейс IDamageable для игрока и врагов).
