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
        GameManager.Event.Subscribe<ClimbCheckEvent>(ChangePointer);
        GameManager.Event.Subscribe<InteractTextEvent>(CheckItem);
        GameManager.Event.Subscribe<InfoButtonTextEvent>(InfoButtonText);
    }

    private void OnDisable()
    {
        GameManager.Event.Unsubscribe<ClimbCheckEvent>(ChangePointer);
        GameManager.Event.Unsubscribe<InteractTextEvent>(CheckItem);
        GameManager.Event.Unsubscribe<InfoButtonTextEvent>(InfoButtonText);
    }

    private void ChangePointer(ClimbCheckEvent e)
    {
        bool climb = e.HasClimbSurface;
        LocomotionState state = e.State;
        float stamina = e.Stamina;
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

    private void CheckItem(InteractTextEvent e)
    {
        PeakCarryable item = e.Target;
        interactText.SetActive(item != null);
    }

    private void InfoButtonText(InfoButtonTextEvent e)
    {
        bool isheld = e.IsHeld;
        dropButtonText.SetActive(isheld);
    }

}
