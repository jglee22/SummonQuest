using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance;

    [Header("UI")]
    public GameObject battleUI;
    public TextMeshProUGUI battleLogText;
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;
    public Button battleStartButton;
    public ScrollRect battleLogScrollRect;
    public Button battleEndButton;

    [Header("캐릭터/몬스터 데이터")]
    public CharacterData playerCharacter;
    public OwnedCharacter playerOwnedCharacter;
    public List<MonsterData> monsterList = new List<MonsterData>();

    private int currentMonsterIndex;
    private MonsterData monsterData;

    private bool isStageMode;
    private bool isBattleActive;
    private bool isAwaitingBattleEndConfirm;

    private const int MaxTurns = 200;

    private int totalExpGained;
    private int totalGoldGained;

    private int totalBattlesWon;
    private int totalBattlesLost;
    private int totalExpGainedAllTime;
    private int totalGoldGainedAllTime;

    private readonly BattleState battleState = new BattleState();
    private BattleUIController uiController;
    private BattleRewardHandler rewardHandler;
    private BattleStatusEffectProcessor statusProcessor;
    private BattleCombatResolver combatResolver;

    private GameConfig Config => GameConfig.Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        uiController = new BattleUIController(
            battleUI,
            battleLogText,
            battleLogScrollRect,
            resultPanel,
            resultText,
            battleStartButton,
            battleEndButton);

        rewardHandler = new BattleRewardHandler();
        statusProcessor = new BattleStatusEffectProcessor(Config);
        combatResolver = new BattleCombatResolver(Config, statusProcessor);
    }

    private void Start()
    {
        uiController.BindButtons(OnBattleStartButtonClicked, OnBattleEndButtonClicked);
        uiController.InitializeHidden();
        LoadBattleStatistics();
    }

    private void LoadBattleStatistics()
    {
        if (SaveManager.Instance == null)
            return;

        SaveWrapper saveData = SaveManager.Instance.GetSaveData();
        SetTotalBattlesWon(saveData.totalBattlesWon);
        SetTotalBattlesLost(saveData.totalBattlesLost);
        SetTotalExpGainedAllTime(saveData.totalExpGainedAllTime);
        SetTotalGoldGainedAllTime(saveData.totalGoldGainedAllTime);
    }

    private void OnBattleStartButtonClicked()
    {
        if (PlayerInventory.Instance == null || !PlayerInventory.Instance.HasCharacters)
        {
            NotiManager.Instance?.Show("보유한 캐릭터가 없습니다!");
            return;
        }

        if (UIManager.Instance != null)
            UIManager.Instance.OpenStageSelection();
    }

    private void OnBattleEndButtonClicked()
    {
        if (isBattleActive || !isAwaitingBattleEndConfirm)
            return;

        isAwaitingBattleEndConfirm = false;
        CancelInvoke();

        PlayerInventory.Instance?.Save();

        if (GameManager.Instance != null)
            GameManager.Instance.SetGameState(GameState.Playing);

        uiController.HideAll();
    }

    public void StartBattle(OwnedCharacter ownedCharacter, List<MonsterData> monsters)
    {
        CancelInvoke();
        isAwaitingBattleEndConfirm = false;

        playerOwnedCharacter = ownedCharacter;
        playerCharacter = ownedCharacter.characterData;
        monsterList = monsters ?? new List<MonsterData>();
        currentMonsterIndex = 0;
        monsterData = monsterList.Count > 0 ? monsterList[currentMonsterIndex] : BattleMonsterProvider.GetRandomMonster();

        isStageMode = monsterList.Count > 0;

        int playerMaxHp = Config.GetMaxHP(playerOwnedCharacter.characterData, playerOwnedCharacter.level);
        battleState.Reset(playerMaxHp, monsterData.maxHP);

        if (playerOwnedCharacter != null)
            playerOwnedCharacter.currentMana = playerOwnedCharacter.maxMana;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("battle_start");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameState.Battle);
            GameManager.Instance.IncrementBattleCount();
        }

        totalExpGained = 0;
        totalGoldGained = 0;
        isBattleActive = true;

        uiController.ShowBattleScreen();

        string selectedLabel = PlayerInventory.Instance != null && PlayerInventory.Instance.IsSelected(ownedCharacter)
            ? "[출전] "
            : string.Empty;

        if (isStageMode)
            uiController.AppendLog($"스테이지 모드: {selectedLabel}{playerCharacter.characterName} vs {monsterData.monsterName}");
        else
            uiController.AppendLog($"{selectedLabel}{playerCharacter.characterName} vs {monsterData.monsterName}");

        Invoke(nameof(PlayerTurn), 1f);
    }

    void PlayerTurn()
    {
        if (!isBattleActive)
            return;

        battleState.TurnCount++;
        if (battleState.TurnCount > MaxTurns)
        {
            PrepareBattleEnd("전투가 너무 길어져 종료되었습니다.");
            return;
        }

        BattleTurnResult dotResult = statusProcessor.ProcessDotDamage(
            battleState,
            true,
            playerCharacter.characterName);
        LogTurnResult(dotResult);
        if (!isBattleActive)
            return;

        if (statusProcessor.ShouldSkipTurn(battleState.PlayerStatusEffects))
        {
            uiController.AppendLog($"{playerCharacter.characterName}은(는) 행동할 수 없습니다!");
            statusProcessor.EndPlayerTurn(battleState, playerOwnedCharacter);
            Invoke(nameof(MonsterTurn), 1f);
            return;
        }

        Invoke(nameof(ExecuteRandomAction), 0.5f);
    }

    private void ExecuteNormalAttack()
    {
        if (!isBattleActive)
            return;

        BattleTurnResult result = combatResolver.ApplyNormalAttack(
            battleState,
            playerOwnedCharacter,
            playerCharacter,
            monsterData);
        LogTurnResult(result);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("attack");

        statusProcessor.EndPlayerTurn(battleState, playerOwnedCharacter);

        if (result.MonsterDefeated)
        {
            EndBattle(true);
            return;
        }

        Invoke(nameof(MonsterTurn), 1f);
    }

    private void ExecuteRandomAction()
    {
        if (!isBattleActive)
            return;

        int skillIndex = combatResolver.PickSkillIndex(playerOwnedCharacter, Config.skillUseChance);
        if (skillIndex >= 0)
        {
            SkillData selectedSkill = playerOwnedCharacter.characterData.skills[skillIndex];
            if (!BattleCombatResolver.CanUseSkillInBattle(selectedSkill))
            {
                uiController.AppendLog($"{playerCharacter.characterName}이(가) 일반 공격을 준비합니다...");
                ExecuteNormalAttack();
                return;
            }

            uiController.AppendLog($"{playerCharacter.characterName}이(가) {selectedSkill.skillName} 스킬을 준비합니다...");
            UseSkill(skillIndex);
            return;
        }

        uiController.AppendLog($"{playerCharacter.characterName}이(가) 일반 공격을 준비합니다...");
        ExecuteNormalAttack();
    }

    private void UseSkill(int skillIndex)
    {
        if (playerOwnedCharacter == null || !playerOwnedCharacter.CanUseSkill(skillIndex))
        {
            ExecuteNormalAttack();
            return;
        }

        SkillData skill = playerOwnedCharacter.characterData.skills[skillIndex];
        playerOwnedCharacter.UseSkill(skillIndex);
        battleState.PlayerSkillUsedThisTurn = true;

        uiController.AppendLog($"{playerCharacter.characterName}이(가) {skill.skillName}을(를) 사용!");

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("skill_use");

        BattleTurnResult result = combatResolver.ApplySkill(
            skill,
            battleState,
            playerOwnedCharacter,
            playerCharacter,
            monsterData);
        LogTurnResult(result);

        statusProcessor.EndPlayerTurn(battleState, playerOwnedCharacter);

        if (result.MonsterDefeated)
        {
            EndBattle(true);
            return;
        }

        Invoke(nameof(MonsterTurn), 1f);
    }

    void MonsterTurn()
    {
        if (!isBattleActive)
            return;

        BattleTurnResult dotResult = statusProcessor.ProcessDotDamage(
            battleState,
            false,
            monsterData.monsterName);
        LogTurnResult(dotResult);
        if (!isBattleActive)
            return;

        if (statusProcessor.ShouldSkipTurn(battleState.MonsterStatusEffects))
        {
            uiController.AppendLog($"{monsterData.monsterName}은(는) 행동할 수 없습니다!");
            statusProcessor.EndMonsterTurn(battleState);
            Invoke(nameof(PlayerTurn), 1f);
            return;
        }

        BattleTurnResult result = combatResolver.ApplyMonsterAttack(battleState, playerCharacter, monsterData);
        LogTurnResult(result);
        statusProcessor.EndMonsterTurn(battleState);

        if (result.PlayerDefeated)
        {
            EndBattle(false);
            return;
        }

        Invoke(nameof(PlayerTurn), 1f);
    }

    void EndBattle(bool playerWin)
    {
        if (!isBattleActive)
            return;

        if (playerWin)
        {
            uiController.AppendLog($"{monsterData.monsterName} 처치!");

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX("victory");

            totalBattlesWon++;

            if (isStageMode)
                ApplyReward(rewardHandler.GrantWinReward(playerOwnedCharacter, Config));

            if (isStageMode)
            {
                currentMonsterIndex++;
                if (currentMonsterIndex >= monsterList.Count)
                {
                    StageData currentStage = StageManager.Instance != null
                        ? StageManager.Instance.GetCurrentStage()
                        : null;

                    if (currentStage != null && StageManager.Instance != null)
                    {
                        ApplyReward(rewardHandler.GrantStageClearReward(playerOwnedCharacter, currentStage));
                        StageManager.Instance.ClearStage(StageManager.Instance.currentStageIndex);
                        PrepareBattleEnd($"스테이지 클리어! {currentStage.stageName}");
                    }
                    else
                    {
                        PrepareBattleEnd("스테이지 클리어!");
                    }

                    return;
                }

                monsterData = monsterList[currentMonsterIndex];
                battleState.ResetMonster(monsterData.maxHP);
                uiController.AppendSectionBreak();
                uiController.AppendLog($"다음 몬스터 등장! {playerCharacter.characterName} vs {monsterData.monsterName}");
                Invoke(nameof(PlayerTurn), 1f);
                return;
            }

            ApplyReward(rewardHandler.GrantWinReward(playerOwnedCharacter, Config));

            monsterData = BattleMonsterProvider.GetRandomMonster();
            battleState.ResetMonster(monsterData.maxHP);
            uiController.AppendSectionBreak();
            uiController.AppendLog($"새로운 몬스터 등장! {playerCharacter.characterName} vs {monsterData.monsterName}");
            Invoke(nameof(PlayerTurn), 1f);
            return;
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("defeat");

        totalBattlesLost++;
        PrepareBattleEnd("플레이어가 패배했습니다...");
    }

    private void PrepareBattleEnd(string endMessage)
    {
        isBattleActive = false;
        isAwaitingBattleEndConfirm = true;
        CancelInvoke();

        uiController.AppendSectionBreak();

        if (!string.IsNullOrEmpty(endMessage))
            uiController.AppendLog(endMessage);

        string resultMessage = $"보상\n\n경험치 +{totalExpGained}\n\n골드 +{totalGoldGained}";
        uiController.ShowBattleResult(resultMessage);
    }

    private void ApplyReward(BattleRewardResult reward)
    {
        totalExpGained += reward.exp;
        totalGoldGained += reward.gold;
        totalExpGainedAllTime += reward.exp;
        totalGoldGainedAllTime += reward.gold;
    }

    private void LogTurnResult(BattleTurnResult result)
    {
        if (result == null)
            return;

        foreach (string message in result.Messages)
            uiController.AppendLog(message);

        if (result.PlayerDefeated)
            EndBattle(false);
        else if (result.MonsterDefeated)
            EndBattle(true);
    }

    public int GetTotalBattlesWon() => totalBattlesWon;
    public int GetTotalBattlesLost() => totalBattlesLost;
    public int GetTotalExpGainedAllTime() => totalExpGainedAllTime;
    public int GetTotalGoldGainedAllTime() => totalGoldGainedAllTime;

    public void SetTotalBattlesWon(int value) => totalBattlesWon = value;
    public void SetTotalBattlesLost(int value) => totalBattlesLost = value;
    public void SetTotalExpGainedAllTime(int value) => totalExpGainedAllTime = value;
    public void SetTotalGoldGainedAllTime(int value) => totalGoldGainedAllTime = value;

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            AutoSave();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            AutoSave();
    }

    private void AutoSave()
    {
        if (PlayerInventory.Instance != null)
            SaveManager.Instance?.SaveAllData(PlayerInventory.Instance.Characters);
    }
}
