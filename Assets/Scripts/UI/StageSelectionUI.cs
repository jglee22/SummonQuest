using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StageSelectionUI : MonoBehaviour
{
    [Header("UI 참조")]
    public GameObject stageSlotPrefab;     // 스테이지 슬롯 프리팹
    public Transform contentParent;        // 스테이지 슬롯들이 들어갈 부모
    public Button closeButton;             // 닫기 버튼
    
    [Header("스테이지 정보 패널")]
    public GameObject stageInfoPanel;      // 스테이지 정보 패널
    public TextMeshProUGUI stageNameText;  // 스테이지 이름
    public TextMeshProUGUI stageDescText;  // 스테이지 설명
    public TextMeshProUGUI difficultyText; // 난이도 정보
    public TextMeshProUGUI rewardText;     // 보상 정보
    public Button startStageButton;        // 스테이지 시작 버튼
    
    private List<GameObject> stageSlots = new List<GameObject>();
    private int selectedStageIndex = -1;
    
    private void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseButtonClicked);
        
        if (startStageButton != null)
            startStageButton.onClick.AddListener(OnStartStageButtonClicked);
        
        stageInfoPanel.SetActive(false);
    }
    
    /// <summary>
    /// 스테이지 선택 UI 표시
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
        
        // 테스트용: 간단한 UI 생성
        CreateTestUI();
        
        Debug.Log("테스트용 스테이지 선택 UI 표시");
    }
    
    /// <summary>
    /// 스테이지 선택 UI 숨기기
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
        stageInfoPanel.SetActive(false);
    }
    
    /// <summary>
    /// 스테이지 리스트 새로고침
    /// </summary>
    private void RefreshStageList()
    {
        // 기존 슬롯들 제거
        foreach (var slot in stageSlots)
        {
            Destroy(slot);
        }
        stageSlots.Clear();
        
        if (StageManager.Instance == null)
        {
            Debug.LogError("StageManager가 없습니다!");
            return;
        }
        
        StageData[] allStages = StageManager.Instance.GetAllStages();
        
        // 스테이지 슬롯 생성
        for (int i = 0; i < allStages.Length; i++)
        {
            GameObject slot = Instantiate(stageSlotPrefab, contentParent);
            StageSlotUI slotUI = slot.GetComponent<StageSlotUI>();
            
            if (slotUI != null)
            {
                slotUI.SetStageData(allStages[i], i);
                int stageIndex = i; // 클로저 문제 해결
                
                // 클릭 이벤트 추가
                slot.GetComponent<Button>().onClick.AddListener(() => OnStageSlotClicked(stageIndex));
            }
            
            stageSlots.Add(slot);
        }
    }
    
    /// <summary>
    /// 스테이지 슬롯 클릭 처리
    /// </summary>
    private void OnStageSlotClicked(int stageIndex)
    {
        selectedStageIndex = stageIndex;
        ShowStageInfo(stageIndex);
    }
    
    /// <summary>
    /// 스테이지 정보 표시
    /// </summary>
    private void ShowStageInfo(int stageIndex)
    {
        if (StageManager.Instance == null) return;
        
        StageData[] allStages = StageManager.Instance.GetAllStages();
        if (stageIndex < 0 || stageIndex >= allStages.Length) return;
        
        StageData stage = allStages[stageIndex];
        
        // 스테이지 정보 업데이트
        stageNameText.text = stage.stageName;
        stageDescText.text = stage.stageDescription;
        
        // 난이도 정보
        string difficultyInfo = $"권장 레벨: {stage.recommendedLevel}\n";
        difficultyInfo += $"난이도: {stage.difficultyMultiplier:F1}배\n";
        difficultyInfo += $"몬스터 수: {stage.monsterCount}";
        if (stage.bossMonster != null)
            difficultyInfo += " + 보스";
        difficultyText.text = difficultyInfo;
        
        // 보상 정보
        string rewardInfo = $"골드: {stage.GetTotalGoldReward()}\n";
        rewardInfo += $"경험치: {stage.GetTotalExpReward()}";
        rewardText.text = rewardInfo;
        
        // 스테이지 시작 버튼 활성화/비활성화
        bool canStart = stage.isUnlocked;
        startStageButton.interactable = canStart;
        
        if (!canStart)
        {
            startStageButton.GetComponentInChildren<TextMeshProUGUI>().text = "해금 필요";
        }
        else
        {
            startStageButton.GetComponentInChildren<TextMeshProUGUI>().text = "스테이지 시작";
        }
        
        stageInfoPanel.SetActive(true);
    }
    
    /// <summary>
    /// 스테이지 시작 버튼 클릭 처리
    /// </summary>
    private void OnStartStageButtonClicked()
    {
        if (selectedStageIndex < 0) return;
        
        // 스테이지 선택
        StageManager.Instance.SelectStage(selectedStageIndex);
        
        // BattleManager에 스테이지 몬스터 전달
        if (BattleManager.Instance != null)
        {
            List<MonsterData> stageMonsters = StageManager.Instance.GetCurrentStageMonsters();
            if (stageMonsters.Count > 0)
            {
                // 스테이지 선택 UI 숨기기
                Hide();
                
                // 전투 시작
                if (GachaManager.Instance != null && GachaManager.Instance.ownedCharacters.Count > 0)
                {
                    BattleManager.Instance.StartBattle(GachaManager.Instance.ownedCharacters[0], stageMonsters);
                }
            }
        }
    }
    
    /// <summary>
    /// 닫기 버튼 클릭 처리
    /// </summary>
    private void OnCloseButtonClicked()
    {
        Hide();
    }

    /// <summary>
    /// 테스트용 간단한 UI 생성
    /// </summary>
    private void CreateTestUI()
    {
        // 기존 UI 제거
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        
        // 배경 패널 생성
        GameObject background = new GameObject("Background");
        background.transform.SetParent(transform);
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.8f);
        RectTransform bgRect = background.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        
        // 메인 패널 생성
        GameObject mainPanel = new GameObject("MainPanel");
        mainPanel.transform.SetParent(background.transform);
        Image panelImage = mainPanel.AddComponent<Image>();
        panelImage.color = Color.white;
        RectTransform panelRect = mainPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.2f, 0.2f);
        panelRect.anchorMax = new Vector2(0.8f, 0.8f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        
        // 제목 생성
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(mainPanel.transform);
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "스테이지 선택";
        titleText.fontSize = 24;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.black;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.8f);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        
        // 스테이지 버튼들 생성
        if (StageManager.Instance != null)
        {
            StageData[] stages = StageManager.Instance.GetAllStages();
            for (int i = 0; i < stages.Length; i++)
            {
                CreateStageButton(mainPanel, stages[i], i);
            }
        }
        
        // 닫기 버튼 생성
        CreateCloseButton(mainPanel);
    }
    
    /// <summary>
    /// 스테이지 버튼 생성
    /// </summary>
    private void CreateStageButton(GameObject parent, StageData stage, int index)
    {
        GameObject buttonObj = new GameObject($"StageButton_{index}");
        buttonObj.transform.SetParent(parent.transform);
        
        Button button = buttonObj.AddComponent<Button>();
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = stage.isUnlocked ? Color.green : Color.gray;
        
        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.1f, 0.6f - (index * 0.15f));
        buttonRect.anchorMax = new Vector2(0.9f, 0.75f - (index * 0.15f));
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;
        
        // 텍스트 생성
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform);
        TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = $"Stage {stage.stageNumber}: {stage.stageName}\n난이도: {stage.difficultyMultiplier:F1}배 | 몬스터: {stage.monsterCount}마리";
        buttonText.fontSize = 16;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.color = Color.black;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        // 클릭 이벤트 추가
        int stageIndex = index;
        button.onClick.AddListener(() => OnStageButtonClicked(stageIndex));
        
        // 잠금 상태 표시
        if (!stage.isUnlocked)
        {
            buttonText.text += "\n[해금 필요]";
        }
    }
    
    /// <summary>
    /// 닫기 버튼 생성
    /// </summary>
    private void CreateCloseButton(GameObject parent)
    {
        GameObject closeObj = new GameObject("CloseButton");
        closeObj.transform.SetParent(parent.transform);
        
        Button closeButton = closeObj.AddComponent<Button>();
        Image closeImage = closeObj.AddComponent<Image>();
        closeImage.color = Color.red;
        
        RectTransform closeRect = closeObj.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.4f, 0.05f);
        closeRect.anchorMax = new Vector2(0.6f, 0.15f);
        closeRect.offsetMin = Vector2.zero;
        closeRect.offsetMax = Vector2.zero;
        
        // 텍스트 생성
        GameObject closeTextObj = new GameObject("Text");
        closeTextObj.transform.SetParent(closeObj.transform);
        TextMeshProUGUI closeText = closeTextObj.AddComponent<TextMeshProUGUI>();
        closeText.text = "닫기";
        closeText.fontSize = 18;
        closeText.alignment = TextAlignmentOptions.Center;
        closeText.color = Color.white;
        RectTransform closeTextRect = closeTextObj.GetComponent<RectTransform>();
        closeTextRect.anchorMin = Vector2.zero;
        closeTextRect.anchorMax = Vector2.one;
        closeTextRect.offsetMin = Vector2.zero;
        closeTextRect.offsetMax = Vector2.zero;
        
        closeButton.onClick.AddListener(() => Hide());
    }
    
    /// <summary>
    /// 스테이지 버튼 클릭 처리
    /// </summary>
    private void OnStageButtonClicked(int stageIndex)
    {
        if (StageManager.Instance == null) return;
        
        StageData[] stages = StageManager.Instance.GetAllStages();
        if (stageIndex < 0 || stageIndex >= stages.Length) return;
        
        StageData stage = stages[stageIndex];
        
        if (!stage.isUnlocked)
        {
            NotiManager.Instance.Show("아직 해금되지 않은 스테이지입니다!");
            return;
        }
        
        // 스테이지 선택
        StageManager.Instance.SelectStage(stageIndex);
        
        // BattleManager에 스테이지 몬스터 전달
        if (BattleManager.Instance != null)
        {
            List<MonsterData> stageMonsters = StageManager.Instance.GetCurrentStageMonsters();
            if (stageMonsters.Count > 0)
            {
                // 스테이지 선택 UI 숨기기
                Hide();
                
                // 전투 시작
                if (GachaManager.Instance != null && GachaManager.Instance.ownedCharacters.Count > 0)
                {
                    BattleManager.Instance.StartBattle(GachaManager.Instance.ownedCharacters[0], stageMonsters);
                }
                else
                {
                    NotiManager.Instance.Show("보유한 캐릭터가 없습니다!");
                }
            }
        }
    }
} 