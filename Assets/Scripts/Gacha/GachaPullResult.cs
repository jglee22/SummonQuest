public struct GachaPullResult
{
    public CharacterData character;
    public bool isNew;
    public bool isDuplicate;

    public GachaPullResult(CharacterData character, bool isNew, bool isDuplicate)
    {
        this.character = character;
        this.isNew = isNew;
        this.isDuplicate = isDuplicate;
    }
}
