using System;
using System.Collections.Generic;
using UnityEngine;

public class UnitManager : ManagerBase
{
    private readonly Dictionary<Type, object> units = new Dictionary<Type, object>();

    public void Register<T>(T unit) where T : class
    {
        if (unit == null) return;
        Type key = typeof(T);
        if (units.TryGetValue(key, out object existing) && !IsDead(existing) && !ReferenceEquals(existing, unit))
            Debug.LogWarning($"[UnitManager] {key.Name} 이(가) 이미 등록되어 있어 다른 객체로 교체합니다.");
        units[key] = unit;
    }

    public void Unregister<T>(T unit) where T : class
    {
        if (unit == null) return;
        Type key = typeof(T);
        if (units.TryGetValue(key, out object existing) && ReferenceEquals(existing, unit))
            units.Remove(key);
    }

    public T Get<T>() where T : class
    {
        Type key = typeof(T);
        if (!units.TryGetValue(key, out object existing)) return null;
        if (IsDead(existing))
        {
            units.Remove(key);
            return null;
        }
        return existing as T;
    }

    public bool TryGet<T>(out T unit) where T : class
    {
        unit = Get<T>();
        return unit != null;
    }

    private static bool IsDead(object obj)
    {
        return obj is UnityEngine.Object o && o == null;
    }

    protected override void Shutdown()
    {
        units.Clear();
    }
}
