using System.Linq;
using UnityEngine;

[System.Serializable]
public class RarityRateEntry
{
    public Rarity rarity;
    [Range(0f, 100f)]
    public float rate;
}

[CreateAssetMenu(fileName = "GachaTable", menuName = "SummonQuest/GachaTable")]
public class GachaTable : ScriptableObject
{
    [Header("등급별 출현 확률 (%)")]
    public RarityRateEntry[] rarityRates =
    {
        new RarityRateEntry { rarity = Rarity.Five, rate = 1f },
        new RarityRateEntry { rarity = Rarity.Four, rate = 5f },
        new RarityRateEntry { rarity = Rarity.Three, rate = 15f },
        new RarityRateEntry { rarity = Rarity.Two, rate = 30f },
        new RarityRateEntry { rarity = Rarity.One, rate = 49f }
    };

    private static GachaTable instance;

    public static GachaTable Instance
    {
        get
        {
            if (instance == null)
                instance = Resources.Load<GachaTable>("GachaTable");

            if (instance == null)
            {
                instance = CreateInstance<GachaTable>();
                Debug.LogWarning("GachaTable 에셋을 찾을 수 없어 기본값을 사용합니다.");
            }

            return instance;
        }
    }

    public Rarity RollRarity()
    {
        if (rarityRates == null || rarityRates.Length == 0)
            return Rarity.One;

        float rand = Random.Range(0f, 100f);
        float cumulative = 0f;

        foreach (RarityRateEntry entry in rarityRates.OrderByDescending(e => (int)e.rarity))
        {
            cumulative += entry.rate;
            if (rand <= cumulative)
                return entry.rarity;
        }

        return Rarity.One;
    }
}
