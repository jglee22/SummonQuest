using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resources/CharacterData 폴더의 CharacterData를 characterID 기준으로 조회한다.
/// </summary>
public static class CharacterDatabase
{
    private const string ResourcePath = "CharacterData";

    private static CharacterData[] cachedAll;
    private static Dictionary<string, CharacterData> cachedById;

    public static IReadOnlyList<CharacterData> All
    {
        get
        {
            EnsureLoaded();
            return cachedAll;
        }
    }

    public static CharacterData GetById(string characterId)
    {
        if (string.IsNullOrEmpty(characterId))
            return null;

        EnsureLoaded();
        cachedById.TryGetValue(characterId, out CharacterData data);
        return data;
    }

    private static void EnsureLoaded()
    {
        if (cachedAll != null)
            return;

        cachedAll = Resources.LoadAll<CharacterData>(ResourcePath);
        cachedById = new Dictionary<string, CharacterData>();

        foreach (CharacterData data in cachedAll)
        {
            if (data == null || string.IsNullOrEmpty(data.characterID))
            {
                Debug.LogWarning("CharacterData에 characterID가 없습니다.");
                continue;
            }

            if (cachedById.ContainsKey(data.characterID))
            {
                Debug.LogWarning($"중복 characterID: {data.characterID}");
                continue;
            }

            cachedById[data.characterID] = data;
        }
    }
}
