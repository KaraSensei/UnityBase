/*
 * EnemyBase
 * Назначение: базовое поведение врага в runtime (цель, движение, атака, получение урона).
 * Что делает: хранит состояние здоровья экземпляра, преследует цель и выполняет простую атаку по кулдауну.
 * Связи: читает баланс из EnemyData, используется EnemySpawner и адаптером EnemyStats.
 * Паттерны: Single Responsibility (поведение врага), Data + Runtime State.
 */

using System;
using UnityEngine;

/// <summary>
/// Базовый класс поведения врага для simple-ветки.
/// </summary>
public class EnemyBase : MonoBehaviour, IDamageable
{
    private enum EnemyState
    {
        Chase,
        Attack,
        Dead
    }

    [Header("Данные врага")]
    [Tooltip("ScriptableObject с базовыми параметрами врага.")]
    [SerializeField] private EnemyData enemyData;

    [Header("Состояние")]
    [Tooltip("Текущее здоровье врага в рантайме.")]
    [SerializeField] private float currentHealth;

    [Header("Цель")]
    [Tooltip("Текущая цель врага (обычно игрок).")]
    [SerializeField] private Transform target;

    [Tooltip("Пробовать ли автоматически найти цель на старте, если она не задана.")]
    [SerializeField] private bool autoResolveTargetOnStart = true;

    [Header("Тайминги")]
    [Min(0f)]
    [SerializeField] private float attackCooldown = 1f;

    [Header("Смерть")]
    [Tooltip("Нужно ли уничтожать объект при смерти. Для pooled-врагов обычно выключается.")]
    [SerializeField] private bool destroyOnDeath = true;

    [Tooltip("Задержка перед уничтожением после смерти. Нужна для эффектов/анимаций.")]
    [Min(0f)]
    [SerializeField] private float destroyDelayAfterDeath = 0.15f;

    private float nextAttackTime;
    private bool isDead;
    private EnemyState currentState = EnemyState.Chase;

    public EnemyData Data => enemyData;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => enemyData != null ? enemyData.maxHealth : 0f;
    public float MoveSpeed => enemyData != null ? enemyData.moveSpeed : 0f;
    public float Damage => enemyData != null ? enemyData.damage : 0f;
    public float AttackRange => enemyData != null ? enemyData.attackRange : 0f;
    public float DetectionRange => enemyData != null ? enemyData.detectionRange : 0f;
    public bool IsDead => isDead;

    /// <summary>
    /// Событие смерти врага.
    /// Нужен как канал декуплинга для адаптеров и систем наград.
    /// </summary>
    public event Action OnDied;

    private void Awake()
    {
        // Важно для учеников:
        // Awake вызывается при создании объекта (в том числе сразу после Instantiate).
        // Здесь мы можем подготовить runtime-состояние (например, здоровье),
        // если EnemyData уже назначен в инспекторе на префабе.
        if (enemyData != null)
            currentHealth = enemyData.maxHealth;
    }

    private void Start()
    {
        // Start вызывается после Awake (обычно на первом кадре).
        // Здесь удобно делать “поиск внешнего мира”: найти игрока и выставить цель.
        if (target == null && autoResolveTargetOnStart)
            ResolveTargetOnce();
    }

    private void Update()
    {
        // Update — “мозг” врага. Здесь мы НЕ должны делать тяжёлые операции,
        // поэтому логика максимально простая: проверили дистанцию → решили действие.
        if (enemyData == null || isDead || target == null)
            return;

        IDamageable targetDamageable = target.GetComponent<IDamageable>();
        if (targetDamageable == null)
            targetDamageable = target.GetComponentInParent<IDamageable>();

        // Минимальный guard для завершённого боевого цикла:
        // если цель уже мертва, враг прекращает преследование и атаку.
        if (targetDamageable != null && targetDamageable.IsDead)
            return;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        if (distanceToTarget > DetectionRange)
            return;

        if (distanceToTarget <= AttackRange)
            currentState = EnemyState.Attack;
        else
            currentState = EnemyState.Chase;

        switch (currentState)
        {
            case EnemyState.Attack:
                TryAttack();
                break;

            case EnemyState.Chase:
                MoveTowardsTarget();
                break;
        }
    }

