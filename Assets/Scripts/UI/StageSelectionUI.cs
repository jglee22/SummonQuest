using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StageSelectionUI : MonoBehaviour
{
    [Header("UI 참조 (비어 있으면 런타임 생성)")]
    public GameObject stageSlotPrefab;
    public Transform contentParent;
    public Button closeButton;

    [Header("스테이지 정보 패널")]
    public GameObject stageInfoPanel;
    public TextMeshProUGUI stageNameText;
    public TextMeshProUGUI stageDescText;
    public TextMeshProUGUI difficultyText;
    public TextMeshProUGUI rewardText;
    public Button startStageButton;

    private readonly List<GameObject> stageSlots = new List<GameObject>();
    private int selectedStageIndex = -1;
    private bool defaultUiBuilt;

    private void OnEnable()
    {
        EnsureDefaultUI();
        RefreshStageList();
    }

    private void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);

        if (startStageButton != null)
            startStageButton.onClick.AddListener(StartSelectedStage);

        if (stageInfoPanel != null)
            stageInfoPanel.SetActive(false);
    }

    public void Show()
    {
        EnsureDefaultUI();
        gameObject.SetActive(true);
        RefreshStageList();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        if (stageInfoPanel != null)
            stageInfoPanel.SetActive(false);
    }

    private void EnsureDefaultUI()
    {
        if (defaultUiBuilt || contentParent != null)
            return;

        RectTransform root = GetComponent<RectTransform>();
        if (root == null)
            root = gameObject.AddComponent<RectTransform>();

        StretchFull(root);

        Image background = gameObject.GetComponent<Image>();
        if (background == null)
        {
            background = gameObject.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.85f);
        }

        GameObject mainPanel = CreatePanel("MainPanel", transform, new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.9f), Color.white);

        CreateTMP(mainPanel.transform, "Title", "스테이지 선택", 28, TextAlignmentOptions.Center,
            new Vector2(0f, 0.88f), new Vector2(1f, 0.98f));

        closeButton = CreateButton(mainPanel.transform, "CloseButton", "닫기",
            new Vector2(0.82f, 0.88f), new Vector2(0.98f, 0.97f), new Color(0.8f, 0.2f, 0.2f));

        GameObject scrollRoot = CreatePanel("ScrollRoot", mainPanel.transform,
            new Vector2(0.05f, 0.12f), new Vector2(0.95f, 0.85f), new Color(0.95f, 0.95f, 0.95f));

        GameObject viewport = CreatePanel("Viewport", scrollRoot.transform, Vector2.zero, Vector2.one, Color.clear);
        StretchFull(viewport.GetComponent<RectTransform>());
        viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
        viewport.AddComponent<RectMask2D>();

        contentParent = CreatePanel("Content", viewport.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), Color.clear).transform;
        RectTransform contentRect = contentParent.GetComponent<RectTransform>();
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup layout = contentParent.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 12f;
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        ContentSizeFitter fitter = contentParent.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollRect = scrollRoot.AddComponent<ScrollRect>();
        scrollRect.content = contentRect;
        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        stageInfoPanel = CreatePanel("StageInfoPanel", mainPanel.transform,
            new Vector2(0.08f, 0.02f), new Vector2(0.92f, 0.1f), new Color(0.9f, 0.95f, 1f));

        stageNameText = CreateTMP(stageInfoPanel.transform, "StageName", "스테이지를 선택하세요", 20,
            TextAlignmentOptions.Left, new Vector2(0.02f, 0.55f), new Vector2(0.55f, 0.95f));

        stageDescText = CreateTMP(stageInfoPanel.transform, "StageDesc", "", 14,
            TextAlignmentOptions.Left, new Vector2(0.02f, 0.05f), new Vector2(0.55f, 0.55f));

        difficultyText = CreateTMP(stageInfoPanel.transform, "Difficulty", "", 14,
            TextAlignmentOptions.Left, new Vector2(0.56f, 0.55f), new Vector2(0.78f, 0.95f));

        rewardText = CreateTMP(stageInfoPanel.transform, "Reward", "", 14,
            TextAlignmentOptions.Left, new Vector2(0.56f, 0.05f), new Vector2(0.78f, 0.55f));

        startStageButton = CreateButton(stageInfoPanel.transform, "StartButton", "스테이지 시작",
            new Vector2(0.8f, 0.15f), new Vector2(0.98f, 0.85f), new Color(0.2f, 0.65f, 0.3f));

        stageInfoPanel.SetActive(false);
        defaultUiBuilt = true;
    }

    private void RefreshStageList()
    {
        if (contentParent == null)
            return;

        foreach (var slot in stageSlots)
            Destroy(slot);

        stageSlots.Clear();

        if (StageManager.Instance == null)
            return;

        StageData[] allStages = StageManager.Instance.GetAllStages();
        for (int i = 0; i < allStages.Length; i++)
        {
            int stageIndex = i;
            StageData stage = allStages[i];

            if (stageSlotPrefab != null)
            {
                GameObject slot = Instantiate(stageSlotPrefab, contentParent);
                StageSlotUI slotUI = slot.GetComponent<StageSlotUI>();
                if (slotUI != null)
                {
                    slotUI.SetStageData(stage, stageIndex);
                    slot.GetComponent<Button>()?.onClick.AddListener(() => OnStageSlotClicked(stageIndex));
                }
                stageSlots.Add(slot);
            }
            else
            {
                stageSlots.Add(CreateRuntimeStageSlot(stage, stageIndex));
            }
        }
    }

    private GameObject CreateRuntimeStageSlot(StageData stage, int stageIndex)
    {
        GameObject slot = CreatePanel($"StageSlot_{stageIndex}", contentParent, Vector2.zero, Vector2.one, GetStageColor(stageIndex));
        RectTransform slotRect = slot.GetComponent<RectTransform>();
        slotRect.sizeDelta = new Vector2(0f, 90f);

        LayoutElement layoutElement = slot.AddComponent<LayoutElement>();
        layoutElement.minHeight = 90f;
        layoutElement.preferredHeight = 90f;

        Button button = slot.AddComponent<Button>();
        int index = stageIndex;
        button.onClick.AddListener(() => OnStageSlotClicked(index));

        string status = StageManager.Instance.IsCleared(stageIndex)
            ? $"클리어 ({StageManager.Instance.GetClearCount(stageIndex)}회)"
            : StageManager.Instance.IsUnlocked(stageIndex) ? "도전 가능" : "해금 필요";
        CreateTMP(slot.transform, "Title", $"Stage {stage.stageNumber}: {stage.stageName}", 20,
            TextAlignmentOptions.Left, new Vector2(0.03f, 0.45f), new Vector2(0.97f, 0.95f));
        CreateTMP(slot.transform, "Status", status, 16,
            TextAlignmentOptions.Left, new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.45f));

        return slot;
    }

    private static Color GetStageColor(int stageIndex)
    {
        if (StageManager.Instance == null)
            return new Color(0.92f, 0.92f, 0.92f);

        if (StageManager.Instance.IsCleared(stageIndex))
            return new Color(0.75f, 0.95f, 0.75f);
        if (StageManager.Instance.IsUnlocked(stageIndex))
            return new Color(0.92f, 0.92f, 0.92f);
        return new Color(0.75f, 0.75f, 0.75f);
    }

    private void OnStageSlotClicked(int stageIndex)
    {
        selectedStageIndex = stageIndex;
        ShowStageInfo(stageIndex);
    }

    private void ShowStageInfo(int stageIndex)
    {
        if (StageManager.Instance == null || stageInfoPanel == null)
            return;

        StageData[] allStages = StageManager.Instance.GetAllStages();
        if (stageIndex < 0 || stageIndex >= allStages.Length)
            return;

        StageData stage = allStages[stageIndex];

        stageNameText.text = stage.stageName;
        stageDescText.text = stage.stageDescription;

        string difficultyInfo = $"권장 Lv.{stage.recommendedLevel} / x{stage.difficultyMultiplier:F1}";
        difficultyInfo += stage.bossMonster != null ? " / 보스 포함" : "";
        difficultyText.text = difficultyInfo;

        rewardText.text = $"골드 {stage.GetTotalGoldReward()} / EXP {stage.GetTotalExpReward()}";

        bool canStart = StageManager.Instance.IsUnlocked(stageIndex);
        startStageButton.interactable = canStart;
        startStageButton.GetComponentInChildren<TextMeshProUGUI>().text = canStart ? "스테이지 시작" : "해금 필요";

        stageInfoPanel.SetActive(true);
    }

    private void StartSelectedStage()
    {
        if (selectedStageIndex < 0)
            return;

        StageManager.Instance.SelectStage(selectedStageIndex);

        if (BattleManager.Instance == null)
            return;

        List<MonsterData> stageMonsters = StageManager.Instance.GetCurrentStageMonsters();
        if (stageMonsters.Count == 0)
            return;

        OwnedCharacter playerCharacter = PlayerInventory.Instance?.GetSelectedCharacter();
        if (playerCharacter == null)
        {
            NotiManager.Instance.Show("보유한 캐릭터가 없습니다!");
            return;
        }

        Hide();
        BattleManager.Instance.StartBattle(playerCharacter, stageMonsters);
    }

    private static GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = panel.GetComponent<Image>();
        image.color = color;
        return panel;
    }

    private static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static TextMeshProUGUI CreateTMP(Transform parent, string name, string text, int fontSize,
        TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject textObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(parent, false);

        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = Color.black;
        return tmp;
    }

    private static Button CreateButton(Transform parent, string name, string label,
        Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        GameObject buttonObj = CreatePanel(name, parent, anchorMin, anchorMax, color);
        Button button = buttonObj.AddComponent<Button>();
        CreateTMP(buttonObj.transform, "Text", label, 18, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);
        return button;
    }
}
