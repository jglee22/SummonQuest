using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterDexSlot : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private GameObject lockOverlay;
    [SerializeField] private TMP_Text nameText;

    public void Setup(CharacterData data, bool isOwned)
    {
        icon.sprite = data.portrait;
        nameText.text = isOwned ? data.characterName : "???";
        icon.color = isOwned ? Color.white : new Color(1, 1, 1, 0.3f); // 비활성화 처리
        lockOverlay.SetActive(!isOwned);
    }
}
