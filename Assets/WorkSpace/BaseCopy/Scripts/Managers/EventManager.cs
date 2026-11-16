using System;
using System.Collections.Generic;

public struct EventDebugInfo
{
    public string EventName;
    public int ActionCount;
    public int ListenerCount;
}

public class EventManager : ManagerBase
{
    public void Subscribe<T>(Action<T> handler) where T : IEvent
    {
        EventBus<T>.Subscribe(handler);
    }

    public void Unsubscribe<T>(Action<T> handler) where T : IEvent
    {
        EventBus<T>.Unsubscribe(handler);
    }

    public void Subscribe<T>(IEventListener<T> listener) where T : IEvent
    {
        EventBus<T>.Subscribe(listener);
    }

    public void Unsubscribe<T>(IEventListener<T> listener) where T : IEvent
    {
        EventBus<T>.Unsubscribe(listener);
    }

    public void Publish<T>(in T e) where T : IEvent
    {
        EventBus<T>.Publish(in e);
    }

    public List<EventDebugInfo> GetDebugInfo()
    {
        var result = new List<EventDebugInfo>();
        EventBusRegistry.CollectDebugInfo(result);
        return result;
    }

    protected override void Shutdown()
    {
        EventBusRegistry.ClearAll();
    }
}
