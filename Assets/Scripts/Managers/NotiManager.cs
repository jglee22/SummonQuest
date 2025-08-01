using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections;

/// <summary>
/// 알림 메시지를 표시하는 전역 매니저
/// </summary>
public class NotiManager : MonoBehaviour
{
    public static NotiManager Instance { get; private set; }

    [SerializeField] private TMP_Text notiText;
    [SerializeField] private GameObject notificationPanel;

    private Tween currentTween; // 현재 실행 중인 Tween을 저장

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
    }

    /// <summary>
    /// 알림 표시
    /// </summary>
    public void Show(string message, float duration = 3f)
    {
        // UIManager가 있으면 UIManager의 알림 기능 사용
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowNotification(message, duration);
            return;
        }

        // 기존 방식 (UIManager가 없을 때)
        ShowLegacyNotification(message, duration);
    }

    /// <summary>
    /// 기존 방식 알림 표시
    /// </summary>
    private void ShowLegacyNotification(string message, float duration = 3f)
    {
        // 이전 트윈이 살아있다면 중지
        currentTween?.Kill();
        if (notiText != null)
        {
            notiText.DOKill(); // 혹시 남아 있는 트윈도 제거

            notiText.text = message;
            notiText.alpha = 1f;
            notiText.transform.localScale = Vector3.one * 0.8f;
            notiText.gameObject.SetActive(true);

            // 새로운 시퀀스 생성 및 실행
            Sequence seq = DOTween.Sequence();

            currentTween = seq;
            seq        .Append(notiText.transform.DOScale(1.2f, 0.25f).SetLoops(2, LoopType.Yoyo))
                 .AppendInterval(1.2f)
               .OnComplete(() =>
               {
                   notiText.gameObject.SetActive(false);
                   currentTween = null; // 트윈 종료 후 null 처리
               });
        }
    }
}
