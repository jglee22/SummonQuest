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
    private int currentMonsterIndex = 0;
    private MonsterData monsterData;

    private bool isStageMode = false;
    private bool isBattleActive = false;
    private bool isAwaitingBattleEndConfirm = false;

    private int playerHP;
    private int playerMaxHP;
    private int monsterHP;

    private int totalExpGained = 0;
    private int totalGoldGained = 0;

    private int totalBattlesWon = 0;
    private int totalBattlesLost = 0;
    private int totalExpGainedAllTime = 0;
    private int totalGoldGainedAllTime = 0;

    private readonly List<BattleStatusEffect> playerStatusEffects = new List<BattleStatusEffect>();
    private readonly List<BattleStatusEffect> monsterStatusEffects = new List<BattleStatusEffect>();

    private BattleUIController uiController;
    private BattleRewardHandler rewardHandler;

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

    private MonsterData CreateRandomMonster()
    {
        var monsters = Resources.LoadAll<MonsterData>("MonsterData");
        if (monsters.Length == 0)
        {
            Debug.LogError("MonsterData 리소스가 없습니다!");
            return null;
        }
        return monsters[Random.Range(0, monsters.Length)];
    }

    public void StartBattle(OwnedCharacter ownedCharacter, List<MonsterData> monsters)
    {
        playerOwnedCharacter = ownedCharacter;
        playerCharacter = ownedCharacter.characterData;
        monsterList = monsters;
        currentMonsterIndex = 0;
        monsterData = monsterList.Count > 0 ? monsterList[currentMonsterIndex] : CreateRandomMonster();

        isStageMode = StageManager.Instance != null && StageManager.Instance.GetCurrentStage() != null;

        playerMaxHP = Config.GetMaxHP(playerOwnedCharacter.characterData, playerOwnedCharacter.level);
        playerHP = playerMaxHP;
        monsterHP = monsterData.maxHP;

        playerStatusEffects.Clear();
        monsterStatusEffects.Clear();

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
        isAwaitingBattleEndConfirm = false;

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
        if (!isBattleActive) return;

        ProcessStatusEffects(true);
        if (!isBattleActive) return;

        if (ShouldSkipTurn(playerStatusEffects))
        {
            uiController.AppendLog($"{playerCharacter.characterName}은(는) 행동할 수 없습니다!");
            ClearExpiredEffects(playerStatusEffects);
            Invoke(nameof(MonsterTurn), 1f);
            return;
        }

        Invoke(nameof(ExecuteRandomAction), 0.5f);
    }

    private void ExecuteNormalAttack()
    {
        if (!isBattleActive) return;

        int damage = ApplyElementMultiplier(
            playerOwnedCharacter.AttackPower,
            playerCharacter.element,
            monsterData.element,
            playerCharacter.characterName,
            monsterData.monsterName);

        monsterHP -= damage;
        uiController.AppendLog($"{playerCharacter.characterName}의 공격! {damage} 데미지");

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("attack");

        playerOwnedCharacter.OnTurnEnd();
        ClearExpiredEffects(playerStatusEffects);

        if (monsterHP <= 0)
        {
            EndBattle(true);
            return;
        }

        Invoke(nameof(MonsterTurn), 1f);
    }

    private void ExecuteRandomAction()
    {
        if (!isBattleActive) return;

        List<int> availableSkills = new List<int>();
        if (playerOwnedCharacter != null && playerOwnedCharacter.characterData.skills != null)
        {
            for (int i = 0; i < playerOwnedCharacter.characterData.skills.Length; i++)
            {
                if (playerOwnedCharacter.CanUseSkill(i))
                    availableSkills.Add(i);
            }
        }

        bool useSkill = availableSkills.Count > 0 && Random.Range(0f, 1f) < Config.skillUseChance;

        if (useSkill)
        {
            int randomSkillIndex = availableSkills[Random.Range(0, availableSkills.Count)];
            var selectedSkill = playerOwnedCharacter.characterData.skills[randomSkillIndex];
            uiController.AppendLog($"{playerCharacter.characterName}이(가) {selectedSkill.skillName} 스킬을 준비합니다...");
            UseSkill(randomSkillIndex);
        }
        else
        {
            uiController.AppendLog($"{playerCharacter.characterName}이(가) 일반 공격을 준비합니다...");
            ExecuteNormalAttack();
        }
    }

    private void UseSkill(int skillIndex)
    {
        if (playerOwnedCharacter == null || !playerOwnedCharacter.CanUseSkill(skillIndex))
        {
            ExecuteNormalAttack();
            return;
        }

        var skill = playerOwnedCharacter.characterData.skills[skillIndex];
        playerOwnedCharacter.UseSkill(skillIndex);

        uiController.AppendLog($"{playerCharacter.characterName}이(가) {skill.skillName}을(를) 사용!");

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("skill_use");

        ApplySkillEffect(skill);
        playerOwnedCharacter.OnTurnEnd();
        ClearExpiredEffects(playerStatusEffects);

        if (monsterHP <= 0)
        {
            EndBattle(true);
            return;
        }

        Invoke(nameof(MonsterTurn), 1f);
    }

    private void ApplySkillEffect(SkillData skill)
    {
        switch (skill.skillType)
        {
            case SkillType.Attack:
                int baseDamage = skill.baseDamage + (playerOwnedCharacter.level * (int)skill.effectMultiplier);
                int damage = ApplyElementMultiplier(
                    baseDamage,
                    playerCharacter.element,
                    monsterData.element,
                    playerCharacter.characterName,
                    monsterData.monsterName);
                monsterHP -= damage;
                uiController.AppendLog($"{skill.skillName}으로 {damage} 데미지!");
                break;

            case SkillType.Heal:
                int healAmount = skill.healAmount + (playerOwnedCharacter.level * (int)skill.effectMultiplier);
                playerHP = Mathf.Min(playerHP + healAmount, playerMaxHP);
                uiController.AppendLog($"{skill.skillName}으로 {healAmount} 체력 회복!");
                break;

            case SkillType.Buff:
                uiController.AppendLog($"{skill.skillName}으로 공격력이 일시 상승!");
                break;

            case SkillType.Debuff:
                ApplyStatusEffect(monsterStatusEffects, skill.statusEffect, skill.statusDuration, skill.baseDamage, monsterData.monsterName);
                uiController.AppendLog($"{skill.skillName}으로 {monsterData.monsterName}을(를) 약화!");
                break;

            case SkillType.Status:
                ApplyStatusEffect(monsterStatusEffects, skill.statusEffect, skill.statusDuration, skill.baseDamage, monsterData.monsterName);
                break;
        }

        if (skill.statusEffect != StatusEffectType.None
            && skill.skillType != SkillType.Debuff
            && skill.skillType != SkillType.Status
            && Random.Range(0f, 1f) < skill.statusChance)
        {
            ApplyStatusEffect(monsterStatusEffects, skill.statusEffect, skill.statusDuration, skill.baseDamage, monsterData.monsterName);
        }
    }

    void MonsterTurn()
    {
        if (!isBattleActive) return;

        ProcessStatusEffects(false);
        if (!isBattleActive) return;

        if (ShouldSkipTurn(monsterStatusEffects))
        {
            uiController.AppendLog($"{monsterData.monsterName}은(는) 행동할 수 없습니다!");
            ClearExpiredEffects(monsterStatusEffects);
            Invoke(nameof(PlayerTurn), 1f);
            return;
        }

        int damage = ApplyElementMultiplier(
            monsterData.attack,
            monsterData.element,
            playerCharacter.element,
            monsterData.monsterName,
            playerCharacter.characterName);

        playerHP -= damage;
        uiController.AppendLog($"{monsterData.monsterName}의 반격! {damage} 데미지");
        ClearExpiredEffects(monsterStatusEffects);

        if (playerHP <= 0)
        {
            EndBattle(false);
            return;
        }

        Invoke(nameof(PlayerTurn), 1f);
    }

    private int ApplyElementMultiplier(int baseDamage, string attackerElement, string defenderElement, string attackerName, string defenderName)
    {
        float multiplier = ElementHelper.GetDamageMultiplier(attackerElement, defenderElement);
        int damage = Mathf.Max(1, Mathf.RoundToInt(baseDamage * multiplier));
        string matchupMessage = ElementHelper.GetMatchupMessage(attackerElement, defenderElement);

        if (!string.IsNullOrEmpty(matchupMessage))
            uiController.AppendLog($"({attackerName} → {defenderName}) {matchupMessage}");

        return damage;
    }

    private void ApplyStatusEffect(List<BattleStatusEffect> targetEffects, StatusEffectType type, int duration, int skillDamage, string targetName)
    {
        if (type == StatusEffectType.None)
            return;

        int effectDuration = duration > 0 ? duration : Config.defaultStatusDuration;
        int dotDamage = GetStatusDotDamage(type, skillDamage);
        targetEffects.Add(new BattleStatusEffect(type, effectDuration, dotDamage));
        uiController.AppendLog($"{targetName}에게 {type} 상태이상 적용! ({effectDuration}턴)");
    }

    private int GetStatusDotDamage(StatusEffectType type, int skillDamage)
    {
        switch (type)
        {
            case StatusEffectType.Poison: return Config.poisonDamagePerTurn;
            case StatusEffectType.Burn: return Config.burnDamagePerTurn;
            case StatusEffectType.Bleed: return Config.bleedDamagePerTurn;
            default: return skillDamage > 0 ? skillDamage : 0;
        }
    }

    private void ProcessStatusEffects(bool isPlayer)
    {
        List<BattleStatusEffect> effects = isPlayer ? playerStatusEffects : monsterStatusEffects;
        string targetName = isPlayer ? playerCharacter.characterName : monsterData.monsterName;

        foreach (var effect in effects)
        {
            if (effect.damagePerTurn > 0)
            {
                if (isPlayer)
                {
                    playerHP -= effect.damagePerTurn;
                    uiController.AppendLog($"{targetName} - {effect.type} {effect.damagePerTurn} 데미지");
                }
                else
                {
                    monsterHP -= effect.damagePerTurn;
                    uiController.AppendLog($"{targetName} - {effect.type} {effect.damagePerTurn} 데미지");
                }
            }

            effect.TickTurn();
        }

        ClearExpiredEffects(effects);

        if (isPlayer && playerHP <= 0)
            EndBattle(false);
        else if (!isPlayer && monsterHP <= 0)
            EndBattle(true);
    }

    private bool ShouldSkipTurn(List<BattleStatusEffect> effects)
    {
        foreach (var effect in effects)
        {
            if (!effect.IsExpired && effect.skipTurn)
                return true;
        }
        return false;
    }

    private void ClearExpiredEffects(List<BattleStatusEffect> effects)
    {
        effects.RemoveAll(effect => effect.IsExpired);
    }

    void EndBattle(bool playerWin)
    {
        if (!isBattleActive) return;

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
                    StageData currentStage = StageManager.Instance.GetCurrentStage();
                    ApplyReward(rewardHandler.GrantStageClearReward(playerOwnedCharacter, currentStage));
                    StageManager.Instance.ClearStage(StageManager.Instance.currentStageIndex);

                    PrepareBattleEnd($"스테이지 클리어! {currentStage.stageName}");
                    return;
                }

                monsterData = monsterList[currentMonsterIndex];
                monsterHP = monsterData.maxHP;
                monsterStatusEffects.Clear();
                uiController.AppendSectionBreak();
                uiController.AppendLog($"다음 몬스터 등장! {playerCharacter.characterName} vs {monsterData.monsterName}");
                Invoke(nameof(PlayerTurn), 1f);
                return;
            }

            ApplyReward(rewardHandler.GrantWinReward(playerOwnedCharacter, Config));

            monsterData = CreateRandomMonster();
            monsterHP = monsterData.maxHP;
            monsterStatusEffects.Clear();
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
        var ownedCharacters = GetOwnedCharactersList();
        if (ownedCharacters != null && ownedCharacters.Count > 0)
            SaveManager.Instance.SaveAllData(ownedCharacters);
    }

    private List<OwnedCharacter> GetOwnedCharactersList()
    {
        if (PlayerInventory.Instance != null)
            return PlayerInventory.Instance.Characters;

        return new List<OwnedCharacter>();
    }
}
