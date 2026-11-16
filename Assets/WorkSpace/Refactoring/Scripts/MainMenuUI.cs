using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class MainMenuUI : WindowUI
{
    private const float DebugRefreshInterval = 0.5f;

    private readonly StringBuilder sb = new StringBuilder();
    private float nextRefreshTime;

    protected override void Awake()
    {
        base.Awake();
        buttons["Game1Button"].onClick.AddListener(() => OpenConfirm("Game1으로 이동할까요?", RefactoringPaths.Game1Scene));
        buttons["Game2Button"].onClick.AddListener(() => OpenConfirm("Game2로 이동할까요?", RefactoringPaths.Game2Scene));
    }

    private void OpenConfirm(string message, string scenePath)
    {
        GameManager.UI.ShowPopUpUI<ConfirmPopUpUI>(RefactoringPaths.ConfirmPopUpUI,
            p => p.Setup(message, () => GameManager.Scene.Load(scenePath)));
    }

    protected override void OnOpened()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        nextRefreshTime = 0f;
    }

    private void Update()
    {
        if (!IsOpen) return;
        if (Time.unscaledTime < nextRefreshTime) return;
        nextRefreshTime = Time.unscaledTime + DebugRefreshInterval;
        RefreshDebugText();
    }

    private void RefreshDebugText()
    {
        if (GameManager.Event == null) return;

        List<EventDebugInfo> infos = GameManager.Event.GetDebugInfo();
        sb.Clear();
        sb.Append("이벤트 구독 현황");
        if (infos.Count == 0)
        {
            sb.Append('\n').Append("구독 없음");
        }
        else
        {
            for (int i = 0; i < infos.Count; i++)
                sb.Append('\n').Append(infos[i].EventName).Append("  함수 ").Append(infos[i].ActionCount).Append(" / 리스너 ").Append(infos[i].ListenerCount);
        }
        texts["DebugText"].text = sb.ToString();
    }
}
