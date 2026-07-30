using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// 뽑기 결과 패널의 등장/퇴장 애니메이션을 관리하는 UI 컨트롤러
/// </summary>
public class GachaResultUI : MonoBehaviour
{
    private static readonly Color NameTextColor = new Color(0.22f, 0.16f, 0.12f, 1f);
    private static readonly Color SummaryTextColor = new Color(0.22f, 0.16f, 0.12f, 1f);

    public CanvasGroup canvasGroup;
    public RectTransform panelTransform;
    public Image characterImage;
    public TextMeshProUGUI characterNameText;
    public TextMeshProUGUI summaryText;

    private bool layoutApplied;
    private RectTransform animateTarget;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    public void Show(CharacterData data, string summaryMessage = null)
    {
        if (data == null)
            return;

        if (NotiManager.Instance != null)
            NotiManager.Instance.HideImmediate();

        EnsureStyledLayout();

        characterImage.sprite = data.portrait;
        characterNameText.text = data.characterName;
        characterNameText.color = NameTextColor;
        ApplySummary(summaryMessage);

        EnsureActiveInHierarchy();
        BringToFront();
        gameObject.SetActive(true);
        RectTransform target = animateTarget != null ? animateTarget : panelTransform;
        target.localScale = Vector3.zero;
        target.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
    }

    public void Hide()
    {
        EnsureStyledLayout();
        RectTransform target = animateTarget != null ? animateTarget : panelTransform;
        target.DOScale(Vector3.zero, 0.2f)
            .OnComplete(() => gameObject.SetActive(false));
    }

    private void EnsureStyledLayout()
    {
        if (layoutApplied)
            return;

        animateTarget = GachaResultPanelStyle.ApplySinglePullLayout(transform, characterImage, characterNameText);
        layoutApplied = true;
    }

    private void ApplySummary(string message)
    {
        EnsureSummaryText();

        if (summaryText == null)
            return;

        summaryText.text = message ?? string.Empty;
        summaryText.gameObject.SetActive(!string.IsNullOrEmpty(message));
    }

    private void EnsureSummaryText()
    {
        if (summaryText != null)
            return;

        Transform found = transform.Find("Summary_Text");
        if (found != null)
        {
            summaryText = found.GetComponent<TextMeshProUGUI>();
            return;
        }

        GameObject summaryObject = new GameObject("Summary_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        summaryObject.transform.SetParent(transform, false);

        RectTransform rect = summaryObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -32f);
        rect.sizeDelta = new Vector2(600f, 48f);

        summaryText = summaryObject.GetComponent<TextMeshProUGUI>();
        summaryText.fontSize = 28;
        summaryText.alignment = TextAlignmentOptions.Center;
        summaryText.color = new Color(0.95f, 0.92f, 0.88f, 1f);
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

    private void BringToFront()
    {
        if (transform.parent != null)
            transform.parent.SetAsLastSibling();

        transform.SetAsLastSibling();
    }
}
