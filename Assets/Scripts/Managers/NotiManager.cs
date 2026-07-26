using UnityEngine;
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

    private Tween currentTween;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (notiText != null)
            notiText.gameObject.SetActive(false);

        if (notificationPanel != null)
            notificationPanel.SetActive(false);
    }

    public void Show(string message, float duration = 3f)
    {
        currentTween?.Kill();

        if (notificationPanel != null)
            EnsureActiveInHierarchy(notificationPanel.transform);

        if (notiText == null)
            return;

        notiText.DOKill();
        notiText.text = message;
        notiText.alpha = 1f;
        notiText.transform.localScale = Vector3.one * 0.8f;
        notiText.gameObject.SetActive(true);

        Sequence seq = DOTween.Sequence();
        currentTween = seq;
        seq.Append(notiText.transform.DOScale(1.2f, 0.25f).SetLoops(2, LoopType.Yoyo))
           .AppendInterval(Mathf.Max(0f, duration - 1.7f))
           .OnComplete(HideNotification);
    }

    private void HideNotification()
    {
        if (notiText != null)
            notiText.gameObject.SetActive(false);

        if (notificationPanel != null)
            notificationPanel.SetActive(false);

        currentTween = null;
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
}
