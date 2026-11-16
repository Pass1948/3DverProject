using UnityEngine;

public abstract class ManagerBase : MonoBehaviour
{
    public bool IsInitialized { get; private set; }
    internal void RunInitialize() { if (IsInitialized) return; Initialize(); IsInitialized = true; }
    internal void RunShutdown()   { if (!IsInitialized) return; Shutdown(); IsInitialized = false; }
    protected virtual void Initialize() { }
    protected virtual void Shutdown() { }
}
