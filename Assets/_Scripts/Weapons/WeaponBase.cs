using UnityEngine;

/// <summary>
/// Базовый класс для любого оружия.
/// Хранит ссылку на WeaponData и управляет перезарядкой.
/// Наследники должны реализовать конкретный способ атаки.
/// </summary>
public abstract class WeaponBase : MonoBehaviour
{
    [Header("Данные оружия")]
    [Tooltip("ScriptableObject с базовыми параметрами оружия.")]
    public WeaponData weaponData;

    [Header("Владелец оружия (опционально)")]
    [Tooltip("Кто держит это оружие (например, Player). Используется для анимаций/направления атаки.")]
    public Transform owner;

    // Время, когда оружие снова готово атаковать (Time.time)
    protected float nextAttackTime = 0f;

    /// <summary>
    /// Текущий урон оружия, удобный геттер к WeaponData.
    /// </summary>
    public float Damage => weaponData != null ? weaponData.damage : 0f;

    /// <summary>
    /// Текущая дальность оружия.
    /// </summary>
    public float Range => weaponData != null ? weaponData.range : 0f;

    /// <summary>
    /// Скорость атаки (атак в секунду).
    /// </summary>
    public float AttackSpeed => weaponData != null ? weaponData.attackSpeed : 1f;

    /// <summary>
    /// Можно ли сейчас атаковать (учитывает перезарядку и наличие WeaponData).
    /// </summary>
    public virtual bool CanAttack()
    {
        if (weaponData == null)
        {
            Debug.LogWarning($"{name}: WeaponData не назначен, атака невозможна.", this);
            return false;
        }

        return Time.time >= nextAttackTime;
    }

    /// <summary>
    /// Устанавливает время следующей доступной атаки на основе AttackSpeed.
    /// </summary>
    protected void StartAttackCooldown()
    {
        // Если AttackSpeed = 2 → cooldown = 0.5 секунды
        float cooldown = AttackSpeed > 0f ? (1f / AttackSpeed) : 0.5f;
        nextAttackTime = Time.time + cooldown;
    }

    /// <summary>
    /// Абстрактный метод атаки.
    /// Каждый конкретный вид оружия реализует его по-своему.
    /// </summary>
    public abstract void Attack();
}