# Урок 6.4: WeaponManager — управление оружием игрока и атакой

---

## 0. Теория: связываем Player, Input System и Weapons

К этому моменту у нас есть несколько независимых подсистем:

- **Игрок**:
  - `PlayerData` + `PlayerStats` (статы и базовые параметры);
  - `PlayerController` (движение);
  - `PlayerProgression` (уровни и опыт).
- **Input System**:
  - `InputManager`, который даёт нам действия `Move`, `Jump`, `Attack`, `Pause` и т.д.
- **Оружие**:
  - `WeaponData` — данные оружия;
  - `WeaponBase` — базовый класс;
  - `MeleeWeapon` и `RangedWeapon` — конкретные виды оружия;
  - `Projectile` — снаряд для дальнего боя.

Теперь нужно сделать так, чтобы **игрок полноценно атаковал**:

- при нажатии кнопки атаки в `InputManager`;
- с учётом текущего экипированного оружия (меч/лук/посох);
- с возможностью смены оружия в будущем.

Для этого мы создаём **`WeaponManager`** — компонент на игроке, который:

- знает, какое оружие сейчас экипировано;
- по сигналу от `InputManager` вызывает `Attack()` у текущего оружия;
- в будущем будет отвечать за смену оружия, интеграцию с инвентарём и лутом.

---

## 1. Цели урока

- **Техническая цель**:
  - создать компонент `WeaponManager` на игроке;
  - связать его с `InputManager` (атака по нажатию);
  - настроить стартовое оружие (меч) так, чтобы игрок мог реально атаковать.
- **Обучающая цель**:
  - показать, как несколько подсистем (Player, Input, Weapons) связываются через понятные интерфейсы;
  - закрепить идею, что `WeaponManager` работает с оружием через базовый тип `WeaponBase` (полиморфизм).

После урока у нас будет **полноценный персонаж, который может атаковать** (логика нанесения урона врагам появится на Этапе 8).

---

## 2. Подготовка

Перед началом убедись, что:

1. `InputManager` настроен (см. `Этап_3_Input_System_Подробно.md`):
   - есть действие `Attack` в Action Map **Player**;
   - `InputManager` поднимает событие `OnAttackPressed`.
2. Урок 6.3 выполнен:
   - есть скрипты `MeleeWeapon`, `RangedWeapon`, `Projectile`;
   - есть префабы оружия (например, `Sword_Melee`, `Bow_Ranged`) в `Assets/Prefabs/Weapons/`;
   - у префаба ближнего оружия настроены `WeaponData`, `attackOrigin` и `hitLayers`.
3. У тебя готов префаб `Player` (см. `Этап_5_4_PlayerPrefab_Сборка_Подробно.md`).

---

## 3. Дизайн WeaponManager

### 3.1. Задачи WeaponManager

`WeaponManager` — это компонент на объекте `Player`, который:

- хранит ссылку на **текущее оружие** (`WeaponBase currentWeapon`);
- знает, какое оружие выдавать игроку при старте (например, меч по умолчанию);
- подписывается на событие атаки из `InputManager` и делегирует его текущему оружию:
  - `currentWeapon.Attack();`
- в будущем сможет:
  - переключать оружие (по кнопкам/через инвентарь);
  - интегрироваться с системой лута (подбор нового оружия).

### 3.2. Почему через события, а не через прямой опрос в Update

В `InputManager` уже есть:

- флаг `AttackPressed`;
- событие `OnAttackPressed`.

Для одноразовых действий (атака, прыжок, взаимодействие) **события удобнее**:

- они вызываются ровно **один раз** на одно нажатие;
- не надо думать о порядке вызова `Update()` и `ResetButtonFlags()`;
- код получается чище: `WeaponManager` просто подписывается на `OnAttackPressed`.

---

## 4. Создание скрипта WeaponManager

### 4.1. Шаги в Unity

1. В окне `Project` перейди в `Assets/Scripts/Weapons/`.
2. ПКМ → `Create` → `C# Script`.
3. Назови скрипт **`WeaponManager`**.
4. Открой его в редакторе.

### 4.2. Реализация WeaponManager

Замените содержимое на следующий код:

