using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// 알림 메시지를 표시하는 전역 매니저
/// </summary>
public class NotiManager : MonoBehaviour
{
    public static NotiManager Instance { get; private set; }

    [SerializeField] private TMP_Text notiText;
    [SerializeField] private GameObject notificationPanel;

    private static readonly Color NotiTextColor = new Color(0.22f, 0.16f, 0.12f, 1f);
    private static readonly Vector2 ToastSize = new Vector2(560f, 72f);
    private static readonly Vector2 ToastTopOffset = new Vector2(0f, -48f);

    private RectTransform toastCard;
    private bool layoutApplied;
    private Tween currentTween;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (notificationPanel != null)
            notificationPanel.SetActive(false);
    }

    public void HideImmediate()
    {
        currentTween?.Kill();
        currentTween = null;

        if (notiText != null)
        {
            notiText.DOKill();
            notiText.gameObject.SetActive(false);
        }

        if (toastCard != null)
        {
            toastCard.DOKill();
            toastCard.gameObject.SetActive(false);
        }

        if (notificationPanel != null)
            notificationPanel.SetActive(false);
    }

    public void Show(string message, float duration = 3f)
    {
        currentTween?.Kill();
        EnsureToastLayout();

        if (notificationPanel != null)
        {
            EnsureActiveInHierarchy(notificationPanel.transform);
            BringToFront(notificationPanel.transform);
            notificationPanel.SetActive(true);
        }

        if (notiText == null || toastCard == null)
            return;

        notiText.text = message;
        notiText.color = NotiTextColor;
        notiText.alpha = 1f;
        notiText.gameObject.SetActive(true);

        toastCard.DOKill();
        toastCard.localScale = Vector3.one * 0.85f;
        toastCard.gameObject.SetActive(true);

        Sequence seq = DOTween.Sequence();
        currentTween = seq;
        seq.Append(toastCard.DOScale(1f, 0.25f).SetEase(Ease.OutBack))
           .AppendInterval(Mathf.Max(0f, duration - 1.7f))
           .OnComplete(HideNotification);
    }

    private void HideNotification()
    {
        if (notiText != null)
            notiText.gameObject.SetActive(false);

        if (toastCard != null)
            toastCard.gameObject.SetActive(false);

        if (notificationPanel != null)
            notificationPanel.SetActive(false);

        currentTween = null;
    }

    private void EnsureToastLayout()
    {
        if (layoutApplied || notificationPanel == null)
            return;

        layoutApplied = true;

        Image rootImage = notificationPanel.GetComponent<Image>();
        if (rootImage != null)
        {
            rootImage.sprite = null;
            rootImage.color = new Color(1f, 1f, 1f, 0f);
            rootImage.raycastTarget = false;
        }

        Transform existingCard = notificationPanel.transform.Find("ToastCard");
        if (existingCard != null)
        {
            toastCard = existingCard as RectTransform;
        }
        else
        {
            GameObject cardObject = new GameObject("ToastCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            cardObject.transform.SetParent(notificationPanel.transform, false);

            toastCard = cardObject.GetComponent<RectTransform>();
            toastCard.anchorMin = toastCard.anchorMax = new Vector2(0.5f, 1f);
            toastCard.pivot = new Vector2(0.5f, 1f);
            toastCard.anchoredPosition = ToastTopOffset;
            toastCard.sizeDelta = ToastSize;

            KenneyUITheme.ApplyPanel(cardObject.GetComponent<Image>());
        }

        if (notiText == null)
            return;

        notiText.transform.SetParent(toastCard, false);

        RectTransform textRect = notiText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(20f, 8f);
        textRect.offsetMax = new Vector2(-20f, -8f);
        notiText.alignment = TextAlignmentOptions.Center;
        notiText.fontSize = 28f;
        notiText.raycastTarget = false;
    }

    private static void EnsureActiveInHierarchy(Transform target)
    {
        Transform current = target;
        while (current != null)
        {
            if (!current.gameObject.activeSelf)
                current.gameObject.SetActive(true);
            current = current.parent;
        }
    }

    private static void BringToFront(Transform target)
    {
        if (target == null)
            return;

        target.SetAsLastSibling();
    }
}
