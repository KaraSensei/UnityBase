# Урок 7.1: EnemyData — ScriptableObject с базовыми данными врага

---

## 0. Теория и темы урока

В этом уроке мы делаем для врагов то же самое, что уже делали для игрока в уроке 5.1 (`PlayerData`) и для оружия в уроке 6.1 (`WeaponData`):

- `PlayerData` хранит **базовые числа** игрока (макс. здоровье, скорость и т.п.).
- `WeaponData` хранит **базовые числа** оружия (урон, скорость атаки, дальность).
- Теперь **EnemyData** будет хранить базовые числа **врага**:
  - здоровье;
  - скорость движения;
  - урон;
  - дальность атаки;
  - дальность обнаружения игрока;
  - награда за убийство (опыт);
  - префаб врага.

Позже `EnemyBase`, `EnemyFactory`, `EnemySpawner` будут эти данные **читать**, а не «зашивать» числа прямо в код.

**Ключевые идеи:**

- **Разделение данных и логики** (как с `PlayerData`/`PlayerStats` и `WeaponData`/`WeaponBase`).
- **Переиспользование** одного `EnemyData` для создания множества одинаковых врагов.
- Подготовка к **паттерну Factory** (Этап 7.3): фабрика будет создавать врагов на основе `EnemyData`, не зная конкретного типа врага.

---

## 1. Цели урока

- **Техническая цель**: создать ScriptableObject `EnemyData` с полями здоровья, скорости, урона, дальности и т.п., и несколько ассетов врагов (гоблин, орк, лучник).
- **Обучающая цель**: закрепить подход «данные отдельно от логики» и подготовить фундамент для системы врагов и паттерна Factory.

После выполнения урока у тебя будут ассеты, например:

- `Enemy_Goblin_Default`
- `Enemy_Orc_Default`
- `Enemy_Archer_Default`

которые будут использоваться в следующих уроках Этапа 7 и позже — в системе спавна, ИИ и боевой системе.

---

## 2. Подготовка папок

Перед началом убедись, что структура проекта соответствует общему плану:

- для скриптов:
  - `Assets/_Scripts/Enemies/`
- для ScriptableObject:
  - `Assets/_ScriptableObjects/Enemies/`

Если каких‑то папок нет — создай их:

1. В окне `Project`:
   - ПКМ по `Assets/_Scripts` → `Create` → `Folder` → `Enemies`
2. Аналогично для ScriptableObject:
   - ПКМ по `Assets/_ScriptableObjects` → `Create` → `Folder` → `Enemies`

---

## 3. Создание скрипта EnemyData

### 3.1. Шаги в Unity

1. В окне `Project` перейди в папку `Assets/_Scripts/Enemies/`.
2. ПКМ → `Create` → `C# Script`.
3. Назови скрипт **`EnemyData`** (точно так же, как имя класса).
4. Дважды кликни по скрипту, чтобы открыть его в редакторе.

### 3.2. Что нужно сделать в коде

По умолчанию Unity создаст класс, наследующийся от `MonoBehaviour`. Нам нужно:

1. Изменить наследование с `MonoBehaviour` на `ScriptableObject`.
2. Добавить атрибут `[CreateAssetMenu]`, чтобы можно было создавать `EnemyData` через меню `Create`.
3. Добавить публичные поля с понятными атрибутами (`[Header]`, `[Tooltip]`, `[Min]`).

Ниже пример итогового кода. Используй его как ориентир, но набирай код **самостоятельно**, понимая каждую строку.

```csharp
using UnityEngine;

/// <summary>
/// Данные для врага (здоровье, скорость, урон и т.п.).
/// Используется EnemyFactory для создания врагов и EnemyBase для чтения параметров.
/// </summary>
[CreateAssetMenu(
    fileName = "EnemyData",
    menuName = "Game Data/Enemy Data",
    order = 0)]
public class EnemyData : ScriptableObject
{
    public enum EnemyType
    {
        Melee,   // Ближний бой (гоблины, орки)
        Ranged,  // Дальний бой (лучники, маги)
        Boss     // Боссы (особые враги)
    }

    [Header("Общее")]
    [Tooltip("Читаемое название врага (для UI и логирования).")]
    public string enemyName = "New Enemy";

    [Tooltip("Тип врага (ближний, дальний, босс).")]
    public EnemyType enemyType = EnemyType.Melee;

    [Header("Характеристики")]
    [Min(1f)]
    [Tooltip("Максимальное здоровье врага.")]
    public float maxHealth = 50f;

    [Min(0f)]
    [Tooltip("Скорость движения врага (единиц в секунду).")]
    public float moveSpeed = 3f;

    [Min(0f)]
    [Tooltip("Урон, который враг наносит за одну атаку.")]
    public float damage = 10f;

    [Header("Бой")]
    [Min(0f)]
    [Tooltip("Дальность атаки врага (радиус ближнего боя или дальность выстрела).")]
    public float attackRange = 2f;

    [Min(0f)]
    [Tooltip("Дальность обнаружения игрока (на каком расстоянии враг начинает преследовать).")]
    public float detectionRange = 10f;

    [Header("Награды")]
    [Min(0f)]
    [Tooltip("Опыт, который получает игрок за убийство этого врага.")]
    public float experienceReward = 10f;

    [Header("Префаб")]
    [Tooltip("Префаб врага, который будет использоваться для создания экземпляров.")]
    public GameObject prefab;
}
```

