using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 플레이어 보유 캐릭터 데이터를 관리한다.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }

    public List<OwnedCharacter> Characters { get; private set; } = new List<OwnedCharacter>();
    public string SelectedCharacterId { get; private set; } = string.Empty;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        Load();
        CleanupDuplicates();
        EnsureSelectedCharacter();
    }

    public void Load()
    {
        if (SaveManager.Instance == null)
            return;

        SaveWrapper saveData = SaveManager.Instance.GetSaveData();
        Characters = SaveManager.Instance.LoadOwnedCharacters();
        SelectedCharacterId = saveData.selectedCharacterId ?? string.Empty;
    }

    public void Save()
    {
        if (SaveManager.Instance == null)
            return;

        SaveManager.Instance.SaveAllData(Characters, SelectedCharacterId);
    }

    public bool HasCharacters => Characters.Count > 0;

    public OwnedCharacter GetFirstCharacter()
    {
        return HasCharacters ? Characters[0] : null;
    }

    public OwnedCharacter GetSelectedCharacter()
    {
        if (!HasCharacters)
            return null;

        if (!string.IsNullOrEmpty(SelectedCharacterId))
        {
            OwnedCharacter selected = Characters.Find(c =>
                c.characterData != null && c.characterData.characterID == SelectedCharacterId);
            if (selected != null)
                return selected;
        }

        return GetFirstCharacter();
    }

    public bool SelectCharacter(OwnedCharacter character)
    {
        if (character?.characterData == null)
            return false;

        SelectedCharacterId = character.characterData.characterID;
        Save();
        return true;
    }

    public bool IsSelected(OwnedCharacter character)
    {
        if (character?.characterData == null)
            return false;

        return !string.IsNullOrEmpty(SelectedCharacterId)
            && SelectedCharacterId == character.characterData.characterID;
    }

    private void EnsureSelectedCharacter()
    {
        if (!HasCharacters)
        {
            SelectedCharacterId = string.Empty;
            return;
        }

        if (GetSelectedCharacter() == null)
            SelectedCharacterId = Characters[0].characterData.characterID;
    }

    public bool TryAddCharacter(CharacterData data, out bool isDuplicate)
    {
        isDuplicate = false;
        if (data == null)
            return false;

        OwnedCharacter existing = Characters.Find(c => c.characterData.characterName == data.characterName);
        if (existing != null)
        {
            existing.count++;
            isDuplicate = true;
        }
        else
        {
            Characters.Add(new OwnedCharacter(data));
            if (string.IsNullOrEmpty(SelectedCharacterId))
                SelectedCharacterId = data.characterID;
        }

        Save();
        return true;
    }

    private void CleanupDuplicates()
    {
        var groupedCharacters = Characters
            .GroupBy(c => c.characterData.characterName)
            .Select(g => new
            {
                Characters = g.ToList(),
                TotalCount = g.Sum(c => c.count),
                IsFavorite = g.Any(c => c.isFavorite),
                MaxAwakening = g.Max(c => c.awakeningLevel)
            })
            .ToList();

        Characters.Clear();

        foreach (var group in groupedCharacters)
        {
            var representative = group.Characters.OrderByDescending(c => c.level).First();
            representative.count = group.TotalCount;
            representative.isFavorite = group.IsFavorite;
            representative.awakeningLevel = group.MaxAwakening;
            Characters.Add(representative);
        }

        EnsureSelectedCharacter();
    }
}
