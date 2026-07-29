using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("스테이지 데이터")]
    public StageData[] allStages;

    [Header("현재 진행 상황")]
    public int currentStageIndex = 0;
    public int highestClearedStage = -1;

    private StageProgress[] progressList;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        LoadStagesFromResources();
        EnsureProgressArray();
        LoadStageProgress();
        UnlockStagesBasedOnProgress();
    }

    private void LoadStagesFromResources()
    {
        if (allStages != null && allStages.Length > 0)
            return;

        StageData[] loadedStages = Resources.LoadAll<StageData>("StageData");
        allStages = loadedStages.OrderBy(stage => stage.stageNumber).ToArray();

        for (int i = 0; i < allStages.Length; i++)
        {
            if (string.IsNullOrEmpty(allStages[i].stageId))
                allStages[i].stageId = allStages[i].name;
        }

        if (allStages.Length == 0)
            Debug.LogError("Resources/StageData 에 스테이지 데이터가 없습니다.");
    }

    private void EnsureProgressArray()
    {
        if (allStages == null || allStages.Length == 0)
            return;

        if (progressList != null && progressList.Length == allStages.Length)
            return;

        progressList = new StageProgress[allStages.Length];
        for (int i = 0; i < progressList.Length; i++)
            progressList[i] = new StageProgress(i == 0, false, 0);
    }

    public StageProgress GetProgress(int stageIndex)
    {
        EnsureProgressArray();

        if (stageIndex < 0 || stageIndex >= progressList.Length)
            return new StageProgress(false, false, 0);

        return progressList[stageIndex];
    }

    public bool IsUnlocked(int stageIndex) => GetProgress(stageIndex).isUnlocked;
    public bool IsCleared(int stageIndex) => GetProgress(stageIndex).isCleared;
    public int GetClearCount(int stageIndex) => GetProgress(stageIndex).clearCount;

    public int GetStageIndex(string stageId)
    {
        if (string.IsNullOrEmpty(stageId) || allStages == null)
            return -1;

        for (int i = 0; i < allStages.Length; i++)
        {
            if (allStages[i] != null && allStages[i].stageId == stageId)
                return i;
        }

        return -1;
    }

    public string GetStageId(int stageIndex)
    {
        if (allStages == null || stageIndex < 0 || stageIndex >= allStages.Length || allStages[stageIndex] == null)
            return string.Empty;

        return allStages[stageIndex].stageId;
    }

    public void ApplySaveProgress(int highestCleared, List<StageProgressSaveData> stageProgress, string highestClearedStageId = null)
    {
        EnsureProgressArray();

        if (!string.IsNullOrEmpty(highestClearedStageId))
        {
            int resolvedIndex = GetStageIndex(highestClearedStageId);
            highestClearedStage = resolvedIndex >= 0 ? resolvedIndex : highestCleared;
        }
        else
        {
            highestClearedStage = highestCleared;
        }

        if (stageProgress != null)
        {
            foreach (StageProgressSaveData progress in stageProgress)
            {
                int stageIndex = !string.IsNullOrEmpty(progress.stageId)
                    ? GetStageIndex(progress.stageId)
                    : progress.stageIndex;

                if (stageIndex < 0 || stageIndex >= progressList.Length)
                    continue;

                progressList[stageIndex].isCleared = progress.isCleared;
                progressList[stageIndex].clearCount = progress.clearCount;
            }
        }
        else
        {
            for (int i = 0; i < progressList.Length; i++)
            {
                progressList[i].isCleared = PlayerPrefs.GetInt($"Stage_{i}_Cleared", 0) == 1;
                progressList[i].clearCount = PlayerPrefs.GetInt($"Stage_{i}_ClearCount", 0);
            }
        }

        UnlockStagesBasedOnProgress();
    }

    public void WriteSaveProgress(SaveWrapper wrapper)
    {
        EnsureProgressArray();
        wrapper.highestClearedStage = highestClearedStage;
        wrapper.highestClearedStageId = GetStageId(highestClearedStage);
        wrapper.stageProgress = new List<StageProgressSaveData>();

        for (int i = 0; i < progressList.Length; i++)
        {
            wrapper.stageProgress.Add(new StageProgressSaveData
            {
                stageId = GetStageId(i),
                stageIndex = i,
                isCleared = progressList[i].isCleared,
                clearCount = progressList[i].clearCount
            });
        }
    }

    private void LoadStageProgress()
    {
        if (SaveManager.Instance == null)
            return;

        SaveWrapper saveData = SaveManager.Instance.GetSaveData();
        ApplySaveProgress(saveData.highestClearedStage, saveData.stageProgress, saveData.highestClearedStageId);
    }

    public void SaveStageProgress()
    {
        if (PlayerInventory.Instance != null)
            PlayerInventory.Instance.Save();
    }

    private void UnlockStagesBasedOnProgress()
    {
        EnsureProgressArray();

        for (int i = 0; i < progressList.Length; i++)
        {
            if (i == 0)
            {
                progressList[i].isUnlocked = true;
                continue;
            }

            progressList[i].isUnlocked = progressList[i - 1].isCleared;
        }
    }

    public void ClearStage(int stageIndex)
    {
        EnsureProgressArray();

        if (stageIndex < 0 || stageIndex >= progressList.Length)
        {
            Debug.LogError($"잘못된 스테이지 인덱스: {stageIndex}");
            return;
        }

        StageProgress progress = progressList[stageIndex];
        progress.isCleared = true;
        progress.clearCount++;

        if (stageIndex > highestClearedStage)
            highestClearedStage = stageIndex;

        if (stageIndex + 1 < progressList.Length)
            progressList[stageIndex + 1].isUnlocked = true;

        SaveStageProgress();
    }

    public List<MonsterData> GetCurrentStageMonsters()
    {
        if (currentStageIndex < 0 || currentStageIndex >= allStages.Length)
            return new List<MonsterData>();

        StageData currentStage = allStages[currentStageIndex];
        List<MonsterData> monsters = new List<MonsterData>();

        for (int i = 0; i < currentStage.monsterCount; i++)
        {
            if (currentStage.normalMonsters.Length > 0)
            {
                MonsterData randomMonster = currentStage.normalMonsters[Random.Range(0, currentStage.normalMonsters.Length)];
                MonsterData adjustedMonster = currentStage.GetAdjustedMonster(randomMonster, false);
                monsters.Add(adjustedMonster);
            }
        }

        if (currentStage.bossMonster != null)
        {
            MonsterData bossMonster = currentStage.GetAdjustedMonster(currentStage.bossMonster, true);
            monsters.Add(bossMonster);
        }

        return monsters;
    }

    public void SelectStage(int stageIndex)
    {
        if (stageIndex < 0 || stageIndex >= allStages.Length)
        {
            Debug.LogError($"잘못된 스테이지 인덱스: {stageIndex}");
            return;
        }

        if (!IsUnlocked(stageIndex))
        {
            NotiManager.Instance.Show("아직 해금되지 않은 스테이지입니다!");
            return;
        }

        currentStageIndex = stageIndex;
    }

    public StageData GetCurrentStage()
    {
        if (currentStageIndex < 0 || currentStageIndex >= allStages.Length)
            return null;

        return allStages[currentStageIndex];
    }

    public StageData[] GetAllStages()
    {
        return allStages;
    }
}
