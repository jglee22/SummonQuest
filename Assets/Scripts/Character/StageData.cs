using UnityEngine;

[CreateAssetMenu(fileName = "StageData", menuName = "SummonTale/Stage")]
public class StageData : ScriptableObject
{
    [Header("기본 정보")]
    public string stageName;           // 스테이지 이름
    public int stageNumber;            // 스테이지 번호
    public string stageDescription;    // 스테이지 설명
    
    [Header("난이도 설정")]
    public int recommendedLevel;       // 권장 레벨
    public float difficultyMultiplier; // 난이도 배수 (1.0 = 기본)
    
    [Header("몬스터 설정")]
    public MonsterData[] normalMonsters;   // 일반 몬스터들
    public MonsterData bossMonster;        // 보스 몬스터 (선택사항)
    public int monsterCount;               // 일반 몬스터 수
    
    [Header("보상 설정")]
    public int baseGoldReward;         // 기본 골드 보상
    public int baseExpReward;          // 기본 경험치 보상
    public int bonusGoldReward;        // 보스 클리어 추가 골드
    public int bonusExpReward;         // 보스 클리어 추가 경험치
    
    [Header("스테이지 상태")]
    public bool isUnlocked = true;     // 스테이지 해금 여부
    public bool isCleared = false;     // 클리어 여부
    public int clearCount = 0;         // 클리어 횟수
    
    /// <summary>
    /// 스테이지 클리어 시 총 골드 보상
    /// </summary>
    public int GetTotalGoldReward()
    {
        int total = baseGoldReward;
        if (bossMonster != null)
            total += bonusGoldReward;
        return total;
    }
    
    /// <summary>
    /// 스테이지 클리어 시 총 경험치 보상
    /// </summary>
    public int GetTotalExpReward()
    {
        int total = baseExpReward;
        if (bossMonster != null)
            total += bonusExpReward;
        return total;
    }
    
    /// <summary>
    /// 스테이지 난이도에 따른 몬스터 능력치 조정
    /// </summary>
    public MonsterData GetAdjustedMonster(MonsterData originalMonster, bool isBoss = false)
    {
        if (originalMonster == null) return null;
        
        // 난이도 배수 적용
        MonsterData adjustedMonster = Instantiate(originalMonster);
        adjustedMonster.maxHP = Mathf.RoundToInt(originalMonster.maxHP * difficultyMultiplier);
        adjustedMonster.attack = Mathf.RoundToInt(originalMonster.attack * difficultyMultiplier);
        
        // 보스 몬스터는 추가 강화
        if (isBoss)
        {
            adjustedMonster.maxHP = Mathf.RoundToInt(adjustedMonster.maxHP * 1.5f);
            adjustedMonster.attack = Mathf.RoundToInt(adjustedMonster.attack * 1.3f);
        }
        
        return adjustedMonster;
    }
} 