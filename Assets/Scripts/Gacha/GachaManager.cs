using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 캐릭터 뽑기(가챠) 시스템 관리 클래스
/// 1회 뽑기 기능과 결과 UI 출력 기능 포함
/// </summary>
public class GachaManager : MonoBehaviour
{
    public static GachaManager Instance;
    private const int DuplicateRewardGold = 300; // 중복 보상 금액

    [Header("가챠 데이터")]
    public CharacterData[] characterPool; // 뽑기 대상 캐릭터 목록

    [Header("UI 요소")]
    public TextMeshProUGUI resultText;    // 뽑기 결과를 출력할 텍스트
    public Image resultPortrait;          // 뽑은 캐릭터 이미지 표시

    // 보유 캐릭터 리스트 (런타임 기준)
    public List<OwnedCharacter> ownedCharacters = new List<OwnedCharacter>();

    public CharacterListUI characterListUI; // 인스펙터에서 연결

    public GachaResultUI gachaResultUI; // 인스펙터에서 필요

    public GachaResult10UI gachaResult10UI; // 인스펙터 연결 필요

    // ★ 등급별 뽑기 확률 설정
    private Dictionary<Rarity, float> rarityRates = new Dictionary<Rarity, float>()
{
    { Rarity.Five, 1f },
    { Rarity.Four, 5f },
    { Rarity.Three, 15f },
    { Rarity.Two, 30f },
    { Rarity.One, 49f }
};
    private void Awake()
    {
        Instance = this;
    }
    private void Start()
    {
        ownedCharacters = SaveManager.Instance.LoadOwnedCharacters();

        // 중복 데이터 정리
        CleanupDuplicateCharacters();

        // UI 갱신
        characterListUI.ShowOwnedCharacters(ownedCharacters);

        Debug.Log("CharacterListUI 연결 상태: " + (characterListUI == null ? "NULL" : "OK"));
        Debug.Log("OwnedCharacters 연결 상태: " + (ownedCharacters == null ? "NULL" : "OK"));
        Debug.Log("SaveManager.Instance 상태: " + (SaveManager.Instance == null ? "NULL" : "OK"));
    }

    /// <summary>
    /// 중복 캐릭터 데이터 정리
    /// </summary>
    private void CleanupDuplicateCharacters()
    {
        var groupedCharacters = ownedCharacters
            .GroupBy(c => c.characterData.characterName)
            .Select(g => new { 
                CharacterName = g.Key, 
                Characters = g.ToList(),
                TotalCount = g.Sum(c => c.count),
                MaxLevel = g.Max(c => c.level),
                IsFavorite = g.Any(c => c.isFavorite)
            })
            .ToList();

        // 정리된 리스트로 교체
        ownedCharacters.Clear();
        
        foreach (var group in groupedCharacters)
        {
            // 가장 높은 레벨의 캐릭터를 대표로 사용
            var representative = group.Characters.OrderByDescending(c => c.level).First();
            representative.count = group.TotalCount;
            representative.isFavorite = group.IsFavorite;
            
            ownedCharacters.Add(representative);
            
            Debug.Log($"캐릭터 정리: {group.CharacterName} - count={group.TotalCount}, level={group.MaxLevel}");
        }
        
        // 정리된 데이터 저장 (캐릭터와 골드 모두 저장)
        SaveManager.Instance.SaveAllData(ownedCharacters);
    }

    /// <summary>
    /// 버튼을 눌렀을 때 실행되는 뽑기 함수
    /// </summary>
    public void DrawCharacter()
    {
        int cost = 300;

        if (!CurrencyManager.Instance.SpendGold(cost))
        {
            NotiManager.Instance.Show("골드가 부족합니다!");
            return;
        }

        // 가챠 효과음 재생
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("gacha_pull");
        }

