using UnityEngine;

public static class BattleMonsterProvider
{
    public static MonsterData GetRandomMonster()
    {
        MonsterData[] monsters = Resources.LoadAll<MonsterData>("MonsterData");
        if (monsters.Length == 0)
        {
            Debug.LogError("MonsterData 리소스가 없습니다!");
            return null;
        }

        return monsters[Random.Range(0, monsters.Length)];
    }
}
