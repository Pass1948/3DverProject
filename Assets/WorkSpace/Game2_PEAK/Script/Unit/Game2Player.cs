using UnityEngine;

public class Game2Player : MonoBehaviour
{
    public PeakRigidbodyController controller;
    private void Awake()
    {
        GameManager.Unit.Game2Player = this;
        controller = GetComponent<PeakRigidbodyController>();
    }

    private void Start()
    {
        GameManager.UI.ShowWindowUI<StaminaUI>(Game2Path.UI + "StaminaUI");
        GameManager.UI.ShowWindowUI<PointerUI>(Game2Path.UI + "PointerUI");
    }
}
