using System;
using UnityEngine;

/// <summary>
/// Отвечает за текущие характеристики игрока:
/// здоровье, мана и события, связанные с ними.
/// </summary>
public class PlayerStats : MonoBehaviour
{
    [Header("Данные игрока")]
    [Tooltip("ScriptableObject с базовыми параметрами игрока.")]
    public PlayerData playerData;

    [Header("Текущее состояние")]
    [Tooltip("Текущее здоровье игрока.")]
    public float currentHealth;

    [Tooltip("Текущая мана (или энергия).")]
    public float currentMana;

    // События для связи с другими системами (UI, эффекты и т.п.)
    public event Action<float, float> OnHealthChanged; // (текущее, максимальное)
    public event Action<float, float> OnManaChanged;   // (текущая, максимальная)
    public event Action OnDeath;

    private void Awake()
    {
        InitializeFromData();
    }

    /// <summary>
    /// Инициализация текущих значений из PlayerData.
    /// </summary>
    public void InitializeFromData()
    {
        if (playerData == null)
        {
            Debug.LogError("PlayerStats: PlayerData не назначен!", this);
            return;
        }

        currentHealth = Mathf.Clamp(playerData.maxHealth, 1f, float.MaxValue);
        currentMana = Mathf.Clamp(playerData.maxMana, 0f, float.MaxValue);

        // Сигнализируем подписчикам начальные значения
        OnHealthChanged?.Invoke(currentHealth, playerData.maxHealth);
        OnManaChanged?.Invoke(currentMana, playerData.maxMana);
    }

    /// <summary>
    /// Нанесение урона игроку.
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (playerData == null)
        {
            Debug.LogWarning("PlayerStats.TakeDamage: PlayerData не назначен.", this);
            return;
        }

        if (amount <= 0f || currentHealth <= 0f)
            return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, playerData.maxHealth);

        OnHealthChanged?.Invoke(currentHealth, playerData.maxHealth);

        if (currentHealth <= 0f)
        {
            // Игрок "умирает"
            OnDeath?.Invoke();
        }
    }

    /// <summary>
    /// Лечение игрока.
    /// </summary>
    public void Heal(float amount)
    {
        if (playerData == null)
        {
            Debug.LogWarning("PlayerStats.Heal: PlayerData не назначен.", this);
            return;
        }

        if (amount <= 0f || currentHealth <= 0f)
            return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, playerData.maxHealth);

        OnHealthChanged?.Invoke(currentHealth, playerData.maxHealth);
    }

    /// <summary>
    /// Изменение маны (может быть как расход, так и восстановление).
    /// </summary>
    public void AddMana(float amount)
    {
        if (playerData == null)
        {
            Debug.LogWarning("PlayerStats.AddMana: PlayerData не назначен.", this);
            return;
        }

        if (Mathf.Approximately(amount, 0f))
            return;

        currentMana += amount;
        currentMana = Mathf.Clamp(currentMana, 0f, playerData.maxMana);

        OnManaChanged?.Invoke(currentMana, playerData.maxMana);
    }
}