using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "SummonQuest/GameConfig")]
public class GameConfig : ScriptableObject
{
    [Header("가챠")]
    public int gachaSingleCost = 300;
    public int gachaTenCost = 2700;
    public int duplicateRewardGold = 300;

    [Header("전투 보상")]
    public int winExpReward = 50;
    public int winGoldReward = 30;
    public float skillUseChance = 0.7f;

    [Header("캐릭터 체력")]
    public int hpPerLevel = 10;

    [Header("속성 상성")]
    public float elementAdvantageMultiplier = 1.5f;
    public float elementDisadvantageMultiplier = 0.75f;

    [Header("각성")]
    public int duplicatesPerAwakening = 1;
    public int attackBonusPerAwakening = 5;
    public int maxLevelBonusPerAwakening = 2;
    public int powerBonusPerAwakening = 15;
    public int maxAwakeningLevel = 5;

    [Header("상태이상")]
    public int defaultStatusDuration = 2;
    public int poisonDamagePerTurn = 5;
    public int burnDamagePerTurn = 8;
    public int bleedDamagePerTurn = 6;

    private static GameConfig instance;

    public static GameConfig Instance
    {
        get
        {
            if (instance == null)
                instance = Resources.Load<GameConfig>("GameConfig");

            if (instance == null)
            {
                instance = CreateInstance<GameConfig>();
                Debug.LogWarning("GameConfig 에셋을 찾을 수 없어 기본값을 사용합니다.");
            }

            return instance;
        }
    }

    public int GetMaxHP(CharacterData data, int level)
    {
        if (data == null)
            return 100;

        return data.baseHP + Mathf.Max(0, level - 1) * hpPerLevel;
    }
}
