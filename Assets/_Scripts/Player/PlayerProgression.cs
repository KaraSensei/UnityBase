using System;
using UnityEngine;

/// <summary>
/// Отвечает за прогрессию игрока:
/// уровень, опыт и повышение уровня.
/// </summary>
public class PlayerProgression : MonoBehaviour
{
    [Header("Связи")]
    [Tooltip("Ссылка на PlayerStats для возможного усиления характеристик при уровне.")]
    public PlayerStats playerStats;

    [Header("Уровень")]
    [SerializeField]
    [Tooltip("Текущий уровень игрока.")]
    private int currentLevel = 1;

    [Header("Опыт")]
    [SerializeField]
    [Tooltip("Текущее количество опыта.")]
    private float currentExperience = 0f;

    /// <summary>
    /// Текущий уровень игрока (только для чтения).
    /// Для изменения уровня используйте метод AddExperience().
    /// </summary>
    public int CurrentLevel => currentLevel;

    /// <summary>
    /// Текущее количество опыта игрока (только для чтения).
    /// Для добавления опыта используйте метод AddExperience().
    /// </summary>
    public float CurrentExperience => currentExperience;

    [Tooltip("Базовое количество опыта для перехода с 1 на 2 уровень.")]
    public float baseExperienceToNextLevel = 100f;

    [Tooltip("Множитель роста требуемого опыта на каждый следующий уровень.")]
    public float experienceGrowthFactor = 1.5f;

    // Событие, вызываемое при повышении уровня
    public event Action<int> OnLevelUp;

    // Событие для обновления UI опыта: (текущий опыт, опыт до следующего уровня)
    public event Action<float, float> OnExperienceChanged;

    private void Awake()
    {
        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();

        // Инициализируем подписчиков начальными значениями
        float required = GetRequiredExperienceForNextLevel();
        OnExperienceChanged?.Invoke(currentExperience, required);
    }

    /// <summary>
    /// Сколько опыта нужно для перехода на следующий уровень.
    /// </summary>
    private float GetRequiredExperienceForNextLevel()
    {
        // Например: baseExp * factor^(level-1)
        float required = baseExperienceToNextLevel;

        // Для 1 уровня (currentLevel = 1) степень будет 0 → множитель = 1
        int power = Mathf.Max(0, currentLevel - 1);
        required *= Mathf.Pow(experienceGrowthFactor, power);

        return required;
    }

    /// <summary>
    /// Добавление опыта. Можно вызывать из других систем
    /// (убийство врага, выполнение квеста и т.д.).
    /// </summary>
    public void AddExperience(float amount)
    {
        if (amount <= 0f)
            return;

        currentExperience += amount;

        // Проверяем, хватает ли опыта для повышения уровня (возможно, несколько раз подряд)
        bool leveledUpAtLeastOnce = false;

        while (true)
        {
            float required = GetRequiredExperienceForNextLevel();

            if (currentExperience < required)
                break;

            currentExperience -= required;
            LevelUpInternal();
            leveledUpAtLeastOnce = true;
        }

        float nextRequired = GetRequiredExperienceForNextLevel();
        OnExperienceChanged?.Invoke(currentExperience, nextRequired);

        if (leveledUpAtLeastOnce)
        {
            Debug.Log($"Новый уровень: {currentLevel}, опыт: {currentExperience}/{nextRequired}");
        }
    }

    /// <summary>
    /// Внутренняя логика повышения уровня.
    /// </summary>
    private void LevelUpInternal()
    {
        currentLevel++;

        // Уведомляем подписчиков
        OnLevelUp?.Invoke(currentLevel);

        // Пример: усиливаем характеристики игрока при каждом уровне
        if (playerStats != null)
        {
            // Все изменения здоровья/маны и вызовы событий
            // делаем через PlayerStats, чтобы события вызывались
            // только изнутри класса-источника.
            playerStats.ApplyLevelUpBonuses(10f, 5f);
        }
    }
}