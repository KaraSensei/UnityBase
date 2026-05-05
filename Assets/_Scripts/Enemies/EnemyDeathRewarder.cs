using System.Collections.Generic;
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

    private readonly HashSet<EnemyStats> registeredEnemies = new HashSet<EnemyStats>();

    private void Awake()
    {
        ResolvePlayerProgressionIfNeeded();
    }

    private void Start()
    {
        RegisterExistingEnemiesInScene();
    }

    /// <summary>
    /// Регистрирует врага: подписывается на его событие смерти.
    /// </summary>
    public void RegisterEnemy(EnemyStats stats)
    {
        if (stats == null || registeredEnemies.Contains(stats))
            return;

        ResolvePlayerProgressionIfNeeded();

        // Подписываемся на событие смерти конкретного врага.
        stats.OnDied += HandleEnemyDied;
        registeredEnemies.Add(stats);
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
        registeredEnemies.Remove(stats);

        ResolvePlayerProgressionIfNeeded();
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

    private void OnDisable()
    {
        foreach (EnemyStats stats in registeredEnemies)
        {
            if (stats != null)
                stats.OnDied -= HandleEnemyDied;
        }

        registeredEnemies.Clear();
    }

    private void ResolvePlayerProgressionIfNeeded()
    {
        if (playerProgression != null)
            return;

        playerProgression = FindFirstObjectByType<PlayerProgression>();
    }

    private void RegisterExistingEnemiesInScene()
    {
        EnemyBase[] enemies = FindObjectsByType<EnemyBase>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (EnemyBase enemy in enemies)
        {
            if (enemy == null)
                continue;

            EnemyStats stats = enemy.GetComponent<EnemyStats>();
            if (stats == null)
                stats = enemy.gameObject.AddComponent<EnemyStats>();

            RegisterEnemy(stats);
        }
    }
}
