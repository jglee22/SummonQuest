using System.Collections.Generic;

public class BattleState
{
    public int PlayerHP;
    public int PlayerMaxHP;
    public int MonsterHP;
    public int TurnCount;
    public bool PlayerSkillUsedThisTurn;

    public readonly List<BattleStatusEffect> PlayerStatusEffects = new List<BattleStatusEffect>();
    public readonly List<BattleStatusEffect> MonsterStatusEffects = new List<BattleStatusEffect>();

    public void Reset(int playerMaxHp, int monsterHp)
    {
        PlayerMaxHP = playerMaxHp;
        PlayerHP = playerMaxHp;
        MonsterHP = monsterHp;
        TurnCount = 0;
        PlayerSkillUsedThisTurn = false;
        PlayerStatusEffects.Clear();
        MonsterStatusEffects.Clear();
    }

    public void ResetMonster(int monsterHp)
    {
        MonsterHP = monsterHp;
        MonsterStatusEffects.Clear();
    }
}
