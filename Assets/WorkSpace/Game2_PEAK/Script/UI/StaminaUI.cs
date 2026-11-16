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
        GameManager.Event.Subscribe<StaminaChangedEvent>(StaminaUpdate);
    }
    private void OnDisable()
    {
        GameManager.Event.Unsubscribe<StaminaChangedEvent>(StaminaUpdate);
    }

    private void StaminaUpdate(StaminaChangedEvent e)
    {
        float currentStamina = e.Current;
        float maxStamina = e.Max;
        float staminaPercent = currentStamina / maxStamina;
        staminaBar.fillAmount = staminaPercent;
    }


}
