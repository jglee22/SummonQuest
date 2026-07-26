using UnityEngine;

[CreateAssetMenu(menuName = "Battle/MonsterData")]
public class MonsterData : ScriptableObject
{
    public string monsterName;
    public int maxHP;
    public int attack;
    public string element = "Earth";
    public Sprite icon;
}
