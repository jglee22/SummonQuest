using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterDexUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject slotPrefab; // 도감에 표시할 슬롯 프리팹
    [SerializeField] private Transform contentParent; // 슬롯들이 들어갈 부모 오브젝트
    [SerializeField] private Button closeButton;

    private List<CharacterData> allCharacters = new List<CharacterData>();
    private List<OwnedCharacter> ownedCharacters = new List<OwnedCharacter>();

    private void Start()
    {
        closeButton.onClick.AddListener(() => gameObject.SetActive(false));
        gameObject.SetActive(false);
    }

    public void Open(List<OwnedCharacter> ownedList)
    {
        gameObject.SetActive(true);
        ownedCharacters = ownedList;
        LoadAllCharacters();
        CreateSlots();
    }

    private void LoadAllCharacters()
    {
        allCharacters.Clear();
        allCharacters.AddRange(Resources.LoadAll<CharacterData>("CharacterData"));
    }

    private void CreateSlots()
    {
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        foreach (var character in allCharacters)
        {
            GameObject slot = Instantiate(slotPrefab, contentParent);
            CharacterDexSlot dexSlot = slot.GetComponent<CharacterDexSlot>();

            bool isOwned = ownedCharacters.Exists(c => c.characterData.characterID == character.characterID);
            dexSlot.Setup(character, isOwned);
        }
    }
}
