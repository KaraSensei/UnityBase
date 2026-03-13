# Урок 7.2: EnemyBase — базовый класс врага

---

## 0. Теория: наследование и полиморфизм на примере врагов

На Этапе 6 мы применяли наследование для оружия (`WeaponBase` → `MeleeWeapon`, `RangedWeapon`). Теперь мы применяем те же принципы к врагам.

### 0.1. Что такое наследование в ООП (повторение)

- **Наследование** — это механизм, который позволяет создать новый класс на основе существующего:
  - дочерний класс **получает** поля и методы базового;
  - может **добавлять** новые поля и методы;
  - может **изменять** (переопределять) поведение базовых методов.

В нашем случае:

- `EnemyBase` — **базовый класс** для всех врагов.
- Позже можно создать `MeleeEnemy`, `RangedEnemy`, `BossEnemy` — **наследники** `EnemyBase`.

Принцип «is-a»:

- Гоблин **является** врагом → `MeleeEnemy : EnemyBase`.
- Лучник **является** врагом → `RangedEnemy : EnemyBase`.

### 0.2. Полиморфизм

- **Полиморфизм** позволяет работать с разными объектами через один общий тип.

Для нас это означает:

- `EnemySpawner` и `EnemyFactory` работают с типом `EnemyBase`:
  - `EnemyBase CreateEnemy(EnemyData data)`;
  - не важно, какой конкретный тип врага создаётся — все они наследуются от `EnemyBase`.
- Позже, когда появится ИИ (Этап 9), он будет работать с `EnemyBase`, не зная конкретного типа.

### 0.3. Связь с SOLID

- **S — Single Responsibility**:
  - `EnemyData` отвечает за данные (базовые числа);
  - `EnemyStats` — за состояние и статы конкретного экземпляра врага (текущее здоровье, расчёт урона, смерть);
  - `EnemyBase` — за общую логику поведения (поиск цели, движение, базовая атака);
  - будущие наследники `EnemyBase` — за специфическое поведение (ближний/дальний бой, боссы).
- **O — Open/Closed**:
  - мы можем добавлять новые типы врагов (`FlyingEnemy`, `StealthEnemy`), не изменяя код `EnemySpawner` и `EnemyFactory`.
- **L — Liskov Substitution**:
  - любой наследник `EnemyBase` должен корректно работать там, где ожидается `EnemyBase`.

### 0.4. Теория: слои, теги и Raycast

Прежде чем использовать поиск цели в коде, важно понимать три связанных понятия в Unity:

- **Слои (Layers)** — используются для **фильтрации физики и лучей**.  
  - В `Project Settings → Tags and Layers` можно создать слой `Player` и назначить его объекту игрока.  
  - Поле `LayerMask playerLayer` в `EnemyBase` говорит `Physics.OverlapSphere`, **по каким слоям вообще искать коллайдеры**.
- **Теги (Tags)** — текстовые ярлыки для объектов (например, `Player`, `Enemy`).  
  - Один и тот же тег можно повесить на несколько объектов.  
  - Тег удобен, когда нам нужно понять **«кто это»**, а не управлять физикой. В этом уроке мы храним тег игрока в поле `playerTag`, чтобы позже иметь возможность дополнительно проверять цель.
- **Raycast** — «луч» из точки в направлении, который проверяет, **что он «ударил» по пути**.  
  - Пример: `Physics.Raycast(origin, direction, out hitInfo, maxDistance, layerMask)`.  
  - В этом уроке мы используем **OverlapSphere** (проверка «кто находится внутри сферы»), но на презентации полезно показать, что Raycast позволяет:
    - проверять, есть ли **прямая видимость** до игрока (нет ли стены между врагом и игроком);
    - делать точечные проверки под ногами (есть ли земля под врагом/игроком);
    - использовать те же **LayerMask** для фильтрации, по чему луч может «попасть».

