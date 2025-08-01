using UnityEngine;

/// <summary>
/// 유저가 보유한 캐릭터 정보를 담는 클래스
/// ScriptableObject(CharacterData) 참조 + 수량/레벨 정보 포함
/// </summary>
[System.Serializable]
public class OwnedCharacter
{
    public CharacterData characterData; // 기본 정보 참조
    public int count = 1;               // 중복 획득 수량
    public int level = 1;               // 향후 강화용 (선택사항)
    public int AttackPower => characterData.baseAttack + (level * 5);

    public int power = 10;
    public string element;

    public bool isFavorite = false; //
    public int exp = 0;
    public int expToLevelUp = 100;

    // 스킬 시스템 관련
    public int currentMana;           // 현재 마나
    public int[] skillCooldowns;      // 각 스킬의 쿨다운 (턴 수)
    public int maxMana => characterData != null ? characterData.maxMana : 100;

    private int CalculatePower() => level * level;
    // 기본 생성자 추가
    public OwnedCharacter() { }

    public OwnedCharacter(CharacterData data)
    {
        characterData = data;
        count = 1;
        level = 1;
        power = CalculatePower();
        element = data.element;
        this.isFavorite = false; // 기본값
        
        // 스킬 시스템 초기화
        currentMana = data.baseMana;
        InitializeSkillCooldowns();
    }

    public OwnedCharacter(CharacterData data, int level,int power)
    {
        this.characterData = data;
        this.level = level;
        this.power = power;
        element = data.element;
        
        // 스킬 시스템 초기화
        currentMana = data.baseMana;
        InitializeSkillCooldowns();
    }

    private void InitializeSkillCooldowns()
    {
        Debug.Log($"InitializeSkillCooldowns 호출 - characterData: {characterData != null}, skills: {characterData?.skills != null}");
        
        if (characterData != null && characterData.skills != null)
        {
            Debug.Log($"스킬 개수: {characterData.skills.Length}");
            skillCooldowns = new int[characterData.skills.Length];
            for (int i = 0; i < skillCooldowns.Length; i++)
            {
                skillCooldowns[i] = 0; // 쿨다운 초기화
                Debug.Log($"스킬 {i} 쿨다운 초기화: {skillCooldowns[i]}");
            }
            Debug.Log($"skillCooldowns 배열 생성 완료 - 길이: {skillCooldowns.Length}");
        }
        else
        {
            Debug.LogWarning("characterData 또는 skills가 null입니다. skillCooldowns 초기화 실패.");
            skillCooldowns = new int[0]; // 빈 배열로 초기화
        }
    }

    // 스킬 사용 가능 여부 확인
    public bool CanUseSkill(int skillIndex)
    {
        if (characterData == null || characterData.skills == null || 
            skillIndex < 0 || skillIndex >= characterData.skills.Length)
        {
            Debug.Log($"스킬 {skillIndex} 사용 불가: 기본 조건 실패 (characterData: {characterData != null}, skills: {characterData?.skills != null})");
            return false;
        }
        
        // skillCooldowns 배열 안전장치 추가
        if (skillCooldowns == null || skillIndex >= skillCooldowns.Length)
        {
            Debug.Log($"스킬 {skillIndex} 사용 불가: 쿨다운 배열 문제 (skillCooldowns: {skillCooldowns != null}, 길이: {skillCooldowns?.Length})");
            return false;
        }
        Debug.Log(characterData.characterName);
        var skill = characterData.skills[skillIndex];
        bool cooldownOk = skillCooldowns[skillIndex] <= 0;
        bool manaOk = currentMana >= skill.manaCost;
        
        if (!cooldownOk)
            Debug.Log($"스킬 {skillIndex} 사용 불가: 쿨다운 {skillCooldowns[skillIndex]}턴 남음");
        if (!manaOk)
            Debug.Log($"스킬 {skillIndex} 사용 불가: 마나 부족 (현재: {currentMana}, 필요: {skill.manaCost})");
            
        return cooldownOk && manaOk;
    }

    // 스킬 사용
    public bool UseSkill(int skillIndex)
    {
        if (!CanUseSkill(skillIndex))
            return false;

        // skillCooldowns 배열 안전장치 추가
        if (skillCooldowns == null || skillIndex >= skillCooldowns.Length)
            return false;

        var skill = characterData.skills[skillIndex];
        currentMana -= skill.manaCost;
        skillCooldowns[skillIndex] = skill.cooldown;
        return true;
    }

    // 턴 종료 시 쿨다운 감소
    public void OnTurnEnd()
    {
        if (skillCooldowns != null)
        {
            for (int i = 0; i < skillCooldowns.Length; i++)
            {
                if (skillCooldowns[i] > 0)
                    skillCooldowns[i]--;
            }
        }
    }

    // 마나 회복
    public void RestoreMana(int amount)
    {
        currentMana = Mathf.Min(currentMana + amount, maxMana);
    }
   
    public void Upgrade()
    {
        level++;
        power += CalculatePower(); // 예시: 강화 시 능력치 증가
    }

    public void AddExp(int amount)
    {
        exp += amount;
        while (exp >= expToLevelUp)
        {
            exp -= expToLevelUp;
            LevelUp();
        }
    }

    private void LevelUp()
    {
        level++;
        power += CalculatePower();
        expToLevelUp += 50; // 레벨업마다 필요 경험치 증가 (예시)
        // 레벨업 연출/로그 등은 외부에서 호출
    }
}
