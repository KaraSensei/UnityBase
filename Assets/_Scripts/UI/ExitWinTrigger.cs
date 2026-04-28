using UnityEngine;

/*
 * ExitWinTrigger
 * Назначение: trigger-точка, через которую игрок завершает уровень (win path).
 * Что делает: при входе игрока запрашивает победу у GameLoopFlowController.
 * Связи: GameLoopFlowController (RequestWinFromExit), коллайдер игрока и tag-фильтр.
 * Паттерны: trigger-driven gameplay.
 */
public class ExitWinTrigger : MonoBehaviour
{
    [Tooltip("Контроллер игрового flow, который принимает решение о win.")]
    [SerializeField] private GameLoopFlowController flowController;

    [Tooltip("Tag, который считается игроком для активации выхода.")]
    [SerializeField] private string requiredTag = "Player";

    [Tooltip("Показывать ли отладочные сообщения в консоли.")]
    [SerializeField] private bool showDebugLogs = true;

    /// <summary>
    /// Автопоиск контроллера при добавлении компонента в сцену.
    /// </summary>
    private void Reset()
    {
        if (flowController == null)
            flowController = FindFirstObjectByType<GameLoopFlowController>();
    }

    /// <summary>
    /// При входе игрока в trigger запрашивает win у GameLoopFlowController.
    /// Сам trigger не решает, можно ли выигрывать, он только делегирует запрос.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
            return;

        if (!IsPlayerCollider(other))
            return;

        if (flowController == null)
        {
            if (showDebugLogs)
                Debug.LogWarning($"{name}: flowController is not assigned.", this);

            return;
        }

        flowController.RequestWinFromExit();

        if (showDebugLogs)
            Debug.Log($"{name}: player entered exit trigger, win requested.", this);
    }

    /// <summary>
    /// Проверка, что вошедший collider принадлежит игроку.
    /// Поддерживает проверку самого collider, его rigidbody и root-transform.
    /// </summary>
    private bool IsPlayerCollider(Collider other)
    {
        if (other == null)
            return false;

        if (string.IsNullOrWhiteSpace(requiredTag))
            return true;

        if (other.CompareTag(requiredTag))
            return true;

        if (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(requiredTag))
            return true;

        Transform root = other.transform.root;
        return root != null && root.CompareTag(requiredTag);
    }
}
