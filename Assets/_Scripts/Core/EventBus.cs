using System;
using UnityEngine;

public class EventBus : MonoBehaviour
{
    public static EventBus Instance { get; private set; }

    // События - можно подписаться из любого места
    public event Action OnGamePaused;
    public event Action OnGameResumed;

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

    // Методы для вызова событий (raise/trigger)
    public void RaiseGamePaused()
    {
        OnGamePaused?.Invoke(); // вызываем событие, если есть подписчики
    }

    public void RaiseGameResumed()
    {
        OnGameResumed?.Invoke();
    }
}
