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