Разбор ключевых моментов:

- `EnemyType` помогает отличать типы врагов в других системах (ИИ, анимации, спавн).
- `maxHealth` — максимальное здоровье, которое будет использоваться при создании врага.
- `moveSpeed` — скорость движения врага (единиц в секунду).
- `damage` — урон за одну атаку (будет использоваться в боевой системе на Этапе 8).
- `attackRange` — дальность атаки (для ближнего боя — радиус, для дальнего — дальность выстрела).
- `detectionRange` — дальность обнаружения игрока (для ИИ на Этапе 9).
- `experienceReward` — опыт за убийство (для системы прогрессии игрока).
- Поле `prefab` нужно для `EnemyFactory` — фабрика будет создавать врагов из этого префаба.

---

## 4. Создание ассетов EnemyData

Теперь создадим конкретные экземпляры данных врагов.

1. Перейди в папку `Assets/_ScriptableObjects/Enemies/`.
2. ПКМ → `Create` → `Game Data` → `Enemy Data`.
3. Создай минимум 3 ассета:
   - `Enemy_Goblin_Default`
   - `Enemy_Orc_Default`
   - `Enemy_Archer_Default`

4. Для каждого ассета в Inspector укажи примерные значения:

- **Enemy_Goblin_Default**:
  - `Enemy Name` — «Гоблин»
  - `Enemy Type` — `Melee`
  - `Max Health` — `50`
  - `Move Speed` — `3`
  - `Damage` — `10`
  - `Attack Range` — `2`
  - `Detection Range` — `10`
  - `Experience Reward` — `10`
  - `Prefab` — пока оставить пустым (настроим позже)

- **Enemy_Orc_Default**:
  - `Enemy Name` — «Орк»
  - `Enemy Type` — `Melee`
  - `Max Health` — `100`
  - `Move Speed` — `2`
  - `Damage` — `20`
  - `Attack Range` — `2.5`
  - `Detection Range` — `12`
  - `Experience Reward` — `25`
  - `Prefab` — пока оставить пустым

- **Enemy_Archer_Default**:
  - `Enemy Name` — «Лучник»
  - `Enemy Type` — `Ranged`
  - `Max Health` — `40`
  - `Move Speed` — `4`
  - `Damage` — `15`
  - `Attack Range` — `8`
  - `Detection Range` — `15`
  - `Experience Reward` — `15`
  - `Prefab` — пока оставить пустым

Префабы можно не заполнять сразу — к ним вернёмся после создания префаба врага в уроке 7.2.

---

## 5. Как EnemyData будет использоваться дальше

На следующих уроках Этапа 7:

- `EnemyBase` будет хранить ссылку на `EnemyData` и:
  - использовать `maxHealth` для инициализации здоровья;
  - использовать `moveSpeed` для движения;
  - использовать `damage` при атаке;
  - использовать `attackRange` и `detectionRange` для ИИ.
- `EnemyFactory` будет:
  - принимать `EnemyData` как параметр;
  - создавать экземпляр `prefab` из данных;
  - настраивать компонент `EnemyBase` на созданном объекте.
- `EnemySpawner` будет:
  - хранить список `EnemyData`;
  - случайно выбирать тип врага для спавна;
  - передавать данные в `EnemyFactory`.

Так же, как и с `PlayerData` и `WeaponData`, ты сможешь:

- создавать разные варианты одного врага (например, `Enemy_Goblin_Weak`, `Enemy_Goblin_Strong`) без изменения кода;
- настраивать баланс через Inspector, а не через переписывание скриптов.

---

## 6. Мини‑проверка

Ответь на вопросы (устно или письменно):

1. Чем `EnemyData` похож на `PlayerData` и `WeaponData`?
2. Какие параметры врага удобно хранить в ScriptableObject, а какие — в компонентах?
3. Почему выгодно хранить префаб (`prefab`) прямо в `EnemyData`?

И проверь в проекте:

- Класс `EnemyData` наследуется от `ScriptableObject`, а не от `MonoBehaviour`.
- В папке `Assets/_ScriptableObjects/Enemies/` есть ассеты для разных видов врагов.
- Изменяя `maxHealth` в `Enemy_Goblin_Default`, ты понимаешь, что это изменит здоровье гоблинов во всех системах, которые используют этот `EnemyData`.

Если всё это верно — переходи к уроку 7.2 (`EnemyBase` — базовый класс врага).
