using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance;

    [Header("UI")]
    public GameObject battleUI; // 배틀 UI
    public TextMeshProUGUI battleLogText; // 전투 로그 텍스트
    public GameObject resultPanel; // 결과 패널
    public TextMeshProUGUI resultText; // 결과 텍스트
    public Button battleStartButton; // 배틀 시작 버튼 추가
    public ScrollRect battleLogScrollRect; // 전투 로그 스크롤뷰 추가
    public Button battleEndButton; // 전투 종료 버튼 추가

    [Header("캐릭터/몬스터 데이터")]
    public CharacterData playerCharacter; // 현재 선택된 캐릭터
    public OwnedCharacter playerOwnedCharacter; // 보유 캐릭터 정보 추가
    public List<MonsterData> monsterList = new List<MonsterData>(); // 전투 몬스터 리스트
    private int currentMonsterIndex = 0;
    private MonsterData monsterData; // 현재 전투 몬스터
    
    // 스테이지 모드 관련
    private bool isStageMode = false; // 스테이지 모드인지 여부

    private int playerHP;
    private int monsterHP;
    private const int WIN_EXP_REWARD = 50; // 승리 시 획득 경험치
    private const int WIN_GOLD_REWARD = 30; // 승리 시 획득 골드

    // 스킬 사용 확률 설정
    private const float SKILL_USE_CHANCE = 0.7f; // 70% 확률로 스킬 사용

    private bool autoScroll = true;
    private bool isBattleActive = false;
    private int totalExpGained = 0; // 이번 전투에서 획득한 총 경험치
    private int totalGoldGained = 0; // 이번 전투에서 획득한 총 골드

    // 전체 게임 통계 (저장용)
    private int totalBattlesWon = 0;
    private int totalBattlesLost = 0;
    private int totalExpGainedAllTime = 0;
    private int totalGoldGainedAllTime = 0;

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
    }

    private void OnBattleStartButtonClicked()
    {
        // GachaManager에서 보유한 캐릭터 가져오기
        if (GachaManager.Instance != null && GachaManager.Instance.ownedCharacters != null && GachaManager.Instance.ownedCharacters.Count > 0)
        {
            // 첫 번째 보유 캐릭터 사용
            playerOwnedCharacter = GachaManager.Instance.ownedCharacters[0];
            Debug.Log($"전투 시작 - 사용할 캐릭터: {playerOwnedCharacter.characterData.characterName}");
        }
        else
        {
            Debug.LogWarning("보유한 캐릭터가 없습니다!");
            return;
        }

        // 예시: 인스펙터에서 monsterList에 1개만 넣어두면 됨
        if (playerOwnedCharacter != null && monsterList.Count > 0)
        {
            isBattleActive = true;
            battleStartButton.interactable = false; // 전투 중 버튼 비활성화
            StartBattle(playerOwnedCharacter, monsterList);
        }
        else
            Debug.LogWarning("플레이어 캐릭터 또는 몬스터 데이터가 없습니다.");
    }
    private void OnBattleEndButtonClicked()
    {
        isBattleActive = false;
        CancelInvoke(); // 예약된 Invoke 모두 취소
        
        // GameManager 상태 변경
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameState.Playing);
        }
        
        resultPanel.SetActive(true);
        battleUI.SetActive(false); // 필요시 전투 UI도 비활성화
        battleStartButton.interactable = true; // 전투 종료 후 버튼 다시 활성화
        resultText.text = $"전투 종료\n총 경험치: {totalExpGained}\n총 골드: {totalGoldGained}"; // 명확한 텍스트 할당
        AppendBattleLog("전투가 종료되었습니다.");
        StartCoroutine(HideResultPanelAfterDelay());
    }

    private System.Collections.IEnumerator HideResultPanelAfterDelay()
    {
        yield return new WaitForSeconds(1.7f); // 1.5~2초 사이 값
        resultPanel.SetActive(false);
    }
    private void AppendBattleLog(string log)
    {
        battleLogText.text += log + "\n";
        Canvas.ForceUpdateCanvases();
        // if (battleLogScrollRect != null)
        //     battleLogScrollRect.verticalNormalizedPosition = 0f; // 맨 아래로 스크롤
    }

    private MonsterData CreateRandomMonster()
    {
        // Resources 폴더의 MonsterData에서 랜덤으로 하나 선택 (폴더명은 프로젝트에 맞게 수정)
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
        playerCharacter = ownedCharacter.characterData; // 기존 CharacterData도 저장
        monsterList = monsters;
        currentMonsterIndex = 0;
        monsterData = monsterList.Count > 0 ? monsterList[currentMonsterIndex] : CreateRandomMonster();

        // 스테이지 모드인지 확인
        isStageMode = (StageManager.Instance != null && StageManager.Instance.GetCurrentStage() != null);

        playerHP = 100; // 임시 체력
        monsterHP = monsterData.maxHP;

        // 마나 초기화
        if (playerOwnedCharacter != null)
        {
            playerOwnedCharacter.currentMana = playerOwnedCharacter.maxMana;
            Debug.Log($"전투 시작 - 마나 초기화: {playerOwnedCharacter.currentMana}/{playerOwnedCharacter.maxMana}");
        }

        // 전투 시작 효과음 재생
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("battle_start");
        }

        // GameManager 상태 변경
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameState.Battle);
            GameManager.Instance.IncrementBattleCount();
        }

        // 누적 보상 초기화
        totalExpGained = 0;
        totalGoldGained = 0;

        // 전투 활성화
        isBattleActive = true;

        battleUI.SetActive(true);
        resultPanel.SetActive(false);
        if (string.IsNullOrEmpty(battleLogText.text))
            battleLogText.text = ""; // 첫 전투만 초기화
            
        if (isStageMode)
        {
            AppendBattleLog($"스테이지 모드: {playerCharacter.characterName} vs {monsterData.monsterName}");
            Debug.Log($"스테이지 모드 전투 시작: {monsterList.Count}마리 몬스터");
        }
        else
        {
            AppendBattleLog($"{playerCharacter.characterName} vs {monsterData.monsterName}");
        }
        
        Debug.Log($"전투 시작 - isBattleActive: {isBattleActive}, monsterHP: {monsterHP}");
        Invoke(nameof(PlayerTurn), 1f);
    }

    void PlayerTurn()
    {
        Debug.Log($"PlayerTurn 호출 - isBattleActive: {isBattleActive}");
        if (!isBattleActive) return;
        
        // 랜덤하게 스킬 또는 일반 공격 선택
        Invoke(nameof(ExecuteRandomAction), 0.5f);
    }

    private void ExecuteNormalAttack()
    {
        if (!isBattleActive) return;
        
        int damage = playerOwnedCharacter.AttackPower;
        monsterHP -= damage;
        AppendBattleLog($"{playerCharacter.characterName}의 공격! {damage} 데미지");
        
        // 공격 효과음 재생
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("attack");
        }
        
        // 턴 종료 시 쿨다운 감소
        playerOwnedCharacter.OnTurnEnd();

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
        
        // 사용 가능한 스킬 목록 생성
        List<int> availableSkills = new List<int>();
        if (playerOwnedCharacter != null && playerOwnedCharacter.characterData.skills != null)
        {
            Debug.Log($"캐릭터 스킬 개수: {playerOwnedCharacter.characterData.skills.Length}");
            for (int i = 0; i < playerOwnedCharacter.characterData.skills.Length; i++)
            {
                bool canUse = playerOwnedCharacter.CanUseSkill(i);
                Debug.Log($"스킬 {i} 사용 가능: {canUse}");
                if (canUse)
                {
                    availableSkills.Add(i);
                }
            }
        }
        else
        {
            Debug.Log("캐릭터 또는 스킬 데이터가 null입니다.");
        }
        
        Debug.Log($"사용 가능한 스킬 개수: {availableSkills.Count}");
        Debug.Log($"현재 마나: {playerOwnedCharacter?.currentMana}");
        
        // 스킬 사용 여부 결정
        bool useSkill = availableSkills.Count > 0 && Random.Range(0f, 1f) < SKILL_USE_CHANCE;
        Debug.Log($"스킬 사용 결정: {useSkill}");
        
        if (useSkill)
        {
            // 랜덤하게 스킬 선택
            int randomSkillIndex = availableSkills[Random.Range(0, availableSkills.Count)];
            var selectedSkill = playerOwnedCharacter.characterData.skills[randomSkillIndex];
            AppendBattleLog($"{playerCharacter.characterName}이(가) {selectedSkill.skillName} 스킬을 준비합니다...");
            UseSkill(randomSkillIndex);
        }
        else
        {
            // 일반 공격
            AppendBattleLog($"{playerCharacter.characterName}이(가) 일반 공격을 준비합니다...");
            ExecuteNormalAttack();
        }
    }

    private void UseSkill(int skillIndex)
    {
        if (playerOwnedCharacter == null || !playerOwnedCharacter.CanUseSkill(skillIndex))
        {
            // 스킬 사용 불가능하면 일반 공격으로 대체
            ExecuteNormalAttack();
            return;
        }

        var skill = playerOwnedCharacter.characterData.skills[skillIndex];
        playerOwnedCharacter.UseSkill(skillIndex);
        
        AppendBattleLog($"{playerCharacter.characterName}이(가) {skill.skillName}을(를) 사용!");
        
        // 스킬 효과음 재생
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("skill_use");
        }
        
        // 스킬 효과 적용
        ApplySkillEffect(skill);
        
        // 턴 종료 시 쿨다운 감소
        playerOwnedCharacter.OnTurnEnd();
        
        // 다음 턴으로
        Invoke(nameof(MonsterTurn), 1f);
    }

    private void ApplySkillEffect(SkillData skill)
    {
        switch (skill.skillType)
        {
            case SkillType.Attack:
                int damage = skill.baseDamage + (playerOwnedCharacter.level * (int)skill.effectMultiplier);
                monsterHP -= damage;
                AppendBattleLog($"{skill.skillName}으로 {damage} 데미지!");
                break;
                
            case SkillType.Heal:
                int healAmount = skill.healAmount + (playerOwnedCharacter.level * (int)skill.effectMultiplier);
                playerHP = Mathf.Min(playerHP + healAmount, 100); // 최대 체력 제한
                AppendBattleLog($"{skill.skillName}으로 {healAmount} 체력 회복!");
                break;
                
            case SkillType.Buff:
                // 버프 효과 (임시로 공격력 증가)
                AppendBattleLog($"{skill.skillName}으로 버프 효과!");
                break;
                
            case SkillType.Debuff:
                // 디버프 효과
                AppendBattleLog($"{skill.skillName}으로 디버프 효과!");
                break;
        }
        
        // 상태이상 적용
        if (skill.statusEffect != StatusEffectType.None && Random.Range(0f, 1f) < skill.statusChance)
        {
            AppendBattleLog($"{monsterData.monsterName}에게 {skill.statusEffect} 상태이상 적용!");
        }
    }

    void MonsterTurn()
    {
        if (!isBattleActive) return;
        int damage = monsterData.attack;
        playerHP -= damage;

        AppendBattleLog($"{monsterData.monsterName}의 반격! {damage} 데미지");

        if (playerHP <= 0)
        {
            EndBattle(false);
            return;
        }

        Invoke(nameof(PlayerTurn), 1f);
    }

    void EndBattle(bool playerWin)
    {
        if (!isBattleActive) return;
        if (playerWin)
        {
            AppendBattleLog($"{monsterData.monsterName} 처치!");
            
            // 승리 효과음 재생
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("victory");
            }
            
            totalBattlesWon++;
            
            // 스테이지 모드인지 확인
            if (isStageMode)
            {
                // 스테이지 모드: 각 몬스터 처치 시 보상 지급
                int monsterExpReward = WIN_EXP_REWARD;
                int monsterGoldReward = WIN_GOLD_REWARD;
                
                // 캐릭터와 골드에 보상 지급
                playerOwnedCharacter.AddExp(monsterExpReward);
                CurrencyManager.Instance.AddGold(monsterGoldReward);
                
                // 누적 보상 업데이트
                totalExpGained += monsterExpReward;
                totalGoldGained += monsterGoldReward;
                
                totalExpGainedAllTime += monsterExpReward;
                totalGoldGainedAllTime += monsterGoldReward;
                
                AppendBattleLog($"보상: 경험치 {monsterExpReward} / 골드 {monsterGoldReward} 획득!");
            }
            else
            {
                // 일반 모드: 기존 로직
                totalExpGainedAllTime += WIN_EXP_REWARD;
                totalGoldGainedAllTime += WIN_GOLD_REWARD;
            }
            
            // 스테이지 모드인지 확인
            if (isStageMode)
            {
                // 스테이지 모드: 모든 몬스터 처치 시 스테이지 클리어
                currentMonsterIndex++;
                if (currentMonsterIndex >= monsterList.Count)
                {
                    // 스테이지 클리어!
                    StageData currentStage = StageManager.Instance.GetCurrentStage();
                    
                    // 스테이지 보상 지급
                    int stageExpReward = currentStage.GetTotalExpReward();
                    int stageGoldReward = currentStage.GetTotalGoldReward();
                    
                    // 캐릭터와 골드에 보상 지급
                    playerOwnedCharacter.AddExp(stageExpReward);
                    CurrencyManager.Instance.AddGold(stageGoldReward);
                    
                    // 누적 보상 업데이트
                    totalExpGained += stageExpReward;
                    totalGoldGained += stageGoldReward;
                    
                    // 스테이지 클리어 처리
                    StageManager.Instance.ClearStage(StageManager.Instance.currentStageIndex);
                    
                    AppendBattleLog($"스테이지 클리어! {currentStage.stageName}");
                    AppendBattleLog($"보상: 경험치 {stageExpReward} / 골드 {stageGoldReward} 획득!");
                    
                    // 전투 UI 숨기고 결과 UI만 표시
                    battleUI.SetActive(false);
                    resultPanel.SetActive(true);
                    resultText.text = $"스테이지 클리어!\n{currentStage.stageName}\n총 경험치: {totalExpGained}\n총 골드: {totalGoldGained}";
                    battleStartButton.interactable = true;
                    isBattleActive = false;
                    return;
                }
                else
                {
                    // 다음 몬스터와 전투
                    monsterData = monsterList[currentMonsterIndex];
                    monsterHP = monsterData.maxHP;
                    AppendBattleLog($"다음 몬스터 등장! {playerCharacter.characterName} vs {monsterData.monsterName}");
                    Invoke(nameof(PlayerTurn), 1f);
                    return;
                }
            }
            else
            {
                // 일반 모드: 기존 로직
                totalExpGainedAllTime += WIN_EXP_REWARD;
                totalGoldGainedAllTime += WIN_GOLD_REWARD;
                // 보상 지급
                playerOwnedCharacter.AddExp(WIN_EXP_REWARD);
                CurrencyManager.Instance.AddGold(WIN_GOLD_REWARD);
                totalExpGained += WIN_EXP_REWARD;
                totalGoldGained += WIN_GOLD_REWARD;
                AppendBattleLog($"보상: 경험치 {WIN_EXP_REWARD} / 골드 {WIN_GOLD_REWARD} 획득!");
                // 새로운 몬스터 생성 및 전투 재시작
                monsterData = CreateRandomMonster();
                monsterHP = monsterData.maxHP;
                AppendBattleLog($"새로운 몬스터 등장! {playerCharacter.characterName} vs {monsterData.monsterName}");
                Invoke(nameof(PlayerTurn), 1f);
                return;
            }
        }
        else
        {
            AppendBattleLog("플레이어가 패배했습니다... 전투 종료.");
            
            // 패배 효과음 재생
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("defeat");
            }
            
            totalBattlesLost++;
            
            // 전투 UI 숨기고 결과 UI만 표시
            battleUI.SetActive(false);
            resultPanel.SetActive(true);
            resultText.text = $"패배...\n총 경험치: {totalExpGained}\n총 골드: {totalGoldGained}"; // 패배 시 명확한 텍스트
            battleStartButton.interactable = true; // 전투 종료 후 버튼 다시 활성화
        }
    }

    // 전투 통계 반환 메서드들 (SaveSystem에서 사용)
    public int GetTotalBattlesWon() => totalBattlesWon;
    public int GetTotalBattlesLost() => totalBattlesLost;
    public int GetTotalExpGainedAllTime() => totalExpGainedAllTime;
    public int GetTotalGoldGainedAllTime() => totalGoldGainedAllTime;

    // 전투 통계 설정 메서드들 (SaveSystem에서 사용)
    public void SetTotalBattlesWon(int value) => totalBattlesWon = value;
    public void SetTotalBattlesLost(int value) => totalBattlesLost = value;
    public void SetTotalExpGainedAllTime(int value) => totalExpGainedAllTime = value;
    public void SetTotalGoldGainedAllTime(int value) => totalGoldGainedAllTime = value;

    // 게임 종료 시 자동 저장
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) // 게임이 백그라운드로 갈 때
        {
            AutoSave();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) // 게임이 포커스를 잃을 때
        {
            AutoSave();
        }
    }

    private void AutoSave()
    {
        // 현재 보유한 캐릭터 리스트를 가져와서 저장
        // (실제로는 CharacterManager나 다른 매니저에서 가져와야 함)
        var ownedCharacters = GetOwnedCharactersList();
        if (ownedCharacters != null && ownedCharacters.Count > 0)
        {
            SaveManager.Instance.SaveAllData(ownedCharacters);
            Debug.Log("게임 자동 저장 완료");
        }
    }

    // 임시 메서드 - 실제로는 CharacterManager에서 가져와야 함
    private List<OwnedCharacter> GetOwnedCharactersList()
    {
        // 현재 플레이어 캐릭터가 있다면 리스트로 만들어서 반환
        if (playerOwnedCharacter != null)
        {
            return new List<OwnedCharacter> { playerOwnedCharacter };
        }
        return new List<OwnedCharacter>();
    }
}
