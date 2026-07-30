using System.Collections.Generic;

public class BattleStatusEffectProcessor
{
    private readonly GameConfig config;

    public BattleStatusEffectProcessor(GameConfig config)
    {
        this.config = config;
    }

    public bool ShouldSkipTurn(List<BattleStatusEffect> effects)
    {
        foreach (BattleStatusEffect effect in effects)
        {
            if (!effect.IsExpired && effect.skipTurn)
                return true;
        }

        return false;
    }

    public BattleTurnResult ProcessDotDamage(BattleState state, bool isPlayer, string targetName)
    {
        BattleTurnResult result = new BattleTurnResult();
        List<BattleStatusEffect> effects = isPlayer ? state.PlayerStatusEffects : state.MonsterStatusEffects;

        foreach (BattleStatusEffect effect in effects)
        {
            if (effect.IsExpired || effect.damagePerTurn <= 0)
                continue;

            if (isPlayer)
                state.PlayerHP -= effect.damagePerTurn;
            else
                state.MonsterHP -= effect.damagePerTurn;

            result.AddMessage($"{targetName} - {effect.type} {effect.damagePerTurn} 데미지");
        }

        if (isPlayer && state.PlayerHP <= 0)
            result.PlayerDefeated = true;
        else if (!isPlayer && state.MonsterHP <= 0)
            result.MonsterDefeated = true;

        return result;
    }

    public void ApplyStatusEffect(
        List<BattleStatusEffect> targetEffects,
        StatusEffectType type,
        int duration,
        int skillDamage,
        string targetName,
        BattleTurnResult result)
    {
        if (type == StatusEffectType.None)
            return;

        int effectDuration = duration > 0 ? duration : config.defaultStatusDuration;
        int dotDamage = GetStatusDotDamage(type, skillDamage);
        targetEffects.Add(new BattleStatusEffect(type, effectDuration, dotDamage));
        result.AddMessage($"{targetName}에게 {type} 상태이상 적용! ({effectDuration}턴)");
    }

    public void ApplyAttackBuff(List<BattleStatusEffect> targetEffects, int attackBonus, int duration)
    {
        if (attackBonus <= 0 || duration <= 0)
            return;

        targetEffects.Add(new BattleStatusEffect(duration, attackBonus));
    }

    public void EndPlayerTurn(BattleState state, OwnedCharacter owner)
    {
        if (owner != null)
        {
            int excludeSkillIndex = state.PlayerSkillUsedThisTurn ? state.PlayerLastUsedSkillIndex : -1;
            owner.OnTurnEnd(excludeSkillIndex);
            state.PlayerSkillUsedThisTurn = false;
            state.PlayerLastUsedSkillIndex = -1;
        }

        AdvanceStatusEffectTurns(state.PlayerStatusEffects);
    }

    public void EndMonsterTurn(BattleState state)
    {
        AdvanceStatusEffectTurns(state.MonsterStatusEffects);
    }

    private int GetStatusDotDamage(StatusEffectType type, int skillDamage)
    {
        switch (type)
        {
            case StatusEffectType.Poison: return config.poisonDamagePerTurn;
            case StatusEffectType.Burn: return config.burnDamagePerTurn;
            case StatusEffectType.Bleed: return config.bleedDamagePerTurn;
            default: return skillDamage > 0 ? skillDamage : 0;
        }
    }

    private static void AdvanceStatusEffectTurns(List<BattleStatusEffect> effects)
    {
        foreach (BattleStatusEffect effect in effects)
            effect.TickTurn();

        effects.RemoveAll(effect => effect.IsExpired);
    }
}
