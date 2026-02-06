using UnityEngine;

[CreateAssetMenu(fileName = "New Player Data", menuName = "Game Data/Player Data", order = 0)]
public class PlayerData : ScriptableObject
{
    [Header("Основные характеристики")]
    [Min(1f)] public float maxHealth = 100f;
    [Min(0f)] public float maxMana = 0f;

    [Header("Движение")]
    [Min(0f)] public float moveSpeed = 5f;
    [Min(0f)] public float jumpForce = 5f;

    [Header("Дополнительные параметры движения")]
    [Min(0f)] public float acceleration = 10f; // Ускорение при начале движения
    [Min(0f)] public float rotationSpeed = 720f; // Градусы в секунду
}
