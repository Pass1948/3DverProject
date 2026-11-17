using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class AddressablesTestPanel : MonoBehaviour
{
    private const string TestKey = "Refactoring/Test/AsyncTestCube";
    private const string MissingKey = "Refactoring/Test/NoSuchKey";
    private const float PanelWidth = 360f;

    private readonly List<GameObject> spawned = new List<GameObject>();
    private readonly List<string> logs = new List<string>();
    private string debugText = string.Empty;
    private float nextRefresh;

    private void AddLog(string msg)
    {
        logs.Add(msg);
        while (logs.Count > 5) logs.RemoveAt(0);
    }

    private void OnGUI()
    {
        if (GameManager.Instance == null || GameManager.Resource == null) return;

        if (Time.unscaledTime >= nextRefresh)
        {
            nextRefresh = Time.unscaledTime + 0.5f;
            debugText = GameManager.Resource.GetDebugInfo();
        }

        GUILayout.BeginArea(new Rect(Screen.width - PanelWidth - 10f, 10f, PanelWidth, Screen.height - 20f), GUI.skin.box);
        GUILayout.Label("Addressables Test");

        if (GUILayout.Button("LoadAsync")) OnLoadAsync();
        if (GUILayout.Button("InstantiateAsync")) OnInstantiateAsync();
        if (GUILayout.Button("Release")) OnRelease();
        if (GUILayout.Button("같은 키 동시 3회 로드")) OnLoadThree();
        if (GUILayout.Button("ReleaseScope(Scene)")) OnReleaseScope();
        if (GUILayout.Button("없는 키 로드")) OnLoadMissing();

        GUILayout.Space(6f);
        GUILayout.Label(debugText);
        GUILayout.Space(6f);
        foreach (var line in logs) GUILayout.Label(line);

        GUILayout.EndArea();
    }

    private async void OnLoadAsync()
    {
        try
        {
            var go = await GameManager.Resource.LoadAsync<GameObject>(TestKey);
            AddLog(go != null ? "LoadAsync 성공" : "LoadAsync null");
        }
        catch (System.OperationCanceledException)
        {
            AddLog("LoadAsync 취소됨");
        }
    }

    private async void OnInstantiateAsync()
    {
        try
        {
            var pos = new Vector3(spawned.Count * 1.5f, 0f, 0f);
            var go = await GameManager.Resource.InstantiateAsync<GameObject>(TestKey, pos, Quaternion.identity);
            if (go != null)
            {
                spawned.Add(go);
                AddLog($"InstantiateAsync 성공 ({spawned.Count})");
            }
            else AddLog("InstantiateAsync null");
        }
        catch (System.OperationCanceledException)
        {
            AddLog("InstantiateAsync 취소됨");
        }
    }

    private void OnRelease()
    {
        GameManager.Resource.Release<GameObject>(TestKey);
        AddLog("Release 호출");
    }

    private async void OnLoadThree()
    {
        try
        {
            int before = GameManager.Resource.AsyncLoadStartCount;
            var tasks = new List<Awaitable<GameObject>>();
            for (int i = 0; i < 3; i++)
                tasks.Add(StartLoad());
            // 각 Awaitable은 한 번씩만 기다린다.
            foreach (var t in tasks) await t;
            AddLog($"동시 3회 완료: 로드 시작 +{GameManager.Resource.AsyncLoadStartCount - before}");
        }
        catch (System.OperationCanceledException)
        {
            AddLog("동시 3회 취소됨");
        }
    }

    private Awaitable<GameObject> StartLoad()
    {
        return GameManager.Resource.LoadAsync<GameObject>(TestKey);
    }

    private void OnReleaseScope()
    {
        GameManager.Resource.ReleaseScope(ResourceScope.Scene);
        DestroySpawned();
        AddLog("ReleaseScope(Scene) 호출");
    }

    private async void OnLoadMissing()
    {
        try
        {
            var go = await GameManager.Resource.LoadAsync<GameObject>(MissingKey);
            AddLog(go == null ? "없는 키: null 반환" : "없는 키: 예상 외 성공");
        }
        catch (System.OperationCanceledException)
        {
            AddLog("없는 키 로드 취소됨");
        }
    }

    private void DestroySpawned()
    {
        foreach (var go in spawned)
        {
            if (go != null) Destroy(go);
        }
        spawned.Clear();
    }

    private void OnDestroy()
    {
        DestroySpawned();
    }
}
