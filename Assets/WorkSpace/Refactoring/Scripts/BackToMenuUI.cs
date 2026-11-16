public class BackToMenuUI : WindowUI
{
    protected override void Awake()
    {
        base.Awake();
        buttons["BackButton"].onClick.AddListener(() =>
            GameManager.UI.ShowPopUpUI<ConfirmPopUpUI>(RefactoringPaths.ConfirmPopUpUI,
                p => p.Setup("메인메뉴로 이동할까요?", () => GameManager.Scene.Load(RefactoringPaths.MenuScene))));
    }
}
