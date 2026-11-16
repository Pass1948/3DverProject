using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class PoolManager : ManagerBase
{
    private class PoolEntry
    {
        public GameObject Prefab;
        public ObjectPool<GameObject> Pool;
        public Transform Container;
        public bool IsUI;
    }

    // 키: 프리팹 GameObject의 InstanceID
    private readonly Dictionary<int, PoolEntry> prefabPools = new Dictionary<int, PoolEntry>();
    // 키: 생성된 인스턴스 GameObject의 InstanceID (반납할 때 어느 풀인지 찾기용)
    private readonly Dictionary<int, PoolEntry> instancePools = new Dictionary<int, PoolEntry>();
    // 현재 빌려간(활성) 인스턴스
    private readonly HashSet<int> activeInstances = new HashSet<int>();

    private Transform poolRoot;
    private Transform uiRoot;

    protected override void Initialize()
    {
        EnsurePoolRoot();
    }

    protected override void Shutdown()
    {
        Clear();
    }

    private void EnsurePoolRoot()
    {
        if (poolRoot != null) return;
        poolRoot = new GameObject("PoolRoot").transform;
        poolRoot.SetParent(transform, false);
    }

    // UI 풀이 반납된 UI를 보관할 부모. null이면 해당 풀의 컨테이너를 사용한다.
    public void SetUIRoot(Transform root)
    {
        uiRoot = root;
    }

    public void Clear()
    {
        foreach (PoolEntry entry in prefabPools.Values)
        {
            entry.Pool.Clear();   // 보관 중인 인스턴스 파괴
            if (entry.Container != null)
                Object.Destroy(entry.Container.gameObject);
        }
        prefabPools.Clear();
        instancePools.Clear();    // 빌려간 인스턴스는 표에서만 제거
        activeInstances.Clear();
    }

    // ===================== 월드용 =====================
    public T Get<T>(T original, Vector3 position, Quaternion rotation, Transform parent) where T : Object
    {
        GameObject go = Acquire(original, false);
        if (go == null) return null;

        go.transform.SetParent(parent);
        go.transform.SetPositionAndRotation(position, rotation);
        return ConvertTo<T>(go);
    }

    public T Get<T>(T original, Vector3 position, Quaternion rotation) where T : Object
    {
        return Get<T>(original, position, rotation, null);
    }

    public T Get<T>(T original, Transform parent) where T : Object
    {
        return Get<T>(original, Vector3.zero, Quaternion.identity, parent);
    }

    public T Get<T>(T original) where T : Object
    {
        return Get<T>(original, Vector3.zero, Quaternion.identity, null);
    }

    // ===================== UI용 =====================
    public T GetUI<T>(T original, Vector3 position) where T : Object
    {
        GameObject go = Acquire(original, true);
        if (go == null) return null;

        go.transform.position = position;
        return ConvertTo<T>(go);
    }

    public T GetUI<T>(T original) where T : Object
    {
        GameObject go = Acquire(original, true);
        if (go == null) return null;

        return ConvertTo<T>(go);
    }

    // ===================== 반납 =====================
    public bool Release<T>(T instance) where T : Object
    {
        GameObject go = ToGameObject(instance);
        if (go == null) return false;

        int id = go.GetInstanceID();
        if (!instancePools.TryGetValue(id, out PoolEntry entry))
            return false;

        if (!activeInstances.Contains(id))
        {
            Debug.LogWarning($"[PoolManager] 이미 반납된 인스턴스입니다: {go.name}");
            return false;
        }

        entry.Pool.Release(go);
        return true;
    }

    public bool ReleaseUI<T>(T instance) where T : Object
    {
        return Release(instance);
    }

    public bool IsContain<T>(T instance) where T : Object
    {
        GameObject go = ToGameObject(instance);
        if (go == null) return false;
        return instancePools.ContainsKey(go.GetInstanceID());
    }

    // ===================== 내부 =====================
    private static GameObject ToGameObject(Object obj)
    {
        if (obj is GameObject go) return go;
        if (obj is Component component) return component.gameObject;
        return null;
    }

    private static T ConvertTo<T>(GameObject go) where T : Object
    {
        T asT = go as T;
        if (asT != null) return asT;
        if (typeof(Component).IsAssignableFrom(typeof(T))) return go.GetComponent<T>();
        return null;
    }

    private GameObject Acquire(Object original, bool isUI)
    {
        GameObject prefab = ToGameObject(original);
        if (prefab == null) return null;

        if (!prefabPools.TryGetValue(prefab.GetInstanceID(), out PoolEntry entry))
            entry = CreateEntry(prefab, isUI);

        return entry.Pool.Get();
    }

    private PoolEntry CreateEntry(GameObject prefab, bool isUI)
    {
        EnsurePoolRoot();

        var container = new GameObject($"{prefab.name}Container").transform;
        container.SetParent(poolRoot, false);

        var entry = new PoolEntry { Prefab = prefab, Container = container, IsUI = isUI };
        entry.Pool = new ObjectPool<GameObject>(
            createFunc: () =>
            {
                GameObject obj = Object.Instantiate(prefab);
                instancePools[obj.GetInstanceID()] = entry;
                return obj;
            },
            actionOnGet: (GameObject obj) =>
            {
                obj.SetActive(true);
                activeInstances.Add(obj.GetInstanceID());
            },
            actionOnRelease: (GameObject obj) =>
            {
                activeInstances.Remove(obj.GetInstanceID());
                obj.SetActive(false);
                if (entry.IsUI && uiRoot != null)
                    obj.transform.SetParent(uiRoot, false);
                else
                    obj.transform.SetParent(entry.Container, false);
            },
            actionOnDestroy: (GameObject obj) =>
            {
                int id = obj.GetInstanceID();
                instancePools.Remove(id);
                activeInstances.Remove(id);
                Object.Destroy(obj);
            });

        prefabPools.Add(prefab.GetInstanceID(), entry);
        return entry;
    }
}
