using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 캐릭터 하나의 정보를 UI에 표시해주는 역할 (이름, 이미지, 등급 등)
/// </summary>
public class CharacterSlotUI : MonoBehaviour
{
    [Header("UI 요소")]
    public Image portraitImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI starText;
    public string element;

    public Button favoriteButton;
    public GameObject favoriteOnIcon;
    public GameObject favoriteOffIcon;
    public GameObject selectedIndicator;

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
                GachaManager.Instance.characterListUI.ShowOwnedCharacters(PlayerInventory.Instance.Characters);

            PlayerInventory.Instance?.Save();
        });
    }

    public void SetCharacter(OwnedCharacter ownedCharacter, int totalCount = 1, bool isSelected = false)
    {
        characterData = ownedCharacter.characterData;
        string selectedPrefix = isSelected ? "[출전] " : string.Empty;
        nameText.text = selectedPrefix + characterData.characterName;

        string awakeningText = ownedCharacter.awakeningLevel > 0 ? $"\n각성 {ownedCharacter.awakeningLevel}" : string.Empty;

        if (totalCount > 1)
            starText.text = $"Lv. {ownedCharacter.level}\nPower: {ownedCharacter.power}\n수량: {totalCount}{awakeningText}";
        else
            starText.text = $"Lv. {ownedCharacter.level}\nPower: {ownedCharacter.power}{awakeningText}";

        portraitImage.sprite = characterData.portrait;
        element = ownedCharacter.element;

        ownedRef = ownedCharacter;
        UpdateFavoriteIcon();
        UpdateSelectedIndicator(isSelected);
    }

    private void UpdateFavoriteIcon()
    {
        favoriteOnIcon.SetActive(ownedRef.isFavorite);
        favoriteOffIcon.SetActive(!ownedRef.isFavorite);
        ownedRef.isFavorite = favoriteOnIcon.activeSelf;
    }

    private void UpdateSelectedIndicator(bool isSelected)
    {
        if (selectedIndicator != null)
            selectedIndicator.SetActive(isSelected);
    }
}
