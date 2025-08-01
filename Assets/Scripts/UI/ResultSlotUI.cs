using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 10연 결과 슬롯에 캐릭터 정보를 표시하는 UI 스크립트
/// </summary>
public class ResultSlotUI : MonoBehaviour
{
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
        characterImage.sprite = data.portrait;
        nameText.text = data.characterName;
        newMark.SetActive(isNew);
    }
}
