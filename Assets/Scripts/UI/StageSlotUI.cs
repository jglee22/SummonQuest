using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StageSlotUI : MonoBehaviour
{
    [Header("UI 요소")]
    public TextMeshProUGUI stageNumberText;    // 스테이지 번호
    public TextMeshProUGUI stageNameText;      // 스테이지 이름
    public TextMeshProUGUI statusText;         // 상태 텍스트 (클리어/해금/잠김)
    public Image backgroundImage;              // 배경 이미지
    public Image lockIcon;                     // 잠금 아이콘
    public Image clearIcon;                    // 클리어 아이콘
    
    [Header("색상 설정")]
    public Color unlockedColor = Color.white;  // 해금된 스테이지 색상
    public Color lockedColor = Color.gray;     // 잠긴 스테이지 색상
    public Color clearedColor = new Color(0.75f, 0.95f, 0.75f, 1f);
    
    private static readonly Color SlotTextColor = new Color(0.15f, 0.12f, 0.1f, 1f);
    
    private StageData stageData;
    private int stageIndex;
    private bool layoutInitialized;

    /// <summary>
    /// 스테이지 데이터 설정
    /// </summary>
    public void SetStageData(StageData data, int index)
    {
        stageData = data;
        stageIndex = index;

        EnsureSlotLayout();
        UpdateUI();
    }

    private void EnsureSlotLayout()
    {
        if (layoutInitialized)
            return;

        layoutInitialized = true;

        RectTransform slotRect = transform as RectTransform;
        if (slotRect != null)
        {
            slotRect.anchorMin = new Vector2(0f, 1f);
            slotRect.anchorMax = new Vector2(1f, 1f);
            slotRect.pivot = new Vector2(0.5f, 1f);
            slotRect.sizeDelta = new Vector2(0f, 100f);
        }

        LayoutElement layoutElement = GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = gameObject.AddComponent<LayoutElement>();

        layoutElement.minHeight = 100f;
        layoutElement.preferredHeight = 100f;
        layoutElement.flexibleWidth = 1f;

        StretchText(stageNumberText?.rectTransform, 0.04f, 0.22f);
        StretchText(stageNameText?.rectTransform, 0.24f, 0.62f);
        StretchText(statusText?.rectTransform, 0.64f, 0.98f);

        if (lockIcon != null)
            PlaceIcon(lockIcon.rectTransform, 0.92f);

        if (clearIcon != null)
            clearIcon.gameObject.SetActive(false);
    }

    private static void StretchText(RectTransform rect, float minX, float maxX)
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(minX, 0f);
        rect.anchorMax = new Vector2(maxX, 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void PlaceIcon(RectTransform rect, float anchorX)
    {
        if (rect == null)
            return;

        rect.anchorMin = rect.anchorMax = new Vector2(anchorX, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(32f, 32f);
    }
    
    /// <summary>
    /// UI 업데이트
    /// </summary>
    private void UpdateUI()
    {
        if (stageData == null) return;

        StageProgress progress = StageManager.Instance != null
            ? StageManager.Instance.GetProgress(stageIndex)
            : new StageProgress(false, false, 0);
        
        // 기본 정보 설정
        stageNumberText.text = $"Stage {stageData.stageNumber}";
        stageNameText.text = stageData.stageName;
        stageNumberText.color = SlotTextColor;
        stageNameText.color = SlotTextColor;
        statusText.color = SlotTextColor;
        
        // 상태에 따른 UI 업데이트
        if (progress.isCleared)
        {
            statusText.text = $"클리어 ({progress.clearCount}회)";
            if (backgroundImage != null)
                backgroundImage.color = clearedColor;
            if (lockIcon != null)
                lockIcon.gameObject.SetActive(false);
            if (clearIcon != null)
                clearIcon.gameObject.SetActive(false);
        }
        else if (progress.isUnlocked)
        {
            statusText.text = "도전 가능";
            if (backgroundImage != null)
                backgroundImage.color = unlockedColor;
            if (lockIcon != null)
                lockIcon.gameObject.SetActive(false);
            if (clearIcon != null)
                clearIcon.gameObject.SetActive(false);
        }
        else
        {
            statusText.text = "해금 필요";
            if (backgroundImage != null)
                backgroundImage.color = lockedColor;
            if (lockIcon != null)
                lockIcon.gameObject.SetActive(true);
            if (clearIcon != null)
                clearIcon.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// 스테이지 데이터 반환
    /// </summary>
    public StageData GetStageData()
    {
        return stageData;
    }
    
    /// <summary>
    /// 스테이지 인덱스 반환
    /// </summary>
    public int GetStageIndex()
    {
        return stageIndex;
    }
} 