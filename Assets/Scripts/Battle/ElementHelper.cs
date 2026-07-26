using UnityEngine;

/// <summary>
/// 속성 상성 배율 계산 (Fire &gt; Wind &gt; Earth &gt; Water &gt; Fire, Light는 중립)
/// </summary>
public static class ElementHelper
{
    public enum MatchupResult
    {
        Neutral,
        Advantage,
        Disadvantage
    }

    public static MatchupResult GetMatchup(string attackerElement, string defenderElement)
    {
        if (string.IsNullOrEmpty(attackerElement) || string.IsNullOrEmpty(defenderElement))
            return MatchupResult.Neutral;

        if (attackerElement == "Light" || defenderElement == "Light")
            return MatchupResult.Neutral;

        if (IsStrongAgainst(attackerElement, defenderElement))
            return MatchupResult.Advantage;

        if (IsStrongAgainst(defenderElement, attackerElement))
            return MatchupResult.Disadvantage;

        return MatchupResult.Neutral;
    }

    public static float GetDamageMultiplier(string attackerElement, string defenderElement)
    {
        GameConfig config = GameConfig.Instance;
        switch (GetMatchup(attackerElement, defenderElement))
        {
            case MatchupResult.Advantage:
                return config.elementAdvantageMultiplier;
            case MatchupResult.Disadvantage:
                return config.elementDisadvantageMultiplier;
            default:
                return 1f;
        }
    }

    public static string GetMatchupMessage(string attackerElement, string defenderElement)
    {
        switch (GetMatchup(attackerElement, defenderElement))
        {
            case MatchupResult.Advantage:
                return "약점!";
            case MatchupResult.Disadvantage:
                return "저항";
            default:
                return string.Empty;
        }
    }

    private static bool IsStrongAgainst(string attacker, string defender)
    {
        return (attacker == "Fire" && defender == "Wind")
            || (attacker == "Wind" && defender == "Earth")
            || (attacker == "Earth" && defender == "Water")
            || (attacker == "Water" && defender == "Fire");
    }
}
