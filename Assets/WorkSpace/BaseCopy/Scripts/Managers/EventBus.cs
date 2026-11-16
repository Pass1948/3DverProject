using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>모든 이벤트가 구현하는 표시용 인터페이스. struct, class 모두 가능.</summary>
public interface IEvent { }

public interface IEventListener<T> where T : IEvent
{
    void OnEvent(T e);
}

internal static class EventBus<T> where T : IEvent
{
    private static readonly List<Action<T>> actions = new List<Action<T>>();
    private static readonly List<IEventListener<T>> listeners = new List<IEventListener<T>>();
    private static int publishDepth;
    private static bool dirty;

    static EventBus()
    {
        EventBusRegistry.Register(Clear, GetInfo);
    }

    public static void Subscribe(Action<T> handler)
    {
        if (handler == null) return;
        if (actions.Contains(handler)) return;
        actions.Add(handler);
    }

    public static void Unsubscribe(Action<T> handler)
    {
        if (handler == null) return;
        int index = actions.IndexOf(handler);
        if (index < 0) return;
        if (publishDepth > 0) { actions[index] = null; dirty = true; }
        else actions.RemoveAt(index);
    }

    public static void Subscribe(IEventListener<T> listener)
    {
        if (listener == null) return;
        if (listeners.Contains(listener)) return;
        listeners.Add(listener);
    }

    public static void Unsubscribe(IEventListener<T> listener)
    {
        if (listener == null) return;
        int index = listeners.IndexOf(listener);
        if (index < 0) return;
        if (publishDepth > 0) { listeners[index] = null; dirty = true; }
        else listeners.RemoveAt(index);
    }

    public static void Publish(in T e)
    {
        // 발행 도중 추가된 구독은 이번 발행에서 호출하지 않는다.
        int actionCount = actions.Count;
        int listenerCount = listeners.Count;
        publishDepth++;
        try
        {
            for (int i = 0; i < actionCount; i++)
            {
                Action<T> handler = actions[i];
                if (handler == null) continue;
                if (handler.Target is UnityEngine.Object target && target == null)
                {
                    actions[i] = null;
                    dirty = true;
                    continue;
                }
                try { handler(e); }
                catch (Exception ex) { Debug.LogException(ex); }
            }

            for (int i = 0; i < listenerCount; i++)
            {
                IEventListener<T> listener = listeners[i];
                if (listener == null) continue;
                if (listener is UnityEngine.Object target && target == null)
                {
                    listeners[i] = null;
                    dirty = true;
                    continue;
                }
                try { listener.OnEvent(e); }
                catch (Exception ex) { Debug.LogException(ex); }
            }
        }
        finally
        {
            publishDepth--;
            if (publishDepth == 0 && dirty)
            {
                actions.RemoveAll(IsNullAction);
                listeners.RemoveAll(IsNullListener);
                dirty = false;
            }
        }
    }

    public static void Clear()
    {
        if (publishDepth > 0)
        {
            for (int i = 0; i < actions.Count; i++) actions[i] = null;
            for (int i = 0; i < listeners.Count; i++) listeners[i] = null;
            dirty = true;
            return;
        }
        actions.Clear();
        listeners.Clear();
        dirty = false;
    }

    public static EventDebugInfo GetInfo()
    {
        int actionCount = 0;
        for (int i = 0; i < actions.Count; i++) if (actions[i] != null) actionCount++;
        int listenerCount = 0;
        for (int i = 0; i < listeners.Count; i++) if (listeners[i] != null) listenerCount++;
        return new EventDebugInfo { EventName = typeof(T).Name, ActionCount = actionCount, ListenerCount = listenerCount };
    }

    private static readonly Predicate<Action<T>> IsNullAction = a => a == null;
    private static readonly Predicate<IEventListener<T>> IsNullListener = l => l == null;
}

internal static class EventBusRegistry
{
    private static readonly List<Action> clearers = new List<Action>();
    private static readonly List<Func<EventDebugInfo>> infoGetters = new List<Func<EventDebugInfo>>();

    public static void Register(Action clear, Func<EventDebugInfo> getInfo)
    {
        clearers.Add(clear);
        infoGetters.Add(getInfo);
    }

    public static void ClearAll()
    {
        for (int i = 0; i < clearers.Count; i++) clearers[i]();
    }

    public static void CollectDebugInfo(List<EventDebugInfo> result)
    {
        for (int i = 0; i < infoGetters.Count; i++) result.Add(infoGetters[i]());
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnLoad()
    {
        ClearAll();
    }
}
