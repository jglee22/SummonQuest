using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[DefaultExecutionOrder(-200)]
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private const string SaveFileName = "character_save.json";
    private const string BackupFileName = "character_save.bak";
    private const string TempFileName = "character_save.tmp";

    private string savePath => Path.Combine(Application.persistentDataPath, SaveFileName);
    private string backupPath => Path.Combine(Application.persistentDataPath, BackupFileName);
    private string tempPath => Path.Combine(Application.persistentDataPath, TempFileName);
    private SaveWrapper cachedWrapper;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void OnApplicationQuit()
    {
        FlushSave();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            FlushSave();
    }

    private void FlushSave()
    {
        if (PlayerInventory.Instance != null)
            SaveAllData(PlayerInventory.Instance.Characters);
    }

    public bool HasSaveFile() => File.Exists(savePath) || File.Exists(backupPath);

    public SaveWrapper GetSaveData()
    {
        return LoadWrapper();
    }

    private SaveWrapper LoadWrapper()
    {
        if (cachedWrapper != null)
            return cachedWrapper;

        cachedWrapper = TryLoadFromFile(savePath);
        if (cachedWrapper != null)
            return cachedWrapper;

        cachedWrapper = TryLoadFromFile(backupPath);
        if (cachedWrapper != null)
        {
            Debug.LogWarning("메인 저장 파일을 읽지 못해 백업 파일을 사용합니다.");
            return cachedWrapper;
        }

        cachedWrapper = CreateDefaultWrapper();
        return cachedWrapper;
    }

    private SaveWrapper TryLoadFromFile(string path)
    {
        if (!File.Exists(path))
            return null;

        try
        {
            string json = File.ReadAllText(path);
            if (!TryParseSaveJson(json, out SaveWrapper wrapper))
                return null;

            return wrapper;
        }
        catch (Exception ex)
        {
            Debug.LogError($"저장 파일 로드 실패 ({path}): {ex.Message}");
            return null;
        }
    }

    private bool TryParseSaveJson(string json, out SaveWrapper wrapper)
    {
        wrapper = null;

        if (string.IsNullOrWhiteSpace(json))
            return false;

        wrapper = JsonUtility.FromJson<SaveWrapper>(json);
        if (wrapper == null)
            return false;

        NormalizeWrapper(wrapper);
        return IsValidWrapper(wrapper);
    }

    private bool IsValidWrapper(SaveWrapper wrapper)
    {
        if (wrapper == null)
            return false;

        if (wrapper.saveVersion <= 0)
            return false;

        if (wrapper.ownedList == null || wrapper.stageProgress == null)
            return false;

        if (wrapper.playerGold < 0)
            return false;

        foreach (OwnedCharacterSaveData owned in wrapper.ownedList)
        {
            if (owned == null || string.IsNullOrEmpty(owned.characterID))
                return false;
        }

        return true;
    }

    private static void DeleteFileIfExists(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }

    private bool WriteValidatedSaveFile(string json)
    {
        DeleteFileIfExists(tempPath);

        try
        {
            File.WriteAllText(tempPath, json);

            SaveWrapper validated = TryLoadFromFile(tempPath);
            if (validated == null)
            {
                Debug.LogError("저장 데이터 검증 실패: 임시 파일 내용이 유효하지 않습니다.");
                DeleteFileIfExists(tempPath);
                return false;
            }

            if (File.Exists(savePath))
                File.Replace(tempPath, savePath, backupPath, ignoreMetadataErrors: true);
            else
                File.Move(tempPath, savePath);

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"저장 실패: {ex.Message}");
            DeleteFileIfExists(tempPath);
            return false;
        }
    }

    private SaveWrapper CreateDefaultWrapper()
    {
        SaveWrapper wrapper = new SaveWrapper();
        NormalizeWrapper(wrapper);
        MigrateLegacyStatistics(wrapper);
        return wrapper;
    }

    private void NormalizeWrapper(SaveWrapper wrapper)
    {
        if (wrapper.saveVersion <= 0)
            wrapper.saveVersion = SaveWrapper.CurrentSaveVersion;

        if (wrapper.stageProgress == null)
            wrapper.stageProgress = new List<StageProgressSaveData>();

        if (wrapper.ownedList == null)
            wrapper.ownedList = new List<OwnedCharacterSaveData>();

        MigrateLegacyStatistics(wrapper);
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
            CharacterData data = CharacterDatabase.GetById(saved.characterID);
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
        wrapper.saveVersion = SaveWrapper.CurrentSaveVersion;
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
        string json = JsonUtility.ToJson(wrapper, true);

        if (!TryParseSaveJson(json, out SaveWrapper validatedWrapper))
        {
            Debug.LogError("저장 데이터 검증 실패: 직렬화된 데이터가 유효하지 않습니다.");
            return;
        }

        if (!WriteValidatedSaveFile(json))
            return;

        cachedWrapper = validatedWrapper;
    }
}
