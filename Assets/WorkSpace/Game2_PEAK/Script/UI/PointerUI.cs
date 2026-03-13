using UnityEngine;
using UnityEngine.UI;
using static Unity.Entities.EntitiesJournaling;

public class PointerUI : WindowUI
{
    [SerializeField] Sprite normalPointer;
    [SerializeField] Sprite climbCheckPointer;
    [SerializeField] Sprite climingPointer;
    [SerializeField] GameObject interactText;
    [SerializeField] GameObject dropButtonText;

    Image uiImage;

    protected override void Awake()
    {
        base.Awake();
        uiImage = GetComponent<Image>();
    }

    private void OnEnable()
    {
        GameManager.Event.Subscribe<bool, LocomotionState, float>(EventType.ClimbCheck, ChangePointer);
        GameManager.Event.Subscribe<PeakCarryable>(EventType.InteractText, CheckItem);
        GameManager.Event.Subscribe<bool>(EventType.InfoButtonText, InfoButtonText);
    }

    private void OnDisable()
    {
        GameManager.Event.Unsubscribe<bool, LocomotionState, float>(EventType.ClimbCheck, ChangePointer);
        GameManager.Event.Unsubscribe<PeakCarryable>(EventType.InteractText, CheckItem);
        GameManager.Event.Unsubscribe<bool>(EventType.InfoButtonText, InfoButtonText);
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

    private void CheckItem(PeakCarryable item)
    {
        interactText.SetActive(item != null);
    }

    private void InfoButtonText(bool isheld)
    {
        dropButtonText.SetActive(isheld);
    }

}
