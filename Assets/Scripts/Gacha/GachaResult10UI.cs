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
    private static readonly Color SummaryTextColor = new Color(0.22f, 0.16f, 0.12f, 1f);

    [Header("슬롯 프리팹")]
    public GameObject resultSlotPrefab;
    public Transform gridParent;

    [Header("애니메이션")]
    public CanvasGroup canvasGroup;
    public RectTransform panelTransform;

    [Header("요약 텍스트")]
    public TextMeshProUGUI summaryText;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    public void Show(List<CharacterData> resultList, List<CharacterData> newCharacters, string summaryMessage = null)
    {
        if (NotiManager.Instance != null)
            NotiManager.Instance.HideImmediate();

        EnsureActiveInHierarchy();
        BringToFront();
        ApplySummary(summaryMessage);

        foreach (Transform child in gridParent)
            Destroy(child.gameObject);

        foreach (CharacterData data in resultList)
        {
            GameObject slot = Instantiate(resultSlotPrefab, gridParent);
            ResultSlotUI ui = slot.GetComponent<ResultSlotUI>();
            bool isNew = newCharacters.Contains(data);
            ui.Set(data, isNew);
        }

        gameObject.SetActive(true);
        panelTransform.localScale = Vector3.zero;

        Sequence seq = DOTween.Sequence();
        seq.Append(panelTransform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack));
    }

    public void Hide()
    {
        Sequence seq = DOTween.Sequence();
        seq.Append(panelTransform.DOScale(Vector3.zero, 0.2f));
        seq.OnComplete(() => gameObject.SetActive(false));
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
        rect.sizeDelta = new Vector2(900f, 48f);

        summaryText = summaryObject.GetComponent<TextMeshProUGUI>();
        summaryText.fontSize = 28;
        summaryText.alignment = TextAlignmentOptions.Center;
        summaryText.color = SummaryTextColor;
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
