using UnityEngine;



[System.Serializable]

public class OwnedCharacter

{

    public CharacterData characterData;

    public int count = 1;

    public int level = 1;

    public int AttackPower

    {

        get

        {

            if (characterData == null)

                return 0;



            GameConfig config = GameConfig.Instance;

            return characterData.baseAttack

                + (level * 5)

                + (awakeningLevel * config.attackBonusPerAwakening);

        }

    }



    public int power = 10;

    public string element;

    public bool isFavorite = false;

    public int exp = 0;

    public int expToLevelUp = 100;

    public int awakeningLevel = 0;



    public int currentMana;

    public int[] skillCooldowns;

    public int maxMana => characterData != null ? characterData.maxMana : 100;



    public int EffectiveMaxLevel

    {

        get

        {

            if (characterData == null)

                return 10;



            return characterData.maxLevel + (awakeningLevel * GameConfig.Instance.maxLevelBonusPerAwakening);

        }

    }



    private int CalculatePower() => level * level + (awakeningLevel * GameConfig.Instance.powerBonusPerAwakening);



    public OwnedCharacter() { }



    public OwnedCharacter(CharacterData data)

    {

        characterData = data;

        count = 1;

        level = 1;

        power = CalculatePower();

        element = data.element;

        isFavorite = false;

        currentMana = data.baseMana;

        InitializeSkillCooldowns();

    }



    public OwnedCharacter(CharacterData data, int level, int power)

    {

        characterData = data;

        this.level = level;

        this.power = power;

        element = data.element;

        currentMana = data.baseMana;

        InitializeSkillCooldowns();

    }



    private void InitializeSkillCooldowns()

    {

        if (characterData != null && characterData.skills != null)

            skillCooldowns = new int[characterData.skills.Length];

        else

            skillCooldowns = new int[0];

    }



    public bool CanUseSkill(int skillIndex)

    {

        if (characterData == null || characterData.skills == null ||

            skillIndex < 0 || skillIndex >= characterData.skills.Length)

            return false;



        if (skillCooldowns == null || skillIndex >= skillCooldowns.Length)

            return false;



        var skill = characterData.skills[skillIndex];

        return skillCooldowns[skillIndex] <= 0 && currentMana >= skill.manaCost;

    }



    public bool UseSkill(int skillIndex)

    {

        if (!CanUseSkill(skillIndex))

            return false;



        if (skillCooldowns == null || skillIndex >= skillCooldowns.Length)

            return false;



        var skill = characterData.skills[skillIndex];

        currentMana -= skill.manaCost;

        skillCooldowns[skillIndex] = skill.cooldown;

        return true;

    }



    public void OnTurnEnd(int excludeSkillIndex = -1)
    {
        if (skillCooldowns == null)
            return;

        for (int i = 0; i < skillCooldowns.Length; i++)
        {
            if (i == excludeSkillIndex)
                continue;

            if (skillCooldowns[i] > 0)
                skillCooldowns[i]--;
        }
    }



    public void RestoreMana(int amount)

    {

        currentMana = Mathf.Min(currentMana + amount, maxMana);

    }



    public void Upgrade()

    {

        level++;

        power = CalculatePower();

    }



    public void AddExp(int amount)

    {

        exp += amount;

        while (expToLevelUp > 0 && exp >= expToLevelUp && level < EffectiveMaxLevel)

        {

            exp -= expToLevelUp;

            LevelUp();

        }



        if (level >= EffectiveMaxLevel)

            exp = Mathf.Min(exp, expToLevelUp - 1);

    }



    private void LevelUp()

    {

        level++;

        power = CalculatePower();

        expToLevelUp += 50;

    }



    public bool CanAwaken()

    {

        GameConfig config = GameConfig.Instance;

        int requiredDuplicates = config.duplicatesPerAwakening + 1;

        return awakeningLevel < config.maxAwakeningLevel && count >= requiredDuplicates;

    }



    public bool TryAwaken(out string message)

    {

        GameConfig config = GameConfig.Instance;



        if (awakeningLevel >= config.maxAwakeningLevel)

        {

            message = "최대 각성 단계입니다.";

            return false;

        }



        int requiredDuplicates = config.duplicatesPerAwakening + 1;

        if (count < requiredDuplicates)

        {

            message = $"각성에 중복 {config.duplicatesPerAwakening}개가 더 필요합니다.";

            return false;

        }



        count -= config.duplicatesPerAwakening;

        awakeningLevel++;

        power = CalculatePower();

        message = $"각성 {awakeningLevel}단계! (최대 Lv.{EffectiveMaxLevel}, 공격 +{config.attackBonusPerAwakening})";

        return true;

    }

}

