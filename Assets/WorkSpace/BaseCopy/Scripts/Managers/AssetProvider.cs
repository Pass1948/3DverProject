public interface IAssetProvider
{
    T Load<T>(string path) where T : UnityEngine.Object;
}

public sealed class ResourcesAssetProvider : IAssetProvider
{
    public T Load<T>(string path) where T : UnityEngine.Object => UnityEngine.Resources.Load<T>(path);
}
