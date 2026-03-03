using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Управляет оружием игрока (вариант B: все оружия — дочерние объекты, смена через Enable/Disable):
/// - хранит экземпляры оружия из префаба игрока;
/// - листает только «доступные» оружия (список availableWeapons);
/// - переключение по кнопкам 1 (назад) и 2 (вперёд) через InputManager.
/// </summary>
public class WeaponManager : MonoBehaviour
{
    [Header("Связи")]
    [SerializeField]
    [Tooltip("Статы игрока (могут понадобиться для модификаторов урона, критов и т.п.).")]
    private PlayerStats playerStats;

    [Header("Оружия на игроке")]
    [SerializeField]
    [Tooltip("Все экземпляры оружия — дочерние объекты игрока. Порядок = порядок слотов. Назначаются в инспекторе.")]
    private WeaponBase[] weaponInstances;

    [SerializeField]
    [Tooltip("Какие слоты доступны для переключения при старте (по индексу в Weapon Instances). Если короче массива — остальные считаются доступными.")]
    private bool[] weaponAvailableAtStart;

    [SerializeField]
    [Tooltip("Индекс в списке доступных оружий, которое экипируется при старте (0 = первое доступное).")]
    private int defaultWeaponIndexInAvailable = 0;

    private List<WeaponBase> availableWeapons = new List<WeaponBase>();
    private int currentAvailableIndex;
    private WeaponBase currentWeapon;

    /// <summary> Текущее активное оружие игрока (только чтение). </summary>
    public WeaponBase CurrentWeapon => currentWeapon;

    /// <summary> Статы игрока (для модификаторов урона и т.п.). </summary>
    public PlayerStats PlayerStats => playerStats;

    private void Awake()
    {
        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();

        BuildAvailableWeaponsList();
        if (availableWeapons.Count == 0)
        {
            Debug.LogError("WeaponManager: нет доступных оружий. Заполните Weapon Instances и при необходимости Weapon Available At Start.", this);
            return;
        }

        // Защищаемся от выхода за границы: если в инспекторе задали индекс стартового оружия
        // меньше 0 или больше/равно числу доступных оружий, Mathf.Clamp "прижмёт" его
        // в диапазон [0, availableWeapons.Count - 1].
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
    /// Строит список доступных оружий из weaponInstances по флагам weaponAvailableAtStart.
    /// </summary>
    private void BuildAvailableWeaponsList()
    {
        availableWeapons.Clear();
        if (weaponInstances == null) return;

        for (int i = 0; i < weaponInstances.Length; i++)
        {
            if (weaponInstances[i] == null) continue;

            // Логика флага доступности:
            // - если weaponAvailableAtStart == null, то считаем, что ВСЕ слоты доступны;
            // - если индекс i вышел за длину weaponAvailableAtStart, тоже считаем слот доступным;
            // - иначе берём конкретное значение weaponAvailableAtStart[i].
            bool available = weaponAvailableAtStart == null || i >= weaponAvailableAtStart.Length || weaponAvailableAtStart[i];
            if (available)
                availableWeapons.Add(weaponInstances[i]);
        }
    }

    /// <summary>
    /// Включить только выбранное оружие, остальные выключить.
    /// </summary>
    private void EquipByEnableDisable(WeaponBase weapon)
    {
        if (weapon == null) return;

        if (weaponInstances != null)
        {
            // Проходим по всем оружиям-слотам и включаем только то, которое выбрали,
            // а остальные принудительно выключаем.
            foreach (WeaponBase w in weaponInstances)
            {
                if (w != null)
                    w.gameObject.SetActive(w == weapon);
            }
        }

        currentWeapon = weapon;
        SetupWeapon(currentWeapon);
    }

    /// <summary>
    /// Переключиться на следующее оружие в списке доступных (кнопка 2).
    /// </summary>
    private void SwitchToNextWeapon()
    {
        if (availableWeapons.Count == 0) return;
        currentAvailableIndex = (currentAvailableIndex + 1) % availableWeapons.Count;
        EquipByEnableDisable(availableWeapons[currentAvailableIndex]);
    }

    /// <summary>
    /// Переключиться на предыдущее оружие в списке доступных (кнопка 1).
    /// </summary>
    private void SwitchToPrevWeapon()
    {
        if (availableWeapons.Count == 0) return;
        // (currentAvailableIndex - 1 + availableWeapons.Count) % availableWeapons.Count
        // даёт нам "круговой" индекс:
        // - вычитаем 1 (листание назад),
        // - добавляем Count, чтобы не уйти в отрицательные числа,
        // - берём % Count, чтобы зациклиться от 0 к последнему и обратно.
        currentAvailableIndex = (currentAvailableIndex - 1 + availableWeapons.Count) % availableWeapons.Count;
        EquipByEnableDisable(availableWeapons[currentAvailableIndex]);
    }

    private void HandleAttackPressed()
    {
        if (currentWeapon == null)
        {
            Debug.LogWarning("WeaponManager: у игрока нет текущего оружия, атаковать нечем.");
            return;
        }
        currentWeapon.Attack();
    }

    /// <summary>
    /// Разблокировать оружие по индексу в weaponInstances (добавить в список доступных для переключения).
    /// </summary>
    public void UnlockWeaponBySlotIndex(int slotIndex)
    {
        // Проверяем границы индекса слота и сам массив weaponInstances, чтобы не словить исключения.
        if (weaponInstances == null || slotIndex < 0 || slotIndex >= weaponInstances.Length) return;
        WeaponBase w = weaponInstances[slotIndex];
        if (w != null && !availableWeapons.Contains(w))
            availableWeapons.Add(w);
    }

    // На будущее: сейчас оружие просто привязывается к transform игрока.
    // Позже можно:
    // 1) добавить в инспекторе поле handBone (Transform) и вешать оружие на конкретную кость/сокет руки;
    // 2) или получать кость из Animator через GetBoneTransform(HumanBodyBones.RightHand/LeftHand)
    //    и делать parent = этой кости, чтобы оружие точно следовало за анимацией руки.
    private void SetupWeapon(WeaponBase weapon)
    {
        if (weapon == null) return;
        // Сохраняем ссылку на владельца (обычно это игрок), чтобы оружие знало,
        // от кого «летят» пули/урон.
        weapon.Owner = transform;
        // Привязываем оружие к локальному нулю и нулевой ротации относительно игрока,
        // чтобы оно занимало правильное положение в руке/точке крепления.
        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localRotation = Quaternion.identity;
    }
}
