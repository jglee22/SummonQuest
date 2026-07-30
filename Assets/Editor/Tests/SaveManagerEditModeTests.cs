using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class SaveManagerEditModeTests
{
    private string testDirectory;
    private SaveManager saveManager;

    [SetUp]
    public void SetUp()
    {
        SaveManager.ResetInstanceForTests();
        testDirectory = Path.Combine(Path.GetTempPath(), "SummonQuestTests", System.Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDirectory);
        saveManager = SaveManager.CreateForTests(testDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        SaveManager.ResetInstanceForTests();
        SaveManager.SaveDirectoryOverride = null;

        if (Directory.Exists(testDirectory))
            Directory.Delete(testDirectory, recursive: true);
    }

    [Test]
    public void SaveLoad_RestoresSameData()
    {
        SaveWrapper original = CreateSampleWrapper();
        Assert.IsTrue(saveManager.SaveWrapperForTests(original));

        saveManager.ClearCacheForTests();
        SaveWrapper loaded = saveManager.GetSaveData();
        List<OwnedCharacter> characters = saveManager.LoadOwnedCharacters();

        Assert.AreEqual(5000, loaded.playerGold);
        Assert.AreEqual("Char_1", loaded.selectedCharacterId);
        Assert.AreEqual(1, loaded.ownedList.Count);
        Assert.AreEqual("Char_1", loaded.ownedList[0].characterID);
        Assert.AreEqual(7, loaded.ownedList[0].level);
        Assert.AreEqual(2, loaded.ownedList[0].awakeningLevel);
        Assert.AreEqual(1, loaded.stageProgress.Count);
        Assert.AreEqual("Stage_3", loaded.stageProgress[0].stageId);
        Assert.IsTrue(loaded.stageProgress[0].isCleared);

        Assert.AreEqual(1, characters.Count);
        Assert.AreEqual(7, characters[0].level);
        Assert.AreEqual(2, characters[0].awakeningLevel);
        Assert.AreEqual("Char_1", characters[0].characterData.characterID);
    }

    [Test]
    public void CorruptedMainSave_LoadsBackup()
    {
        SaveWrapper first = CreateSampleWrapper();
        first.playerGold = 1000;
        Assert.IsTrue(saveManager.SaveWrapperForTests(first));

        SaveWrapper second = CreateSampleWrapper();
        second.playerGold = 5000;
        Assert.IsTrue(saveManager.SaveWrapperForTests(second));

        string savePath = Path.Combine(testDirectory, "character_save.json");
        File.WriteAllText(savePath, "{ invalid json }");

        saveManager.ClearCacheForTests();
        LogAssert.Expect(LogType.Warning, "메인 저장 파일을 읽지 못해 백업 파일을 사용합니다.");
        SaveWrapper loaded = saveManager.GetSaveData();

        Assert.AreEqual(1000, loaded.playerGold);
        Assert.AreEqual("Char_1", loaded.selectedCharacterId);
    }

    private static SaveWrapper CreateSampleWrapper()
    {
        return new SaveWrapper
        {
            saveVersion = SaveWrapper.CurrentSaveVersion,
            playerGold = 5000,
            selectedCharacterId = "Char_1",
            ownedList = new List<OwnedCharacterSaveData>
            {
                new OwnedCharacterSaveData
                {
                    characterID = "Char_1",
                    level = 7,
                    power = 59,
                    element = "Fire",
                    awakeningLevel = 2,
                    count = 1
                }
            },
            stageProgress = new List<StageProgressSaveData>
            {
                new StageProgressSaveData
                {
                    stageId = "Stage_3",
                    stageIndex = 2,
                    isCleared = true,
                    clearCount = 1
                }
            }
        };
    }
}
