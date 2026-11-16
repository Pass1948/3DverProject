using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BaseUI : MonoBehaviour
{
    protected Dictionary<string, RectTransform> transforms;
    protected Dictionary<string, Button> buttons;
    protected Dictionary<string, TMP_Text> texts;

    private readonly List<Action> unsubscribeActions = new List<Action>();

    public bool IsOpen { get; private set; }

    protected virtual void Awake()
    {
        BindChildren();
    }

    // 열릴 때마다 호출 (풀에서 재사용될 때도)
    protected virtual void OnOpened() { }
    // 닫힐 때 호출
    protected virtual void OnClosed() { }

    internal void NotifyOpened()
    {
        IsOpen = true;
        OnOpened();
    }

    internal void NotifyClosed()
    {
        OnClosed();
        for (int i = 0; i < unsubscribeActions.Count; i++)
            unsubscribeActions[i]();
        unsubscribeActions.Clear();
        IsOpen = false;
    }

    // 구독 수명: 열림 ~ 닫힘
    protected void Subscribe<T>(Action<T> handler) where T : IEvent
    {
        GameManager.Event.Subscribe(handler);
        unsubscribeActions.Add(() =>
        {
            EventManager eventManager = GameManager.Event;
            if (eventManager != null) eventManager.Unsubscribe(handler);
        });
    }

    protected void Subscribe<T>(IEventListener<T> listener) where T : IEvent
    {
        GameManager.Event.Subscribe(listener);
        unsubscribeActions.Add(() =>
        {
            EventManager eventManager = GameManager.Event;
            if (eventManager != null) eventManager.Unsubscribe(listener);
        });
    }

    private void BindChildren()
    {
        transforms = new Dictionary<string, RectTransform>();
        buttons = new Dictionary<string, Button>();
        texts = new Dictionary<string, TMP_Text>();

        RectTransform[] children = GetComponentsInChildren<RectTransform>();
        foreach (RectTransform child in children)
        {
            string key = child.gameObject.name;

            if (transforms.ContainsKey(key))
                continue;

            transforms.Add(key, child);


            Button button = child.GetComponent<Button>();
            if (button != null)
                buttons.Add(key, button);

            TMP_Text text = child.GetComponent<TMP_Text>();
            if (text != null)
                texts.Add(key, text);
        }
    }
}
