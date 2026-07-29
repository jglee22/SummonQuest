using UnityEngine;

[System.Serializable]
public class BattleStatusEffect
{
    public StatusEffectType type;
    public int remainingTurns;
    public int damagePerTurn;
    public int attackBonus;
    public bool skipTurn;

    public BattleStatusEffect(StatusEffectType type, int duration, int damagePerTurn = 0)
    {
        this.type = type;
        remainingTurns = duration;
        this.damagePerTurn = damagePerTurn;
        attackBonus = 0;
        skipTurn = type == StatusEffectType.Stun || type == StatusEffectType.Freeze;
    }

    public BattleStatusEffect(int duration, int attackBonus)
    {
        type = StatusEffectType.None;
        remainingTurns = duration;
        damagePerTurn = 0;
        this.attackBonus = attackBonus;
        skipTurn = false;
    }

    public bool IsExpired => remainingTurns <= 0;

    public void TickTurn()
    {
        if (remainingTurns > 0)
            remainingTurns--;
    }
}
