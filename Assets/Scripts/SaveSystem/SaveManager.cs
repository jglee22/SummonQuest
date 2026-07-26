using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private string savePath => Path.Combine(Application.persistentDataPath, "character_save.json");
    private SaveWrapper cachedWrapper;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public bool HasSaveFile() => File.Exists(savePath);

    public SaveWrapper GetSaveData()
    {
        return LoadWrapper();
    }

    private SaveWrapper LoadWrapper()
    {
        if (cachedWrapper != null)
            return cachedWrapper;

        if (!File.Exists(savePath))
        {
            cachedWrapper = new SaveWrapper();
            MigrateLegacyStatistics(cachedWrapper);
            return cachedWrapper;
        }

        string json = File.ReadAllText(savePath);
        cachedWrapper = JsonUtility.FromJson<SaveWrapper>(json) ?? new SaveWrapper();

        if (cachedWrapper.stageProgress == null)
            cachedWrapper.stageProgress = new List<StageProgressSaveData>();

        if (cachedWrapper.ownedList == null)
            cachedWrapper.ownedList = new List<OwnedCharacterSaveData>();

        MigrateLegacyStatistics(cachedWrapper);
        return cachedWrapper;
    }

    private void MigrateLegacyStatistics(SaveWrapper wrapper)
    {
        if (wrapper.totalBattles > 0 || wrapper.totalGachaPulls > 0)
            return;

        wrapper.totalPlayTime = PlayerPrefs.GetInt("TotalPlayTime", wrapper.totalPlayTime);
        wrapper.totalBattles = PlayerPrefs.GetInt("TotalBattles", wrapper.totalBattles);
        wrapper.totalGachaPulls = PlayerPrefs.GetInt("TotalGachaPulls", wrapper.totalGachaPulls);
        wrapper.playerName = PlayerPrefs.GetString("PlayerName", wrapper.playerName);
        wrapper.highestClearedStage = PlayerPrefs.GetInt("HighestClearedStage", wrapper.highestClearedStage);
    }

    public List<OwnedCharacter> LoadOwnedCharacters()
    {
        SaveWrapper wrapper = LoadWrapper();
        List<OwnedCharacter> loadedList = new List<OwnedCharacter>();

        foreach (var saved in wrapper.ownedList)
        {
            CharacterData data = Resources.Load<CharacterData>($"CharacterData/{saved.characterID}");
            if (data == null)
            {
                Debug.LogWarning($"CharacterData {saved.characterID} 를 찾을 수 없습니다.");
                continue;
            }

            var owned = new OwnedCharacter(data, saved.level, saved.power)
            {
                element = saved.element,
                isFavorite = saved.isFavorite,
                count = saved.count > 0 ? saved.count : 1,
                exp = saved.exp,
                expToLevelUp = saved.expToLevelUp > 0 ? saved.expToLevelUp : 100,
                awakeningLevel = saved.awakeningLevel
            };
            loadedList.Add(owned);
        }

        return loadedList;
    }

    private SaveWrapper BuildCurrentSaveData(List<OwnedCharacter> ownedCharacters, string selectedCharacterId = null)
    {
        SaveWrapper wrapper = LoadWrapper();
        wrapper.ownedList.Clear();

        foreach (var owned in ownedCharacters)
        {
            if (owned?.characterData == null)
                continue;

            wrapper.ownedList.Add(new OwnedCharacterSaveData
            {
                characterID = owned.characterData.characterID,
                level = owned.level,
                power = owned.power,
                element = owned.element,
                isFavorite = owned.isFavorite,
                count = owned.count,
                exp = owned.exp,
                expToLevelUp = owned.expToLevelUp,
                awakeningLevel = owned.awakeningLevel
            });
        }

        if (!string.IsNullOrEmpty(selectedCharacterId))
            wrapper.selectedCharacterId = selectedCharacterId;
        else if (PlayerInventory.Instance != null)
            wrapper.selectedCharacterId = PlayerInventory.Instance.SelectedCharacterId;

        if (CurrencyManager.Instance != null)
            wrapper.playerGold = CurrencyManager.Instance.GetGold();

        if (StageManager.Instance != null)
            StageManager.Instance.WriteSaveProgress(wrapper);

        if (GameManager.Instance != null)
            GameManager.Instance.WriteSaveStatistics(wrapper);

        if (BattleManager.Instance != null)
        {
            wrapper.totalBattlesWon = BattleManager.Instance.GetTotalBattlesWon();
            wrapper.totalBattlesLost = BattleManager.Instance.GetTotalBattlesLost();
            wrapper.totalExpGainedAllTime = BattleManager.Instance.GetTotalExpGainedAllTime();
            wrapper.totalGoldGainedAllTime = BattleManager.Instance.GetTotalGoldGainedAllTime();
        }

        return wrapper;
    }

    public void SaveOwnedCharactersMerged(List<OwnedCharacter> ownedCharacters)
    {
        SaveAllData(ownedCharacters);
    }

    public void SaveGold()
    {
        if (PlayerInventory.Instance != null)
            SaveAllData(PlayerInventory.Instance.Characters);
    }

    public void SaveAllData(List<OwnedCharacter> ownedCharacters, string selectedCharacterId = null)
    {
        SaveWrapper wrapper = BuildCurrentSaveData(ownedCharacters, selectedCharacterId);
        cachedWrapper = wrapper;

        string json = JsonUtility.ToJson(wrapper, true);
        File.WriteAllText(savePath, json);
    }
}
