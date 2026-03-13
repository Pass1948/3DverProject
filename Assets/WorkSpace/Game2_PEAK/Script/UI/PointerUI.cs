using UnityEngine;
using UnityEngine.UI;

public class PointerUI : WindowUI
{
    [SerializeField] Sprite normalPointer;
    [SerializeField] Sprite climbCheckPointer;
    [SerializeField] Sprite climingPointer;

    Image uiImage;

    protected override void Awake()
    {
        base.Awake();
        uiImage = GetComponent<Image>();
    }

    private void OnEnable()
    {
        GameManager.Event.Subscribe<bool, LocomotionState, float>(EventType.ClimbCheck, ChangePointer);
    }

    private void OnDisable()
    {
        GameManager.Event.Unsubscribe<bool, LocomotionState, float>(EventType.ClimbCheck, ChangePointer);
    }

    private void ChangePointer(bool climb, LocomotionState state, float stamina)
    {
        if (stamina > 2f && climb && state == LocomotionState.Grounded || stamina > 2f && climb && state == LocomotionState.Airborne)
        {
            uiImage.sprite = climbCheckPointer;
        }
        else if (climb && state == LocomotionState.Climbing)
        {
            uiImage.sprite = climingPointer;
        }
        else
        {
            uiImage.sprite = normalPointer;
        }
    }



}
