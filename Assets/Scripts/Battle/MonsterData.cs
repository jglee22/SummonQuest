using UnityEngine;

[CreateAssetMenu(menuName = "Battle/MonsterData")]
public class MonsterData : ScriptableObject
{
    public string monsterName;
    public int maxHP;
    public int attack;
    public Sprite icon; // 몬스터 이미지
}
