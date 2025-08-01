using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// 게임의 모든 UI를 통합 관리하는 매니저 클래스
/// UI 패널 관리, 전환 애니메이션, UI 상태 관리 등을 담당
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("메인 UI 패널들")]
    public GameObject battlePanel;        // 전투 패널
    public GameObject gachaPanel;         // 가챠 패널
    public GameObject characterPanel;     // 캐릭터 관리 패널
    public GameObject settingsPanel;      // 설정 패널
    public GameObject stageSelectionPanel; // 스테이지 선택 패널 추가

    [Header("팝업 UI들")]
    public GameObject notificationPanel;  // 알림 패널
    public GameObject confirmPanel;       // 확인 다이얼로그

    [Header("UI 설정")]
    public float fadeDuration = 0.3f;     // 페이드 애니메이션 시간
    public float scaleDuration = 0.2f;    // 스케일 애니메이션 시간
    public Ease fadeEase = Ease.InOutQuad;
    public Ease scaleEase = Ease.OutBack;

    [Header("UI 상태")]
    public UIPanel currentPanel = UIPanel.None;
    public bool isUIBusy = false;         // UI 전환 중인지 여부

    // UI 패널 딕셔너리
    private Dictionary<UIPanel, GameObject> panelDictionary = new Dictionary<UIPanel, GameObject>();
    
    // 이전 패널 스택 (뒤로가기 기능용)
    private Stack<UIPanel> panelHistory = new Stack<UIPanel>();

    // 이벤트
    public System.Action<UIPanel> OnPanelChanged;
    public System.Action<bool> OnUIBusyStateChanged;

    private void Awake()
    {
        // 싱글톤 패턴
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeUIManager();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 초기 UI 설정
        SetupInitialUI();
    }

    private void Update()
    {
        // 테스트용: O키로 설정 패널 열기
        if (Input.GetKeyDown(KeyCode.O))
        {
            OpenSettings();
            Debug.Log("O키를 눌러서 설정 패널을 열었습니다.");
        }
        
        // 테스트용: P키로 스테이지 선택 패널 열기
        if (Input.GetKeyDown(KeyCode.P))
        {
            OpenStageSelection();
            Debug.Log("P키를 눌러서 스테이지 선택 패널을 열었습니다.");
        }
        
        // 테스트용: 숫자키 1,2,3으로 스테이지 바로 시작
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            StartStageDirectly(0);
            Debug.Log("1키를 눌러서 Stage 1을 시작했습니다.");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            StartStageDirectly(1);
            Debug.Log("2키를 눌러서 Stage 2를 시작했습니다.");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            StartStageDirectly(2);
            Debug.Log("3키를 눌러서 Stage 3을 시작했습니다.");
        }
    }

    /// <summary>
    /// UI 매니저 초기화
    /// </summary>
    private void InitializeUIManager()
    {
        // 패널 딕셔너리 초기화
        panelDictionary[UIPanel.Battle] = battlePanel;
        panelDictionary[UIPanel.Gacha] = gachaPanel;
        panelDictionary[UIPanel.Character] = characterPanel;
        panelDictionary[UIPanel.Settings] = settingsPanel;
        panelDictionary[UIPanel.StageSelection] = stageSelectionPanel;
        panelDictionary[UIPanel.Notification] = notificationPanel;
        panelDictionary[UIPanel.Confirm] = confirmPanel;
    }

    /// <summary>
    /// 초기 UI 설정
    /// </summary>
    private void SetupInitialUI()
    {
        // 모든 패널 비활성화
        foreach (var panel in panelDictionary.Values)
        {
            if (panel != null)
                panel.SetActive(false);
        }

        // 기본적으로 아무 패널도 표시하지 않음
        currentPanel = UIPanel.None;
    }

    #region 패널 관리

    /// <summary>
    /// 패널 표시
    /// </summary>
    public void ShowPanel(UIPanel panelType, bool addToHistory = true)
    {
        if (isUIBusy) return;

        StartCoroutine(ShowPanelCoroutine(panelType, addToHistory));
    }

    /// <summary>
    /// 패널 표시 코루틴
    /// </summary>
    private System.Collections.IEnumerator ShowPanelCoroutine(UIPanel panelType, bool addToHistory)
    {
        SetUIBusy(true);

        // 이전 패널 숨기기
        if (currentPanel != UIPanel.None)
        {
            yield return StartCoroutine(HidePanelCoroutine(currentPanel));
        }

        // 새 패널 표시
        if (panelDictionary.TryGetValue(panelType, out GameObject panel))
        {
            if (panel != null)
            {
                panel.SetActive(true);
                
                // 페이드 인 애니메이션 제거 - CanvasGroup 사용 안함

                // 스케일 애니메이션
                RectTransform rectTransform = panel.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.localScale = Vector3.zero;
                    rectTransform.DOScale(Vector3.one, scaleDuration).SetEase(scaleEase);
                }

                // 히스토리에 추가
                if (addToHistory && currentPanel != UIPanel.None)
                {
                    panelHistory.Push(currentPanel);
                }

                currentPanel = panelType;
                OnPanelChanged?.Invoke(panelType);

                Debug.Log($"UI 패널 표시: {panelType}");
            }
        }
        else
        {
            Debug.LogWarning($"패널을 찾을 수 없습니다: {panelType}");
        }

        SetUIBusy(false);
    }

    /// <summary>
    /// 패널 숨기기
    /// </summary>
    public void HidePanel(UIPanel panelType)
    {
        if (isUIBusy) return;

        StartCoroutine(HidePanelCoroutine(panelType));
    }

    /// <summary>
    /// 패널 숨기기 코루틴
    /// </summary>
    private System.Collections.IEnumerator HidePanelCoroutine(UIPanel panelType)
    {
        if (panelDictionary.TryGetValue(panelType, out GameObject panel))
        {
            if (panel != null && panel.activeSelf)
            {
                // 페이드 아웃 애니메이션 제거 - CanvasGroup 사용 안함

                panel.SetActive(false);
                Debug.Log($"UI 패널 숨김: {panelType}");
            }
        }
        yield return null; // 코루틴 완료
    }

    /// <summary>
    /// 이전 패널로 돌아가기
    /// </summary>
    public void GoBack()
    {
        if (isUIBusy || panelHistory.Count == 0) return;

        UIPanel previousPanel = panelHistory.Pop();
        ShowPanel(previousPanel, false);
    }

    /// <summary>
    /// 모든 패널 숨기기
    /// </summary>
    public void HideAllPanels()
    {
        foreach (var panel in panelDictionary.Values)
        {
            if (panel != null)
                panel.SetActive(false);
        }
        currentPanel = UIPanel.None;
        panelHistory.Clear();
    }

    #endregion

    #region 팝업 관리

    /// <summary>
    /// 알림 표시
    /// </summary>
    public void ShowNotification(string message, float duration = 3f)
    {
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(true);
            
            // 알림 텍스트 설정
            Text notificationText = notificationPanel.GetComponentInChildren<Text>();
            if (notificationText != null)
                notificationText.text = message;

            // 자동 숨김
            StartCoroutine(HideNotificationAfterDelay(duration));
        }
    }

    /// <summary>
    /// 알림 자동 숨김
    /// </summary>
    private System.Collections.IEnumerator HideNotificationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (notificationPanel != null)
            notificationPanel.SetActive(false);
    }

    /// <summary>
    /// 확인 다이얼로그 표시
    /// </summary>
    public void ShowConfirmDialog(string message, System.Action onConfirm, System.Action onCancel = null)
    {
        if (confirmPanel != null)
        {
            confirmPanel.SetActive(true);
            
            // 메시지 설정
            Text messageText = confirmPanel.GetComponentInChildren<Text>();
            if (messageText != null)
                messageText.text = message;

            // 버튼 이벤트 설정
            Button confirmButton = confirmPanel.transform.Find("ConfirmButton")?.GetComponent<Button>();
            Button cancelButton = confirmPanel.transform.Find("CancelButton")?.GetComponent<Button>();

            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveAllListeners();
                confirmButton.onClick.AddListener(() => {
                    confirmPanel.SetActive(false);
                    onConfirm?.Invoke();
                });
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveAllListeners();
                cancelButton.onClick.AddListener(() => {
                    confirmPanel.SetActive(false);
                    onCancel?.Invoke();
                });
            }
        }
    }

    #endregion

    #region UI 상태 관리

    /// <summary>
    /// UI 바쁜 상태 설정
    /// </summary>
    private void SetUIBusy(bool busy)
    {
        isUIBusy = busy;
        OnUIBusyStateChanged?.Invoke(busy);
    }

    /// <summary>
    /// 현재 패널 확인
    /// </summary>
    public bool IsCurrentPanel(UIPanel panelType)
    {
        return currentPanel == panelType;
    }

    /// <summary>
    /// 패널이 활성화되어 있는지 확인
    /// </summary>
    public bool IsPanelActive(UIPanel panelType)
    {
        if (panelDictionary.TryGetValue(panelType, out GameObject panel))
        {
            return panel != null && panel.activeSelf;
        }
        return false;
    }

    #endregion

    #region 게임 상태별 UI 관리

    /// <summary>
    /// 게임 상태에 따른 UI 업데이트
    /// </summary>
    public void UpdateUIForGameState(GameState gameState)
    {
        switch (gameState)
        {
            case GameState.Battle:
                ShowPanel(UIPanel.Battle);
                break;
            case GameState.Gacha:
                ShowPanel(UIPanel.Gacha);
                break;
        }
    }

    #endregion

    #region 유틸리티

    /// <summary>
    /// 설정 패널 열기
    /// </summary>
    public void OpenSettings()
    {
        ShowPanel(UIPanel.Settings);
    }

    /// <summary>
    /// 설정 패널 닫기
    /// </summary>
    public void CloseSettings()
    {
        HidePanel(UIPanel.Settings);
    }

    /// <summary>
    /// 스테이지 선택 UI 열기
    /// </summary>
    public void OpenStageSelection()
    {
        if (stageSelectionPanel == null)
        {
            Debug.LogError("stageSelectionPanel이 할당되지 않았습니다!");
            return;
        }
        
        ShowPanel(UIPanel.StageSelection); // UIPanel.StageSelection 추가
        Debug.Log("스테이지 선택 UI 열기");
    }
    
    /// <summary>
    /// 스테이지를 바로 시작 (테스트용)
    /// </summary>
    public void StartStageDirectly(int stageIndex)
    {
        if (StageManager.Instance == null)
        {
            Debug.LogError("StageManager가 없습니다!");
            return;
        }
        
        StageData[] stages = StageManager.Instance.GetAllStages();
        if (stageIndex < 0 || stageIndex >= stages.Length)
        {
            Debug.LogError($"잘못된 스테이지 인덱스: {stageIndex}");
            return;
        }
        
        StageData stage = stages[stageIndex];
        
        if (!stage.isUnlocked)
        {
            NotiManager.Instance.Show($"Stage {stage.stageNumber}은 아직 해금되지 않았습니다!");
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
                // 전투 시작
                if (GachaManager.Instance != null && GachaManager.Instance.ownedCharacters.Count > 0)
                {
                    BattleManager.Instance.StartBattle(GachaManager.Instance.ownedCharacters[0], stageMonsters);
                    NotiManager.Instance.Show($"Stage {stage.stageNumber} 시작!");
                }
                else
                {
                    NotiManager.Instance.Show("보유한 캐릭터가 없습니다!");
                }
            }
        }
    }

    /// <summary>
    /// UI 효과음 재생
    /// </summary>
    public void PlayUISound(string soundName)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUISound(soundName);
        }
    }

    /// <summary>
    /// 버튼 클릭 효과음 재생
    /// </summary>
    public void PlayButtonClickSound()
    {
        PlayUISound("button_click");
    }

    #endregion
}

/// <summary>
/// UI 패널 열거형
/// </summary>
public enum UIPanel
{
    None,
    Battle,         // 전투
    Gacha,          // 가챠
    Character,      // 캐릭터 관리
    Settings,       // 설정
    Notification,   // 알림
    Confirm,        // 확인 다이얼로그
    StageSelection // 스테이지 선택 패널 추가
} 