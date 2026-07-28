public struct BattleRewardResult
{
    public int exp;
    public int gold;
}

public class BattleRewardHandler
{
    public BattleRewardResult GrantWinReward(OwnedCharacter character, GameConfig config)
    {
        int exp = config.winExpReward;
        int gold = config.winGoldReward;
        ApplyReward(character, exp, gold);
        return new BattleRewardResult { exp = exp, gold = gold };
    }

    public BattleRewardResult GrantStageClearReward(OwnedCharacter character, StageData stage)
    {
        int exp = stage.GetTotalExpReward();
        int gold = stage.GetTotalGoldReward();
        ApplyReward(character, exp, gold);
        return new BattleRewardResult { exp = exp, gold = gold };
    }

    private static void ApplyReward(OwnedCharacter character, int exp, int gold)
    {
        character.AddExp(exp);
        CurrencyManager.Instance.AddGold(gold);
    }
}
