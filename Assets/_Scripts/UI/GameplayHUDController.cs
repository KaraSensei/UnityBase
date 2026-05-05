using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD presenter for gameplay:
/// HP (text + fill), Mana (fill), XP (fill), Level (text), Weapon icon (with switching).
/// The script assumes UI is prepared in a prefab and only updates bound references.
/// </summary>
public class GameplayHUDController : MonoBehaviour
{
    [Header("Data Sources")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private PlayerProgression playerProgression;
    [SerializeField] private WeaponManager weaponManager;

    [Header("HP")]
    [SerializeField] private Image hpFillImage;
    [SerializeField] private Text hpValueText;

    [Header("Mana")]
    [SerializeField] private Image manaFillImage;

    [Header("XP + Level")]
    [SerializeField] private Image xpFillImage;
    [SerializeField] private Text levelValueText;

    [Header("Weapon HUD")]
    [SerializeField] private Image weaponIconImage;

    private void Awake()
    {
        ResolveSourcesIfNeeded();
    }

    private void OnEnable()
    {
        Bind();
        RefreshAll();
    }

    private void OnDisable()
    {
        Unbind();
    }

    private void ResolveSourcesIfNeeded()
    {
        if (playerStats == null)
            playerStats = FindFirstObjectByType<PlayerStats>();

        if (playerProgression == null && playerStats != null)
            playerProgression = playerStats.GetComponent<PlayerProgression>();

        if (playerProgression == null)
            playerProgression = FindFirstObjectByType<PlayerProgression>();

        if (weaponManager == null && playerStats != null)
            weaponManager = playerStats.GetComponent<WeaponManager>();

        if (weaponManager == null)
            weaponManager = FindFirstObjectByType<WeaponManager>();
    }

    private void Bind()
    {
        ResolveSourcesIfNeeded();

        if (playerStats != null)
        {
            playerStats.OnHealthChanged += HandleHealthChanged;
            playerStats.OnManaChanged += HandleManaChanged;
        }

        if (playerProgression != null)
        {
            playerProgression.OnExperienceChanged += HandleExperienceChanged;
            playerProgression.OnLevelUp += HandleLevelUp;
        }

        if (weaponManager != null)
            weaponManager.OnWeaponChanged += HandleWeaponChanged;
    }

    private void Unbind()
    {
        if (playerStats != null)
        {
            playerStats.OnHealthChanged -= HandleHealthChanged;
            playerStats.OnManaChanged -= HandleManaChanged;
        }

        if (playerProgression != null)
        {
            playerProgression.OnExperienceChanged -= HandleExperienceChanged;
            playerProgression.OnLevelUp -= HandleLevelUp;
        }

        if (weaponManager != null)
            weaponManager.OnWeaponChanged -= HandleWeaponChanged;
    }

    private void RefreshAll()
    {
        if (playerStats != null && playerStats.playerData != null)
        {
            HandleHealthChanged(playerStats.CurrentHealth, playerStats.playerData.maxHealth);
            HandleManaChanged(playerStats.CurrentMana, playerStats.playerData.maxMana);
        }

        if (playerProgression != null)
        {
            HandleLevelUp(playerProgression.CurrentLevel);

            float requiredXp = CalculateRequiredExperienceForCurrentLevel(playerProgression);
            HandleExperienceChanged(playerProgression.CurrentExperience, requiredXp);
        }

        if (weaponManager != null)
            HandleWeaponChanged(
                weaponManager.CurrentWeapon,
                weaponManager.CurrentWeaponIndex,
                weaponManager.AvailableWeaponCount);
    }

    private static float CalculateRequiredExperienceForCurrentLevel(PlayerProgression progression)
    {
        if (progression == null)
            return 1f;

        int power = Mathf.Max(0, progression.CurrentLevel - 1);
        return progression.baseExperienceToNextLevel * Mathf.Pow(progression.experienceGrowthFactor, power);
    }

    private void HandleHealthChanged(float current, float max)
    {
        if (hpFillImage != null)
            hpFillImage.fillAmount = max > 0.01f ? Mathf.Clamp01(current / max) : 0f;

        if (hpValueText != null)
            hpValueText.text = Mathf.CeilToInt(current).ToString();
    }

    private void HandleManaChanged(float current, float max)
    {
        if (manaFillImage != null)
            manaFillImage.fillAmount = max > 0.01f ? Mathf.Clamp01(current / max) : 0f;
    }

    private void HandleExperienceChanged(float current, float required)
    {
        if (xpFillImage != null)
            xpFillImage.fillAmount = required > 0.01f ? Mathf.Clamp01(current / required) : 0f;
    }

    private void HandleLevelUp(int level)
    {
        if (levelValueText != null)
            levelValueText.text = level.ToString();
    }

    private void HandleWeaponChanged(WeaponBase weapon, int index, int total)
    {
        if (weaponIconImage == null)
            return;

        Sprite icon = weapon != null && weapon.WeaponData != null
            ? weapon.WeaponData.icon
            : null;
        weaponIconImage.enabled = icon != null;
        weaponIconImage.sprite = icon;
    }
}
