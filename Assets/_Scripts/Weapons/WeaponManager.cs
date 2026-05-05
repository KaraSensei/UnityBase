using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Назначение: управляет доступными оружиями игрока и переключением между ними.
/// Что делает: хранит экземпляры оружия на префабе игрока, включает нужный слот и передаёт выполнение атаки текущему оружию.
/// Связи: работает вместе с PlayerStats и PlayerCombatController. Сам больше не решает, в какой кадр должна произойти атака.
/// Паттерны: Single Responsibility, композиция оружий через дочерние объекты.
/// </summary>
public class WeaponManager : MonoBehaviour
{
    public event Action<WeaponBase, int, int> OnWeaponChanged;

    [Header("Связи")]
    [Tooltip("Статы игрока. Нужны для проверки смерти и будущих модификаторов урона.")]
    [SerializeField] private PlayerStats playerStats;

    [Tooltip("Боевой контроллер игрока. Он запускает анимацию и ждёт Animation Event.")]
    [SerializeField] private PlayerCombatController playerCombatController;

    [Header("Оружия на игроке")]
    [Tooltip("Все экземпляры оружия — дочерние объекты игрока. Порядок в массиве = порядок слотов.")]
    [SerializeField] private WeaponBase[] weaponInstances;

    [Tooltip("Какие слоты доступны при старте. Если массив короче weaponInstances, остальные считаются доступными.")]
    [SerializeField] private bool[] weaponAvailableAtStart;

    [Tooltip("Индекс стартового оружия внутри списка доступных оружий.")]
    [SerializeField] private int defaultWeaponIndexInAvailable = 0;

    private readonly List<WeaponBase> availableWeapons = new List<WeaponBase>();
    private int currentAvailableIndex;
    private WeaponBase currentWeapon;

    /// <summary>
    /// Текущее активное оружие игрока.
    /// </summary>
    public WeaponBase CurrentWeapon => currentWeapon;

    public int CurrentWeaponIndex => currentAvailableIndex;

    public int AvailableWeaponCount => availableWeapons.Count;

    /// <summary>
    /// Статы игрока.
    /// </summary>
    public PlayerStats PlayerStats => playerStats;

    private void Awake()
    {
        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();

        if (playerCombatController == null)
            playerCombatController = GetComponent<PlayerCombatController>();

        BuildAvailableWeaponsList();
        if (availableWeapons.Count == 0)
        {
            Debug.LogError("WeaponManager: нет доступных оружий. Заполните Weapon Instances и при необходимости Weapon Available At Start.", this);
            return;
        }

        int startIndex = Mathf.Clamp(defaultWeaponIndexInAvailable, 0, availableWeapons.Count - 1);
        currentAvailableIndex = startIndex;
        EquipByEnableDisable(availableWeapons[startIndex]);
    }

    private void OnEnable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnAttackPressed += HandleAttackPressed;
            InputManager.Instance.OnWeaponNextPressed += SwitchToNextWeapon;
            InputManager.Instance.OnWeaponPrevPressed += SwitchToPrevWeapon;
        }
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnAttackPressed -= HandleAttackPressed;
            InputManager.Instance.OnWeaponNextPressed -= SwitchToNextWeapon;
            InputManager.Instance.OnWeaponPrevPressed -= SwitchToPrevWeapon;
        }
    }

    /// <summary>
    /// Строит список доступных оружий по флагам weaponAvailableAtStart.
    /// </summary>
    private void BuildAvailableWeaponsList()
    {
        availableWeapons.Clear();
        if (weaponInstances == null)
            return;

        for (int i = 0; i < weaponInstances.Length; i++)
        {
            if (weaponInstances[i] == null)
                continue;

            bool available = weaponAvailableAtStart == null ||
                             i >= weaponAvailableAtStart.Length ||
                             weaponAvailableAtStart[i];

            if (available)
                availableWeapons.Add(weaponInstances[i]);
        }
    }

    /// <summary>
    /// Включает только выбранное оружие, остальные выключает.
    /// </summary>
    private void EquipByEnableDisable(WeaponBase weapon)
    {
        if (weapon == null)
            return;

        if (weaponInstances != null)
        {
            foreach (WeaponBase weaponInstance in weaponInstances)
            {
                if (weaponInstance != null)
                    weaponInstance.gameObject.SetActive(weaponInstance == weapon);
            }
        }

        currentWeapon = weapon;
        SetupWeapon(currentWeapon);
        RaiseWeaponChanged();
    }

    /// <summary>
    /// Переключает на следующее доступное оружие.
    /// </summary>
    private void SwitchToNextWeapon()
    {
        if (availableWeapons.Count == 0)
            return;

        currentAvailableIndex = (currentAvailableIndex + 1) % availableWeapons.Count;
        EquipByEnableDisable(availableWeapons[currentAvailableIndex]);
    }

    /// <summary>
    /// Переключает на предыдущее доступное оружие.
    /// </summary>
    private void SwitchToPrevWeapon()
    {
        if (availableWeapons.Count == 0)
            return;

        currentAvailableIndex = (currentAvailableIndex - 1 + availableWeapons.Count) % availableWeapons.Count;
        EquipByEnableDisable(availableWeapons[currentAvailableIndex]);
    }

    private void HandleAttackPressed()
    {
        if (playerStats != null && playerStats.IsDead)
            return;

        if (playerCombatController == null)
        {
            Debug.LogWarning("WeaponManager: не найден PlayerCombatController, атаку нельзя синхронизировать через Animator.", this);
            return;
        }

        if (currentWeapon == null)
        {
            Debug.LogWarning("WeaponManager: у игрока нет текущего оружия, атаковать нечем.", this);
            return;
        }

        playerCombatController.TryStartAttack();
    }

    /// <summary>
    /// Выполняет действие текущего оружия в точке синхронизации из Animation Event.
    /// Для melee это удар, для ranged — выстрел или спавн projectile.
    /// </summary>
    public void PerformCurrentWeaponAttack()
    {
        if (playerStats != null && playerStats.IsDead)
            return;

        if (currentWeapon == null)
        {
            Debug.LogWarning("WeaponManager: нет активного оружия для выполнения атаки через Animation Event.", this);
            return;
        }

        currentWeapon.Attack();
    }

    /// <summary>
    /// Разблокирует оружие по индексу слота в weaponInstances.
    /// </summary>
    public void UnlockWeaponBySlotIndex(int slotIndex)
    {
        if (weaponInstances == null || slotIndex < 0 || slotIndex >= weaponInstances.Length)
            return;

        WeaponBase weapon = weaponInstances[slotIndex];
        if (weapon != null && !availableWeapons.Contains(weapon))
        {
            availableWeapons.Add(weapon);
            RaiseWeaponChanged();
        }
    }

    /// <summary>
    /// Настраивает владельца и локальное положение оружия после экипировки.
    /// </summary>
    private void SetupWeapon(WeaponBase weapon)
    {
        if (weapon == null)
            return;

        // Пока оружие привязано к текущей иерархии игрока.
        // Позже сюда можно добавить более точную привязку к кости руки через Animator/GetBoneTransform.
        weapon.Owner = transform;
        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localRotation = Quaternion.identity;
    }

    private void RaiseWeaponChanged()
    {
        OnWeaponChanged?.Invoke(currentWeapon, currentAvailableIndex, availableWeapons.Count);
    }
}