Важно:  
- **Слои** — про то, какие объекты участвуют в физике и лучах.  
- **Теги** — про то, «кто это» логически (игрок, враг, предмет).  
- **Raycast/OverlapSphere** — инструменты физики, которые используют слои и помогают врагам «видеть» игрока.

---

## 1. Цели урока

- **Техническая цель**: создать два связанных класса:
  - `EnemyStats` — компонент, который хранит ссылку на `EnemyData`, текущее здоровье и реализует получение урона/смерть;
  - `EnemyBase` — компонент, который использует `EnemyStats` для чисел и реализует базовую логику поиска цели, движения и атаки.
- **Обучающая цель**: показать, как разделить статы и поведение между разными компонентами и как базовый класс задаёт «контракт» для всех врагов.

После урока у тебя будет фундамент: статы (`EnemyStats`) и базовое поведение (`EnemyBase`), от которого при желании можно наследоваться для специализированных врагов.

---

## 2. Подготовка

Перед началом убедись, что:

1. **Урок 7.1** выполнен:
   - есть класс `EnemyData`;
   - есть несколько ассетов врагов в `Assets/_ScriptableObjects/Enemies/`.
2. В проекте есть папка:
   - `Assets/_Scripts/Enemies/`

---

## 3. Проектирование структуры EnemyStats и EnemyBase

Подумай, что должно быть общим для ЛЮБОГО врага, но раздели это на **статы** и **поведение**:

- **Данные (конфигурация)**:
  - `EnemyData` — ScriptableObject с базовыми числами (здоровье, урон, скорость и т.д.).
- **Состояние и статы конкретного экземпляра** — это зона ответственности `EnemyStats`:
  - ссылка на `EnemyData`;
  - текущее здоровье;
  - доступ к урону, скорости движения, дальности атаки/обнаружения, награде за убийство;
  - получение урона (`TakeDamage`) и смерть (`Die`).
- **Поведение/логика движения и атаки** — это зона ответственности `EnemyBase`:
  - хранит цель (игрок или другой объект);
  - ищет цель (`FindTarget`);
  - двигается к цели (`MoveTowardsTarget`);
  - атакует цель (`Attack`, базовая реализация);
  - использует значения из `EnemyStats` (скорость, дальности, урон), но сам не хранит числа.

Из этого следует структура:

- Компонент `EnemyStats`:
  - поле `EnemyData enemyData` (назначается в инспекторе или через фабрику);
  - приватное поле `currentHealth`;
  - свойства `MaxHealth`, `MoveSpeed`, `Damage`, `AttackRange`, `DetectionRange`, `ExperienceReward`;
  - методы:
    - `Setup(EnemyData data)` — назначение данных и инициализация;
    - `InitializeFromData()` — установка текущего здоровья по данным (используется внутри `Setup`);
    - `TakeDamage(float damage)` — получение урона и проверка смерти;
    - `Die()` — базовая логика смерти врага.
- Компонент `EnemyBase`:
  - ссылка на `EnemyStats stats`;
  - поле `Transform target` — текущая цель (обычно игрок);
  - поля `LayerMask playerLayer` и `string playerTag` для поиска цели;
  - простые тайминги:
    - `targetSearchInterval` — как часто искать цель;
    - `attackCooldown` — пауза между атаками;
  - методы:
    - `FindTarget()` — поиск цели в радиусе обнаружения, используя `stats.DetectionRange`;
    - `MoveTowardsTarget()` — движение к цели с использованием `stats.MoveSpeed`;
    - `Attack()` — базовая атака, использующая `stats.Damage`.

---

## 4. Создание скриптов EnemyStats и EnemyBase

### 4.1. Шаги в Unity

1. В окне `Project` перейди в `Assets/_Scripts/Enemies/`.
2. ПКМ → `Create` → `C# Script` и создай два скрипта:
   - **`EnemyStats`**;
   - **`EnemyBase`**.
3. Открой оба скрипта в редакторе.

### 4.2. Реализация EnemyStats

Скрипт `EnemyStats` отвечает за связь с `EnemyData` и текущее состояние врага. Пример итогового кода (набирай сам, понимая каждую строку):

