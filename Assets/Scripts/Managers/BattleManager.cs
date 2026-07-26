using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;
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

    private int playerHP;
    private int playerMaxHP;
    private int monsterHP;

    private bool isBattleActive = false;
    private int totalExpGained = 0;
    private int totalGoldGained = 0;

    private int totalBattlesWon = 0;
    private int totalBattlesLost = 0;
    private int totalExpGainedAllTime = 0;
    private int totalGoldGainedAllTime = 0;

    private readonly List<BattleStatusEffect> playerStatusEffects = new List<BattleStatusEffect>();
    private readonly List<BattleStatusEffect> monsterStatusEffects = new List<BattleStatusEffect>();

    private GameConfig Config => GameConfig.Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        if (battleStartButton != null)
            battleStartButton.onClick.AddListener(OnBattleStartButtonClicked);
        if (battleEndButton != null)
            battleEndButton.onClick.AddListener(OnBattleEndButtonClicked);
        resultPanel.SetActive(false);
        battleUI.SetActive(false);
        SetBattleEndButtonEnabled(false);
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
        if (isBattleActive)
            return;

        CancelInvoke();

        if (GameManager.Instance != null)
            GameManager.Instance.SetGameState(GameState.Playing);

        resultPanel.SetActive(false);
        battleUI.SetActive(false);

        if (battleStartButton != null)
            battleStartButton.interactable = true;

        SetBattleEndButtonEnabled(false);
    }

    private void SetBattleEndButtonEnabled(bool enabled)
    {
        if (battleEndButton == null)
            return;

        battleEndButton.interactable = enabled;

        if (!enabled)
            ResetSelectableVisualState(battleEndButton);
    }

    private static void ResetSelectableVisualState(Selectable selectable)
    {
        if (selectable == null)
            return;

        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == selectable.gameObject)
            EventSystem.current.SetSelectedGameObject(null);

        selectable.OnDeselect(null);
    }

    private void FinalizeBattleUI(string resultMessage)
    {
        isBattleActive = false;
        CancelInvoke();

        battleUI.SetActive(false);
        resultPanel.SetActive(true);
        resultText.text = resultMessage;

        if (battleStartButton != null)
            battleStartButton.interactable = true;

        SetBattleEndButtonEnabled(true);
        PlayerInventory.Instance?.Save();
    }

    private void AppendBattleLog(string log)
    {
        battleLogText.text += log + "\n";
        Canvas.ForceUpdateCanvases();
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

        battleUI.SetActive(true);
        resultPanel.SetActive(false);
        battleLogText.text = "";

        if (battleStartButton != null)
            battleStartButton.interactable = false;

        SetBattleEndButtonEnabled(false);

        string selectedLabel = PlayerInventory.Instance != null && PlayerInventory.Instance.IsSelected(ownedCharacter)
            ? "[출전] "
            : string.Empty;

        if (isStageMode)
            AppendBattleLog($"스테이지 모드: {selectedLabel}{playerCharacter.characterName} vs {monsterData.monsterName}");
        else
            AppendBattleLog($"{selectedLabel}{playerCharacter.characterName} vs {monsterData.monsterName}");

        Invoke(nameof(PlayerTurn), 1f);
    }

    void PlayerTurn()
    {
        if (!isBattleActive) return;

        ProcessStatusEffects(true);
        if (!isBattleActive) return;

        if (ShouldSkipTurn(playerStatusEffects))
        {
            AppendBattleLog($"{playerCharacter.characterName}은(는) 행동할 수 없습니다!");
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
        AppendBattleLog($"{playerCharacter.characterName}의 공격! {damage} 데미지");

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
            AppendBattleLog($"{playerCharacter.characterName}이(가) {selectedSkill.skillName} 스킬을 준비합니다...");
            UseSkill(randomSkillIndex);
        }
        else
        {
            AppendBattleLog($"{playerCharacter.characterName}이(가) 일반 공격을 준비합니다...");
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

        AppendBattleLog($"{playerCharacter.characterName}이(가) {skill.skillName}을(를) 사용!");

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
                AppendBattleLog($"{skill.skillName}으로 {damage} 데미지!");
                break;

            case SkillType.Heal:
                int healAmount = skill.healAmount + (playerOwnedCharacter.level * (int)skill.effectMultiplier);
                playerHP = Mathf.Min(playerHP + healAmount, playerMaxHP);
                AppendBattleLog($"{skill.skillName}으로 {healAmount} 체력 회복!");
                break;

            case SkillType.Buff:
                AppendBattleLog($"{skill.skillName}으로 공격력이 일시 상승!");
                break;

            case SkillType.Debuff:
                ApplyStatusEffect(monsterStatusEffects, skill.statusEffect, skill.statusDuration, skill.baseDamage, monsterData.monsterName);
                AppendBattleLog($"{skill.skillName}으로 {monsterData.monsterName}을(를) 약화!");
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
            AppendBattleLog($"{monsterData.monsterName}은(는) 행동할 수 없습니다!");
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
        AppendBattleLog($"{monsterData.monsterName}의 반격! {damage} 데미지");
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
            AppendBattleLog($"({attackerName} → {defenderName}) {matchupMessage}");

        return damage;
    }

    private void ApplyStatusEffect(List<BattleStatusEffect> targetEffects, StatusEffectType type, int duration, int skillDamage, string targetName)
    {
        if (type == StatusEffectType.None)
            return;

        int effectDuration = duration > 0 ? duration : Config.defaultStatusDuration;
        int dotDamage = GetStatusDotDamage(type, skillDamage);
        targetEffects.Add(new BattleStatusEffect(type, effectDuration, dotDamage));
        AppendBattleLog($"{targetName}에게 {type} 상태이상 적용! ({effectDuration}턴)");
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
                    AppendBattleLog($"{targetName} - {effect.type} {effect.damagePerTurn} 데미지");
                }
                else
                {
                    monsterHP -= effect.damagePerTurn;
                    AppendBattleLog($"{targetName} - {effect.type} {effect.damagePerTurn} 데미지");
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
            AppendBattleLog($"{monsterData.monsterName} 처치!");

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX("victory");

            totalBattlesWon++;

            if (isStageMode)
            {
                int monsterExpReward = Config.winExpReward;
                int monsterGoldReward = Config.winGoldReward;

                playerOwnedCharacter.AddExp(monsterExpReward);
                CurrencyManager.Instance.AddGold(monsterGoldReward);

                totalExpGained += monsterExpReward;
                totalGoldGained += monsterGoldReward;
                totalExpGainedAllTime += monsterExpReward;
                totalGoldGainedAllTime += monsterGoldReward;

                AppendBattleLog($"보상: 경험치 {monsterExpReward} / 골드 {monsterGoldReward} 획득!");
            }

            if (isStageMode)
            {
                currentMonsterIndex++;
                if (currentMonsterIndex >= monsterList.Count)
                {
                    StageData currentStage = StageManager.Instance.GetCurrentStage();

                    int stageExpReward = currentStage.GetTotalExpReward();
                    int stageGoldReward = currentStage.GetTotalGoldReward();

                    playerOwnedCharacter.AddExp(stageExpReward);
                    CurrencyManager.Instance.AddGold(stageGoldReward);

                    totalExpGained += stageExpReward;
                    totalGoldGained += stageGoldReward;

                    StageManager.Instance.ClearStage(StageManager.Instance.currentStageIndex);

                    AppendBattleLog($"스테이지 클리어! {currentStage.stageName}");
                    AppendBattleLog($"보상: 경험치 {stageExpReward} / 골드 {stageGoldReward} 획득!");

                    FinalizeBattleUI($"스테이지 클리어!\n{currentStage.stageName}\n총 경험치: {totalExpGained}\n총 골드: {totalGoldGained}");
                    return;
                }

                monsterData = monsterList[currentMonsterIndex];
                monsterHP = monsterData.maxHP;
                monsterStatusEffects.Clear();
                AppendBattleLog($"다음 몬스터 등장! {playerCharacter.characterName} vs {monsterData.monsterName}");
                Invoke(nameof(PlayerTurn), 1f);
                return;
            }

            totalExpGainedAllTime += Config.winExpReward;
            totalGoldGainedAllTime += Config.winGoldReward;
            playerOwnedCharacter.AddExp(Config.winExpReward);
            CurrencyManager.Instance.AddGold(Config.winGoldReward);
            totalExpGained += Config.winExpReward;
            totalGoldGained += Config.winGoldReward;
            AppendBattleLog($"보상: 경험치 {Config.winExpReward} / 골드 {Config.winGoldReward} 획득!");

            monsterData = CreateRandomMonster();
            monsterHP = monsterData.maxHP;
            monsterStatusEffects.Clear();
            AppendBattleLog($"새로운 몬스터 등장! {playerCharacter.characterName} vs {monsterData.monsterName}");
            Invoke(nameof(PlayerTurn), 1f);
            return;
        }

        AppendBattleLog("플레이어가 패배했습니다... 전투 종료.");

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("defeat");

        totalBattlesLost++;

        FinalizeBattleUI($"패배...\n총 경험치: {totalExpGained}\n총 골드: {totalGoldGained}");
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
