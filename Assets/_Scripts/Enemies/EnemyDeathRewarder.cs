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

    /// <summary>
    /// Регистрирует врага: подписывается на его событие смерти.
    /// </summary>
    public void RegisterEnemy(EnemyStats stats)
    {
        if (stats == null)
            return;

        // Подписываемся на событие смерти конкретного врага.
        stats.OnDied += HandleEnemyDied;
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
}