        // GameManager 상태 변경 및 통계 증가
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameState.Gacha);
            GameManager.Instance.IncrementGachaCount();
        }

        CharacterData selected = PickCharacter();

        if (selected != null)
        {
            AddToOwnedCharacters(selected);

            // 연출 호출
            gachaResultUI.Show(selected);
        }
    }

    /// <summary>
    /// 보유 리스트에 캐릭터 추가 or 중복 처리
    /// </summary>
    private void AddToOwnedCharacters(CharacterData data)
    {
        // 캐릭터 이름으로 중복 체크 (더 안전한 방법)
        OwnedCharacter existing = ownedCharacters.Find(c => c.characterData.characterName == data.characterName);

        if (existing != null)
        {
            // 중복 캐릭터일 경우 보상
            existing.count++;

            CurrencyManager.Instance.AddGold(DuplicateRewardGold);
            NotiManager.Instance.Show($"중복 보상: {DuplicateRewardGold:N0} G 지급!");
            
            Debug.Log($"중복 캐릭터 처리: {data.characterName}, count: {existing.count}");
        }
        else
        {
            // 신규 캐릭터
            ownedCharacters.Add(new OwnedCharacter(data));
            NotiManager.Instance.Show("새 캐릭터 획득!");
            
            Debug.Log($"신규 캐릭터 추가: {data.characterName}");
        }

        // 디버그: 현재 보유 캐릭터 목록 출력
        Debug.Log($"현재 보유 캐릭터 수: {ownedCharacters.Count}");
        foreach (var owned in ownedCharacters)
        {
            Debug.Log($"- {owned.characterData.characterName}: count={owned.count}, level={owned.level}");
        }

        // 보유 리스트 UI 갱신
        characterListUI.ShowOwnedCharacters(ownedCharacters);

        // 캐릭터와 골드 모두 저장
        SaveManager.Instance.SaveAllData(ownedCharacters);
    }

    /// <summary>
    /// 확률 기반으로 캐릭터 1명을 랜덤으로 선택
    /// </summary>
    private CharacterData GetRandomCharacter()
    {
        float total = 0f;

        // 전체 확률 총합 계산
        foreach (var c in characterPool)
            total += c.gachaRate;

        float rand = Random.Range(0f, total);
        float current = 0f;

        foreach (CharacterData c in characterPool)
        {
            current += c.gachaRate;
            if (rand <= current)
                return c;
        }

        return characterPool[characterPool.Length - 1];
    }

    /// <summary>
    /// 10연 뽑기 실행 함수
    /// 캐릭터를 10번 랜덤으로 뽑고 보유 리스트에 반영
    /// </summary>
    public void DrawTenCharacters()
    {
        int cost = 2700;

        if (!CurrencyManager.Instance.SpendGold(cost))
        {
            NotiManager.Instance.Show("골드가 부족합니다!");
            return;
        }

        // 10연 가챠 효과음 재생
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("gacha_10pull");
        }

        // GameManager 상태 변경 및 통계 증가
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameState.Gacha);
            GameManager.Instance.IncrementGachaCount();
        }

        List<CharacterData> pulledCharacters = new List<CharacterData>();
        List<CharacterData> newCharacters = new List<CharacterData>();

        for (int i = 0; i < 10; i++)
        {
            CharacterData selected = PickCharacter();
            if (selected != null)
            {
                pulledCharacters.Add(selected);

                // 보유 리스트에 이미 존재하지 않으면 NEW 처리
                bool alreadyOwned = ownedCharacters.Any(c => c.characterData == selected);
                if (!alreadyOwned)
                {
                    newCharacters.Add(selected);
                }

                AddToOwnedCharacters(selected);
            }
        }

        characterListUI.ShowOwnedCharacters(ownedCharacters);

        // NEW 여부 포함하여 연출 호출
        gachaResult10UI.Show(pulledCharacters, newCharacters);
    }

    private Rarity GetRandomRarity()
    {
        float rand = Random.Range(0f, 100f);
        float cumulative = 0f;

        foreach (var kvp in rarityRates.OrderByDescending(k => (int)k.Key)) // ★ 높은 등급 우선
        {
            cumulative += kvp.Value;
            if (rand <= cumulative)
                return kvp.Key;
        }

        return Rarity.One; // fallback
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
    CharacterData PickCharacter()
    {
        Rarity rarity = GetRandomRarity();
        CharacterData selected = GetRandomCharacterByRarity(rarity);
        return selected;
    }
}
