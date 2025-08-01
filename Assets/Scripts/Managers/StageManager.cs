using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }
    
    [Header("스테이지 데이터")]
    public StageData[] allStages;      // 모든 스테이지 데이터
    
    [Header("현재 진행 상황")]
    public int currentStageIndex = 0;  // 현재 선택된 스테이지
    public int highestClearedStage = -1; // 가장 높은 클리어 스테이지
    
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    
    private void Start()
    {
        // 테스트용 스테이지 데이터 생성
        CreateTestStages();
        
        LoadStageProgress();
        UnlockStagesBasedOnProgress();
    }
    
    /// <summary>
    /// 테스트용 스테이지 데이터 생성
    /// </summary>
    private void CreateTestStages()
    {
        if (allStages == null || allStages.Length == 0)
        {
            allStages = new StageData[3];
            
            // Stage 1
            allStages[0] = CreateTestStage("초급 던전", 1, "초보자를 위한 간단한 던전입니다.", 1, 1.0f, 3, 100, 50);
            
            // Stage 2
            allStages[1] = CreateTestStage("중급 던전", 2, "경험을 쌓은 모험가를 위한 던전입니다.", 5, 1.5f, 5, 200, 100);
            
            // Stage 3
            allStages[2] = CreateTestStage("고급 던전", 3, "숙련된 모험가만 도전할 수 있는 던전입니다.", 10, 2.0f, 7, 300, 150);
            
            Debug.Log("테스트용 스테이지 데이터 생성 완료");
        }
    }
    
    /// <summary>
    /// 테스트용 스테이지 생성 헬퍼 메서드
    /// </summary>
    private StageData CreateTestStage(string name, int number, string description, int recommendedLevel, 
                                    float difficulty, int monsterCount, int goldReward, int expReward)
    {
        StageData stage = ScriptableObject.CreateInstance<StageData>();
        stage.stageName = name;
        stage.stageNumber = number;
        stage.stageDescription = description;
        stage.recommendedLevel = recommendedLevel;
        stage.difficultyMultiplier = difficulty;
        stage.monsterCount = monsterCount;
        stage.baseGoldReward = goldReward;
        stage.baseExpReward = expReward;
        stage.bonusGoldReward = goldReward / 2;
        stage.bonusExpReward = expReward / 2;
        
        // 테스트용 몬스터 데이터 생성
        stage.normalMonsters = CreateTestMonsters();
        stage.bossMonster = CreateTestBossMonster();
        
        return stage;
    }
    
    /// <summary>
    /// 테스트용 일반 몬스터 생성
    /// </summary>
    private MonsterData[] CreateTestMonsters()
    {
        MonsterData[] monsters = new MonsterData[3];
        
        // 고블린
        monsters[0] = ScriptableObject.CreateInstance<MonsterData>();
        monsters[0].monsterName = "고블린";
        monsters[0].maxHP = 50;
        monsters[0].attack = 10;
        
        // 오크
        monsters[1] = ScriptableObject.CreateInstance<MonsterData>();
        monsters[1].monsterName = "오크";
        monsters[1].maxHP = 80;
        monsters[1].attack = 15;
        
        // 트롤
        monsters[2] = ScriptableObject.CreateInstance<MonsterData>();
        monsters[2].monsterName = "트롤";
        monsters[2].maxHP = 120;
        monsters[2].attack = 20;
        
        return monsters;
    }
    
    /// <summary>
    /// 테스트용 보스 몬스터 생성
    /// </summary>
    private MonsterData CreateTestBossMonster()
    {
        MonsterData boss = ScriptableObject.CreateInstance<MonsterData>();
        boss.monsterName = "드래곤";
        boss.maxHP = 200;
        boss.attack = 30;
        return boss;
    }
    
    /// <summary>
    /// 스테이지 진행도 로드
    /// </summary>
    private void LoadStageProgress()
    {
        highestClearedStage = PlayerPrefs.GetInt("HighestClearedStage", -1);
        
        // 각 스테이지의 클리어 상태 로드
        for (int i = 0; i < allStages.Length; i++)
        {
            string key = $"Stage_{i}_Cleared";
            allStages[i].isCleared = PlayerPrefs.GetInt(key, 0) == 1;
            
            string countKey = $"Stage_{i}_ClearCount";
            allStages[i].clearCount = PlayerPrefs.GetInt(countKey, 0);
        }
        
        Debug.Log($"스테이지 진행도 로드 완료: 최고 클리어 스테이지 {highestClearedStage}");
    }
    
    /// <summary>
    /// 스테이지 진행도 저장
    /// </summary>
    public void SaveStageProgress()
    {
        PlayerPrefs.SetInt("HighestClearedStage", highestClearedStage);
        
        // 각 스테이지의 클리어 상태 저장
        for (int i = 0; i < allStages.Length; i++)
        {
            string key = $"Stage_{i}_Cleared";
            PlayerPrefs.SetInt(key, allStages[i].isCleared ? 1 : 0);
            
            string countKey = $"Stage_{i}_ClearCount";
            PlayerPrefs.SetInt(countKey, allStages[i].clearCount);
        }
        
        PlayerPrefs.Save();
        Debug.Log($"스테이지 진행도 저장 완료: 최고 클리어 스테이지 {highestClearedStage}");
    }
    
    /// <summary>
    /// 진행도에 따른 스테이지 해금
    /// </summary>
    private void UnlockStagesBasedOnProgress()
    {
        for (int i = 0; i < allStages.Length; i++)
        {
            // 첫 번째 스테이지는 항상 해금
            if (i == 0)
            {
                allStages[i].isUnlocked = true;
                continue;
            }
            
            // 이전 스테이지를 클리어했으면 해금
            allStages[i].isUnlocked = allStages[i - 1].isCleared;
        }
    }
    
    /// <summary>
    /// 스테이지 클리어 처리
    /// </summary>
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
        
        // 최고 클리어 스테이지 업데이트
        if (stageIndex > highestClearedStage)
        {
            highestClearedStage = stageIndex;
        }
        
        // 다음 스테이지 해금
        if (stageIndex + 1 < allStages.Length)
        {
            allStages[stageIndex + 1].isUnlocked = true;
        }
        
        // 진행도 저장
        SaveStageProgress();
        
        // 보상 지급
        GiveStageReward(stage);
        
        Debug.Log($"스테이지 {stage.stageName} 클리어! 보상: 골드 {stage.GetTotalGoldReward()}, 경험치 {stage.GetTotalExpReward()}");
    }
    
    /// <summary>
    /// 스테이지 보상 지급
    /// </summary>
    private void GiveStageReward(StageData stage)
    {
        // 골드 보상
        int goldReward = stage.GetTotalGoldReward();
        CurrencyManager.Instance.AddGold(goldReward);
        
        // 경험치 보상 (첫 번째 캐릭터에게)
        int expReward = stage.GetTotalExpReward();
        if (GachaManager.Instance != null && GachaManager.Instance.ownedCharacters.Count > 0)
        {
            GachaManager.Instance.ownedCharacters[0].AddExp(expReward);
        }
        
        // 알림 표시
        NotiManager.Instance.Show($"스테이지 클리어! 골드 +{goldReward}, 경험치 +{expReward}");
    }
    
    /// <summary>
    /// 현재 스테이지의 몬스터 리스트 생성
    /// </summary>
    public List<MonsterData> GetCurrentStageMonsters()
    {
        if (currentStageIndex < 0 || currentStageIndex >= allStages.Length)
            return new List<MonsterData>();
        
        StageData currentStage = allStages[currentStageIndex];
        List<MonsterData> monsters = new List<MonsterData>();
        
        // 일반 몬스터 추가
        for (int i = 0; i < currentStage.monsterCount; i++)
        {
            if (currentStage.normalMonsters.Length > 0)
            {
                MonsterData randomMonster = currentStage.normalMonsters[Random.Range(0, currentStage.normalMonsters.Length)];
                MonsterData adjustedMonster = currentStage.GetAdjustedMonster(randomMonster, false);
                monsters.Add(adjustedMonster);
            }
        }
        
        // 보스 몬스터 추가 (있는 경우)
        if (currentStage.bossMonster != null)
        {
            MonsterData bossMonster = currentStage.GetAdjustedMonster(currentStage.bossMonster, true);
            monsters.Add(bossMonster);
        }
        
        return monsters;
    }
    
    /// <summary>
    /// 스테이지 선택
    /// </summary>
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
        Debug.Log($"스테이지 선택: {allStages[stageIndex].stageName}");
    }
    
    /// <summary>
    /// 현재 스테이지 정보 반환
    /// </summary>
    public StageData GetCurrentStage()
    {
        if (currentStageIndex < 0 || currentStageIndex >= allStages.Length)
            return null;
        
        return allStages[currentStageIndex];
    }
    
    /// <summary>
    /// 모든 스테이지 정보 반환
    /// </summary>
    public StageData[] GetAllStages()
    {
        return allStages;
    }
} 