```csharp
using UnityEngine;

/// <summary>
/// Управляет оружием игрока:
/// - хранит текущее оружие (WeaponBase),
/// - реагирует на ввод атаки через InputManager,
/// - в будущем сможет менять оружие.
/// </summary>
public class WeaponManager : MonoBehaviour
{
    [Header("Связи")]
    [Tooltip("Статы игрока (могут понадобиться для модификаторов урона, критов и т.п.).")]
    public PlayerStats playerStats;

    [Tooltip("Текущее активное оружие игрока.")]
    public WeaponBase currentWeapon;

    [Header("Стартовое оружие")]
    [Tooltip("Префаб оружия ближнего боя по умолчанию (например, меч).")]
    public WeaponBase defaultMeleeWeaponPrefab;

    [Tooltip("Позиция, в которой будет располагаться оружие (например, рука игрока).")]
    public Transform weaponSocket;

    private void Awake()
    {
        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();

        // Если у нас нет текущего оружия, но указан стартовый префаб — создаём его
        if (currentWeapon == null && defaultMeleeWeaponPrefab != null)
        {
            EquipNewWeapon(defaultMeleeWeaponPrefab);
        }
        else if (currentWeapon != null)
        {
            // Убедимся, что владелец и позиция корректно назначены
            SetupWeapon(currentWeapon);
        }
    }

    private void OnEnable()
    {
        // Подписка на событие атаки из InputManager
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnAttackPressed += HandleAttackPressed;
        }
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnAttackPressed -= HandleAttackPressed;
        }
    }

    /// <summary>
    /// Обработчик нажатия кнопки атаки.
    /// </summary>
    private void HandleAttackPressed()
    {
        if (currentWeapon == null)
        {
            Debug.LogWarning("WeaponManager: у игрока нет текущего оружия, атаковать нечем.");
            return;
        }

        currentWeapon.Attack();
    }

    /// <summary>
    /// Экипировать оружие из префаба (создаёт его экземпляр как дочерний объект в weaponSocket).
    /// </summary>
    public void EquipNewWeapon(WeaponBase weaponPrefab)
    {
        if (weaponPrefab == null)
        {
            Debug.LogWarning("WeaponManager.EquipNewWeapon: префаб оружия не задан.");
            return;
        }

        // Если есть старое оружие как дочерний объект — удаляем
        if (currentWeapon != null)
        {
            Destroy(currentWeapon.gameObject);
            currentWeapon = null;
        }

        Transform parent = weaponSocket != null ? weaponSocket : transform;

        WeaponBase newWeapon = Instantiate(weaponPrefab, parent);
        currentWeapon = newWeapon;

        SetupWeapon(currentWeapon);
    }

    /// <summary>
    /// Настроить только что экипированное оружие:
    /// - указать владельца,
    /// - обнулить локальную позицию/вращение.
    /// </summary>
    private void SetupWeapon(WeaponBase weapon)
    {
        if (weapon == null)
            return;

        weapon.owner = transform;

        // Привязываем оружие к сокету: локальная позиция/вращение = 0
        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localRotation = Quaternion.identity;
    }
}
```

Разбор:

- `WeaponManager` **не** знает, ближнее это оружие или дальнее — он работает с типом `WeaponBase`.
- Метод `HandleAttackPressed` вызывается только при нажатии кнопки атаки (спасибо `InputManager`).
- Метод `EquipNewWeapon`:
  - удаляет старое оружие (если было);
  - создаёт новое из префаба;
  - позиционирует его в `weaponSocket` (обычно рука/косточка персонажа).

---

## 5. Подключение WeaponManager к Player

### 5.1. Создание сокета для оружия

1. Открой префаб `Player` (см. урок 5.4).
2. Внутри объекта `Player` создай пустой `GameObject`, назови его, например, `WeaponSocket`:
   - его можно разместить:
     - как дочерний объект к визуальной модели (например, к руке);
     - или просто в центре игрока для начала (для теста не критично).
3. Сохраните префаб `Player`.

### 5.2. Добавление WeaponManager на Player

1. Выбери объект `Player` в префабе или в сцене.
2. В Inspector:
   - нажми `Add Component`;
   - добавь `WeaponManager`.
3. В полях `WeaponManager`:
   - `Player Stats` можно оставить пустым — скрипт сам найдёт компонент `PlayerStats` на этом объекте;
   - `Weapon Socket` — перетащи сюда созданный объект `WeaponSocket`;
   - `Default Melee Weapon Prefab` — перетащи префаб `Sword_Melee` (из урока 6.3).
4. Сохрани префаб `Player`.

---

## 6. Проверка работы атаки

1. Запусти игру через `Bootstrap` (как предусмотрено на Этапе 2).
2. Через главное меню перейди в `GameScene` (кнопка «Новая игра»).
3. Во время игры:
   - нажимай кнопку атаки (`Attack` из Input System — ЛКМ/X на геймпаде);
   - следи за консолью.

Ожидаемое поведение:

- При первом входе в сцену:
  - у игрока в иерархии (под `WeaponSocket`) появляется экземпляр меча (`MeleeWeapon`);
  - в `Inspector` у `WeaponManager` поле `Current Weapon` заполнено ссылкой на этот компонент.
- При нажатии `Attack`:
  - событие `OnAttackPressed` в `InputManager` срабатывает;
  - вызывается `WeaponManager.HandleAttackPressed()`;
  - вызывается `currentWeapon.Attack()`:
    - если это `MeleeWeapon`, в консоли появятся логи о ближней атаке;
    - если рядом есть объекты на подходящем слое, будет залогировано, что по ним «попали».

Если всё это работает — значит, цепочка **Input → WeaponManager → WeaponBase/наследник** настроена правильно.

---

## 7. Как расширить WeaponManager в будущем (для продвинутых)

На следующих этапах (`Инвентарь`, `Лут`) можно будет:

- добавить в `WeaponManager`:
  - список доступных оружий;
  - методы `SwitchToNextWeapon()`, `EquipById()` и т.п.;
- интегрировать его с системой инвентаря:
  - при выборе предмета «Меч» в инвентаре вызывать `EquipNewWeapon` с нужным префабом;
- учитывать статы игрока (`PlayerStats`, `PlayerProgression`):
  - модифицировать урон (`Damage * коэффициент от уровня/статистики`);
  - влиять на скорость атаки.

Важно, что при этом:

- базовая структура **не меняется**:
  - `WeaponManager` по‑прежнему работает с `WeaponBase`;
  - новые виды оружия просто наследуются от `WeaponBase` и реализуют `Attack()`.

---

## 8. Мини‑проверка

Ответь на вопросы:

1. Какие три системы участвуют в атаке, начиная от нажатия клавиши и заканчивая выполнением `Attack()`?
2. Почему `WeaponManager` не должен знать, ближнее оружие или дальнее сейчас экипировано?
3. Что нужно изменить, чтобы при старте игрок получал не меч, а лук?

Если ты можешь уверенно ответить на эти вопросы и атака в игре работает — Этап 6 («Оружие и наследование») можно считать завершённым на уровне базовой архитектуры. Далее — переход к Этапу 7 (инвентарь) и Этапу 8 (враги и реальный урон).

