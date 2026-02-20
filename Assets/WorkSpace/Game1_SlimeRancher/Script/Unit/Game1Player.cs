using UnityEngine;

public class Game1Player : MonoBehaviour
{
    public Controller controller;
    public Game1Inventory inventory;
    private void Awake()
    {
        GameManager.Unit.Game1Player = this;
        controller = GetComponent<Controller>();
        inventory = GetComponent<Game1Inventory>();
    }

    private void Start()
    {
        GameManager.UI.ShowPopUpUI<CreatUI>(Game1Path.UI + "CreatUI");
        GameManager.UI.ShowWindowUI<InventoryUI>(Game1Path.UI + "InventoryUI");
    }
}
