using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;

/// <summary>
/// 뽑기 결과 패널의 등장/퇴장 애니메이션을 관리하는 UI 컨트롤러
/// </summary>
public class GachaResultUI : MonoBehaviour
{
    public CanvasGroup canvasGroup;          // 페이드용
    public RectTransform panelTransform;     // 팝업 확대용
    public Image characterImage;
    public TextMeshProUGUI characterNameText;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 뽑기 결과 패널을 애니메이션과 함께 표시
    /// </summary>
    public void Show(CharacterData data)
    {
        characterImage.sprite = data.portrait;
        characterNameText.text = data.characterName;

        gameObject.SetActive(true);
        panelTransform.localScale = Vector3.zero;

        // 연출은 1 프레임 뒤에 실행
        StartCoroutine(PlayShowAnimation());
    }

    private IEnumerator PlayShowAnimation()
    {
        yield return null; // 1 프레임 대기

        Sequence seq = DOTween.Sequence();
        // seq.Append(canvasGroup.DOFade(1, 0.2f));
        seq.Join(panelTransform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack));
    }

    /// <summary>
    /// 결과 패널 닫기 애니메이션
    /// </summary>
    public void Hide()
    {
        Sequence seq = DOTween.Sequence();
        seq.Append(panelTransform.DOScale(Vector3.zero, 0.2f));
        seq.OnComplete(() => gameObject.SetActive(false));
    }
}
