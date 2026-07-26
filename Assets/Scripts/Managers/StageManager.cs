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
        LoadStageProgress();
        UnlockStagesBasedOnProgress();
    }

    private void LoadStagesFromResources()
    {
        if (allStages != null && allStages.Length > 0)
            return;

        StageData[] loadedStages = Resources.LoadAll<StageData>("StageData");
        allStages = loadedStages.OrderBy(stage => stage.stageNumber).ToArray();

        if (allStages.Length == 0)
            Debug.LogError("Resources/StageData 에 스테이지 데이터가 없습니다.");
    }

    public void ApplySaveProgress(int highestCleared, List<StageProgressSaveData> stageProgress)
    {
        highestClearedStage = highestCleared;

        if (stageProgress != null)
        {
            foreach (var progress in stageProgress)
            {
                if (progress.stageIndex < 0 || progress.stageIndex >= allStages.Length)
                    continue;

                allStages[progress.stageIndex].isCleared = progress.isCleared;
                allStages[progress.stageIndex].clearCount = progress.clearCount;
            }
        }
        else if (allStages != null)
        {
            for (int i = 0; i < allStages.Length; i++)
            {
                allStages[i].isCleared = PlayerPrefs.GetInt($"Stage_{i}_Cleared", 0) == 1;
                allStages[i].clearCount = PlayerPrefs.GetInt($"Stage_{i}_ClearCount", 0);
            }
        }

        UnlockStagesBasedOnProgress();
    }

    public void WriteSaveProgress(SaveWrapper wrapper)
    {
        wrapper.highestClearedStage = highestClearedStage;
        wrapper.stageProgress = new List<StageProgressSaveData>();

        for (int i = 0; i < allStages.Length; i++)
        {
            wrapper.stageProgress.Add(new StageProgressSaveData
            {
                stageIndex = i,
                isCleared = allStages[i].isCleared,
                clearCount = allStages[i].clearCount
            });
        }
    }

    private void LoadStageProgress()
    {
        if (SaveManager.Instance == null)
            return;

        SaveWrapper saveData = SaveManager.Instance.GetSaveData();
        ApplySaveProgress(saveData.highestClearedStage, saveData.stageProgress);
    }

    public void SaveStageProgress()
    {
        if (PlayerInventory.Instance != null)
            PlayerInventory.Instance.Save();
    }

    private void UnlockStagesBasedOnProgress()
    {
        for (int i = 0; i < allStages.Length; i++)
        {
            if (i == 0)
            {
                allStages[i].isUnlocked = true;
                continue;
            }

            allStages[i].isUnlocked = allStages[i - 1].isCleared;
        }
    }

    public void ClearStage(int stageIndex)
    {
        if (stageIndex < 0 || stageIndex >= allStages.Length)
        {
            Debug.LogError($"잘못된 스테이지 인덱스: {stageIndex}");
            return;
        }

        StageData stage = allStages[stageIndex];
        stage.isCleared = true;
        stage.clearCount++;

        if (stageIndex > highestClearedStage)
            highestClearedStage = stageIndex;

        if (stageIndex + 1 < allStages.Length)
            allStages[stageIndex + 1].isUnlocked = true;

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

        if (!allStages[stageIndex].isUnlocked)
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
