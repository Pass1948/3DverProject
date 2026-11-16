// =====================================================================
// TestInventoryManager.cs
// [이 스크립트가 하는 일]
//   - I 키 또는 화면 우측 상단 "인벤토리" 버튼으로 인벤토리 창을 열고 닫는다. (같은 ToggleWindow 경로 사용)
//   - 열려 있는지 여부를 public static bool IsOpen 으로 외부(TestController)에 알려 준다.
//   - 열 때 마우스 커서를 풀고, 닫을 때 원래(잠금/숨김)대로 되돌린다.
//   - 칸 수(SlotCount)를 보관한다. (창이 닫혀도 유지)
// [붙인 오브젝트]
//   - 씬 오브젝트가 아니다. GameManager 가 Awake 에서 코드로 생성한다.
//     (GameManager 아래 자식 "TestInventoryManager" 오브젝트, GameManager.TestInventory 로 접근)
//     우측 상단 버튼용 Canvas 도 이 스크립트가 Awake 에서 코드로 만든다. (씬/인스펙터 연결 없음)
//
// ---------------------------------------------------------------------
// [기획 메모]
//   - GameManager 에 등록한 이유: 기존 매니저(Resource/Pool/UI/Event/Sound/Unit)와 같은 방식으로
//     GameManager 가 만들고 GameManager.TestInventory 로 어디서든 접근하게 하려는 것. 씬마다 따로 붙이면
//     씬 오브젝트가 빠지거나 중복될 수 있고, 인스펙터 연결이 끊기는 문제도 없앨 수 있다.
//   - 열기/닫기 키: I (Inventory 의 머리글자라 외우기 쉽고, 이동 키 WASD / 마우스와 겹치지 않는다.)
//   - 우측 상단 버튼: 키를 모르는 사람도 화면에서 인벤토리 존재를 알 수 있게 항상 떠 있는 "인벤토리 [I]" 버튼을 둔다.
//     화면 위치는 우측 상단: 기존 UI 가 하단 중앙(4칸 퀵슬롯)과 상단 중앙(CreatUI)에 있어서 겹치지 않는 곳이다.
//     (플레이 중 커서는 잠겨 있으므로, 기존 LookOff 입력으로 커서가 풀려 있을 때 클릭할 수 있다.)
//   - 칸 수와 배치: 처음 4열 x 2행 = 8칸. 한 줄에 4칸이면 칸이 커서 클릭하기 편하고,
//     8칸이면 "빈 칸 8개 이상" 조건을 만족한다.
//     확장 버튼을 누르면 한 줄(4칸)씩 늘어나 항상 직사각형 격자가 유지되고, 최대 16칸(4행)까지.
//     칸 수가 늘면 창 높이도 줄 수에 맞춰 늘어나서 칸이 창 밖으로 나가지 않는다.
//   - 선택 표시: 선택한 칸은 굵은 노란 테두리 + 올리브색 바탕. 어두운 회색 칸들 사이에서
//     "지금 고른 칸"이 한눈에 보이고, 선택 글자("선택: N번 칸")도 노란색으로 맞춰 연결감을 준다.
//     선택은 한 번에 한 칸뿐이다.
//   - 디자인 방향: 과제 안내서의 완성 화면 예시를 따른다. 어두운 남색 둥근 패널 + 연한 회색 테두리,
//     왼쪽 위 흰색 굵은 제목 "인벤토리", 오른쪽 위 빨간 둥근 "X" 닫기 버튼, 칸 번호는 칸 왼쪽 위 작은 회색 글씨,
//     하단 왼쪽 선택 텍스트, 하단 오른쪽 확장 버튼.
//
// [사용한 UI]
//   (프리팹: Assets/Resources/UI/TestInventoryWindow.prefab, Resources 경로 "UI/TestInventoryWindow")
//   - TestInventoryWindow : 최상위 패널. Image(테두리색 #6B7385, 둥근 9-slice 스프라이트) + TestInventoryUI.
//       인스펙터 연결: slotArea = SlotArea, slotTemplate = SlotTemplate
//     └ Fill            : 패널 안쪽 Image(남색 #262B3A, 둥근). 테두리가 보이도록 2px 안쪽으로 들어가 있음.
//     └ TitleText       : TextMeshPro "인벤토리" (흰색 굵게, 왼쪽 위)
//     └ CloseButton     : Button + Image(빨강 #D04545, 둥근). 자식 Label(TMP) "X" 흰색 굵게. 오른쪽 위.
//     └ SlotArea        : Grid Layout Group(4열 고정, 칸 100x100, 간격 10). 칸 버튼들의 부모.
//         └ SlotTemplate : 칸 버튼 원본. 비활성 상태. Button(Transition None) + Image(테두리색 #8A91A0, 둥근)
//             └ Fill       : 칸 안쪽 Image(어두운 회색 #454B5C). 선택 시 올리브색 #7A6B3A
//             └ NumberText : TextMeshPro, 칸 왼쪽 위 작은 회색 번호
//     └ SelectedText    : TextMeshPro. "선택: 없음"(흰색) / "선택: N번 칸"(노란색). 왼쪽 아래.
//     └ GuideText       : TextMeshPro. 최대 칸 수에서 확장 시 "더 이상 늘릴 수 없습니다". 선택 텍스트 바로 위.
//     └ ExpandButton    : Button + Image(파란색 둥근). 자식 Label "칸 늘리기". 오른쪽 아래.
//     * CloseButton / ExpandButton / SelectedText / GuideText 는 인스펙터 연결 없이
//       BaseUI 의 이름 기반 바인딩(buttons["CloseButton"], texts["SelectedText"] ...)으로 찾는다.
//       (기존 UI 와 같은 방식. 따라서 오브젝트 이름을 바꾸면 안 된다.)
//   (우측 상단 버튼은 프리팹 없이 이 스크립트가 Awake 의 CreateOpenButton 에서 코드로 만든다. 인스펙터 연결 없음)
//   - TestInventoryButtonCanvas : 이 Manager 오브젝트의 자식. Canvas(Screen Space Overlay, Sorting Order 5)
//       + CanvasScaler(1920x1080 기준, 기존 Canvas 와 동일) + GraphicRaycaster. 우측 상단 버튼 전용.
//       창 Canvas(WindowCanvas, order 10)보다 아래라서 창이 열리면 창이 위에 보인다.
//     └ OpenButton : Button + Image(어두운 남색 둥근, 연한 테두리). 우측 상단 고정(앵커 우상단, 190x64).
//         클릭하면 onClick 으로 ToggleWindow 호출 (I 키와 같은 경로).
//         └ Label(TMP) "인벤토리", └ KeyHint(TMP) "I" (작은 키 모양 박스 안 글자)
//   - 연결 방식(인스펙터 대신): 한글 TMP 폰트와 둥근 스프라이트는 Resources 의 창 프리팹("UI/TestInventoryWindow")을
//     Resources.Load 로 읽어 그 안의 TitleText 폰트 / 루트 Image 스프라이트를 가져다 쓴다. (같은 모양 유지)
//   - EventSystem 은 새로 만들지 않는다. UIManager 가 런타임에 하나 만들어 주므로 그것을 함께 쓴다. (중복 방지)
//   - Player 오브젝트 : 기존 Controller 컴포넌트를 TestController 로 교체. (복제 씬 TestInventoryScene 의 Player 에서만)
//
// [설계]
//   - 생성 시점: GameManager.Awake → InitManagers 에서 CreateChildManager<TestInventoryManager> 로 생성.
//     이 스크립트의 Awake 는 그 직후 실행되어 버튼 Canvas 를 만든다. (UIManager 보다 뒤에 만들어진다)
//   - 접근 방법: GameManager.TestInventory (public static 접근자). IsOpen 은 static 이라
//     TestController 는 이전처럼 TestInventoryManager.IsOpen 을 그대로 읽는다.
//   - 중복 생성 방지: GameManager 는 이미 instance 가 있으면 InitManagers 를 실행하지 않고 자신을 파괴한다.
//     따라서 매니저는 한 번만 만들어진다. 씬에는 이 오브젝트와 버튼 Canvas 를 두지 않는다.
//   - 칸들 기억: TestInventoryUI 가 칸 버튼/테두리 Image/안쪽 Image 를 List 로 보관한다. (인덱스 = 칸 번호 - 1)
//   - 선택 번호: TestInventoryUI 의 int selectedSlotNumber 하나. 0 = 선택 없음, 칸 번호는 1부터.
//   - 칸 클릭 처리 순서:
//       1) 모든 칸을 원래 모양(어두운 회색 바탕 + 연한 테두리)으로
//       2) 클릭한 칸만 노란 굵은 테두리 + 올리브 바탕
//       3) selectedSlotNumber 저장
//       4) SelectedText 갱신 ("선택: N번 칸", 노란색)
//     UI 색/글자는 클릭 때와 창을 열 때만 바꾼다. Update 는 이 Manager 의 I 키 검사에만 쓴다.
//   - 열기/닫기 경로 하나: I 키(Update)와 우측 상단 버튼(onClick)과 창의 닫기 버튼이 모두 ToggleWindow / CloseWindow 를
//     거친다. 그래서 IsOpen 과 실제 창 상태가 어긋나지 않는다.
//   - 창 열 때: 처음에는 GameManager.UI.ShowWindowUI 로 창을 만들고, 이후에는 같은 창을 다시 켠다(SetActive).
//     창의 OnEnable 에서 Manager 가 보관한 칸 수로 칸을 새로 만들고 선택을 0 으로 초기화한다. 커서를 풀고 IsOpen = true.
//   - 창 닫을 때(방어 처리): try { 창 끄기 } finally { IsOpen = false; 커서 복원 } 순서.
//     창 끄기 도중 예외가 나더라도 IsOpen 과 커서 복구는 반드시 실행되므로 TestController 가 멈춘 채로 남지 않는다.
//   - 닫을 때 GameManager.UI.CloseWindowUI 를 쓰지 않는 이유: 기존 PoolManager.ReleaseUI 는 내부 canvasRoot 가
//     어디서도 대입되지 않아 항상 NullReferenceException 이 난다. (기존 매니저는 수정 금지) 그래서 만든 창을
//     풀에 반납하지 않고 이 Manager 가 들고 있다가 끄고 켠다.
//   - 칸 수를 Manager 가 보관하는 이유: 창 안의 변수는 창이 파괴되면 사라질 수 있다. 항상 켜져 있는 Manager 가
//     SlotCount 를 갖고 있으면 창을 다시 열어도 같은 칸 수로 다시 만들 수 있다. 반대로 선택 번호는
//     "열 때마다 초기화"가 요구사항이라 창 쪽에만 둔다.
//   - 커서와 LookOff: 열 때 현재 커서 상태를 저장했다가 닫을 때 그대로 복원한다. LookOff 로 커서가 풀린 상태에서
//     열었다면 풀린 상태로 복원되고, 이후 TestController.LockOff 가 매 프레임 상태에 맞게 다시 맞춘다.
// =====================================================================
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TestInventoryManager : MonoBehaviour
{
    // Resources 아래 프리팹 경로 (확장자 제외). Assets/Resources/UI/TestInventoryWindow.prefab
    private const string WindowPrefabPath = "UI/TestInventoryWindow";

    private const int StartSlotCount = 8;   // 처음 칸 수 (4x2)
    private const int ExpandStep = 4;       // 확장 시 늘어나는 칸 수 (한 줄)
    private const int MaxSlotCount = 16;    // 최대 칸 수

    private Button openButton;     // 우측 상단 "인벤토리 [I]" 버튼 (Awake 에서 코드로 생성)

    /// <summary>인벤토리 창이 열려 있는지. TestController 가 발사/회전을 막을 때 읽는다.</summary>
    public static bool IsOpen { get; private set; }

    /// <summary>다른 스크립트(창)가 칸 수를 읽고 확장을 요청하기 위한 접근점. (GameManager.TestInventory 와 같은 객체)</summary>
    public static TestInventoryManager Instance { get; private set; }

    /// <summary>현재 칸 수. 창이 닫혀도 유지된다.</summary>
    public int SlotCount { get; private set; } = StartSlotCount;

    public int MaxSlots => MaxSlotCount;

    private TestInventoryUI window;                 // 한 번 만든 창 (닫을 때는 파괴하지 않고 꺼 둔다)
    private CursorLockMode cursorModeBeforeOpen;    // 열기 전 커서 잠금 상태 (닫을 때 복원)
    private bool cursorVisibleBeforeOpen;

    // GameManager 가 만들어져 있으면 그 자식으로 한 번만 생성한다. (GameManager 에서 옮겨온 생성 방식)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateUnderGameManager()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Unit != null && GameManager.Unit.Get<TestInventoryManager>() != null) return;

        GameObject go = new GameObject("TestInventoryManager");
        go.transform.SetParent(GameManager.Instance.transform, false);
        go.AddComponent<TestInventoryManager>();
    }

    private void Awake()
    {
        Instance = this;
        IsOpen = false;     // 씬이 다시 로드되어도 상태를 초기화
        if (GameManager.Unit != null)
            GameManager.Unit.Register(this);

        CreateOpenButton();
        // 우측 상단 버튼도 I 키와 같은 경로(ToggleWindow)를 사용
        if (openButton != null)
            openButton.onClick.AddListener(ToggleWindow);
    }

    // 우측 상단 "인벤토리 [I]" 버튼을 코드로 만든다. (3단계 씬 오브젝트와 같은 모양/위치)
    private void CreateOpenButton()
    {
        // 폰트/스프라이트는 창 프리팹에서 빌려온다 (인스펙터 연결 대체)
        TMP_FontAsset font = null;
        Sprite round = null;
        GameObject windowPrefab = Resources.Load<GameObject>(WindowPrefabPath);
        if (windowPrefab != null)
        {
            TMP_Text title = windowPrefab.GetComponentInChildren<TMP_Text>(true);
            if (title != null) font = title.font;
            Image panel = windowPrefab.GetComponent<Image>();
            if (panel != null) round = panel.sprite;
        }

        // Canvas: Overlay, Sorting 5, 기준 1920x1080
        GameObject canvasGo = new GameObject("TestInventoryButtonCanvas", typeof(RectTransform));
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // 버튼 본체: 우측 상단 앵커 190x64
        GameObject btnGo = new GameObject("OpenButton", typeof(RectTransform));
        btnGo.transform.SetParent(canvasGo.transform, false);
        RectTransform btnRt = btnGo.GetComponent<RectTransform>();
        btnRt.anchorMin = btnRt.anchorMax = btnRt.pivot = new Vector2(1f, 1f);
        btnRt.sizeDelta = new Vector2(190f, 64f);
        btnRt.anchoredPosition = new Vector2(-30f, -30f);
        Image border = btnGo.AddComponent<Image>();      // 테두리색 바탕
        border.color = new Color32(0x6B, 0x73, 0x85, 0xFF);
        SetRound(border, round);
        openButton = btnGo.AddComponent<Button>();
        openButton.targetGraphic = border;

        Image fill = CreateChildImage(btnGo.transform, "Fill", new Color32(0x26, 0x2B, 0x3A, 0xFF), round, 2f);
        fill.raycastTarget = false;

        CreateText(btnGo.transform, "Label", "인벤토리", font, 28f, Color.white,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(16f, 0f), new Vector2(-62f, 0f), TextAlignmentOptions.MidlineLeft);

        // 키 모양 박스 "I"
        Image keyBox = CreateChildImage(btnGo.transform, "KeyHint", new Color32(0x45, 0x4B, 0x5C, 0xFF), round, 0f);
        keyBox.raycastTarget = false;
        RectTransform keyRt = keyBox.rectTransform;
        keyRt.anchorMin = keyRt.anchorMax = keyRt.pivot = new Vector2(1f, 0.5f);
        keyRt.sizeDelta = new Vector2(36f, 36f);
        keyRt.anchoredPosition = new Vector2(-12f, 0f);
        TMP_Text keyText = CreateText(keyBox.transform, "KeyText", "I", font, 24f, Color.white,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
        keyText.fontStyle = FontStyles.Bold;
    }

    private static void SetRound(Image img, Sprite round)
    {
        if (round == null) return;
        img.sprite = round;
        img.type = Image.Type.Sliced;
    }

    private static Image CreateChildImage(Transform parent, string name, Color color, Sprite round, float inset)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
        Image img = go.AddComponent<Image>();
        img.color = color;
        SetRound(img, round);
        return img;
    }

    private static TMP_Text CreateText(Transform parent, string name, string text, TMP_FontAsset font, float size,
        Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        return tmp;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            IsOpen = false;
        }
    }

    // Update 는 I 키 검사 하나만 한다. (UI 갱신은 하지 않는다)
    private void Update()
    {
        Keyboard keyboard = Keyboard.current;   // 새 Input System. Input.GetKeyDown 은 쓰지 않는다.
        if (keyboard == null) return;

        if (keyboard.iKey.wasPressedThisFrame)
            ToggleWindow();
    }

    /// <summary>열려 있으면 닫고, 닫혀 있으면 연다. I 키와 우측 상단 버튼이 공통으로 사용.</summary>
    public void ToggleWindow()
    {
        if (IsOpen) CloseWindow();
        else OpenWindow();
    }

    /// <summary>창을 연다. 이미 열려 있으면 무시.</summary>
    public void OpenWindow()
    {
        if (IsOpen) return;

        // 닫을 때 되돌릴 수 있게 현재 커서 상태 저장
        cursorModeBeforeOpen = Cursor.lockState;
        cursorVisibleBeforeOpen = Cursor.visible;

        if (window == null)
        {
            // 처음(또는 창이 파괴된 뒤): 기존 UIManager 로 WindowCanvas 아래에 창 생성
            window = GameManager.UI.ShowWindowUI<TestInventoryUI>(WindowPrefabPath);
            if (window == null) return;     // 프리팹 로드 실패 시 열린 상태로 만들지 않음
        }
        else
        {
            window.gameObject.SetActive(true);  // 꺼 둔 창을 다시 켬 → OnEnable 에서 칸을 새로 만든다
        }

        IsOpen = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>창을 닫는다. 닫기 버튼, I 키, 우측 상단 버튼이 함께 사용.</summary>
    public void CloseWindow()
    {
        if (!IsOpen) return;

        try
        {
            // 창 끄기 (풀 반납 CloseWindowUI 는 PoolManager 버그로 예외가 나므로 쓰지 않음. 위 [설계] 참고)
            if (window != null)
                window.gameObject.SetActive(false);
        }
        finally
        {
            // 방어 처리: 위에서 예외가 나도 상태 복구는 반드시 실행
            IsOpen = false;
            Cursor.lockState = cursorModeBeforeOpen;
            Cursor.visible = cursorVisibleBeforeOpen;
        }
    }

    /// <summary>칸을 4칸 늘린다. 최대면 false (칸 수 그대로).</summary>
    public bool TryExpandSlots()
    {
        if (SlotCount >= MaxSlotCount) return false;
        SlotCount = Mathf.Min(SlotCount + ExpandStep, MaxSlotCount);
        return true;
    }
}
