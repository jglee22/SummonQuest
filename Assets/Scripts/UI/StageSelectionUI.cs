using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StageSelectionUI : MonoBehaviour
{
    [Header("UI 참조")]
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
    private bool panelPrefabLoaded;

    private void Awake()
    {
        EnsurePanelPrefab();
        ResolvePrefabs();
        ApplyPanelBindings();
    }

    private void OnEnable()
    {
        EnsurePanelPrefab();
        ApplyPanelBindings();
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
        EnsurePanelPrefab();
        ApplyPanelBindings();
        gameObject.SetActive(true);
        RefreshStageList();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        if (stageInfoPanel != null)
            stageInfoPanel.SetActive(false);
    }

    private void EnsurePanelPrefab()
    {
        if (panelPrefabLoaded || GetComponentInChildren<StageSelectionPanelView>(true) != null)
        {
            panelPrefabLoaded = true;
            return;
        }

        GameObject prefab = UIPrefabLoader.LoadStageSelectionPanel();
        if (prefab == null)
        {
            Debug.LogError("StageSelectionPanel prefab을 Resources/Prefabs/ 에서 찾을 수 없습니다.");
            return;
        }

        GameObject instance = Instantiate(prefab, transform);
        RectTransform rectTransform = instance.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.localScale = Vector3.one;
        }

        panelPrefabLoaded = true;
    }

    private void ResolvePrefabs()
    {
        if (stageSlotPrefab == null)
            stageSlotPrefab = UIPrefabLoader.LoadStageSlot();
    }

    private void ApplyPanelBindings()
    {
        if (contentParent != null)
            return;

        StageSelectionPanelView view = GetComponentInChildren<StageSelectionPanelView>(true);
        if (view == null)
            return;

        contentParent = view.contentParent;
        closeButton = view.closeButton;
        stageInfoPanel = view.stageInfoPanel;
        stageNameText = view.stageNameText;
        stageDescText = view.stageDescText;
        difficultyText = view.difficultyText;
        rewardText = view.rewardText;
        startStageButton = view.startStageButton;
    }

    private void RefreshStageList()
    {
        if (contentParent == null)
            return;

        foreach (GameObject slot in stageSlots)
            Destroy(slot);

        stageSlots.Clear();

        if (StageManager.Instance == null)
            return;

        if (stageSlotPrefab == null)
        {
            Debug.LogWarning("StageSlot prefab이 없습니다.");
            return;
        }

        StageData[] allStages = StageManager.Instance.GetAllStages();
        for (int i = 0; i < allStages.Length; i++)
        {
            int stageIndex = i;
            StageData stage = allStages[i];

            GameObject slot = Instantiate(stageSlotPrefab, contentParent);
            StageSlotUI slotUI = slot.GetComponent<StageSlotUI>();
            if (slotUI != null)
            {
                slotUI.SetStageData(stage, stageIndex);
                slot.GetComponent<Button>()?.onClick.AddListener(() => OnStageSlotClicked(stageIndex));
            }

            stageSlots.Add(slot);
        }
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
}
