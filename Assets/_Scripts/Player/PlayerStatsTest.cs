using UnityEngine;

/// <summary>
/// Временный скрипт для проверки работы PlayerStats.
/// </summary>
public class PlayerStatsTest : MonoBehaviour
{
    public PlayerStats playerStats;

    private void Awake()
    {
        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();
    }

    private void OnEnable()
    {
        if (playerStats == null) return;

        playerStats.OnHealthChanged += HandleHealthChanged;
        playerStats.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        if (playerStats == null) return;

        playerStats.OnHealthChanged -= HandleHealthChanged;
        playerStats.OnDeath -= HandleDeath;
    }

    private void Update()
    {
        if (playerStats == null) return;

        // Нанести урон по нажатию клавиши H
        if (Input.GetKeyDown(KeyCode.H))
        {
            playerStats.TakeDamage(10f);
        }

        // Вылечить по нажатию клавиши J
        if (Input.GetKeyDown(KeyCode.J))
        {
            playerStats.Heal(10f);
        }
    }

    private void HandleHealthChanged(float current, float max)
    {
        Debug.Log($"Здоровье изменилось: {current} / {max}");
    }

    private void HandleDeath()
    {
        Debug.Log("Игрок умер!");
    }
}