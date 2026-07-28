using System.Collections.Generic;

[System.Serializable]
public class OwnedCharacterSaveData
{
    public string characterID;
    public int level;
    public int power;
    public string element;
    public bool isFavorite;
    public int count = 1;
    public int exp;
    public int expToLevelUp = 100;
    public int awakeningLevel;
}

[System.Serializable]
public class StageProgressSaveData
{
    public int stageIndex;
    public bool isCleared;
    public int clearCount;
}

[System.Serializable]
public class SaveWrapper
{
    public const int CurrentSaveVersion = 1;

    public int saveVersion = CurrentSaveVersion;
    public List<OwnedCharacterSaveData> ownedList = new List<OwnedCharacterSaveData>();
    public int playerGold;
    public int highestClearedStage = -1;
    public List<StageProgressSaveData> stageProgress = new List<StageProgressSaveData>();
    public int totalPlayTime;
    public int totalBattles;
    public int totalGachaPulls;
    public string playerName = "플레이어";
    public int totalBattlesWon;
    public int totalBattlesLost;
    public int totalExpGainedAllTime;
    public int totalGoldGainedAllTime;
    public string selectedCharacterId = string.Empty;
}
