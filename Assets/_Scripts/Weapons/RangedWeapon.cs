using UnityEngine;

/// <summary>
/// Оружие дальнего боя.
/// Создаёт снаряд, который летит вперёд.
/// </summary>
public class RangedWeapon : WeaponBase
{
    [Header("Параметры дальнего боя")]
    [Tooltip("Точка, из которой вылетают снаряды (конец ствола/лука).")]
    public Transform shootOrigin;

    [Tooltip("Скорость снаряда. Если 0, используется значение по умолчанию в префабе.")]
    public float projectileSpeedOverride = 0f;

    [Tooltip("Слои, по которым может быть нанесён урон.")]
    public LayerMask projectileHitLayers;

    public override void Attack()
    {
        if (!CanAttack())
            return;

        StartAttackCooldown();

        if (weaponData == null)
        {
            Debug.LogWarning($"{name}: WeaponData не назначен, дальняя атака невозможна.", this);
            return;
        }

        if (weaponData.projectilePrefab == null)
        {
            Debug.LogWarning($"{name}: projectilePrefab в WeaponData не назначен, нечего стрелять.", this);
            return;
        }

        // Определяем точку выстрела
        Vector3 spawnPosition = shootOrigin != null
            ? shootOrigin.position
            : (owner != null ? owner.position : transform.position);

        Quaternion spawnRotation = shootOrigin != null
            ? shootOrigin.rotation
            : (owner != null ? owner.rotation : transform.rotation);

        // Создаём снаряд
        GameObject projectileObject = Instantiate(
            weaponData.projectilePrefab,
            spawnPosition,
            spawnRotation
        );

        Projectile projectile = projectileObject.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.damage = Damage;
            projectile.maxDistance = Range;
            projectile.hitLayers = projectileHitLayers;

            if (projectileSpeedOverride > 0f)
            {
                projectile.speed = projectileSpeedOverride;
            }
        }

        Debug.Log($"{name}: дальняя атака, выпущен снаряд с уроном {Damage} и дальностью {Range}.");
    }
}