Почему мы делаем инициализацию именно так:

- `EnemyData` — это **конфиг** (числа “по умолчанию”), а `currentHealth` — **состояние конкретного экземпляра** врага в сцене.
- Мы инициализируем `currentHealth` из `EnemyData`, чтобы у каждого созданного врага было корректное стартовое здоровье.
- В Unity есть нюанс порядка вызовов: `Awake()` вызывается сразу после `Instantiate`, а фабрика часто назначает `EnemyData` уже после создания объекта.
  Поэтому у нас есть единая точка инициализации `Setup(data)` — её вызывает фабрика.
  А `Awake()` инициализирует только тот случай, когда данные назначены прямо в инспекторе (для простых тестов/ручной настройки префаба).

```csharp
using System;
using UnityEngine;

/// <summary>
/// Отвечает за статы врага: здоровье, урон, скорость и т.п.
/// Читает базовые числа из EnemyData и хранит текущее состояние.
/// </summary>
public class EnemyStats : MonoBehaviour
{
    [Header("Данные врага")]
    [Tooltip("ScriptableObject с базовыми параметрами врага.")]
    [SerializeField] private EnemyData enemyData;

    [Header("Состояние")]
    [Tooltip("Текущее здоровье врага.")]
    [SerializeField] private float currentHealth;

    public EnemyData EnemyData => enemyData;
    public float MaxHealth => enemyData != null ? enemyData.maxHealth : 0f;
    public float MoveSpeed => enemyData != null ? enemyData.moveSpeed : 0f;
    public float Damage => enemyData != null ? enemyData.damage : 0f;
    public float AttackRange => enemyData != null ? enemyData.attackRange : 0f;
    public float DetectionRange => enemyData != null ? enemyData.detectionRange : 0f;
    public float ExperienceReward => enemyData != null ? enemyData.experienceReward : 0f;

    /// <summary>
    /// Событие "враг умер".
    /// На него будут подписываться другие системы: пул врагов, система опыта и т.д.
    /// </summary>
    public event Action<EnemyStats> OnDied;

    private void Awake()
    {
        // Awake вызывается сразу после создания объекта.
        // Если данные назначены в инспекторе — можно инициализировать состояние сразу.
        // Если враг создаётся фабрикой, она вызовет Setup(data) после назначения EnemyData.
        if (enemyData != null)
        {
            InitializeFromData();
        }
    }

    /// <summary>
    /// Единая точка инициализации для врагов, созданных через фабрику.
    /// </summary>
    public void Setup(EnemyData data)
    {
        enemyData = data;
        InitializeFromData();
    }

    /// <summary>
    /// Инициализирует текущее здоровье из EnemyData.
    /// </summary>
    public void InitializeFromData()
    {
        // EnemyData — конфиг, currentHealth — runtime-состояние экземпляра.
        if (enemyData != null)
        {
            currentHealth = enemyData.maxHealth;
        }
        else
        {
            currentHealth = 0f;
        }
    }

    /// <summary>
    /// Получение урона.
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (damage <= 0f)
            return;

        currentHealth -= damage;
        Debug.Log($"{name}: получил {damage} урона. Здоровье: {currentHealth}/{MaxHealth}");

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    /// <summary>
    /// Смерть врага.
    /// </summary>
    public virtual void Die()
    {
        Debug.Log($"{name}: умер! Награда за убийство: {ExperienceReward} опыта.");

        // Здесь мы только "сообщаем" всем подписчикам, что этот враг умер.
        // Что именно делать дальше (вернуть в пул, дать опыт игроку и т.п.)
        // решают другие системы, которые подписались на это событие.
        OnDied?.Invoke(this);
    }
}
```

### 4.3. Реализация EnemyBase

Скрипт `EnemyBase` отвечает за поведение: поиск цели, движение, атаку. Он не хранит числа напрямую, а использует `EnemyStats`:

Почему в `EnemyBase` есть тайминги:

