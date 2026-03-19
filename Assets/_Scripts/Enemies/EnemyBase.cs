using UnityEngine;

/// <summary>
/// Базовое поведение врага: поиск цели, движение, атака.
/// Все статы (здоровье, урон, скорости) живут в EnemyStats.
/// </summary>
public class EnemyBase : MonoBehaviour
{
    [Header("Компоненты")]
    [Tooltip("Компонент со статами врага.")]
    [SerializeField] private EnemyStats stats;

    [Header("Цель")]
    [Tooltip("Текущая цель врага (обычно игрок). Если не назначена, будет найдена по тегу.")]
    [SerializeField] private Transform target;

    [Header("Поиск цели (упрощённо)")]
    [Tooltip("Тег игрока, по которому враг ищет цель.")]
    [SerializeField] private string playerTag = "Player";

    [Header("Простые тайминги (учебно)")]
    [Tooltip("Как часто враг пытается искать цель, если цель не найдена.")]
    [Min(0.05f)]
    [SerializeField] private float targetSearchInterval = 0.5f;

    [Tooltip("Минимальная пауза между атаками (чтобы не атаковать каждый кадр).")]
    [Min(0f)]
    [SerializeField] private float attackCooldown = 1f;

    private float nextTargetSearchTime;
    private float nextAttackTime;

    private void Awake()
    {
        // Подтягиваем EnemyStats автоматически, чтобы префаб был устойчивым.
        if (stats == null)
        {
            stats = GetComponent<EnemyStats>();
        }
    }

    private void Start()
    {
        // Если цель не назначена в инспекторе — пробуем найти игрока по тегу.
        if (target == null)
        {
            FindTarget();
        }
    }

    private void Update()
    {
        if (stats == null)
            return;

        if (target == null)
        {
            // Ищем цель не каждый кадр, а по интервалу.
            if (Time.time >= nextTargetSearchTime)
            {
                nextTargetSearchTime = Time.time + targetSearchInterval;
                FindTarget();
            }
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        if (distanceToTarget > stats.DetectionRange)
        {
            target = null;
            return;
        }

        if (distanceToTarget <= stats.AttackRange)
        {
            TryAttack();
        }
        else
        {
            MoveTowardsTarget();
        }
    }

    private void TryAttack()
    {
        // Кулдаун защищает от атаки "каждый кадр".
        if (Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + attackCooldown;
        Attack();
    }

    public void FindTarget()
    {
        if (stats == null)
            return;

        // Упрощённый поиск: ищем объект с нужным тегом.
        if (!string.IsNullOrEmpty(playerTag))
        {
            GameObject playerObject = GameObject.FindWithTag(playerTag);
            target = playerObject != null ? playerObject.transform : null;
        }

        if (target != null)
        {
            Debug.Log($"{name}: нашёл цель — {target.name}");
        }
    }

    public void MoveTowardsTarget()
    {
        if (stats == null || target == null)
            return;

        Vector3 direction = (target.position - transform.position).normalized;
        direction.y = 0f;

        transform.position += direction * stats.MoveSpeed * Time.deltaTime;

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }

    public virtual void Attack()
    {
        if (stats == null || target == null)
            return;

        Debug.Log($"{name}: атакует {target.name} с уроном {stats.Damage}");

        // На Этапе 8 здесь можно будет вызывать IDamageable у цели и передавать stats.Damage.
    }

    private void OnDrawGizmosSelected()
    {
        // Gizmos рисуются в редакторе, даже если Awake ещё не вызывался.
        if (stats == null)
        {
            stats = GetComponent<EnemyStats>();
        }

        if (stats == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stats.DetectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stats.AttackRange);
    }
}