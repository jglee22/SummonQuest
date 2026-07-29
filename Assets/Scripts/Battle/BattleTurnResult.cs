using System.Collections.Generic;

public class BattleTurnResult
{
    public bool PlayerDefeated;
    public bool MonsterDefeated;
    public List<string> Messages = new List<string>();

    public void AddMessage(string message)
    {
        if (!string.IsNullOrEmpty(message))
            Messages.Add(message);
    }
}
