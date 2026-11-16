using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFlowManager : ManagerBase
{
    public bool ClearOnSceneChange { get; set; } = true;
    public string CurrentScenePath => SceneManager.GetActiveScene().path;

    private bool cleanedByLoad;

    protected override void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    protected override void Shutdown()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void Load(string scenePath)
    {
        if (string.IsNullOrEmpty(scenePath))
        {
            Debug.LogWarning("[SceneFlowManager] scenePath가 비어 있습니다.");
            return;
        }

        GameManager.Event?.Publish(new SceneChangingEvent(CurrentScenePath, scenePath));

        if (ClearOnSceneChange)
        {
            Cleanup();
            cleanedByLoad = true;
        }

        SceneManager.LoadScene(scenePath);
    }

    private void Cleanup()
    {
        GameManager.UI?.CloseAll();
        GameManager.Pool?.Clear();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single)
        {
            // 주의: Load()를 거치지 않은 직접 호출은 여기서 정리하므로 새 씬 Awake에서 만든 풀 객체도 함께 정리될 수 있다. (Load()를 쓰면 문제 없음)
            if (!cleanedByLoad && ClearOnSceneChange) Cleanup();
            cleanedByLoad = false;
        }

        GameManager.Event?.Publish(new SceneLoadedEvent(scene.path, scene.name, mode));
    }
}
