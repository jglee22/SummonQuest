using UnityEngine;

public enum Rarity
{
    One = 1,
    Two,
    Three,
    Four,
    Five
}

/// <summary>
/// 수집형 캐릭터 정보를 담는 ScriptableObject 클래스
/// 캐릭터 이름, 이미지, 속성, 능력치, 설명 등을 포함
/// </summary>
[CreateAssetMenu(fileName = "CharacterData",menuName = "SummonTale/Character")]
public class CharacterData : ScriptableObject
{
    [Header("기본 정보")]
    public string characterName; // 캐릭터 이름
    public Sprite portrait;      // 캐릭터 초상 이미지
    public Rarity rarity; // ⭐ 등급 추가 (1~5성)
    public string characterID; // 캐릭터 고유 ID (저장용)

    [Header("등급 및 속성")]
    public int starGrade => (int)rarity; // 별 등급 (1~5성)
    public string element;       // 속성 (예: Fire, Water 등)

    [Header("기본 능력치")]
    public int baseHP;           // 기본 체력
    public int baseAttack;       // 기본 공격력
    public int baseSpeed;        // 기본 속도 (턴 순서 등에 사용 가능)

    [Header("강화 관련 설정")]
    public int baseUpgradeCost = 100;
    public int upgradeCostPerLevel = 100;
    public int maxLevel = 10; // 최대 레벨


    [Header("가챠 확률")]
    [Range(0f, 100f)]
    public float gachaRate; // 이 캐릭터의 등장 확률

    [Header("설명")]
    [TextArea]
    public string description;   // 캐릭터 설명 (UI에 출력용)

    [Header("스킬")]
    public SkillData[] skills;   // 캐릭터가 보유한 스킬들
    public int maxMana = 100;    // 최대 마나
    public int baseMana = 50;    // 기본 마나


}
