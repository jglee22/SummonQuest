using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// 10연 뽑기 결과를 한 번에 보여주는 연출용 UI
/// </summary>
public class GachaResult10UI : MonoBehaviour
{
    [Header("슬롯 프리팹")]
    public GameObject resultSlotPrefab; // 슬롯 프리팹
    public Transform gridParent;        // 10개 슬롯을 넣을 부모

    [Header("애니메이션")]
    public CanvasGroup canvasGroup;     // 페이드용
    public RectTransform panelTransform; // 팝업 스케일용

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 10개의 캐릭터 데이터를 받아와 UI를 표시
    /// </summary>
    public void Show(List<CharacterData> resultList, List<CharacterData> newCharacters)
    {
        // 초기화
        foreach (Transform child in gridParent)
            Destroy(child.gameObject);

        // 슬롯 생성
        foreach (CharacterData data in resultList)
        {
            GameObject slot = Instantiate(resultSlotPrefab, gridParent);
            ResultSlotUI ui = slot.GetComponent<ResultSlotUI>();

            bool isNew = newCharacters.Contains(data); // 새로 획득한 캐릭터인지 확인
            ui.Set(data, isNew);
        }

        // 연출
        gameObject.SetActive(true);
        panelTransform.localScale = Vector3.zero;

        Sequence seq = DOTween.Sequence();
        seq.Append(panelTransform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack));
    }

    /// <summary>
    /// 닫기 애니메이션 + 패널 숨기기
    /// </summary>
    public void Hide()
    {
        Sequence seq = DOTween.Sequence();
        seq.Append(panelTransform.DOScale(Vector3.zero, 0.2f));
        seq.OnComplete(() => gameObject.SetActive(false));
    }
}
