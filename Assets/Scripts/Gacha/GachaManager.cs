using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 캐릭터 가챠 뽑기와 결과 연출을 담당한다.
/// </summary>
public class GachaManager : MonoBehaviour
{
    public static GachaManager Instance;

    private GameConfig Config => GameConfig.Instance;

    [Header("UI")]
    public CharacterListUI characterListUI;
    public GachaResultUI gachaResultUI;
    public GachaResult10UI gachaResult10UI;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        RefreshCharacterListUI();
    }

    public void DrawCharacter()
    {
        int cost = Config.gachaSingleCost;

        if (!CurrencyManager.Instance.SpendGold(cost, persist: false))
        {
            NotiManager.Instance.Show("골드가 부족합니다!");
            return;
        }

        PlayGachaSfx("gacha_pull");
        UpdateGachaState(persist: false);

        CharacterData selected = PickCharacter();
        if (selected == null)
            return;

        PullResultType result = ApplyPullResult(selected, refreshUi: false, showNotification: false, persist: false);
        CommitGachaSession();
        gachaResultUI.Show(selected, BuildSinglePullSummary(result));
    }

    public void DrawTenCharacters()
    {
        int cost = Config.gachaTenCost;

        if (!CurrencyManager.Instance.SpendGold(cost, persist: false))
        {
            NotiManager.Instance.Show("골드가 부족합니다!");
            return;
        }

        PlayGachaSfx("gacha_10pull");
        UpdateGachaState(persist: false);

        List<GachaPullResult> results = new List<GachaPullResult>();

        for (int i = 0; i < 10; i++)
        {
            CharacterData selected = PickCharacter();
            if (selected == null)
                continue;

            PullResultType result = ApplyPullResult(selected, refreshUi: false, showNotification: false, persist: false);
            results.Add(new GachaPullResult(
                selected,
                result == PullResultType.New,
                result == PullResultType.Duplicate));
        }

        CommitGachaSession();

        int newCount = results.Count(r => r.isNew);
        int duplicateCount = results.Count(r => r.isDuplicate);
        gachaResult10UI.Show(results, BuildTenPullSummary(newCount, duplicateCount));
    }

    public enum PullResultType
    {
        New,
        Duplicate
    }

    private void CommitGachaSession()
    {
        PlayerInventory.Instance.Save();
        RefreshCharacterListUI();
    }

    private string BuildSinglePullSummary(PullResultType result)
    {
        return result == PullResultType.Duplicate
            ? $"중복 보상: {Config.duplicateRewardGold:N0} G 지급!"
            : "새 캐릭터 획득!";
    }

    private string BuildTenPullSummary(int newCount, int duplicateCount)
    {
        if (newCount > 0 && duplicateCount > 0)
            return $"10연 완료! 신규 {newCount}명, 중복 {duplicateCount}명";

        if (newCount > 0)
            return $"10연 완료! 신규 {newCount}명 획득!";

        if (duplicateCount > 0)
            return $"10연 완료! 중복 {duplicateCount}명 - 골드 지급";

        return "10연 완료!";
    }

    private PullResultType ApplyPullResult(CharacterData data, bool refreshUi = true, bool showNotification = true, bool persist = true)
    {
        PlayerInventory.Instance.TryAddCharacter(data, out bool isDuplicate, persist);

        if (isDuplicate)
        {
            CurrencyManager.Instance.AddGold(Config.duplicateRewardGold, persist);

            if (showNotification)
                NotiManager.Instance.Show($"중복 보상: {Config.duplicateRewardGold:N0} G 지급!");

            if (refreshUi)
                RefreshCharacterListUI();

            return PullResultType.Duplicate;
        }

        if (showNotification)
            NotiManager.Instance.Show("새 캐릭터 획득!");

        if (refreshUi)
            RefreshCharacterListUI();

        return PullResultType.New;
    }

    private void RefreshCharacterListUI()
    {
        if (characterListUI != null && PlayerInventory.Instance != null)
            characterListUI.ShowOwnedCharacters(PlayerInventory.Instance.Characters);
    }

    private void UpdateGachaState(bool persist = true)
    {
        if (GameManager.Instance == null)
            return;

        GameManager.Instance.SetGameState(GameState.Gacha);
        GameManager.Instance.IncrementGachaSessionCount(persist);
    }

    private void PlayGachaSfx(string clipName)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(clipName);
    }

    private Rarity GetRandomRarity()
    {
        return GachaTable.Instance.RollRarity();
    }

    private CharacterData GetRandomCharacterByRarity(Rarity rarity)
    {
        var candidates = CharacterDatabase.All.Where(c => c.rarity == rarity).ToList();
        if (candidates.Count == 0)
        {
            Debug.LogWarning($"등급 {rarity} 캐릭터가 없습니다!");
            return null;
        }

        return candidates[Random.Range(0, candidates.Count)];
    }

    private CharacterData PickCharacter()
    {
        return GetRandomCharacterByRarity(GetRandomRarity());
    }
}
