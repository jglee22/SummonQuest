using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 1연·10연 가챠 결과 패널의 배경·카드·레이아웃을 런타임에 통일 적용한다.
/// </summary>
public static class GachaResultPanelStyle
{
    private static readonly Color OverlayColor = new Color(0.08f, 0.07f, 0.1f, 0.78f);
    private static readonly Color SummaryOnOverlayColor = new Color(0.95f, 0.92f, 0.88f, 1f);
    private static readonly Color NameTextColor = new Color(0.22f, 0.16f, 0.12f, 1f);

    public static RectTransform ApplySinglePullLayout(
        Transform root,
        Image characterImage,
        TextMeshProUGUI characterNameText)
    {
        ApplySharedOverlay(root);

        RectTransform card = EnsureContentCard(root, new Vector2(480f, 520f));
        ReparentToCard(card, characterImage != null ? characterImage.transform : null);
        ReparentToCard(card, characterNameText != null ? characterNameText.transform : null);
        ReparentToCard(card, root.Find("Close_Btn"));

        LayoutCenteredElement(characterImage != null ? characterImage.rectTransform : null, new Vector2(0f, 56f), new Vector2(180f, 180f));
        if (characterImage != null)
            characterImage.preserveAspect = true;

        LayoutCenteredElement(characterNameText != null ? characterNameText.rectTransform : null, new Vector2(0f, -92f), new Vector2(360f, 52f));
        if (characterNameText != null)
        {
            characterNameText.alignment = TextAlignmentOptions.Center;
            characterNameText.fontSize = 32f;
            characterNameText.color = NameTextColor;
        }

        Transform closeButton = card.Find("Close_Btn");
        LayoutCenteredElement(closeButton != null ? closeButton as RectTransform : null, new Vector2(0f, -208f), new Vector2(180f, 48f));
        if (closeButton != null && closeButton.TryGetComponent(out Button button))
            KenneyUITheme.ApplyButton(button);

        StyleSummaryText(root);
        return card;
    }

    public static RectTransform ApplyTenPullLayout(Transform root, Transform gridParent)
    {
        ApplySharedOverlay(root);

        RectTransform card = EnsureContentCard(root, new Vector2(780f, 520f));
        ReparentToCard(card, gridParent);
        ReparentToCard(card, root.Find("Close_Btn"));

        if (gridParent is RectTransform gridRect)
        {
            gridRect.anchorMin = gridRect.anchorMax = new Vector2(0.5f, 0.5f);
            gridRect.pivot = new Vector2(0.5f, 0.5f);
            gridRect.anchoredPosition = new Vector2(0f, 24f);
            gridRect.sizeDelta = new Vector2(630f, 320f);
        }

        Transform closeButton = card.Find("Close_Btn");
        LayoutCenteredElement(closeButton != null ? closeButton as RectTransform : null, new Vector2(0f, -220f), new Vector2(180f, 48f));
        if (closeButton != null && closeButton.TryGetComponent(out Button button))
            KenneyUITheme.ApplyButton(button);

        StyleSummaryText(root);
        return card;
    }

    private static void ApplySharedOverlay(Transform root)
    {
        Image rootImage = root.GetComponent<Image>();
        if (rootImage != null)
        {
            rootImage.sprite = null;
            rootImage.color = new Color(1f, 1f, 1f, 0f);
            rootImage.raycastTarget = false;
        }

        Transform background = root.Find("Background");
        if (background == null)
            return;

        if (!background.TryGetComponent(out Image overlayImage))
            return;

        overlayImage.sprite = null;
        overlayImage.color = OverlayColor;
        overlayImage.raycastTarget = true;
    }

    private static RectTransform EnsureContentCard(Transform root, Vector2 size)
    {
        Transform existing = root.Find("ContentCard");
        if (existing != null)
        {
            RectTransform existingRect = existing as RectTransform;
            existingRect.sizeDelta = size;
            return existingRect;
        }

        GameObject cardObject = new GameObject("ContentCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        cardObject.transform.SetParent(root, false);
        cardObject.transform.SetSiblingIndex(1);

        RectTransform cardRect = cardObject.GetComponent<RectTransform>();
        cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.sizeDelta = size;

        Image cardImage = cardObject.GetComponent<Image>();
        KenneyUITheme.ApplyPanel(cardImage);

        return cardRect;
    }

    private static void ReparentToCard(RectTransform card, Transform child)
    {
        if (card == null || child == null || child.parent == card)
            return;

        child.SetParent(card, false);
    }

    private static void LayoutCenteredElement(RectTransform rect, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        if (rect == null)
            return;

        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private static void StyleSummaryText(Transform root)
    {
        Transform summary = root.Find("Summary_Text");
        if (summary == null || !summary.TryGetComponent(out TextMeshProUGUI summaryText))
            return;

        summaryText.color = SummaryOnOverlayColor;
    }
}
