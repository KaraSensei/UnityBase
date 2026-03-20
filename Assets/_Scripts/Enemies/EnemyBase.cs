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
public class EnemyBase : MonoBehaviour
{
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

    private float nextAttackTime;
    private bool isDead;

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
        if (enemyData != null)
            currentHealth = enemyData.maxHealth;
    }

    private void Start()
    {
        if (target == null && autoResolveTargetOnStart)
            ResolveTargetOnce();
    }

    private void Update()
    {
        if (enemyData == null || isDead || target == null)
            return;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        if (distanceToTarget > DetectionRange)
            return;

        if (distanceToTarget <= AttackRange)
            TryAttack();
        else
            MoveTowardsTarget();
    }

    /// <summary>
    /// Инициализирует врага данными и сбрасывает runtime-состояние.
    /// </summary>
    public void Setup(EnemyData data)
    {
        enemyData = data;
        currentHealth = enemyData != null ? enemyData.maxHealth : 0f;
        isDead = false;
        nextAttackTime = 0f;
    }

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

    public virtual void Die()
    {
        if (isDead)
            return;

        isDead = true;
        Debug.Log($"{name}: умер.");
        OnDied?.Invoke();
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
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
            target = player.transform;
    }

    public void MoveTowardsTarget()
    {
        if (target == null)
            return;

        Vector3 direction = (target.position - transform.position).normalized;
        direction.y = 0f;

        transform.position += direction * MoveSpeed * Time.deltaTime;

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }

    public virtual void Attack()
    {
        if (target == null)
            return;

        Debug.Log($"{name}: атакует {target.name} с уроном {Damage}");
    }

    private void TryAttack()
    {
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