    /// <summary>
    /// Инициализирует врага данными и сбрасывает runtime-состояние.
    /// Обычно вызывается спавнером сразу после создания врага.
    /// </summary>
    public void Setup(EnemyData data)
    {
        enemyData = data;
        currentHealth = enemyData != null ? enemyData.maxHealth : 0f;
        isDead = false;
        nextAttackTime = 0f;
        currentState = EnemyState.Chase;
    }

    /// <summary>
    /// Получение урона врагом.
    /// virtual — чтобы в будущем можно было переопределить правила урона у наследников
    /// (например, броня/щит/иммунитеты), не меняя код спавнера и оружия.
    /// </summary>
    public virtual void TakeDamage(float damage)
    {
        if (enemyData == null || damage <= 0f || isDead)
            return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, MaxHealth);
        Debug.Log($"{name}: получил {damage} урона. Здоровье: {currentHealth}/{MaxHealth}");

        if (currentHealth <= 0f)
            Die();
    }

    /// <summary>
    /// Смерть врага (базовая реализация).
    /// Здесь мы:
    ///  - защищаемся от двойной смерти (isDead);
    ///  - отправляем событие OnDied как “сигнал” для других систем;
    ///  - удаляем объект со сцены.
    /// </summary>
    public virtual void Die()
    {
        if (isDead)
            return;

        isDead = true;
        currentState = EnemyState.Dead;
        Debug.Log($"{name}: умер.");
        OnDied?.Invoke();

        // Для объектов из пула уничтожение не выполняем:
        // смерть обрабатывается подписчиками события OnDied (Release в пул).
        // Проверяем "пуловость" по имени компонента, чтобы не иметь жёсткой зависимости
        // от класса PooledEnemy (его может не быть в simple-ветке проекта).
        bool isPooledEnemy = GetComponent("PooledEnemy") != null;
        if (!destroyOnDeath || isPooledEnemy)
            return;

        if (destroyDelayAfterDeath > 0f)
            Destroy(gameObject, destroyDelayAfterDeath);
        else
            Destroy(gameObject);
    }

    /// <summary>
    /// Внешняя установка цели (предпочтительный путь для спавнера/encounter-системы).
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void ResolveTargetOnce()
    {
        // Учебное упрощение:
        // ищем первый PlayerController на сцене и используем его transform как цель.
        // Это проще, чем слои/рейкасты/“зрение” с препятствиями.
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
            target = player.transform;
    }

    /// <summary>
    /// Простое движение к цели “вручную” (без NavMesh).
    /// В более продвинутой версии это заменится на NavMeshAgent.
    /// </summary>
    public void MoveTowardsTarget()
    {
        if (target == null)
            return;

        // Точка расширения для advanced AI:
        // на следующих этапах здесь планируется переход на NavMeshAgent
        // и state-driven перемещение вместо прямой правки transform.position.
        Vector3 direction = (target.position - transform.position).normalized;
        direction.y = 0f;

        transform.position += direction * MoveSpeed * Time.deltaTime;

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }

    /// <summary>
    /// Атака врага (базовая реализация).
    /// В этом файле атака пока “символическая” (лог в консоль) — так проще отделить поведение
    /// (подошёл и атакует) от боевой модели (кто кому наносит урон), которая разбирается отдельно.
    /// </summary>
    public virtual void Attack()
    {
        if (target == null)
            return;

        // Важно для урока 7.2 (урон через IDamageable):
        // враг не делает “поиск целей по слоям” и не бьёт всех вокруг.
        // Он наносит урон строго своей цели target (обычно игрок), которую нужно корректно назначить
        // через авто-резолв на старте или методом SetTarget() при спавне/инициализации.
        IDamageable damageable = target.GetComponent<IDamageable>();
        if (damageable == null)
            damageable = target.GetComponentInParent<IDamageable>();

        if (damageable != null && !damageable.IsDead)
            damageable.TakeDamage(Damage);

        Debug.Log($"{name}: атакует {target.name} с уроном {Damage}");
    }

    private void TryAttack()
    {
        // Кулдаун — защита от атаки “каждый кадр”.
        // Без него враг атаковал бы 60+ раз в секунду, что ломает геймплей.
        if (Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + attackCooldown;
        Attack();
    }

    private void OnDrawGizmosSelected()
    {
        if (enemyData == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, DetectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, AttackRange);
    }
}
