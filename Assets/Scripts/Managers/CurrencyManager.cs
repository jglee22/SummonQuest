using UnityEngine;
using TMPro;

/// <summary>
/// 플레이어의 골드를 관리하는 매니저 (추후 다이아 등 추가 가능)
/// </summary>
public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI goldText;

    [Header("Currency")]
    [SerializeField] private int gold = 1000;

    private void Awake()
    {
        // 싱글톤 패턴
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        UpdateGoldUI();
    }

    /// <summary>
    /// 골드를 소비합니다. 성공 시 true 반환
    /// </summary>
    public bool SpendGold(int amount)
    {
        if (gold < amount)
            return false;

        gold -= amount;
        UpdateGoldUI();
        
        // 골드 변경 시 자동 저장
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGold();
        }
        
        return true;
    }

    /// <summary>
    /// 골드를 추가합니다.
    /// </summary>
    public void AddGold(int amount)
    {
        gold += amount;
        UpdateGoldUI();
        
        // 골드 변경 시 자동 저장
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGold();
        }
    }

    /// <summary>
    /// 현재 골드 수량을 반환합니다.
    /// </summary>
    public int GetGold()
    {
        return gold;
    }

    /// <summary>
    /// 골드 수량을 설정합니다.
    /// </summary>
    public void SetGold(int amount)
    {
        gold = amount;
        UpdateGoldUI();
    }

    /// <summary>
    /// 골드 수치 UI 갱신
    /// </summary>
    private void UpdateGoldUI()
    {
        if (goldText != null)
            goldText.text = $"{gold:N0} G";
    }
}
