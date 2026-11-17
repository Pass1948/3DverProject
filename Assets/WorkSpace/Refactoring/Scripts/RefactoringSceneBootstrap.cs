using UnityEngine;
using UnityEngine.SceneManagement;

public static class RefactoringSceneBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // UI는 바로 열지 않고, 씬 전환 자동 정리가 끝난 뒤 Start에서 연다.
        if (scene.path != RefactoringPaths.MenuScene &&
            scene.path != RefactoringPaths.Game1Scene &&
            scene.path != RefactoringPaths.Game2Scene)
            return;

        var go = new GameObject("RefactoringSceneOpener");
        go.AddComponent<RefactoringSceneOpener>().ScenePath = scene.path;
    }
}

public class RefactoringSceneOpener : MonoBehaviour
{
    public string ScenePath;

    private void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[RefactoringSceneOpener] GameManager가 없어 UI를 열지 않습니다.");
            return;
        }

        if (ScenePath == RefactoringPaths.MenuScene)
        {
            // 테스트용: 메뉴=정지, Game1=교체, Game2=유지
            if (GameManager.Sound != null)
            {
                GameManager.Sound.SetSceneBGMStop(RefactoringPaths.MenuScene, 1f);
                GameManager.Sound.SetSceneBGM(RefactoringPaths.Game1Scene, "Game1/UnwelcomeSchool", 1f);
            }
            GameManager.UI.ShowWindowUI<MainMenuUI>(RefactoringPaths.MainMenuUI);
            new GameObject("AddressablesTestPanel").AddComponent<AddressablesTestPanel>();
        }
        else if (ScenePath == RefactoringPaths.Game1Scene || ScenePath == RefactoringPaths.Game2Scene)
            GameManager.UI.ShowWindowUI<BackToMenuUI>(RefactoringPaths.BackToMenuUI);
    }
}
