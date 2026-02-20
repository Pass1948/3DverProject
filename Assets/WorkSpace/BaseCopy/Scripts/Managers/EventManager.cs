using System;
using System.Collections.Generic;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    // 이벤트별 델리게이트 저장
    private readonly Dictionary<EventType, Delegate> _handlers = new Dictionary<EventType, Delegate>();

    // 구독 (메소드에 매개변수 없을때 사용)
    public void Subscribe(EventType eventType, Action handler)
    {
        if (_handlers.TryGetValue(eventType, out var del))
        {
            _handlers[eventType] = (Action)del + handler;
        }
        else
        {
            _handlers[eventType] = handler;
        }
    }

    // 구독 (메소드에 매개변수 있을때 사용)
    public void Subscribe<T>(EventType eventType, Action<T> handler)
    {
        if (_handlers.TryGetValue(eventType, out var del))
        {
            if (del != null && del.GetType() != typeof(Action<T>))
                throw new InvalidOperationException($"Event {eventType} already registered with different payload type.");
            _handlers[eventType] = (Action<T>)del + handler;
        }
        else
        {
            _handlers[eventType] = handler;
        }
    }

    // 구독 (매개변수 2개)
    public void Subscribe<T1, T2>(EventType eventType, Action<T1, T2> handler)
    {
        if (_handlers.TryGetValue(eventType, out var del))
        {
            if (del != null && del.GetType() != typeof(Action<T1, T2>))
                throw new InvalidOperationException($"Event {eventType} already registered with different payload type.");

            _handlers[eventType] = (Action<T1, T2>)del + handler;
        }
        else
        {
            _handlers[eventType] = handler;
        }
    }
    public void Subscribe<T1, T2,T3>(EventType eventType, Action<T1, T2, T3> handler)
    {
        if (_handlers.TryGetValue(eventType, out var del))
        {
            if (del != null && del.GetType() != typeof(Action<T1, T2, T3>))
                throw new InvalidOperationException($"Event {eventType} already registered with different payload type.");

            _handlers[eventType] = (Action<T1, T2, T3>)del + handler;
        }
        else
        {
            _handlers[eventType] = handler;
        }
    }
    // 구독 해제 (메소드에 매개변수 없을때 사용)
    public void Unsubscribe(EventType eventType, Action handler)
    {
        if (!_handlers.TryGetValue(eventType, out var del)) return;
        del = (Action)del - handler;
        if (del == null) _handlers.Remove(eventType);
        else _handlers[eventType] = del;
    }

    // 구독 해제 (메소드에 매개변수있을 경우 사용)
    public void Unsubscribe<T>(EventType eventType, Action<T> handler)
    {
        if (!_handlers.TryGetValue(eventType, out var del)) return;
        if (del != null && del.GetType() != typeof(Action<T>)) return;

        del = (Action<T>)del - handler;
        if (del == null) _handlers.Remove(eventType);
        else _handlers[eventType] = del;
    }
   // 구독 해제(매개변수 2개)
public void Unsubscribe<T1, T2>(EventType eventType, Action<T1, T2> handler)
    {
        if (!_handlers.TryGetValue(eventType, out var del)) return;
        if (del != null && del.GetType() != typeof(Action<T1, T2>)) return;

        del = (Action<T1, T2>)del - handler;
        if (del == null) _handlers.Remove(eventType);
        else _handlers[eventType] = del;
    }
    public void Unsubscribe<T1, T2, T3>(EventType eventType, Action<T1, T2, T3> handler)
    {
        if (!_handlers.TryGetValue(eventType, out var del)) return;
        if (del != null && del.GetType() != typeof(Action<T1, T2, T3>)) return;

        del = (Action<T1, T2, T3>)del - handler;
        if (del == null) _handlers.Remove(eventType);
        else _handlers[eventType] = del;
    }


    // 발행 (메소드에 매개변수없을 경우 사용)
    public void Publish(EventType eventType)
    {
        if (_handlers.TryGetValue(eventType, out var del))
        {
            (del as Action)?.Invoke();
        }
    }

    // 발행 (메소드에 매개변수있을 경우 사용)
    public void Publish<T>(EventType eventType, T payload)
    {
        if (_handlers.TryGetValue(eventType, out var del))
        {
            (del as Action<T>)?.Invoke(payload);
        }
    }
    // 발행 (매개변수 2개)
    public void Publish<T1, T2>(EventType eventType, T1 payload1, T2 payload2)
    {
        if (_handlers.TryGetValue(eventType, out var del))
            (del as Action<T1, T2>)?.Invoke(payload1, payload2);
    }

    // 발행 (매개변수 2개)
    public void Publish<T1, T2, T3>(EventType eventType, T1 payload1, T2 payload2, T3 payload3)
    {
        if (_handlers.TryGetValue(eventType, out var del))
            (del as Action<T1, T2, T3>)?.Invoke(payload1, payload2, payload3);
    }
}
