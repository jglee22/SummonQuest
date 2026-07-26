using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// 뽑기 결과 패널의 등장/퇴장 애니메이션을 관리하는 UI 컨트롤러
/// </summary>
public class GachaResultUI : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public RectTransform panelTransform;
    public Image characterImage;
    public TextMeshProUGUI characterNameText;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    public void Show(CharacterData data)
    {
        if (data == null)
            return;

        characterImage.sprite = data.portrait;
        characterNameText.text = data.characterName;

        EnsureActiveInHierarchy();
        panelTransform.localScale = Vector3.zero;
        panelTransform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
    }

    public void Hide()
    {
        panelTransform.DOScale(Vector3.zero, 0.2f)
            .OnComplete(() => gameObject.SetActive(false));
    }

    private void EnsureActiveInHierarchy()
    {
        Transform current = transform;
        while (current != null)
        {
            if (!current.gameObject.activeSelf)
                current.gameObject.SetActive(true);
            current = current.parent;
        }
    }
}
