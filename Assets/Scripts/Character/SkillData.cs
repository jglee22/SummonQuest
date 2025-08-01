using UnityEngine;

/// <summary>
/// 스킬 정보를 담는 ScriptableObject 클래스
/// </summary>
[CreateAssetMenu(fileName = "SkillData", menuName = "SummonTale/Skill")]
public class SkillData : ScriptableObject
{
    [Header("기본 정보")]
    public string skillName;           // 스킬 이름
    public string skillID;             // 스킬 고유 ID
    public Sprite skillIcon;           // 스킬 아이콘
    [TextArea]
    public string description;         // 스킬 설명

    [Header("스킬 효과")]
    public SkillType skillType;        // 스킬 타입 (공격, 회복, 버프 등)
    public int baseDamage;             // 기본 데미지 (공격 스킬용)
    public int healAmount;             // 회복량 (회복 스킬용)
    public float effectMultiplier;     // 효과 배율 (레벨에 따른 증가)
    
    [Header("스킬 설정")]
    public int cooldown;               // 쿨다운 (턴 수)
    public int manaCost;               // 마나 소모량
    public bool isAOE;                 // 광역 공격 여부
    
    [Header("특수 효과")]
    public StatusEffectType statusEffect; // 상태이상 타입
    public int statusDuration;         // 상태이상 지속 턴
    public float statusChance;         // 상태이상 확률 (0~1)
}

/// <summary>
/// 스킬 타입 열거형
/// </summary>
public enum SkillType
{
    Attack,     // 공격
    Heal,       // 회복
    Buff,       // 버프 (공격력/방어력 증가)
    Debuff,     // 디버프 (공격력/방어력 감소)
    Status      // 상태이상
}

/// <summary>
/// 상태이상 타입 열거형
/// </summary>
public enum StatusEffectType
{
    None,       // 없음
    Poison,     // 중독 (턴마다 데미지)
    Stun,       // 기절 (턴 스킵)
    Burn,       // 화상 (턴마다 데미지)
    Freeze,     // 빙결 (턴 스킵)
    Bleed       // 출혈 (턴마다 데미지)
} 