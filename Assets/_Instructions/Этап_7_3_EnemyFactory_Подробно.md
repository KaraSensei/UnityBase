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
- Можно добавить валидацию, пулинг объектов (на Этапе 10) и другие улучшения.

### 0.2. Как работает Factory в нашем случае

`EnemyFactory` будет:

1. Принимать `EnemyData` как параметр.
2. Создавать экземпляр префаба из `EnemyData.prefab`.
3. Настраивать компонент `EnemyBase` на созданном объекте:
   - назначать `EnemyData`;
   - инициализировать здоровье и другие параметры.
4. Возвращать созданный объект типа `EnemyBase`.

**Преимущества:**

- `EnemySpawner` не знает, как создавать врагов — он просто вызывает `EnemyFactory.CreateEnemy(data)`.
- Если позже нужно добавить пулинг объектов (Этап 10), мы меняем только `EnemyFactory`.
- Легко добавить логирование, статистику создания врагов и т.п.

### 0.3. Статический класс vs Singleton

Для `EnemyFactory` можно использовать два подхода:

1. **Статический класс** — все методы статические, не нужен экземпляр.
2. **Singleton** — один экземпляр на всю игру, можно использовать нестатические методы.

В этом уроке мы используем **статический класс** для простоты, но можно реализовать и Singleton, если понадобится состояние фабрики.

---

## 1. Цели урока

- **Техническая цель**:
  - создать статический класс `EnemyFactory` с методом создания врагов;
  - реализовать логику создания врага из `EnemyData` и настройки его компонентов.
- **Обучающая цель**:
  - показать паттерн Factory на практике;
  - продемонстрировать, как централизация логики создания упрощает код.

После урока у тебя будет фабрика, которую можно использовать в `EnemySpawner` и других системах для создания врагов.

---

## 2. Подготовка

Перед началом убедись, что:

1. Уроки 7.1 и 7.2 выполнены:
   - есть класс `EnemyData` и несколько ассетов;
   - есть класс `EnemyBase` и префаб врага с этим компонентом.
2. Префаб врага назначен в `EnemyData.prefab` (хотя бы для одного ассета).

---

## 3. Проектирование EnemyFactory

### 3.1. Что должна делать фабрика

`EnemyFactory` должна:

- Создавать врага из `EnemyData`.
- Настраивать компонент `EnemyBase` на созданном объекте.
- Возвращать готовый к использованию объект.
- Обрабатывать ошибки (отсутствие данных, префаба и т.п.).

### 3.2. Методы фабрики

Основной метод:

- `EnemyBase CreateEnemy(EnemyData data, Vector3 position, Quaternion rotation)` — создание врага в указанной позиции.

Дополнительные методы (опционально):

- `EnemyBase CreateEnemy(EnemyData data, Vector3 position)` — создание с поворотом по умолчанию.
- `EnemyBase CreateEnemy(EnemyData data)` — создание в позиции (0, 0, 0).

---

## 4. Создание скрипта EnemyFactory

### 4.1. Шаги в Unity

1. В окне `Project` перейди в `Assets/_Scripts/Enemies/`.
2. ПКМ → `Create` → `C# Script`.
3. Назови скрипт **`EnemyFactory`**.
4. Открой его в редакторе.

### 4.2. Реализация EnemyFactory

Замените содержимое файла на следующий код:

```csharp
using UnityEngine;

/// <summary>
/// Фабрика для создания врагов на основе EnemyData.
/// Использует паттерн Factory для централизации логики создания.
/// </summary>
public static class EnemyFactory
{
    /// <summary>
    /// Создаёт врага на основе EnemyData в указанной позиции и повороте.
    /// </summary>
    /// <param name="data">Данные врага (EnemyData).</param>
    /// <param name="position">Позиция создания.</param>
    /// <param name="rotation">Поворот врага.</param>
    /// <returns>Созданный враг (EnemyBase) или null, если создание не удалось.</returns>
    public static EnemyBase CreateEnemy(EnemyData data, Vector3 position, Quaternion rotation)
    {
        // Проверка входных данных
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

        // Создаём экземпляр префаба
        GameObject enemyObject = Object.Instantiate(data.prefab, position, rotation);

        // Получаем или добавляем компонент EnemyBase
        EnemyBase enemy = enemyObject.GetComponent<EnemyBase>();
        if (enemy == null)
        {
            Debug.LogWarning($"EnemyFactory.CreateEnemy: у префаба {data.prefab.name} нет компонента EnemyBase. Добавляю...");
            enemy = enemyObject.AddComponent<EnemyBase>();
        }

        // Настраиваем компонент EnemyBase
        enemy.enemyData = data;

        // Инициализируем здоровье (это сделает Awake в EnemyBase, но можно и здесь)
        // enemy.currentHealth будет установлен в Awake() компонента EnemyBase

        Debug.Log($"EnemyFactory: создан враг {data.enemyName} в позиции {position}");

        return enemy;
    }

    /// <summary>
    /// Создаёт врага в указанной позиции с поворотом по умолчанию.
    /// </summary>
    public static EnemyBase CreateEnemy(EnemyData data, Vector3 position)
    {
        return CreateEnemy(data, position, Quaternion.identity);
    }

    /// <summary>
    /// Создаёт врага в позиции (0, 0, 0) с поворотом по умолчанию.
    /// </summary>
    public static EnemyBase CreateEnemy(EnemyData data)
    {
        return CreateEnemy(data, Vector3.zero);
    }
}
```

Разбор ключевых моментов:

- Класс помечен как `static` — не нужен экземпляр, все методы статические.
- Метод `CreateEnemy` проверяет входные данные перед созданием.
- После создания префаба мы настраиваем компонент `EnemyBase`:
  - назначаем `enemyData`;
  - компонент сам инициализирует здоровье в `Awake()`.
- Возвращаем `EnemyBase`, а не `GameObject` — это позволяет работать с врагом через единый интерфейс.

---

## 5. Как EnemyFactory будет использоваться дальше

На следующем уроке (7.4):

- `EnemySpawner` будет вызывать:
  - `EnemyFactory.CreateEnemy(enemyData, spawnPoint.position)`
  - для создания врагов в точках спавна.

В будущем:

- На Этапе 10 (Object Pool):
  - `EnemyFactory` можно будет модифицировать для использования пула объектов вместо `Instantiate`.
- В других системах:
  - квесты могут создавать врагов через фабрику;
  - события могут спавнить врагов через фабрику;
  - все используют единый интерфейс создания.

---

## 6. Тестирование EnemyFactory

Для проверки можно создать простой тестовый скрипт:

1. Создай пустой объект в сцене.
2. Добавь компонент с таким кодом:

```csharp
using UnityEngine;

public class EnemyFactoryTest : MonoBehaviour
{
    public EnemyData testEnemyData;

    private void Start()
    {
        if (testEnemyData != null)
        {
            // Создаём врага в позиции этого объекта
            EnemyBase enemy = EnemyFactory.CreateEnemy(testEnemyData, transform.position);
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
- Метод `CreateEnemy` проверяет входные данные и настраивает компонент `EnemyBase`.

Если всё это выполнено и понятно — можно переходить к уроку 7.4 (`EnemySpawner` — спавнер врагов).
