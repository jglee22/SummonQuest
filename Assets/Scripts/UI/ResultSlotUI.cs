using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 10연 결과 슬롯에 캐릭터 정보를 표시하는 UI 스크립트
/// </summary>
public class ResultSlotUI : MonoBehaviour
{
    private static readonly Color NameTextColor = new Color(0.22f, 0.16f, 0.12f, 1f);
    private static readonly Color NewMarkTextColor = new Color(0.85f, 0.15f, 0.15f, 1f);

    public Image characterImage;
    public TextMeshProUGUI nameText;
    public GameObject newMark; // NEW 표시 오브젝트

    /// <summary>
    /// 캐릭터 정보 설정
    /// </summary>
    /// <param name="data">캐릭터 데이터</param>
    /// <param name="isNew">NEW 여부</param>
    public void Set(CharacterData data, bool isNew)
    {
        if (data == null)
            return;

        if (characterImage != null)
            characterImage.sprite = data.portrait;

        if (nameText != null)
        {
            nameText.text = data.characterName;
            nameText.color = NameTextColor;
        }

        if (newMark != null)
        {
            newMark.SetActive(isNew);

            TextMeshProUGUI newMarkText = newMark.GetComponentInChildren<TextMeshProUGUI>();
            if (newMarkText != null)
                newMarkText.color = NewMarkTextColor;
        }
    }
}
