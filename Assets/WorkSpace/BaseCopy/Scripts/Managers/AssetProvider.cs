public interface IAssetProvider
{
    T Load<T>(string path) where T : UnityEngine.Object;
}

public sealed class ResourcesAssetProvider : IAssetProvider
{
    public T Load<T>(string path) where T : UnityEngine.Object => UnityEngine.Resources.Load<T>(path);
}

// ===================== 비동기 (Addressables) =====================
public sealed class AsyncAssetRequest<T> where T : UnityEngine.Object
{
    public System.Threading.Tasks.Task<T> Task;   // 로드 결과
    public System.Action Release;                  // 이 요청이 잡은 핸들 해제
}

public interface IAsyncAssetProvider
{
    AsyncAssetRequest<T> LoadAsync<T>(string key) where T : UnityEngine.Object;
}

public sealed class AddressablesAssetProvider : IAsyncAssetProvider
{
    public AsyncAssetRequest<T> LoadAsync<T>(string key) where T : UnityEngine.Object
    {
        var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<T>(key);
        return new AsyncAssetRequest<T>
        {
            Task = handle.Task,
            Release = () =>
            {
                if (handle.IsValid())
                    UnityEngine.AddressableAssets.Addressables.Release(handle);
            }
        };
    }
}
