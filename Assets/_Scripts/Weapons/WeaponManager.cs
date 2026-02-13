using UnityEngine;

/// <summary>
/// Управляет оружием игрока:
/// - хранит текущее оружие (WeaponBase),
/// - реагирует на ввод атаки через InputManager,
/// - в будущем сможет менять оружие.
/// </summary>
public class WeaponManager : MonoBehaviour
{
    [Header("Связи")]
    [Tooltip("Статы игрока (могут понадобиться для модификаторов урона, критов и т.п.).")]
    public PlayerStats playerStats;

    [Tooltip("Текущее активное оружие игрока.")]
    public WeaponBase currentWeapon;

    [Header("Стартовое оружие")]
    [Tooltip("Префаб оружия ближнего боя по умолчанию (например, меч).")]
    public WeaponBase defaultMeleeWeaponPrefab;

    [Tooltip("Позиция, в которой будет располагаться оружие (например, рука игрока).")]
    public Transform weaponSocket;

    private void Awake()
    {
        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();

        // Если у нас нет текущего оружия, но указан стартовый префаб — создаём его
        if (currentWeapon == null && defaultMeleeWeaponPrefab != null)
        {
            EquipNewWeapon(defaultMeleeWeaponPrefab);
        }
        else if (currentWeapon != null)
        {
            // Убедимся, что владелец и позиция корректно назначены
            SetupWeapon(currentWeapon);
        }
    }

    private void OnEnable()
    {
        // Подписка на событие атаки из InputManager
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnAttackPressed += HandleAttackPressed;
        }
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnAttackPressed -= HandleAttackPressed;
        }
    }

    /// <summary>
    /// Обработчик нажатия кнопки атаки.
    /// </summary>
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
    /// Экипировать оружие из префаба (создаёт его экземпляр как дочерний объект в weaponSocket).
    /// </summary>
    public void EquipNewWeapon(WeaponBase weaponPrefab)
    {
        if (weaponPrefab == null)
        {
            Debug.LogWarning("WeaponManager.EquipNewWeapon: префаб оружия не задан.");
            return;
        }

        // Если есть старое оружие как дочерний объект — удаляем
        if (currentWeapon != null)
        {
            Destroy(currentWeapon.gameObject);
            currentWeapon = null;
        }

        Transform parent = weaponSocket != null ? weaponSocket : transform;

        WeaponBase newWeapon = Instantiate(weaponPrefab, parent);
        currentWeapon = newWeapon;

        SetupWeapon(currentWeapon);
    }

    /// <summary>
    /// Настроить только что экипированное оружие:
    /// - указать владельца,
    /// - обнулить локальную позицию/вращение.
    /// </summary>
    private void SetupWeapon(WeaponBase weapon)
    {
        if (weapon == null)
            return;

        weapon.owner = transform;

        // Привязываем оружие к сокету: локальная позиция/вращение = 0
        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localRotation = Quaternion.identity;
    }
}
