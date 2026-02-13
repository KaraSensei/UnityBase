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
  - `EnemyData` отвечает за данные;
  - `EnemyBase` — за общую логику врага (здоровье, движение, поиск цели);
  - будущие наследники — за специфическое поведение (ближний/дальний бой).
- **O — Open/Closed**:
  - мы можем добавлять новые типы врагов (`FlyingEnemy`, `StealthEnemy`), не изменяя код `EnemySpawner` и `EnemyFactory`.
- **L — Liskov Substitution**:
  - любой наследник `EnemyBase` должен корректно работать там, где ожидается `EnemyBase`.

---

## 1. Цели урока

- **Техническая цель**: создать класс `EnemyBase`, который:
  - хранит ссылку на `EnemyData`;
  - управляет здоровьем врага;
  - реализует базовую логику поиска цели и движения;
  - предоставляет методы для получения урона и смерти.
- **Обучающая цель**: показать, как базовый класс задаёт «контракт» для всех врагов и как наследование упрощает архитектуру.

После урока у тебя будет фундамент, от которого будут наследоваться специализированные враги (если понадобится).

---

## 2. Подготовка

Перед началом убедись, что:

1. **Урок 7.1** выполнен:
   - есть класс `EnemyData`;
   - есть несколько ассетов врагов в `Assets/_ScriptableObjects/Enemies/`.
2. В проекте есть папка:
   - `Assets/_Scripts/Enemies/`

---

## 3. Проектирование структуры EnemyBase

Подумай, что должно быть общим для ЛЮБОГО врага:

- **Данные**:
  - ссылка на `EnemyData` (здоровье, скорость, урон и т.д.).
- **Состояние**:
  - текущее здоровье;
  - цель (игрок или другой объект).
- **Поведение**:
  - получение урона (`TakeDamage`);
  - смерть (`Die`);
  - поиск цели (`FindTarget`);
  - движение к цели (`MoveTowardsTarget`);
  - атака (`Attack` — базовая реализация).

Из этого следует структура:

- Поле `EnemyData enemyData`.
- Поле `float currentHealth` — текущее здоровье.
- Поле `Transform target` — текущая цель (обычно игрок).
- Свойства:
  - `MaxHealth`, `MoveSpeed`, `Damage`, `AttackRange`, `DetectionRange` — удобные геттеры к данным.
- Методы:
  - `void TakeDamage(float damage)` — получение урона.
  - `void Die()` — смерть врага.
  - `void FindTarget()` — поиск цели в радиусе обнаружения.
  - `void MoveTowardsTarget()` — движение к цели.
  - `void Attack()` — атака (базовая реализация, можно переопределить в наследниках).

---

## 4. Создание скрипта EnemyBase

### 4.1. Шаги в Unity

1. В окне `Project` перейди в `Assets/_Scripts/Enemies/`.
2. ПКМ → `Create` → `C# Script`.
3. Назови скрипт **`EnemyBase`**.
4. Открой его в редакторе.

### 4.2. Реализация EnemyBase

Замените содержимое файла на следующий код (набирай вручную, не копируй вслепую):

