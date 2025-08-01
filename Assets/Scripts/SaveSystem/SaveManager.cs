using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    private string savePath => Path.Combine(Application.persistentDataPath, "character_save.json");
    
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
  
    public void SaveOwnedCharactersMerged(List<OwnedCharacter> ownedCharacters)
    {
        SaveWrapper existingWrapper = new SaveWrapper { ownedList = new List<OwnedCharacterSaveData>() };

        // 1. 기존 저장 데이터 불러오기
        if (File.Exists(savePath))
        {
            string existingJson = File.ReadAllText(savePath);
            existingWrapper = JsonUtility.FromJson<SaveWrapper>(existingJson);
        }

        // 2. 기존 ID 추출
        HashSet<string> existingIDs = new HashSet<string>();
        foreach (var item in existingWrapper.ownedList)
            existingIDs.Add(item.characterID);

        // 3. 새로운 데이터만 추가
        foreach (var owned in ownedCharacters)
        {
            // 기존 항목이 있으면 제거 (동일 ID)
            existingWrapper.ownedList.RemoveAll(item => item.characterID == owned.characterData.characterID);
            Debug.Log($"owned fav : {owned.isFavorite}");
            // 새 항목 추가
            existingWrapper.ownedList.Add(new OwnedCharacterSaveData
            {
                characterID = owned.characterData.characterID,
                level = owned.level,
                power = owned.power,
                element = owned.element,
                isFavorite = owned.isFavorite,
            });
        }

        // 4. 병합된 리스트를 저장
        string mergedJson = JsonUtility.ToJson(existingWrapper, true);
        File.WriteAllText(savePath, mergedJson);

        Debug.Log($"병합 저장 완료: {savePath}");
    }

    public List<OwnedCharacter> LoadOwnedCharacters()
    {
        List<OwnedCharacter> loadedList = new List<OwnedCharacter>();

        if (!File.Exists(savePath))
        {
            Debug.Log("저장된 캐릭터 파일이 없습니다.");
            return loadedList;
        }

        string json = File.ReadAllText(savePath);
        SaveWrapper wrapper = JsonUtility.FromJson<SaveWrapper>(json);

        foreach (var saved in wrapper.ownedList)
        {
            // ID로 ScriptableObject를 찾아야 함 (Resources 폴더에서 불러오기 가정)
            CharacterData data = Resources.Load<CharacterData>($"CharacterData/{saved.characterID}");
            if (data != null)
            {
                loadedList.Add(new OwnedCharacter(data, saved.level, saved.power)
                {
                    element = saved.element,
                    isFavorite = saved.isFavorite,
                });
            }
            else
            {
                Debug.LogWarning($"CharacterData {saved.characterID} 를 찾을 수 없습니다.");
            }
        }

        // 골드 로드 추가
        if (wrapper.playerGold > 0 && CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.SetGold(wrapper.playerGold);
            Debug.Log($"골드 로드 완료: {wrapper.playerGold}");
        }

        Debug.Log("캐릭터 로드 완료");
        return loadedList;
    }

    /// <summary>
    /// 골드 저장
    /// </summary>
    public void SaveGold()
    {
        if (CurrencyManager.Instance == null) return;

        SaveWrapper existingWrapper = new SaveWrapper { ownedList = new List<OwnedCharacterSaveData>() };

        // 기존 저장 데이터 불러오기
        if (File.Exists(savePath))
        {
            string existingJson = File.ReadAllText(savePath);
            existingWrapper = JsonUtility.FromJson<SaveWrapper>(existingJson);
        }

        // 골드 저장
        existingWrapper.playerGold = CurrencyManager.Instance.GetGold();

        // 저장
        string mergedJson = JsonUtility.ToJson(existingWrapper, true);
        File.WriteAllText(savePath, mergedJson);

        Debug.Log($"골드 저장 완료: {existingWrapper.playerGold}");
    }

    /// <summary>
    /// 캐릭터와 골드 모두 저장
    /// </summary>
    public void SaveAllData(List<OwnedCharacter> ownedCharacters)
    {
        SaveWrapper existingWrapper = new SaveWrapper { ownedList = new List<OwnedCharacterSaveData>() };

        // 1. 기존 저장 데이터 불러오기
        if (File.Exists(savePath))
        {
            string existingJson = File.ReadAllText(savePath);
            existingWrapper = JsonUtility.FromJson<SaveWrapper>(existingJson);
        }

        // 2. 캐릭터 데이터 저장
        foreach (var owned in ownedCharacters)
        {
            // 기존 항목이 있으면 제거 (동일 ID)
            existingWrapper.ownedList.RemoveAll(item => item.characterID == owned.characterData.characterID);
            
            // 새 항목 추가
            existingWrapper.ownedList.Add(new OwnedCharacterSaveData
            {
                characterID = owned.characterData.characterID,
                level = owned.level,
                power = owned.power,
                element = owned.element,
                isFavorite = owned.isFavorite,
            });
        }

        // 3. 골드 저장
        if (CurrencyManager.Instance != null)
        {
            existingWrapper.playerGold = CurrencyManager.Instance.GetGold();
        }

        // 4. 모든 데이터 저장
        string mergedJson = JsonUtility.ToJson(existingWrapper, true);
        File.WriteAllText(savePath, mergedJson);

        Debug.Log($"모든 데이터 저장 완료: 캐릭터 {existingWrapper.ownedList.Count}개, 골드 {existingWrapper.playerGold}");
    }
} 