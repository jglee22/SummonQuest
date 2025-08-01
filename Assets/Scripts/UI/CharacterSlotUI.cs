using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 캐릭터 하나의 정보를 UI에 표시해주는 역할 (이름, 이미지, 등급 등)
/// </summary>
public class CharacterSlotUI : MonoBehaviour
{
    [Header("UI 요소")]
    public Image portraitImage;       // 캐릭터 이미지
    public TextMeshProUGUI nameText;  // 캐릭터 이름 텍스트
    public TextMeshProUGUI starText;  // 별 등급 텍스트
    public string element; // 속성 텍스트

    public Button favoriteButton;     // 즐겨찾기 버튼
    public GameObject favoriteOnIcon; // On 상태 아이콘 (⭐ 표시용)
    public GameObject favoriteOffIcon; // Off 상태 아이콘 (빈 별)
    private OwnedCharacter ownedRef;

    private CharacterData characterData;

    private void Start()
    {
        favoriteButton.onClick.RemoveAllListeners();
        favoriteButton.onClick.AddListener(() =>
        {
            ownedRef.isFavorite = !ownedRef.isFavorite;
            UpdateFavoriteIcon();
            if (GachaManager.Instance?.characterListUI != null)
            {
                GachaManager.Instance.characterListUI.ShowOwnedCharacters(GachaManager.Instance.ownedCharacters);
            }
            else
            {
                Debug.LogWarning("characterListUI가 연결되지 않았습니다.");
            }

            if (SaveManager.Instance != null && GachaManager.Instance?.ownedCharacters != null)
            {
                SaveManager.Instance.SaveOwnedCharactersMerged(GachaManager.Instance.ownedCharacters);
            }
            else
            {
                Debug.LogWarning("SaveManager or ownedCharacters is null");
            }
        });
    }
    /// <summary>
    /// 외부에서 캐릭터 정보를 받아와 UI에 표시
    /// </summary>
    public void SetCharacter(OwnedCharacter ownedCharacter, int totalCount = 1)
    {
        characterData = ownedCharacter.characterData;
        nameText.text = characterData.characterName;
        
        // 수량이 1보다 크면 수량 정보도 표시
        if (totalCount > 1)
        {
            starText.text = $"Lv. {ownedCharacter.level}\nPower: {ownedCharacter.power}\n수량: {totalCount}";
        }
        else
        {
            starText.text = $"Lv. {ownedCharacter.level}\nPower: {ownedCharacter.power}";
        }
        
        portraitImage.sprite = characterData.portrait;
        element = ownedCharacter.element;

        ownedRef = ownedCharacter;
        UpdateFavoriteIcon();
    }
    private void UpdateFavoriteIcon()
    {
        favoriteOnIcon.SetActive(ownedRef.isFavorite);
        favoriteOffIcon.SetActive(!ownedRef.isFavorite);

        if(favoriteOnIcon.activeSelf)
        {
            ownedRef.isFavorite = true;
        }
        else
        {
            ownedRef.isFavorite = false;
        }
    }
}
