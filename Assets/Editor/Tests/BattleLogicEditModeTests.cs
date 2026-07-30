using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class BattleLogicEditModeTests
{
    private GameConfig config;
    private BattleStatusEffectProcessor processor;
    private BattleCombatResolver resolver;

    [SetUp]
    public void SetUp()
    {
        config = ScriptableObject.CreateInstance<GameConfig>();
        config.poisonDamagePerTurn = 5;
        processor = new BattleStatusEffectProcessor(config);
        resolver = new BattleCombatResolver(config, processor);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(config);
    }

    [Test]
    public void Cooldown_OtherSkillsDecrease()
    {
        OwnedCharacter owner = CreateOwnerWithTwoSkills(cooldown0: 3, cooldown1: 2);
        BattleState state = new BattleState();
        state.Reset(100, 100);

        Assert.IsTrue(owner.UseSkill(0));
        owner.skillCooldowns[1] = 2;

        state.PlayerSkillUsedThisTurn = true;
        state.PlayerLastUsedSkillIndex = 0;

        processor.EndPlayerTurn(state, owner);

        Assert.AreEqual(3, owner.skillCooldowns[0]);
        Assert.AreEqual(1, owner.skillCooldowns[1]);
        Assert.IsFalse(state.PlayerSkillUsedThisTurn);
        Assert.AreEqual(-1, state.PlayerLastUsedSkillIndex);
    }

    [Test]
    public void Stun_SkipsExactlyOneTurn()
    {
        BattleState state = new BattleState();
        state.Reset(100, 100);
        BattleTurnResult result = new BattleTurnResult();

        processor.ApplyStatusEffect(
            state.PlayerStatusEffects,
            StatusEffectType.Stun,
            duration: 1,
            skillDamage: 0,
            targetName: "Player",
            result);

        Assert.IsTrue(processor.ShouldSkipTurn(state.PlayerStatusEffects));

        processor.EndPlayerTurn(state, null);

        Assert.IsFalse(processor.ShouldSkipTurn(state.PlayerStatusEffects));
        Assert.AreEqual(0, state.PlayerStatusEffects.Count);
    }

    [Test]
    public void Poison_DealsDamageForThreeTurns()
    {
        BattleState state = new BattleState();
        state.Reset(100, 100);
        int initialHp = state.MonsterHP;
        BattleTurnResult result = new BattleTurnResult();

        processor.ApplyStatusEffect(
            state.MonsterStatusEffects,
            StatusEffectType.Poison,
            duration: 3,
            skillDamage: 0,
            targetName: "Monster",
            result);

        int totalDamage = 0;
        for (int turn = 0; turn < 4; turn++)
        {
            int hpBefore = state.MonsterHP;
            processor.ProcessDotDamage(state, isPlayer: false, targetName: "Monster");
            totalDamage += hpBefore - state.MonsterHP;
            processor.EndMonsterTurn(state);
        }

        Assert.AreEqual(15, totalDamage);
        Assert.AreEqual(initialHp - 15, state.MonsterHP);
    }

    [Test]
    public void AttackBuff_AppliesForThreeTurns()
    {
        CharacterData charData = ScriptableObject.CreateInstance<CharacterData>();
        charData.baseAttack = 10;
        charData.baseMana = 50;
        charData.maxMana = 100;
        charData.skills = new SkillData[0];

        OwnedCharacter owner = new OwnedCharacter(charData);
        BattleState state = new BattleState();
        state.Reset(100, 100);

        int baseAttack = owner.AttackPower;
        processor.ApplyAttackBuff(state.PlayerStatusEffects, attackBonus: 10, duration: 3);

        Assert.AreEqual(baseAttack + 10, resolver.GetPlayerAttackPower(owner, state.PlayerStatusEffects));

        processor.EndPlayerTurn(state, owner);
        Assert.AreEqual(1, state.PlayerStatusEffects.Count);
        Assert.AreEqual(3, state.PlayerStatusEffects[0].remainingTurns);
        Assert.AreEqual(baseAttack + 10, resolver.GetPlayerAttackPower(owner, state.PlayerStatusEffects));

        processor.EndPlayerTurn(state, owner);
        Assert.AreEqual(2, state.PlayerStatusEffects[0].remainingTurns);
        Assert.AreEqual(baseAttack + 10, resolver.GetPlayerAttackPower(owner, state.PlayerStatusEffects));

        processor.EndPlayerTurn(state, owner);
        Assert.AreEqual(1, state.PlayerStatusEffects[0].remainingTurns);

        processor.EndPlayerTurn(state, owner);
        Assert.AreEqual(0, state.PlayerStatusEffects.Count);
        Assert.AreEqual(baseAttack, resolver.GetPlayerAttackPower(owner, state.PlayerStatusEffects));

        Object.DestroyImmediate(charData);
    }

    private static OwnedCharacter CreateOwnerWithTwoSkills(int cooldown0, int cooldown1)
    {
        SkillData skill0 = ScriptableObject.CreateInstance<SkillData>();
        skill0.cooldown = cooldown0;
        skill0.manaCost = 0;

        SkillData skill1 = ScriptableObject.CreateInstance<SkillData>();
        skill1.cooldown = cooldown1;
        skill1.manaCost = 0;

        CharacterData charData = ScriptableObject.CreateInstance<CharacterData>();
        charData.baseAttack = 10;
        charData.baseMana = 100;
        charData.maxMana = 100;
        charData.skills = new[] { skill0, skill1 };

        return new OwnedCharacter(charData);
    }
}
