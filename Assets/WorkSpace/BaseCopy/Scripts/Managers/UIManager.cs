using System.Collections.Generic;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIManager : MonoBehaviour
{
    private Canvas windowCanvas;
    private Canvas popUpCanvas;
    private Stack<PopUpUI> popUpStack;

    private void Awake()
    {
        InstantsWindowUI();
        InstantsPopUpUI();
        EnsureEventSystem();
    }
    // Scene 변경시 UI를 재생성 메소드
    public void Recreated()
    {
        Clear();
        InstantsWindowUI();
        InstantsPopUpUI();
    }
    public void Clear()
    {    
        // PopUp 정리
        popUpStack.Clear();

        // Canvas 정리
        GameManager.Resource.Destroy(windowCanvas);
        GameManager.Resource.Destroy(popUpCanvas);
    }

    // window, popUp, toast 기능 동일 - 재사용 가능하게구성 
    public void InstantsWindowUI()
    {
        if (windowCanvas == null)
        {
            // 메인 경로는 상수로
            windowCanvas = GameManager.Resource.Instantiate<Canvas>(GamePath.Canvas);
            windowCanvas.gameObject.name = "WindowCanvas";
            windowCanvas.sortingOrder = 10;
        }
    }
    public void InstantsPopUpUI()
    {
        if (popUpCanvas == null)
        {
            popUpCanvas = GameManager.Resource.Instantiate<Canvas>(GamePath.Canvas);
            popUpCanvas.gameObject.name = "PopUpCanvas";
            popUpCanvas.sortingOrder = 100;
            popUpStack = new Stack<PopUpUI>();
        }
    }
    public void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        EventSystem eventSystem = GameManager.Resource.Load<EventSystem>(GamePath.UI + "EventSystem");
        GameManager.Resource.Instantiate(eventSystem, transform);
    }

    public T ShowPopUpUI<T>(T popUpUI) where T : PopUpUI
    {
        if (popUpStack.Count > 0)
        {
            PopUpUI prevUI = popUpStack.Peek();
            prevUI.gameObject.SetActive(false);
        }

        T ui = GameManager.Pool.GetUI(popUpUI);
        ui.transform.SetParent(popUpCanvas.transform, false);

        popUpStack.Push(ui);

        return ui;
    }

    public T ShowPopUpUI<T>(string path) where T : PopUpUI
    {
        T ui = GameManager.Resource.Load<T>(path);
        return ShowPopUpUI(ui);
    }

    public void ClosePopUpUI()
    {
        PopUpUI ui = popUpStack.Pop();
        GameManager.Pool.Release(ui.gameObject);

        if (popUpStack.Count > 0)
        {
            PopUpUI curUI = popUpStack.Peek();
            curUI.gameObject.SetActive(true);
        }
    }


    public T ShowWindowUI<T>(T windowUI) where T : WindowUI
    {
        T ui = GameManager.Pool.GetUI(windowUI);
        ui.transform.SetParent(windowCanvas.transform, false);
        return ui;
    }

    public T ShowWindowUI<T>(string path) where T : WindowUI
    {
        T ui = GameManager.Resource.Load<T>(path);
        return ShowWindowUI(ui);
    }

    public void CloseWindowUI(WindowUI windowUI)
    {
        GameManager.Pool.ReleaseUI(windowUI.gameObject);
    }

    public void SelectWindowUI<T>(T windowUI) where T : WindowUI
    {
        windowUI.transform.SetAsLastSibling();
    }

}