- Без кулдауна базовая атака будет вызываться каждый кадр, что выглядит неправильно и ломает баланс.
- Поиск цели через `Physics.OverlapSphere` тоже не стоит делать каждый кадр для каждого врага — поэтому мы ищем цель раз в небольшой интервал.

```csharp
using UnityEngine;

/// <summary>
/// Базовое поведение врага: поиск цели, движение, атака.
/// Все статы (здоровье, урон, скорости) живут в EnemyStats.
/// </summary>
public class EnemyBase : MonoBehaviour
{
    [Header("Компоненты")]
    [Tooltip("Компонент со статами врага.")]
    [SerializeField] private EnemyStats stats;

    [Header("Цель")]
    [Tooltip("Текущая цель врага (обычно игрок).")]
    [SerializeField] private Transform target;

    [Header("Настройки поиска цели")]
    [Tooltip("Слой, на котором находится игрок.")]
    [SerializeField] private LayerMask playerLayer;

    [Tooltip("Тег игрока.")]
    [SerializeField] private string playerTag = "Player";

    [Header("Простые тайминги (учебно)")]
    [Tooltip("Как часто враг пытается искать цель, если цель не найдена.")]
    [Min(0.05f)]
    [SerializeField] private float targetSearchInterval = 0.5f;

    [Tooltip("Минимальная пауза между атаками (чтобы не атаковать каждый кадр).")]
    [Min(0f)]
    [SerializeField] private float attackCooldown = 1f;

    private float nextTargetSearchTime;
    private float nextAttackTime;

    private void Awake()
    {
        // Подтягиваем EnemyStats автоматически, чтобы префаб был устойчивым.
        if (stats == null)
        {
            stats = GetComponent<EnemyStats>();
        }
    }

    private void Start()
    {
        FindTarget();
    }

    private void Update()
    {
        if (stats == null)
            return;

        if (target == null)
        {
            // Ищем цель не каждый кадр, а по интервалу.
            if (Time.time >= nextTargetSearchTime)
            {
                nextTargetSearchTime = Time.time + targetSearchInterval;
                FindTarget();
            }
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        if (distanceToTarget > stats.DetectionRange)
        {
            target = null;
            return;
        }

        if (distanceToTarget <= stats.AttackRange)
        {
            TryAttack();
        }
        else
        {
            MoveTowardsTarget();
        }
    }

    private void TryAttack()
    {
        // Кулдаун защищает от атаки "каждый кадр".
        if (Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + attackCooldown;
        Attack();
    }

    public void FindTarget()
    {
        if (stats == null)
            return;

        Collider[] colliders = Physics.OverlapSphere(transform.position, stats.DetectionRange, playerLayer);

        if (colliders.Length == 0)
            return;

        if (!string.IsNullOrEmpty(playerTag))
        {
            target = null;
            foreach (Collider col in colliders)
            {
                if (col.CompareTag(playerTag))
                {
                    target = col.transform;
                    break;
                }
            }
        }
        else
        {
            target = colliders[0].transform;
        }

        if (target != null)
        {
            Debug.Log($"{name}: нашёл цель — {target.name}");
        }
    }

    public void MoveTowardsTarget()
    {
        if (stats == null || target == null)
            return;

        Vector3 direction = (target.position - transform.position).normalized;
        direction.y = 0f;

        transform.position += direction * stats.MoveSpeed * Time.deltaTime;

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }

    public virtual void Attack()
    {
        if (stats == null || target == null)
            return;

        Debug.Log($"{name}: атакует {target.name} с уроном {stats.Damage}");

        // На Этапе 8 здесь можно будет вызывать IDamageable у цели и передавать stats.Damage.
    }

    private void OnDrawGizmosSelected()
    {
        // Gizmos рисуются в редакторе, даже если Awake ещё не вызывался.
        if (stats == null)
        {
            stats = GetComponent<EnemyStats>();
        }

        if (stats == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stats.DetectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stats.AttackRange);
    }
}
```

