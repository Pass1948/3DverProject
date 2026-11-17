using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

    // ===================== 비동기 (Addressables) =====================
    private sealed class AsyncEntry
    {
        public System.Type type;
        public string key;
        public ResourceScope scope;
        public int refCount;
        public Task loadTask;
        public Object asset;
        public bool done;
        public bool discarded;
        public bool released;
        public System.Action release;
    }

    private IAsyncAssetProvider asyncProvider = new AddressablesAssetProvider();
    private readonly Dictionary<(System.Type, string), AsyncEntry> asyncEntries = new Dictionary<(System.Type, string), AsyncEntry>();
    private CancellationTokenSource sceneCts;

    // 실제로 공급자에 로드를 시작한 누적 횟수 (공유 확인용)
    public int AsyncLoadStartCount { get; private set; }

    protected override void Initialize()
    {
        if (sceneCts == null) sceneCts = new CancellationTokenSource();
    }

    protected override void Shutdown()
    {
        ReleaseScope(ResourceScope.Scene);
        ReleaseScope(ResourceScope.Global);
        if (sceneCts != null)
        {
            sceneCts.Dispose();
            sceneCts = null;
        }
    }

    public void SetAsyncProvider(IAsyncAssetProvider p)
    {
        if (p == null) return;
        if (asyncEntries.Count > 0)
        {
            ReleaseScope(ResourceScope.Scene);
            ReleaseScope(ResourceScope.Global);
        }
        asyncProvider = p;
    }

    private void ReleaseEntryHandle(AsyncEntry entry)
    {
        if (entry.released) return;
        entry.released = true;
        entry.release?.Invoke();
    }

    private async Task RunLoad<T>(AsyncEntry entry, Task<T> task) where T : Object
    {
        try
        {
            entry.asset = await task;
        }
        catch (System.Exception)
        {
            entry.asset = null;
        }
        entry.done = true;
    }

    private void StartLoad<T>(AsyncEntry entry) where T : Object
    {
        AsyncAssetRequest<T> req = null;
        try
        {
            req = asyncProvider.LoadAsync<T>(entry.key);
        }
        catch (System.Exception)
        {
            req = null;
        }

        AsyncLoadStartCount++;

        if (req == null || req.Task == null)
        {
            entry.release = req != null ? req.Release : null;
            entry.asset = null;
            entry.done = true;
            entry.loadTask = Task.CompletedTask;
            return;
        }

        entry.release = req.Release;
        entry.loadTask = RunLoad(entry, req.Task);
    }

    public async Awaitable<T> LoadAsync<T>(string key, ResourceScope scope = ResourceScope.Scene,
                                          CancellationToken ct = default) where T : Object
    {
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogWarning($"[ResourceManager] 비동기 로드 실패 - 키가 비어 있습니다 ({typeof(T).Name})");
            return null;
        }

        if (sceneCts == null) sceneCts = new CancellationTokenSource();
        CancellationToken sceneToken = sceneCts.Token;

        var k = (typeof(T), key);
        if (!asyncEntries.TryGetValue(k, out AsyncEntry entry))
        {
            entry = new AsyncEntry { type = typeof(T), key = key, scope = scope };
            asyncEntries[k] = entry;
            StartLoad<T>(entry);
        }
        else if (scope == ResourceScope.Global)
        {
            entry.scope = ResourceScope.Global;   // Scene → Global 승격만
        }

        entry.refCount++;

        await entry.loadTask;

        if (entry.discarded)
        {
            ReleaseEntryHandle(entry);
            throw new System.OperationCanceledException();
        }

        if (entry.asset == null)
        {
            Debug.LogWarning($"[ResourceManager] 비동기 로드 실패 - {typeof(T).Name}: {key}");
            if (asyncEntries.TryGetValue(k, out AsyncEntry cur) && cur == entry)
                asyncEntries.Remove(k);
            ReleaseEntryHandle(entry);
            return null;
        }

        if (ct.IsCancellationRequested || sceneToken.IsCancellationRequested)
        {
            if (asyncEntries.TryGetValue(k, out AsyncEntry cur) && cur == entry)
            {
                entry.refCount--;
                if (entry.refCount <= 0)
                {
                    asyncEntries.Remove(k);
                    ReleaseEntryHandle(entry);
                }
            }
            throw new System.OperationCanceledException();
        }

        return entry.asset as T;
    }

    public async Awaitable<T> InstantiateAsync<T>(string key, Vector3 position, Quaternion rotation,
            Transform parent = null, bool pooling = false,
            ResourceScope scope = ResourceScope.Scene, CancellationToken ct = default) where T : Object
    {
        T original = await LoadAsync<T>(key, scope, ct);
        if (original == null) return null;
        return InstantiateInternal(original, position, rotation, parent, pooling);
    }

    public async Awaitable<T> InstantiateAsync<T>(string key, Transform parent = null, bool pooling = false,
            ResourceScope scope = ResourceScope.Scene, CancellationToken ct = default) where T : Object
    {
        T original = await LoadAsync<T>(key, scope, ct);
        if (original == null) return null;
        return InstantiateInternal(original, Vector3.zero, Quaternion.identity, parent, pooling);
    }

    public void Release<T>(string key) where T : Object
    {
        var k = (typeof(T), key ?? string.Empty);
        if (!asyncEntries.TryGetValue(k, out AsyncEntry entry)) return;

        entry.refCount--;
        if (entry.refCount <= 0)
        {
            asyncEntries.Remove(k);
            ReleaseEntryHandle(entry);
        }
    }

    public void ReleaseScope(ResourceScope scope)
    {
        if (scope == ResourceScope.Scene)
        {
            if (sceneCts != null)
            {
                sceneCts.Cancel();
                sceneCts.Dispose();
            }
            sceneCts = new CancellationTokenSource();
        }

        var targets = new List<KeyValuePair<(System.Type, string), AsyncEntry>>();
        foreach (var pair in asyncEntries)
        {
            if (pair.Value.scope == scope) targets.Add(pair);
        }

        foreach (var pair in targets)
        {
            asyncEntries.Remove(pair.Key);
            if (pair.Value.done) ReleaseEntryHandle(pair.Value);
            else pair.Value.discarded = true;   // 완료 시 LoadAsync에서 해제
        }
    }

    public string GetDebugInfo()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append($"비동기 항목 {asyncEntries.Count} / 로드 시작 누적 {AsyncLoadStartCount}");

        var list = new List<AsyncEntry>(asyncEntries.Values);
        list.Sort((a, b) =>
        {
            int c = string.CompareOrdinal(a.key, b.key);
            return c != 0 ? c : string.CompareOrdinal(a.type.Name, b.type.Name);
        });

        foreach (var e in list)
        {
            sb.Append('\n');
            sb.Append($"{e.key} [{e.type.Name}] 범위={e.scope} 참조={e.refCount} 상태={(e.done ? "완료" : "로딩")}");
        }
        return sb.ToString();
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