```csharp
using UnityEngine;

/// <summary>
/// Базовый класс для любого врага.
/// Хранит ссылку на EnemyData и реализует базовую логику здоровья, движения и атаки.
/// </summary>
public class EnemyBase : MonoBehaviour
{
    [Header("Данные врага")]
    [Tooltip("ScriptableObject с базовыми параметрами врага.")]
    public EnemyData enemyData;

    [Header("Состояние")]
    [Tooltip("Текущее здоровье врага.")]
    [SerializeField] private float currentHealth;

    [Tooltip("Текущая цель врага (обычно игрок).")]
    public Transform target;

    [Header("Настройки поиска цели")]
    [Tooltip("Слой, на котором находится игрок.")]
    public LayerMask playerLayer;

    [Tooltip("Тег игрока.")]
    public string playerTag = "Player";

    // Свойства для удобного доступа к данным
    public float MaxHealth => enemyData != null ? enemyData.maxHealth : 0f;
    public float MoveSpeed => enemyData != null ? enemyData.moveSpeed : 0f;
    public float Damage => enemyData != null ? enemyData.damage : 0f;
    public float AttackRange => enemyData != null ? enemyData.attackRange : 0f;
    public float DetectionRange => enemyData != null ? enemyData.detectionRange : 0f;

    private void Awake()
    {
        // Инициализация здоровья при создании
        if (enemyData != null)
        {
            currentHealth = enemyData.maxHealth;
        }
    }

    private void Start()
    {
        // Поиск цели при старте
        FindTarget();
    }

    private void Update()
    {
        // Если цель потеряна, пытаемся найти новую
        if (target == null)
        {
            FindTarget();
            return;
        }

        // Проверяем расстояние до цели
        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        // Если цель слишком далеко, теряем её
        if (distanceToTarget > DetectionRange)
        {
            target = null;
            return;
        }

        // Если цель в радиусе атаки — атакуем
        if (distanceToTarget <= AttackRange)
        {
            Attack();
        }
        else
        {
            // Иначе движемся к цели
            MoveTowardsTarget();
        }
    }

    /// <summary>
    /// Поиск цели (игрока) в радиусе обнаружения.
    /// </summary>
    public void FindTarget()
    {
        // Ищем объекты с тегом Player в радиусе обнаружения
        Collider[] colliders = Physics.OverlapSphere(transform.position, DetectionRange, playerLayer);

        if (colliders.Length > 0)
        {
            // Берём первый найденный объект как цель
            target = colliders[0].transform;
            Debug.Log($"{name}: нашёл цель — {target.name}");
        }
    }

    /// <summary>
    /// Движение к цели.
    /// </summary>
    public void MoveTowardsTarget()
    {
        if (target == null)
            return;

        // Направление к цели
        Vector3 direction = (target.position - transform.position).normalized;
        direction.y = 0f; // Игнорируем вертикальную составляющую для 2D или top-down

        // Движение
        transform.position += direction * MoveSpeed * Time.deltaTime;

        // Поворот к цели (опционально)
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
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
        Debug.Log($"{name}: умер!");

        // Здесь позже можно добавить:
        // - выпадение лута
        // - начисление опыта игроку
        // - анимацию смерти
        // - звук смерти

        Destroy(gameObject);
    }

    /// <summary>
    /// Атака цели (базовая реализация).
    /// </summary>
    public virtual void Attack()
    {
        if (target == null)
            return;

        Debug.Log($"{name}: атакует {target.name} с уроном {Damage}");

        // Здесь позже, на Этапе 8, мы будем вызывать метод получения урона
        // у игрока через интерфейс IDamageable:
        // var damageable = target.GetComponent<IDamageable>();
        // if (damageable != null)
        // {
        //     damageable.TakeDamage(Damage);
        // }
    }

    private void OnDrawGizmosSelected()
    {
        // Рисуем радиус обнаружения и радиус атаки в редакторе
        if (enemyData == null)
            return;

        // Радиус обнаружения (жёлтый)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, DetectionRange);

        // Радиус атаки (красный)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, AttackRange);
    }
}
```

Обрати внимание:

- `EnemyBase` наследуется от `MonoBehaviour`, потому что мы будем вешать его на объекты сцены/префабы.
- Метод `Die()` помечен как `virtual` — наследники могут переопределить его для специфического поведения смерти.
- Метод `Attack()` тоже `virtual` — наследники могут реализовать свою логику атаки (ближний/дальний бой).
- В `Update()` реализована простая логика ИИ: поиск цели → движение → атака.

---

## 5. Как EnemyBase будет использоваться дальше

На следующих уроках:

- `EnemyFactory` будет:
  - создавать экземпляр префаба врага;
  - назначать `EnemyData` в компонент `EnemyBase`;
  - возвращать созданный объект.
- `EnemySpawner` будет:
  - использовать `EnemyFactory` для создания врагов;
  - работать с типом `EnemyBase`, не зная конкретного типа врага.
- На Этапе 8 (IDamageable):
  - `EnemyBase.TakeDamage()` будет вызываться из системы урона игрока;
  - `EnemyBase.Attack()` будет наносить урон игроку через `IDamageable`.
- На Этапе 9 (ИИ врагов):
  - можно будет расширить логику поиска цели и движения;
  - можно будет добавить состояния (патрулирование, преследование, атака).

---

## 6. Мини‑проверка

Ответь на вопросы:

1. Почему `EnemyBase` не сделан абстрактным, в отличие от `WeaponBase`?
2. Какие методы помечены как `virtual` и зачем?
3. Как `EnemyBase` использует `EnemyData` для получения параметров?

Проверь код:

- `EnemyBase` находится в папке `Assets/_Scripts/Enemies/`.
- Класс наследуется от `MonoBehaviour`.
- Методы `Die()` и `Attack()` помечены как `virtual`.

Если всё это выполнено и понятно — можно переходить к уроку 7.3 (`EnemyFactory` — паттерн Factory для создания врагов).
