using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIManager : ManagerBase
{
    private const int WindowSortingOrder = 10;
    private const int PopUpSortingOrder = 100;

    private Canvas windowCanvas;
    private Canvas popUpCanvas;
    private readonly Stack<PopUpUI> popUpStack = new Stack<PopUpUI>();
    private readonly List<WindowUI> openWindows = new List<WindowUI>();

    protected override void Initialize()
    {
        windowCanvas = CreateCanvas("WindowCanvas", WindowSortingOrder);
        popUpCanvas = CreateCanvas("PopUpCanvas", PopUpSortingOrder);
        if (popUpCanvas != null)
            GameManager.Pool.SetUIRoot(popUpCanvas.transform);   // 반납된 UI는 비활성 상태로 보관
        EnsureEventSystem();
    }

    protected override void Shutdown()
    {
        CloseAll();
    }

    // window, popUp, toast 기능 동일 - 재사용 가능하게구성
    private Canvas CreateCanvas(string canvasName, int sortingOrder)
    {
        // 캔버스는 UIManager의 자식으로 만든다. (씬이 바뀌어도 유지)
        Canvas canvas = GameManager.Resource.Instantiate<Canvas>(CorePaths.Canvas, transform);
        if (canvas == null) return null;
        canvas.gameObject.name = canvasName;
        canvas.sortingOrder = sortingOrder;
        return canvas;
    }

    public void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        EventSystem eventSystem = GameManager.Resource.Load<EventSystem>(CorePaths.EventSystem);
        if (eventSystem == null) return;
        GameManager.Resource.Instantiate(eventSystem, transform);
    }

    // ===================== PopUp =====================
    public T ShowPopUpUI<T>(T popUpUI) where T : PopUpUI
    {
        return ShowPopUpUI(popUpUI, null);
    }

    public T ShowPopUpUI<T>(string path) where T : PopUpUI
    {
        return ShowPopUpUI<T>(path, null);
    }

    public T ShowPopUpUI<T>(T popUpUI, Action<T> init) where T : PopUpUI
    {
        if (popUpUI == null || popUpCanvas == null) return null;

        if (popUpStack.Count > 0)
        {
            PopUpUI prevUI = popUpStack.Peek();
            prevUI.gameObject.SetActive(false);
        }

        T ui = GameManager.Pool.GetUI(popUpUI);
        ui.transform.SetParent(popUpCanvas.transform, false);

        popUpStack.Push(ui);

        init?.Invoke(ui);
        ui.NotifyOpened();

        return ui;
    }

    public T ShowPopUpUI<T>(string path, Action<T> init) where T : PopUpUI
    {
        T ui = GameManager.Resource.Load<T>(path);
        if (ui == null) return null;
        return ShowPopUpUI(ui, init);
    }

    public void ClosePopUpUI()
    {
        if (popUpStack.Count == 0)
        {
            Debug.LogWarning("[UIManager] 닫을 팝업이 없습니다.");
            return;
        }

        PopUpUI ui = popUpStack.Pop();
        if (ui != null)
        {
            ui.NotifyClosed();
            GameManager.Pool.Release(ui.gameObject);
        }

        if (popUpStack.Count > 0)
        {
            PopUpUI curUI = popUpStack.Peek();
            curUI.gameObject.SetActive(true);
        }
    }

    public bool HasActivePopUpUI()
    {
        return popUpStack.Count > 0;
    }

    // ===================== Window =====================
    public T ShowWindowUI<T>(T windowUI) where T : WindowUI
    {
        return ShowWindowUI(windowUI, null);
    }

    public T ShowWindowUI<T>(string path) where T : WindowUI
    {
        return ShowWindowUI<T>(path, null);
    }

    public T ShowWindowUI<T>(T windowUI, Action<T> init) where T : WindowUI
    {
        if (windowUI == null || windowCanvas == null) return null;

        T ui = GameManager.Pool.GetUI(windowUI);
        ui.transform.SetParent(windowCanvas.transform, false);

        openWindows.Add(ui);

        init?.Invoke(ui);
        ui.NotifyOpened();

        return ui;
    }

    public T ShowWindowUI<T>(string path, Action<T> init) where T : WindowUI
    {
        T ui = GameManager.Resource.Load<T>(path);
        if (ui == null) return null;
        return ShowWindowUI(ui, init);
    }

    public void CloseWindowUI(WindowUI windowUI)
    {
        if (windowUI == null || !openWindows.Remove(windowUI))
        {
            Debug.LogWarning("[UIManager] 열려 있지 않은 윈도우입니다.");
            return;
        }

        windowUI.NotifyClosed();
        GameManager.Pool.Release(windowUI.gameObject);
    }

    public void SelectWindowUI<T>(T windowUI) where T : WindowUI
    {
        windowUI.transform.SetAsLastSibling();
    }

    // ===================== 전체 닫기 =====================
    public void CloseAll()
    {
        while (popUpStack.Count > 0)
            ClosePopUpUI();

        while (openWindows.Count > 0)
            CloseWindowUI(openWindows[openWindows.Count - 1]);
    }

    // 캔버스는 파괴하지 않는다.
    public void Clear()
    {
        CloseAll();
    }
}
