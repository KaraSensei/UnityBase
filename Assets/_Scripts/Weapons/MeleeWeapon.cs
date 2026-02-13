using UnityEngine;

/// <summary>
/// Оружие ближнего боя.
/// Реализует атаку через сферу вокруг точки удара.
/// </summary>
public class MeleeWeapon : WeaponBase
{
    [Header("Параметры ближней атаки")]
    [Tooltip("Точка, откуда считается удар (обычно у меча/руки).")]
    public Transform attackOrigin;

    [Tooltip("Радиус удара. Если 0, можно использовать Range из WeaponData.")]
    public float hitRadius = 1.5f;

    [Tooltip("Слои, по которым можно наносить урон (враги, разрушаемые объекты).")]
    public LayerMask hitLayers;

    public override void Attack()
    {
        if (!CanAttack())
            return;

        StartAttackCooldown();

        if (weaponData == null)
        {
            Debug.LogWarning($"{name}: WeaponData не назначен, ближняя атака невозможна.", this);
            return;
        }

        // Если не указан радиус, используем Range из WeaponData
        float radius = hitRadius > 0f ? hitRadius : Range;

        // Если attackOrigin не задан, используем позицию owner или самого оружия
        Vector3 origin = attackOrigin != null
            ? attackOrigin.position
            : (owner != null ? owner.position : transform.position);

        // Простой поиск попаданий
        Collider[] hits = Physics.OverlapSphere(origin, radius, hitLayers);

        if (hits.Length == 0)
        {
            Debug.Log($"{name}: ближняя атака — никого не задели.");
        }
        else
        {
            Debug.Log($"{name}: ближняя атака, задели {hits.Length} объект(ов).");

            foreach (Collider collider in hits)
            {
                // Здесь позже, на Этапе 8, мы будем вызывать метод получения урона
                // у врагов (например, через EnemyStats или интерфейс IDamageable).
                Debug.Log($"Попали по объекту: {collider.name}");

                // Псевдокод на будущее (НЕ реализуем сейчас, чтобы не ломать компиляцию):
                // var damageable = collider.GetComponent<IDamageable>();
                // if (damageable != null)
                // {
                //     damageable.TakeDamage(Damage);
                // }
            }
        }

        // Здесь же в будущем можно запускать анимацию атаки и звук удара.
    }

    private void OnDrawGizmosSelected()
    {
        // Рисуем сферу удара в редакторе, чтобы видеть радиус
        Gizmos.color = Color.red;

        float radius = hitRadius > 0f ? hitRadius : (weaponData != null ? weaponData.range : 1.5f);
        Vector3 origin = attackOrigin != null
            ? attackOrigin.position
            : (owner != null ? owner.position : transform.position);

        Gizmos.DrawWireSphere(origin, radius);
    }
}