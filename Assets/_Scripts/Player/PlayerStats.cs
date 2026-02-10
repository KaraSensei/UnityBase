using System;
using UnityEngine;

/// <summary>
/// Отвечает за текущие характеристики игрока:
/// здоровье, мана/энергия и связанные с ними события.
/// Хранит ТЕКУЩИЕ значения в рантайме и даёт методы для урона и лечения.
/// </summary>
public class PlayerStats : MonoBehaviour
{
    [Header("Данные игрока")]
    [Tooltip("ScriptableObject с базовыми параметрами игрока (PlayerData).")]
    public PlayerData playerData;

    [Header("Текущее состояние")]
    [Tooltip("Текущее здоровье игрока.")]
    public float currentHealth;

    [Tooltip("Текущая мана (или энергия) игрока.")]
    public float currentMana;

    // События для связи с другими системами (UI, эффекты и т.п.)
    /// <summary>
    /// Вызывается при изменении здоровья.
    /// Параметры: текущее здоровье, максимальное здоровье.
    /// </summary>
    public event Action<float, float> OnHealthChanged;

    /// <summary>
    /// Вызывается при изменении маны / энергии.
    /// Параметры: текущая мана, максимальная мана.
    /// </summary>
    public event Action<float, float> OnManaChanged;

    /// <summary>
    /// Вызывается один раз в момент "смерти" игрока (здоровье упало до 0).
    /// </summary>
    public event Action OnDeath;

    /// <summary>
    /// Точка входа компонента.
    /// При старте берёт стартовые значения из PlayerData.
    /// </summary>
    private void Awake()
    {
        InitializeFromData();
    }

    /// <summary>
    /// Инициализирует текущие значения из PlayerData.
    /// Можно вызвать повторно, например, при респауне.
    /// </summary>
    public void InitializeFromData()
    {
        if (playerData == null)
        {
            Debug.LogError("PlayerStats: PlayerData не назначен!", this);
            return;
        }

        // Берём стартовые значения и ограничиваем их в разумных пределах.
        currentHealth = Mathf.Clamp(playerData.maxHealth, 1f, float.MaxValue);
        currentMana = Mathf.Clamp(playerData.maxMana, 0f, float.MaxValue);

        // Уведомляем подписчиков о начальных значениях.
        OnHealthChanged?.Invoke(currentHealth, playerData.maxHealth);
        OnManaChanged?.Invoke(currentMana, playerData.maxMana);
    }

    /// <summary>
    /// Применяет бонусы за повышение уровня:
    /// меняет maxHealth / maxMana и обновляет текущие значения
    /// с подниманием событий OnHealthChanged / OnManaChanged.
    /// Вызывать этот метод предпочтительнее, чем напрямую
    /// менять currentHealth / currentMana и ScriptableObject снаружи.
    /// </summary>
    public void ApplyLevelUpBonuses(float healthBonus, float manaBonus)
    {
        if (playerData == null)
        {
            Debug.LogWarning("PlayerStats.ApplyLevelUpBonuses: PlayerData не назначен.", this);
            return;
        }

        // Увеличиваем максимальные значения
        playerData.maxHealth += healthBonus;
        playerData.maxMana += manaBonus;

        // Синхронизируем текущее с новыми максимумами
        currentHealth = playerData.maxHealth;
        currentMana = Mathf.Clamp(currentMana, 0f, playerData.maxMana);

        // События вызываем здесь, внутри PlayerStats
        OnHealthChanged?.Invoke(currentHealth, playerData.maxHealth);
        OnManaChanged?.Invoke(currentMana, playerData.maxMana);
    }

    /// <summary>
    /// Наносит урон игроку.
    /// Не даёт опустить здоровье ниже 0 и при необходимости вызывает OnDeath.
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (playerData == null)
        {
            Debug.LogWarning("PlayerStats.TakeDamage: PlayerData не назначен.", this);
            return;
        }

        // Не реагируем на некорректный урон или если игрок уже мёртв.
        if (amount <= 0f || currentHealth <= 0f)
            return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, playerData.maxHealth);

        OnHealthChanged?.Invoke(currentHealth, playerData.maxHealth);

        if (currentHealth <= 0f)
        {
            // Игрок "умирает" — здесь можно запустить анимацию смерти, перезапуск уровня и т.п.
            OnDeath?.Invoke();
        }
    }

    /// <summary>
    /// Лечит игрока на указанное значение.
    /// Не поднимает здоровье выше максимального и не лечит мёртвого игрока.
    /// </summary>
    public void Heal(float amount)
    {
        if (playerData == null)
        {
            Debug.LogWarning("PlayerStats.Heal: PlayerData не назначен.", this);
            return;
        }

        // Нет смысла лечить на неположительное значение или лечить мёртвого.
        if (amount <= 0f || currentHealth <= 0f)
            return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, playerData.maxHealth);

        OnHealthChanged?.Invoke(currentHealth, playerData.maxHealth);
    }

    /// <summary>
    /// Изменяет ману (может быть как расход, так и восстановление).
    /// Положительное значение — восстановление, отрицательное — расход.
    /// </summary>
    public void AddMana(float amount)
    {
        if (playerData == null)
        {
            Debug.LogWarning("PlayerStats.AddMana: PlayerData не назначен.", this);
            return;
        }

        // Если изменение практически равно 0, ничего не делаем.
        if (Mathf.Approximately(amount, 0f))
            return;

        currentMana += amount;
        currentMana = Mathf.Clamp(currentMana, 0f, playerData.maxMana);

        OnManaChanged?.Invoke(currentMana, playerData.maxMana);
    }
}