using UnityEngine;

/// <summary>
/// Данные для врага (здоровье, скорость, урон и т.п.).
/// Используется EnemyFactory для создания врагов и EnemyBase для чтения параметров.
/// </summary>
[CreateAssetMenu(
    fileName = "EnemyData",
    menuName = "Game Data/Enemy Data",
    order = 0)]
public class EnemyData : ScriptableObject
{
    public enum EnemyType
    {
        Melee,   // Ближний бой (гоблины, орки)
        Ranged,  // Дальний бой (лучники, маги)
        Boss     // Боссы (особые враги)
    }

    [Header("Общее")]
    [Tooltip("Читаемое название врага (для UI и логирования).")]
    public string enemyName = "New Enemy";

    [Tooltip("Тип врага (ближний, дальний, босс).")]
    public EnemyType enemyType = EnemyType.Melee;

    [Header("Характеристики")]
    [Min(1f)]
    [Tooltip("Максимальное здоровье врага.")]
    public float maxHealth = 50f;

    [Min(0f)]
    [Tooltip("Скорость движения врага (единиц в секунду).")]
    public float moveSpeed = 3f;

    [Min(0f)]
    [Tooltip("Урон, который враг наносит за одну атаку.")]
    public float damage = 10f;

    [Header("Бой")]
    [Min(0f)]
    [Tooltip("Дальность атаки врага (радиус ближнего боя или дальность выстрела).")]
    public float attackRange = 2f;

    [Min(0f)]
    [Tooltip("Дальность обнаружения игрока (на каком расстоянии враг начинает преследовать).")]
    public float detectionRange = 10f;

    [Header("Награды")]
    [Min(0f)]
    [Tooltip("Опыт, который получает игрок за убийство этого врага.")]
    public float experienceReward = 10f;

    [Header("Префаб")]
    [Tooltip("Префаб врага, который будет использоваться для создания экземпляров.")]
    public GameObject prefab;
}