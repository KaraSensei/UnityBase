/*
 * EventBus
 * Назначение: централизованный шина‑событий для взаимодействия систем (например, пауза/возобновление игры).
 * Что делает: хранит события и позволяет любым объектов подписываться/отписываться и реагировать на Raise* вызовы.
 * Связи: используется GameManager, PauseController, InputManager и другие системы UI/геймплея.
 * Паттерны: Event Bus, Observer/Publisher‑Subscriber, Singleton.
 */

using System;
using UnityEngine;

public class EventBus : MonoBehaviour
{
    public static EventBus Instance { get; private set; }

    // События — можно подписаться из любого места
    public event Action OnGamePaused;
    public event Action OnGameResumed;

    /// <summary>
    /// Инициализация Singleton и сохранение объекта между сменами сцен.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Вызов события паузы игры.
    /// Подписчики (UI, менеджеры) реагируют на OnGamePaused.
    /// </summary>
    public void RaiseGamePaused()
    {
        OnGamePaused?.Invoke(); // вызываем событие, если есть подписчики
    }

    /// <summary>
    /// Вызов события продолжения игры.
    /// Подписчики реагируют на OnGameResumed.
    /// </summary>
    public void RaiseGameResumed()
    {
        OnGameResumed?.Invoke();
    }
}
