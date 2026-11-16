// =====================================================================
// TestInventoryUI.cs
// [이 스크립트가 하는 일]
//   - 인벤토리 창 하나를 담당한다. 칸 버튼 생성, 칸 클릭 선택(노란 테두리 + 올리브 바탕), 선택 번호 텍스트,
//     닫기 버튼, 칸 늘리기 버튼, 안내 문구, 칸 수에 맞춘 창 높이 조절을 처리한다.
//   - WindowUI(BaseUI)를 상속해서 BaseUI 의 이름 기반 바인딩(buttons / texts 딕셔너리)을 그대로 쓴다.
//   - 열고 닫는 입력과 칸 수 보관은 TestInventoryManager 가 한다. (이 창은 표시만 담당)
// [붙인 오브젝트]
//   - 프리팹 Assets/Resources/UI/TestInventoryWindow.prefab 의 최상위 오브젝트 "TestInventoryWindow"
//     (인스펙터 연결: slotArea = SlotArea, slotTemplate = SlotTemplate)
// 주의: 창은 닫을 때 파괴되지 않고 꺼졌다 켜지므로(Manager 가 보관) Awake 는 처음 한 번만 실행되고,
//       열 때마다 실행되는 것은 OnEnable 이다. 그래서 "열 때 초기화"는 OnEnable 에서 한다.
// =====================================================================
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TestInventoryUI : WindowUI
{
    [Header("프리팹에서 연결")]
    [SerializeField] private RectTransform slotArea;    // 칸 버튼들의 부모 (Grid Layout Group 이 있는 오브젝트)
    [SerializeField] private Button slotTemplate;       // 칸 버튼 원본 (비활성 상태로 둠)

    // ---- 색 (문서 그림 기준) ----
    private static readonly Color SlotBorderNormal = new Color32(0x8A, 0x91, 0xA0, 255);    // 칸 연한 회색 테두리
    private static readonly Color SlotFillNormal = new Color32(0x45, 0x4B, 0x5C, 255);      // 칸 어두운 회색 바탕
    private static readonly Color SlotBorderSelected = new Color32(0xF5, 0xC4, 0x32, 255);  // 선택 칸 노란 테두리
    private static readonly Color SlotFillSelected = new Color32(0x7A, 0x6B, 0x3A, 255);    // 선택 칸 올리브 바탕
    private static readonly Color TextNormal = Color.white;                                 // "선택: 없음"
    private static readonly Color TextSelected = new Color32(0xF5, 0xC4, 0x32, 255);        // "선택: N번 칸"

    private const float NormalBorderThickness = 2f;     // 평소 테두리 두께(px)
    private const float SelectedBorderThickness = 5f;   // 선택 시 굵은 테두리 두께(px)

    // ---- 창 크기 계산용 (프리팹의 Grid 설정과 맞춰야 함: 칸 100, 간격 10, 4열) ----
    private const int Columns = 4;
    private const float CellSize = 100f;
    private const float CellSpacing = 10f;
    private const float TopAreaHeight = 70f;        // 제목 줄
    private const float BottomAreaHeight = 120f;    // 안내 문구 + 선택 텍스트 + 확장 버튼 줄

    private const string NoSelectionMessage = "선택: 없음";
    private const string MaxReachedMessage = "더 이상 늘릴 수 없습니다";

    private readonly List<Button> slotButtons = new List<Button>();     // 현재 만들어진 칸들 (인덱스 0 = 1번 칸)
    private readonly List<Image> slotBorders = new List<Image>();       // 칸 테두리 Image (칸 버튼 자신의 Image)
    private readonly List<Image> slotFills = new List<Image>();         // 칸 바탕 Image (자식 "Fill")
    private readonly List<RectTransform> slotFillRects = new List<RectTransform>();    // 테두리 두께 조절용

    private int selectedSlotNumber = 0;     // 선택된 칸 번호. 0 = 선택 없음, 칸 번호는 1부터

    private RectTransform windowRect;
    private Button closeButton;
    private Button expandButton;
    private TMP_Text selectedText;
    private TMP_Text guideText;

    protected override void Awake()
    {
        base.Awake();   // BaseUI 가 자식 이름으로 buttons / texts 딕셔너리를 채운다

        windowRect = (RectTransform)transform;

        // 이름 기반 바인딩으로 버튼/텍스트 찾기 (오브젝트 이름이 키)
        closeButton = buttons["CloseButton"];
        expandButton = buttons["ExpandButton"];
        selectedText = texts["SelectedText"];
        guideText = texts["GuideText"];

        // 리스너는 Awake 에서 한 번만 등록 (창을 껐다 켜도 중복 등록되지 않음)
        closeButton.onClick.AddListener(OnCloseClicked);
        expandButton.onClick.AddListener(OnExpandClicked);

        slotTemplate.gameObject.SetActive(false);
    }

    // 창이 켜질 때마다 실행: 보관된 칸 수로 칸을 다시 만들고 선택을 초기화한다.
    private void OnEnable()
    {
        if (TestInventoryManager.Instance == null) return;
        RebuildSlots(TestInventoryManager.Instance.SlotCount);
        ResetSelection();
        guideText.text = string.Empty;
    }

    // ---------------- 칸 만들기 ----------------

    private void RebuildSlots(int count)
    {
        // 기존 칸 제거 (템플릿은 따로 있으므로 만든 칸만 지움)
        foreach (Button old in slotButtons)
            Destroy(old.gameObject);
        slotButtons.Clear();
        slotBorders.Clear();
        slotFills.Clear();
        slotFillRects.Clear();

        for (int i = 0; i < count; i++)
            CreateSlot(i + 1);

        ResizeWindow(count);
    }

    private void CreateSlot(int slotNumber)
    {
        Button slot = Instantiate(slotTemplate, slotArea);
        slot.gameObject.name = "Slot_" + slotNumber;
        slot.gameObject.SetActive(true);

        // 칸 번호 글자 (칸 왼쪽 위 작은 글씨)
        TMP_Text numberText = slot.GetComponentInChildren<TMP_Text>();
        if (numberText != null) numberText.text = slotNumber.ToString();

        // 람다가 slotNumber 값을 캡처 (매개변수라 칸마다 값이 따로 저장됨)
        slot.onClick.AddListener(() => OnSlotClicked(slotNumber));

        Transform fill = slot.transform.Find("Fill");
        slotButtons.Add(slot);
        slotBorders.Add(slot.GetComponent<Image>());
        slotFills.Add(fill.GetComponent<Image>());
        slotFillRects.Add((RectTransform)fill);
    }

    // 칸 수(줄 수)에 맞춰 창 높이를 조절한다. 12칸, 16칸이 되어도 창 밖으로 넘치지 않게.
    private void ResizeWindow(int slotCount)
    {
        int rows = Mathf.CeilToInt(slotCount / (float)Columns);
        float gridHeight = rows * CellSize + (rows - 1) * CellSpacing;

        slotArea.sizeDelta = new Vector2(slotArea.sizeDelta.x, gridHeight);
        windowRect.sizeDelta = new Vector2(windowRect.sizeDelta.x, TopAreaHeight + gridHeight + BottomAreaHeight);
    }

    // ---------------- 선택 ----------------

    private void OnSlotClicked(int slotNumber)
    {
        // 1) 모든 칸을 원래 모양으로
        for (int i = 0; i < slotBorders.Count; i++)
            ApplySlotLook(i, false);

        // 2) 클릭한 칸만 노란 굵은 테두리 + 올리브 바탕 (칸 번호는 1부터라 인덱스는 -1)
        ApplySlotLook(slotNumber - 1, true);

        // 3) 선택 번호 저장
        selectedSlotNumber = slotNumber;

        // 4) 텍스트 갱신
        selectedText.text = $"선택: {selectedSlotNumber}번 칸";
        selectedText.color = TextSelected;
    }

    // 한 칸의 모양을 선택/비선택으로 바꾼다. (클릭할 때와 확장 후 복원할 때만 호출)
    private void ApplySlotLook(int index, bool selected)
    {
        slotBorders[index].color = selected ? SlotBorderSelected : SlotBorderNormal;
        slotFills[index].color = selected ? SlotFillSelected : SlotFillNormal;

        // Fill 을 테두리 두께만큼 안쪽으로 들이면 두께가 달라 보인다 (굵은 테두리 효과)
        float t = selected ? SelectedBorderThickness : NormalBorderThickness;
        slotFillRects[index].offsetMin = new Vector2(t, t);
        slotFillRects[index].offsetMax = new Vector2(-t, -t);
    }

    // 열 때 호출: 선택 없음 상태로 되돌린다. (칸은 RebuildSlots 로 새로 만들어져 이미 원래 모양)
    private void ResetSelection()
    {
        selectedSlotNumber = 0;
        selectedText.text = NoSelectionMessage;
        selectedText.color = TextNormal;
    }

    // ---------------- 버튼 ----------------

    private void OnCloseClicked()
    {
        TestInventoryManager.Instance.CloseWindow();
    }

    private void OnExpandClicked()
    {
        if (TestInventoryManager.Instance.TryExpandSlots())
        {
            // 칸 수는 Manager 가 보관. 창은 그 수로 다시 만든다.
            RebuildSlots(TestInventoryManager.Instance.SlotCount);

            // 칸이 새로 만들어졌으니 모두 원래 모양 → 선택된 칸이 있었다면 선택 모양 복원
            if (selectedSlotNumber > 0)
                ApplySlotLook(selectedSlotNumber - 1, true);

            guideText.text = string.Empty;
        }
        else
        {
            guideText.text = MaxReachedMessage;     // 최대 칸 수: 더 늘리지 않고 안내만
        }
    }
}
