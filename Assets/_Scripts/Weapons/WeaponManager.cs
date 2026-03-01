using UnityEngine;

/// <summary>
/// Управляет оружием игрока:
/// - хранит текущее оружие (WeaponBase),
/// - реагирует на ввод атаки через InputManager,
/// - при старте экипирует явно заданное оружие по умолчанию.
/// </summary>
public class WeaponManager : MonoBehaviour
{
    [Header("Связи")]
    [SerializeField]
    [Tooltip("Статы игрока (могут понадобиться для модификаторов урона, критов и т.п.).")]
    private PlayerStats playerStats;

    [SerializeField]
    [Tooltip("Префаб оружия по умолчанию — при старте игрок всегда экипируется им. Обязательно укажите в инспекторе.")]
    private WeaponBase defaultWeaponPrefab;

    [SerializeField]
    [Tooltip("Позиция, в которой будет располагаться оружие (например, рука игрока).")]
    private Transform weaponSocket;

    private WeaponBase currentWeapon;

    /// <summary> Текущее активное оружие игрока (только чтение). </summary>
    public WeaponBase CurrentWeapon => currentWeapon;

    /// <summary> Статы игрока (для модификаторов урона и т.п.). </summary>
    public PlayerStats PlayerStats => playerStats;

    private void Awake()
    {
        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();

        if (defaultWeaponPrefab == null)
        {
            Debug.LogError("WeaponManager: не указано оружие по умолчанию (Default Weapon Prefab). Назначьте префаб в инспекторе.", this);
            return;
        }

        EquipNewWeapon(defaultWeaponPrefab);
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
