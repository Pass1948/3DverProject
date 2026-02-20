using DG.Tweening;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.UI;

public class InventoryUI : WindowUI
{
    [SerializeField] private GameObject slot1;
    [SerializeField] private GameObject slot2;
    [SerializeField] private GameObject slot3;
    [SerializeField] private GameObject slot4;
    [SerializeField] private TMP_Text slot1Text;
    [SerializeField] private TMP_Text slot2Text;
    [SerializeField] private TMP_Text slot3Text;
    [SerializeField] private TMP_Text slot4Text;
   
   [Header("Icon Padding")]
    [SerializeField] private Vector2 iconPadding = new Vector2(10f, 10f);
    private Game1Inventory inventory;

    private const string ICON_NAME = "__ItemIcon";

    private GameObject[] roots;
    private TMP_Text[] texts;
    private Image[] icons;              // 슬롯별 아이콘 캐시

    protected override void Awake()
    {
        base.Awake();
        inventory = GameManager.Unit.Game1Player.inventory;
        roots = new[] { slot1, slot2, slot3, slot4 };
        texts = new[] { slot1Text, slot2Text, slot3Text, slot4Text };
        icons = new Image[4];

        // 초기 표기
        for (int i = 0; i < 4; i++)
            Apply(i, default, null);
    }

    private void OnEnable()
    {
        if (inventory != null)
        {
            RefreshAll();
        }
        GameManager.Event.Subscribe<int>(EventType.SelectSlot, SelectUIAnime);
        GameManager.Event.Subscribe<int, Slots, ItemDefSO>(EventType.SlotChanged, HandleSlotChanged);
    }
    private void OnDisable()
    {
        GameManager.Event.Unsubscribe<int>(EventType.SelectSlot, SelectUIAnime);
        GameManager.Event.Unsubscribe<int, Slots, ItemDefSO>(EventType.SlotChanged, HandleSlotChanged);
    }

    // =========  UI 애니메이션 ==========
    private void SelectUIAnime(int i) 
    {
        switch (i)
        {
            case 0:
                slot1.transform.DOScale(new Vector3(1.2f, 1.2f, 1),0.1f);
                slot2.transform.localScale = new Vector3(1f, 1f, 1);
                slot3.transform.localScale = new Vector3(1f, 1f, 1);
                slot4.transform.localScale = new Vector3(1f, 1f, 1);
                break;
            case 1:
                slot2.transform.DOScale(new Vector3(1.2f, 1.2f, 1), 0.1f);
                slot1.transform.localScale = new Vector3(1f, 1f, 1);
                slot3.transform.localScale = new Vector3(1f, 1f, 1);
                slot4.transform.localScale = new Vector3(1f, 1f, 1);
                break;
            case 2:
                slot3.transform.DOScale(new Vector3(1.2f, 1.2f, 1), 0.1f);
                slot2.transform.localScale = new Vector3(1f, 1f, 1);
                slot1.transform.localScale = new Vector3(1f, 1f, 1);
                slot4.transform.localScale = new Vector3(1f, 1f, 1);
                break;
            case 3:
                slot4.transform.DOScale(new Vector3(1.2f, 1.2f, 1), 0.1f);
                slot2.transform.localScale = new Vector3(1f, 1f, 1);
                slot3.transform.localScale = new Vector3(1f, 1f, 1);
                slot1.transform.localScale = new Vector3(1f, 1f, 1);
                break;
        }
    }

    // =========  Slot UI업데이트 ==========
    private void RefreshAll()
    {
        if (inventory == null) return;
        int n = Mathf.Min(4, inventory.SlotCount);

        for (int i = 0; i < n; i++)
        {
            var slot = inventory._Slots[i];
            var def = inventory.GetDefById(slot.id);
            Apply(i, slot, def);
        }

        for (int i = n; i < 4; i++)
            Apply(i, default, null);
    }

    private void HandleSlotChanged(int index, Slots slot, ItemDefSO def)
    {
        if ((uint)index >= 4u) return;
        Apply(index, slot, def);
    }

    private void Apply(int index, Slots slot, ItemDefSO def)
    {
        // count: 있으면 xN / 없으면 x
        if (texts[index] != null)
            texts[index].text = slot.IsEmpty ? "x" : $"x{slot.count}";

        // icon: 있으면 생성/표시, 없으면 숨김
        if (slot.IsEmpty || def == null || def.icon == null || roots[index] == null)
        {
            HideIcon(index);
            return;
        }

        var img = GetOrCreateIcon(index, roots[index].transform);
        img.sprite = def.icon;
        img.gameObject.SetActive(true);

        if (texts[index] != null)
            texts[index].transform.SetAsLastSibling();
    }

    private Image GetOrCreateIcon(int index, Transform parent)
    {
        if (icons[index] != null) return icons[index];

        var exist = parent.Find(ICON_NAME);
        if (exist != null && exist.TryGetComponent<Image>(out var existImg))
        {
            icons[index] = existImg;
            existImg.raycastTarget = false;
            existImg.preserveAspect = true;
            return existImg;
        }

        var go = new GameObject(ICON_NAME, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = iconPadding;
        rt.offsetMax = -iconPadding;

        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        img.preserveAspect = true;

        go.transform.SetAsFirstSibling();
        icons[index] = img;
        return img;
    }

    private void HideIcon(int index)
    {
        if (icons[index] == null) return;
        icons[index].sprite = null;
        icons[index].gameObject.SetActive(false);
    }
}
