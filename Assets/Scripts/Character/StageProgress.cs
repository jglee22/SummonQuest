[System.Serializable]
public class StageProgress
{
    public bool isUnlocked;
    public bool isCleared;
    public int clearCount;

    public StageProgress()
    {
    }

    public StageProgress(bool unlocked, bool cleared, int count)
    {
        isUnlocked = unlocked;
        isCleared = cleared;
        clearCount = count;
    }
}