Обрати внимание:

- Мы разделили ответственность:
  - `EnemyStats` отвечает за статы и здоровье;
  - `EnemyBase` — за поиск цели, движение и атаку.
- `EnemyBase` не знает про `EnemyData` напрямую, он работает только через `EnemyStats`.
- Такой подход облегчает дальнейшее развитие (например, добавление бафов/дебафов в `EnemyStats` или наследников `EnemyBase` с разным поведением).

---

## 5. Как EnemyBase и EnemyStats будут использоваться дальше

На следующих уроках:

- `EnemyFactory` будет:
  - создавать экземпляр префаба врага;
  - передавать `EnemyData` в компонент `EnemyStats` через `Setup(data)`;
  - проверять наличие `EnemyBase` и связывать его с `EnemyStats`;
  - возвращать созданный объект типа `EnemyBase`.
- `EnemySpawner` будет:
  - использовать `EnemyFactory` для создания врагов;
  - работать с типом `EnemyBase`, не зная конкретного типа врага и не залезая в его статы.
- На Этапе 8 (IDamageable):
  - входящий урон по врагу будет обрабатываться в `EnemyStats.TakeDamage()` (через интерфейс IDamageable или вызовы из других систем);
  - `EnemyBase.Attack()` будет брать величину урона из `stats.Damage` и наносить его игроку через IDamageable.
- На Этапе 9 (ИИ врагов):
  - можно будет расширить логику поиска цели и движения в `EnemyBase`;
  - можно будет добавлять различные состояния (патрулирование, преследование, атака) поверх существующего каркаса.

---

## 6. Мини‑проверка

Ответь на вопросы:

1. За что отвечают `EnemyStats` и за что — `EnemyBase`?
2. Почему выгодно хранить статы и текущее здоровье отдельно от логики движения/атаки?
3. Как `EnemyBase` получает доступ к числам из `EnemyData` (через какой компонент)?

Проверь код:

- `EnemyStats` и `EnemyBase` находятся в папке `Assets/_Scripts/Enemies/`.
- Оба класса наследуются от `MonoBehaviour`.
- Логика получения урона и смерти реализована в `EnemyStats`, а поиск цели/движение/атака — в `EnemyBase`.
- `EnemyStats` инициализируется данными врага через `Setup(EnemyData data)`.

Если всё это выполнено и понятно — можно переходить к уроку 7.3 (`EnemyFactory` — паттерн Factory для создания врагов).

---

## 7. Альтернативные реализации для других типов игр

В этом уроке мы пишем `EnemyStats` и `EnemyBase` для **3D‑игры от третьего лица**, но та же идея разделения статов и поведения работает и в других форматах:

- **2D‑игра (вид сбоку или top‑down):**
  - Вместо `Physics.OverlapSphere` можно использовать 2D‑физику (`Physics2D.OverlapCircle`) и компоненты `Rigidbody2D`/`Collider2D`.
  - При движении к цели игнорировать ось `Z` и работать только с `X`/`Y`.
  - В остальном структура та же: ScriptableObject `EnemyData` + компонент статов (`EnemyStats`) + компонент поведения (`EnemyBase`).
- **Шутер от первого лица:**
  - Целью может быть не только объект игрока, но и его камера (`Camera.main.transform`), чтобы враг смотрел именно туда, где «глаза» игрока.
  - Можно добавить Raycast из врага к игроку, чтобы атаковать только при **прямой видимости** (нет стен/препятствий между ними).
- **Другие жанры (симуляторы, стратегии, tower defense):**
  - Пара `EnemyStats` + `EnemyBase` превращается в базовый набор компонентов «юнита»/«агента»: отдельный компонент для статов и отдельный — для поведения.
  - Логика поиска цели и движения может быть другой (по пути, по точкам интереса, по ближайшей башне), но принципы разделения ответственности и наследования остаются теми же.

Задача этого урока — показать универсальный каркас врага, который можно адаптировать под любой формат игры, не переписывая всё с нуля.
