using System;

public class ConfirmPopUpUI : PopUpUI
{
    private Action onYes;

    protected override void Awake()
    {
        base.Awake();
        buttons["YesButton"].onClick.AddListener(OnYes);
        buttons["NoButton"].onClick.AddListener(OnNo);
    }

    public void Setup(string message, Action onYes)
    {
        texts["MessageText"].text = message;
        this.onYes = onYes;
    }

    private void OnYes()
    {
        Action action = onYes;
        GameManager.UI.ClosePopUpUI();
        action?.Invoke();
    }

    private void OnNo()
    {
        GameManager.UI.ClosePopUpUI();
    }

    protected override void OnClosed()
    {
        onYes = null;
    }
}
