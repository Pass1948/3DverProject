using UnityEngine;

[DefaultExecutionOrder(-100)]       // GameManager가 다른 Script보다 먼저 호출되게 설정
public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    public static GameManager Instance => instance;
    // Managers=========================
    private static ResourceManager resourceManager;
    public static ResourceManager Resource => resourceManager;

    private static PoolManager poolManager;
    public static PoolManager Pool => poolManager;

    private static UIManager uiManager;
    public static UIManager UI => uiManager;

    private static EventManager eventManager;
    public static EventManager Event => eventManager;

    private static SoundManager soundManager;
    public static SoundManager Sound => soundManager;

    private static UnitManager unitManager;
    public static UnitManager Unit => unitManager;

    private static SceneFlowManager sceneFlowManager;
    public static SceneFlowManager Scene => sceneFlowManager;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        ClearManagerFields();
    }

    private void Awake()
    {
        if (instance != null) { Destroy(this); return; }
        instance = this;
        DontDestroyOnLoad(this);
        CreateManagers();
        InitializeManagers();
    }

    private void OnDestroy()
    {
        if (instance != this) return;

        // 초기화의 역순으로 종료
        sceneFlowManager?.RunShutdown();
        uiManager?.RunShutdown();
        soundManager?.RunShutdown();
        unitManager?.RunShutdown();
        eventManager?.RunShutdown();
        poolManager?.RunShutdown();
        resourceManager?.RunShutdown();

        ClearManagerFields();
        instance = null;
    }

    private void CreateManagers()
    {
        resourceManager = CreateChildManager<ResourceManager>("ResourceManager");
        poolManager = CreateChildManager<PoolManager>("PoolManager");
        uiManager = CreateChildManager<UIManager>("UIManager");
        eventManager = CreateChildManager<EventManager>("EventManager");
        soundManager = CreateChildManager<SoundManager>("SoundManager");
        unitManager = CreateChildManager<UnitManager>("UnitManager");
        sceneFlowManager = CreateChildManager<SceneFlowManager>("SceneFlowManager");
    }

    // 생성이 모두 끝난 뒤 Resource → Pool → Event → Unit → Sound → UI → Scene 순서로 초기화
    private void InitializeManagers()
    {
        resourceManager.RunInitialize();
        poolManager.RunInitialize();
        eventManager.RunInitialize();
        unitManager.RunInitialize();
        soundManager.RunInitialize();
        uiManager.RunInitialize();
        sceneFlowManager.RunInitialize();
    }

    private static void ClearManagerFields()
    {
        resourceManager = null;
        poolManager = null;
        uiManager = null;
        eventManager = null;
        soundManager = null;
        unitManager = null;
        sceneFlowManager = null;
    }

    private T CreateChildManager<T>(string goName) where T : Component
    {
        var go = new GameObject(goName);
        go.transform.SetParent(transform, false);
        return go.AddComponent<T>();
    }
}
