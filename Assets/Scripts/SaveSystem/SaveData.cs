using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class OwnedCharacterSaveData
{
    public string characterID;
    public int level;
    public int power;
    public string element; // Element enum 대신 string 사용
    public bool isFavorite;
}

[System.Serializable]
public class SaveWrapper
{
    public List<OwnedCharacterSaveData> ownedList = new List<OwnedCharacterSaveData>();
    public int playerGold; // 골드 저장 추가
} 