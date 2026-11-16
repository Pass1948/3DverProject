using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : ManagerBase
{
    private IAssetProvider provider = new ResourcesAssetProvider();
    private readonly Dictionary<(System.Type, string), Object> cache = new Dictionary<(System.Type, string), Object>();

    // 로딩 방식 교체 지점. null이면 무시하고, 바꾸면 캐시를 비운다.
    public void SetProvider(IAssetProvider newProvider)
    {
        if (newProvider == null) return;
        provider = newProvider;
        cache.Clear();
    }

    public void ClearCache()
    {
        cache.Clear();
    }

    public T Load<T>(string path) where T : Object
    {
        var key = (typeof(T), path);

        if (cache.TryGetValue(key, out Object cached) && cached != null)
            return cached as T;

        T resource = provider.Load<T>(path);

        if (resource == null)
        {
            Debug.LogWarning($"[ResourceManager] 리소스 로드 실패 - {typeof(T).Name}: {path}");
            return null;
        }

        cache[key] = resource;
        return resource;
    }

    // ===================== Instantiate (원본) =====================
    public T Instantiate<T>(T original, Vector3 position, Quaternion rotation, Transform parent, bool pooling = false) where T : Object
    {
        return InstantiateInternal(original, position, rotation, parent, pooling);
    }

    public T Instantiate<T>(T original, Vector3 position, Quaternion rotation, bool pooling = false) where T : Object
    {
        return InstantiateInternal(original, position, rotation, null, pooling);
    }

    public new T Instantiate<T>(T original, Transform parent, bool pooling = false) where T : Object
    {
        return InstantiateInternal(original, Vector3.zero, Quaternion.identity, parent, pooling);
    }

    public T Instantiate<T>(T original, bool pooling = false) where T : Object
    {
        return InstantiateInternal(original, Vector3.zero, Quaternion.identity, null, pooling);
    }

    // ===================== Instantiate (경로) =====================
    // 경로(path)에 따라 오브젝트를 생성하는 기능. 로드에 실패하면 null 반환.
    public T Instantiate<T>(string path, Vector3 position, Quaternion rotation, Transform parent, bool pooling = false) where T : Object
    {
        return InstantiateByPath<T>(path, position, rotation, parent, pooling);
    }

    public T Instantiate<T>(string path, Vector3 position, Quaternion rotation, bool pooling = false) where T : Object
    {
        return InstantiateByPath<T>(path, position, rotation, null, pooling);
    }

    public T Instantiate<T>(string path, Transform parent, bool pooling = false) where T : Object
    {
        return InstantiateByPath<T>(path, Vector3.zero, Quaternion.identity, parent, pooling);
    }

    public T Instantiate<T>(string path, bool pooling = false) where T : Object
    {
        return InstantiateByPath<T>(path, Vector3.zero, Quaternion.identity, null, pooling);
    }

    private T InstantiateByPath<T>(string path, Vector3 position, Quaternion rotation, Transform parent, bool pooling) where T : Object
    {
        T original = Load<T>(path);
        if (original == null) return null;
        return InstantiateInternal(original, position, rotation, parent, pooling);
    }

    private T InstantiateInternal<T>(T original, Vector3 position, Quaternion rotation, Transform parent, bool pooling) where T : Object
    {
        if (pooling)
            return GameManager.Pool.Get(original, position, rotation, parent);
        else
            return Object.Instantiate(original, position, rotation, parent);
    }

    // ===================== Destroy =====================
    public void Destroy(GameObject go)
    {
        if (GameManager.Pool.IsContain(go))
            GameManager.Pool.Release(go);
        else
            Object.Destroy(go);
    }

    public void Destroy(GameObject go, float delay)
    {
        if (GameManager.Pool.IsContain(go))
            StartCoroutine(DelayReleaseRoutine(go, delay));
        else
            Object.Destroy(go, delay);
    }

    private IEnumerator DelayReleaseRoutine(GameObject go, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (go == null) yield break;   // 대기 중 이미 파괴됨
        GameManager.Pool.Release(go);
    }

    public void Destroy(Component component, float delay = 0f)
    {
        Object.Destroy(component, delay);
    }
}
