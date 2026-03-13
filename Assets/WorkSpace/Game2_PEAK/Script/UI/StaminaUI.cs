using UnityEngine;
using UnityEngine.UI;

public class StaminaUI : WindowUI
{
    [SerializeField] Image staminaBar;
    protected override void Awake()
    {
        base.Awake();
    }

    private void OnEnable()
    {
        GameManager.Event.Subscribe<float, float>(EventType.StaminaChanged, StaminaUpdate);
    }
    private void OnDisable()
    {
        GameManager.Event.Unsubscribe<float, float>(EventType.StaminaChanged, StaminaUpdate);
    }

    private void StaminaUpdate(float currentStamina, float maxStamina)
    {
        float staminaPercent = currentStamina / maxStamina;
        staminaBar.fillAmount = staminaPercent;
    }


}
