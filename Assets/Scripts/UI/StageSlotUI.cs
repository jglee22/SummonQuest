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
    public Color clearedColor = Color.green;   // 클리어된 스테이지 색상
    
    private StageData stageData;
    private int stageIndex;
    
    /// <summary>
    /// 스테이지 데이터 설정
    /// </summary>
    public void SetStageData(StageData data, int index)
    {
        stageData = data;
        stageIndex = index;
        
        UpdateUI();
    }
    
    /// <summary>
    /// UI 업데이트
    /// </summary>
    private void UpdateUI()
    {
        if (stageData == null) return;
        
        // 기본 정보 설정
        stageNumberText.text = $"Stage {stageData.stageNumber}";
        stageNameText.text = stageData.stageName;
        
        // 상태에 따른 UI 업데이트
        if (stageData.isCleared)
        {
            // 클리어된 스테이지
            statusText.text = $"클리어 ({stageData.clearCount}회)";
            backgroundImage.color = clearedColor;
            lockIcon.gameObject.SetActive(false);
            clearIcon.gameObject.SetActive(true);
        }
        else if (stageData.isUnlocked)
        {
            // 해금된 스테이지
            statusText.text = "도전 가능";
            backgroundImage.color = unlockedColor;
            lockIcon.gameObject.SetActive(false);
            clearIcon.gameObject.SetActive(false);
        }
        else
        {
            // 잠긴 스테이지
            statusText.text = "해금 필요";
            backgroundImage.color = lockedColor;
            lockIcon.gameObject.SetActive(true);
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