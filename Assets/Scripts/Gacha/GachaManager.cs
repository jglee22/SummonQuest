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

    [Header("가챠 데이터")]
    public CharacterData[] characterPool;

    [Header("UI")]
    public CharacterListUI characterListUI;
    public GachaResultUI gachaResultUI;
    public GachaResult10UI gachaResult10UI;

    private readonly Dictionary<Rarity, float> rarityRates = new Dictionary<Rarity, float>()
    {
        { Rarity.Five, 1f },
        { Rarity.Four, 5f },
        { Rarity.Three, 15f },
        { Rarity.Two, 30f },
        { Rarity.One, 49f }
    };

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

        if (!CurrencyManager.Instance.SpendGold(cost))
        {
            NotiManager.Instance.Show("골드가 부족합니다!");
            return;
        }

        PlayGachaSfx("gacha_pull");
        UpdateGachaState();

        CharacterData selected = PickCharacter();
        if (selected == null)
            return;

        ApplyPullResult(selected);
        gachaResultUI.Show(selected);
    }

    public void DrawTenCharacters()
    {
        int cost = Config.gachaTenCost;

        if (!CurrencyManager.Instance.SpendGold(cost))
        {
            NotiManager.Instance.Show("골드가 부족합니다!");
            return;
        }

        PlayGachaSfx("gacha_10pull");
        UpdateGachaState();

        List<CharacterData> pulledCharacters = new List<CharacterData>();
        List<CharacterData> newCharacters = new List<CharacterData>();

        for (int i = 0; i < 10; i++)
        {
            CharacterData selected = PickCharacter();
            if (selected == null)
                continue;

            pulledCharacters.Add(selected);

            bool alreadyOwned = PlayerInventory.Instance.Characters
                .Any(c => c.characterData.characterName == selected.characterName);
            if (!alreadyOwned)
                newCharacters.Add(selected);

            ApplyPullResult(selected, false);
        }

        RefreshCharacterListUI();
        gachaResult10UI.Show(pulledCharacters, newCharacters);
    }

    private void ApplyPullResult(CharacterData data, bool refreshUi = true)
    {
        PlayerInventory.Instance.TryAddCharacter(data, out bool isDuplicate);

        if (isDuplicate)
        {
            CurrencyManager.Instance.AddGold(Config.duplicateRewardGold);
            NotiManager.Instance.Show($"중복 보상: {Config.duplicateRewardGold:N0} G 지급!");
        }
        else
        {
            NotiManager.Instance.Show("새 캐릭터 획득!");
        }

        if (refreshUi)
            RefreshCharacterListUI();
    }

    private void RefreshCharacterListUI()
    {
        if (characterListUI != null && PlayerInventory.Instance != null)
            characterListUI.ShowOwnedCharacters(PlayerInventory.Instance.Characters);
    }

    private void UpdateGachaState()
    {
        if (GameManager.Instance == null)
            return;

        GameManager.Instance.SetGameState(GameState.Gacha);
        GameManager.Instance.IncrementGachaCount();
    }

    private void PlayGachaSfx(string clipName)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(clipName);
    }

    private Rarity GetRandomRarity()
    {
        float rand = Random.Range(0f, 100f);
        float cumulative = 0f;

        foreach (var kvp in rarityRates.OrderByDescending(k => (int)k.Key))
        {
            cumulative += kvp.Value;
            if (rand <= cumulative)
                return kvp.Key;
        }

        return Rarity.One;
    }

    private CharacterData GetRandomCharacterByRarity(Rarity rarity)
    {
        var candidates = characterPool.Where(c => c.rarity == rarity).ToList();
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
