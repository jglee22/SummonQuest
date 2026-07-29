using System.Collections.Generic;
using UnityEngine;

public class BattleCombatResolver
{
    private readonly GameConfig config;
    private readonly BattleStatusEffectProcessor statusProcessor;

    public BattleCombatResolver(GameConfig config, BattleStatusEffectProcessor statusProcessor)
    {
        this.config = config;
        this.statusProcessor = statusProcessor;
    }

    public int GetPlayerAttackPower(OwnedCharacter owner, List<BattleStatusEffect> statusEffects)
    {
        if (owner == null)
            return 0;

        int attack = owner.AttackPower;

        foreach (BattleStatusEffect effect in statusEffects)
        {
            if (!effect.IsExpired && effect.attackBonus > 0)
                attack += effect.attackBonus;
        }

        return attack;
    }

    public int CalculateDamage(
        int baseDamage,
        string attackerElement,
        string defenderElement,
        string attackerName,
        string defenderName,
        BattleTurnResult result)
    {
        float multiplier = ElementHelper.GetDamageMultiplier(attackerElement, defenderElement);
        int damage = Mathf.Max(1, Mathf.RoundToInt(baseDamage * multiplier));
        string matchupMessage = ElementHelper.GetMatchupMessage(attackerElement, defenderElement);

        if (!string.IsNullOrEmpty(matchupMessage))
            result.AddMessage($"({attackerName} → {defenderName}) {matchupMessage}");

        return damage;
    }

    public int PickSkillIndex(OwnedCharacter owner, float skillUseChance)
    {
        if (owner?.characterData?.skills == null)
            return -1;

        List<int> availableSkills = new List<int>();
        for (int i = 0; i < owner.characterData.skills.Length; i++)
        {
            if (owner.CanUseSkill(i))
                availableSkills.Add(i);
        }

        if (availableSkills.Count == 0 || Random.Range(0f, 1f) >= skillUseChance)
            return -1;

        return availableSkills[Random.Range(0, availableSkills.Count)];
    }

    public static bool CanUseSkillInBattle(SkillData skill)
    {
        if (skill == null)
            return false;

        switch (skill.skillType)
        {
            case SkillType.Attack:
            case SkillType.Heal:
            case SkillType.Buff:
                return true;
            case SkillType.Debuff:
            case SkillType.Status:
                return skill.baseDamage > 0
                    || skill.statusEffect == StatusEffectType.Poison
                    || skill.statusEffect == StatusEffectType.Burn
                    || skill.statusEffect == StatusEffectType.Bleed
                    || skill.statusEffect == StatusEffectType.Stun
                    || skill.statusEffect == StatusEffectType.Freeze;
            default:
                return false;
        }
    }

    public BattleTurnResult ApplyNormalAttack(
        BattleState state,
        OwnedCharacter owner,
        CharacterData playerCharacter,
        MonsterData monsterData)
    {
        BattleTurnResult result = new BattleTurnResult();
        int baseDamage = GetPlayerAttackPower(owner, state.PlayerStatusEffects);
        int damage = CalculateDamage(
            baseDamage,
            playerCharacter.element,
            monsterData.element,
            playerCharacter.characterName,
            monsterData.monsterName,
            result);

        state.MonsterHP -= damage;
        result.AddMessage($"{playerCharacter.characterName}의 공격! {damage} 데미지");

        if (state.MonsterHP <= 0)
            result.MonsterDefeated = true;

        return result;
    }

    public BattleTurnResult ApplyMonsterAttack(
        BattleState state,
        CharacterData playerCharacter,
        MonsterData monsterData)
    {
        BattleTurnResult result = new BattleTurnResult();
        int damage = CalculateDamage(
            monsterData.attack,
            monsterData.element,
            playerCharacter.element,
            monsterData.monsterName,
            playerCharacter.characterName,
            result);

        state.PlayerHP -= damage;
        result.AddMessage($"{monsterData.monsterName}의 반격! {damage} 데미지");

        if (state.PlayerHP <= 0)
            result.PlayerDefeated = true;

        return result;
    }

    public BattleTurnResult ApplySkill(
        SkillData skill,
        BattleState state,
        OwnedCharacter owner,
        CharacterData playerCharacter,
        MonsterData monsterData)
    {
        BattleTurnResult result = new BattleTurnResult();

        switch (skill.skillType)
        {
            case SkillType.Attack:
                int baseDamage = GetPlayerAttackPower(owner, state.PlayerStatusEffects)
                    + skill.baseDamage
                    + GetSkillLevelBonus(skill, owner);
                int damage = CalculateDamage(
                    baseDamage,
                    playerCharacter.element,
                    monsterData.element,
                    playerCharacter.characterName,
                    monsterData.monsterName,
                    result);
                state.MonsterHP -= damage;
                result.AddMessage($"{skill.skillName}으로 {damage} 데미지!");
                break;

            case SkillType.Heal:
                int healAmount = skill.healAmount + GetSkillLevelBonus(skill, owner);
                state.PlayerHP = Mathf.Min(state.PlayerHP + healAmount, state.PlayerMaxHP);
                result.AddMessage($"{skill.skillName}으로 {healAmount} 체력 회복!");
                break;

            case SkillType.Buff:
                int attackBonus = skill.baseDamage + GetSkillLevelBonus(skill, owner);
                int buffDuration = skill.statusDuration > 0 ? skill.statusDuration : config.defaultStatusDuration;
                statusProcessor.ApplyAttackBuff(state.PlayerStatusEffects, attackBonus, buffDuration);
                result.AddMessage($"{skill.skillName}으로 공격력 +{attackBonus}! ({buffDuration}턴)");
                break;

            case SkillType.Debuff:
                statusProcessor.ApplyStatusEffect(
                    state.MonsterStatusEffects,
                    skill.statusEffect,
                    skill.statusDuration,
                    skill.baseDamage,
                    monsterData.monsterName,
                    result);
                result.AddMessage($"{skill.skillName}으로 {monsterData.monsterName}을(를) 약화!");
                break;

            case SkillType.Status:
                statusProcessor.ApplyStatusEffect(
                    state.MonsterStatusEffects,
                    skill.statusEffect,
                    skill.statusDuration,
                    skill.baseDamage,
                    monsterData.monsterName,
                    result);
                break;
        }

        if (skill.statusEffect != StatusEffectType.None
            && skill.skillType != SkillType.Debuff
            && skill.skillType != SkillType.Status
            && Random.Range(0f, 1f) < skill.statusChance)
        {
            statusProcessor.ApplyStatusEffect(
                state.MonsterStatusEffects,
                skill.statusEffect,
                skill.statusDuration,
                skill.baseDamage,
                monsterData.monsterName,
                result);
        }

        if (state.MonsterHP <= 0)
            result.MonsterDefeated = true;

        return result;
    }

    private static int GetSkillLevelBonus(SkillData skill, OwnedCharacter owner)
    {
        if (skill == null || owner == null)
            return 0;

        return Mathf.RoundToInt(owner.level * skill.effectMultiplier);
    }
}
