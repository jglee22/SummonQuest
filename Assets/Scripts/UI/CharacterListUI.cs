using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;
public enum SortType
{
    RarityDescending,    // 등급 높은순
    RarityAscending,     // 등급 낮은순
    LevelDescending,     // 레벨 높은순 (강화순이라고 써도 됨)
    LevelAscending,      // 레벨 낮은순
    NameAscending,       // 이름 오름차순 (가나다순)
    NameDescending,      // 이름 내림차순 (reverse 가나다순)
}
/// <summary>
/// 게임 시작 시 모든 캐릭터 데이터를 불러와 리스트 UI로 표시하는 관리자
/// </summary>
public class CharacterListUI : MonoBehaviour
{
    [Header("참조")]
    public Transform contentParent;          // ScrollView의 Content 오브젝트
    public GameObject characterSlotPrefab;   // 슬롯 프리팹 (CharacterSlotUI가 붙은 프리팹)
    public CharacterDetailUI detailUI; // 인스펙터 연결

    [Header("데이터")]
    public CharacterData[] allCharacters;    // 에디터에서 연결할 캐릭터 ScriptableObject 배열

    public TMP_Dropdown sortDropdown;     // 캐릭터 필터용 Dropdown

    public TMP_Dropdown elementDropdown; // 속성 필터용 Dropdown

    public Toggle favoriteToggle;

    public TMP_InputField searchInputField; // 인스펙터에서 연결
    private string currentSearchText = "";  // 필터링에 사용

    private SortType currentSortType = SortType.RarityDescending;

    private List<OwnedCharacter> ownedCharacters = new List<OwnedCharacter>();
    private string selectedElement = "All";
    private bool showOnlyFavorites = false;

    private void Start()
    {
        sortDropdown.onValueChanged.AddListener(OnSortTypeChanged);
        elementDropdown.onValueChanged.AddListener(OnElementChanged);
        favoriteToggle.onValueChanged.AddListener(OnFavoriteFilterChanged);
        searchInputField.onValueChanged.AddListener(OnSearchValueChanged);
    }

   
    public void ShowOwnedCharacters(List<OwnedCharacter> ownedList)
    {
        // 기존 슬롯 제거
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        ownedCharacters = ownedList; // 리스트 저장
        
        // 디버그: 받은 캐릭터 목록 출력
        Debug.Log($"ShowOwnedCharacters 호출됨 - 받은 캐릭터 수: {ownedList.Count}");
        foreach (var owned in ownedList)
        {
            Debug.Log($"- {owned.characterData.characterName}: count={owned.count}, level={owned.level}");
        }
        
        RefreshCharacterList();      // 정렬된 리스트로 출력
    }
    private void OnSortTypeChanged(int index)
    {
        currentSortType = (SortType)index;
        RefreshCharacterList(); // 다시 그려줌
    }
    private void RefreshCharacterList()
    {
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        // 중복 캐릭터를 그룹화하여 하나의 슬롯으로 표시
        var groupedCharacters = ownedCharacters
            .GroupBy(c => c.characterData)
            .Select(g => new { 
                CharacterData = g.Key, 
                OwnedCharacters = g.ToList(),
                TotalCount = g.Sum(c => c.count),
                MaxLevel = g.Max(c => c.level),
                IsFavorite = g.Any(c => c.isFavorite)
            })
            .ToList();

        // 정렬 적용
        switch (currentSortType)
        {
            case SortType.RarityDescending:
                groupedCharacters = groupedCharacters.OrderByDescending(c => (int)c.CharacterData.rarity).ToList();
                break;
            case SortType.RarityAscending:
                groupedCharacters = groupedCharacters.OrderBy(c => (int)c.CharacterData.rarity).ToList();
                break;
            case SortType.LevelDescending:
                groupedCharacters = groupedCharacters.OrderByDescending(c => c.MaxLevel).ToList();
                break;
            case SortType.LevelAscending:
                groupedCharacters = groupedCharacters.OrderBy(c => c.MaxLevel).ToList();
                break;
            case SortType.NameAscending:
                groupedCharacters = groupedCharacters.OrderBy(c => c.CharacterData.characterName).ToList();
                break;
            case SortType.NameDescending:
                groupedCharacters = groupedCharacters.OrderByDescending(c => c.CharacterData.characterName).ToList();
                break;
        }

        // 즐겨찾기 우선 정렬
        groupedCharacters = groupedCharacters
            .OrderByDescending(c => c.IsFavorite)
            .ThenBy(c => 0)
            .ToList();

        // 속성 필터 적용
        if (selectedElement != "All")
        {
            groupedCharacters = groupedCharacters
                .Where(c => c.CharacterData.element == selectedElement)
                .ToList();
        }

        // ⭐ 즐겨찾기만 보기 필터 적용
        if (showOnlyFavorites)
        {
            groupedCharacters = groupedCharacters
                .Where(c => c.IsFavorite)
                .ToList();
        }

        // 슬롯 생성
        foreach (var grouped in groupedCharacters)
        {
            // ⭐ 이름 검색 필터
            if (!string.IsNullOrEmpty(currentSearchText) &&
                !grouped.CharacterData.characterName.ToLower().Contains(currentSearchText))
            {
                continue;
            }

            // 가장 높은 레벨의 캐릭터를 대표로 사용
            var representative = grouped.OwnedCharacters.OrderByDescending(c => c.level).First();
            
            GameObject slot = Instantiate(characterSlotPrefab, contentParent);
            CharacterSlotUI slotUI = slot.GetComponent<CharacterSlotUI>();
            
            // 수량 정보를 포함하여 설정
            slotUI.SetCharacter(representative, grouped.TotalCount);

            slot.GetComponent<Button>().onClick.AddListener(() =>
            {
                detailUI.Show(representative, ownedCharacters);
            });
        }
    }


    private void OnElementChanged(int index)
    {
        selectedElement = elementDropdown.options[index].text;
        RefreshCharacterList();
    }
    public void OnFavoriteFilterChanged(bool isOn)
    {
        Debug.Log($"[Toggle] 즐겨찾기 토글 상태: {isOn}");
        showOnlyFavorites = isOn;
        RefreshCharacterList();
    }

    public void OnSearchValueChanged(string input)
    {
        currentSearchText = input.ToLower();
        RefreshCharacterList();
    }
}
