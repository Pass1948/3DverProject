using UnityEngine;
using UnityEngine.UI;

public class CreatUI : PopUpUI
{
    [SerializeField] Button createButton;

    protected override void Awake()
    {
        base.Awake();
        buttons[createButton.name].onClick.AddListener(SpawnSlime);
    }

    private void SpawnSlime()
    {
        GameManager.Event.Publish(EventType.Spawn);
    }
}
