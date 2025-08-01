using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using DG.Tweening.Core.Easing;
using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// 캐릭터 상세 정보 및 강화 기능을 제공하는 UI 컨트롤러
/// </summary>
public class CharacterDetailUI : MonoBehaviour
{
    public Image portraitImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI statText;
    public TextMeshProUGUI notiText;
    public TextMeshProUGUI upgradeCostText;
    public TextMeshProUGUI upgradeSuccessText;
    public Button upgradeButton;

    private OwnedCharacter currentCharacter;
    private List<OwnedCharacter> characterList;



    private void Start()
    {
        gameObject.SetActive(false);
    }
    public void SetCharacter(OwnedCharacter owned, List<OwnedCharacter> list)
    {
        currentCharacter = owned;
        characterList = list;

        levelText.text = $"Lv. {currentCharacter.level}";
        statText.text = $"Power: {currentCharacter.power}";
    }

    public void Show(OwnedCharacter character, List<OwnedCharacter> list)
    {
        currentCharacter = character;
        characterList = list;

        portraitImage.sprite = character.characterData.portrait;
        nameText.text = character.characterData.characterName;
        levelText.text = $"Lv. {character.level}";
        statText.text = $"Power: {character.power}";

        upgradeButton.interactable = currentCharacter.level < currentCharacter.characterData.maxLevel;
        
        // 강화 비용 표시 업데이트
        UpdateUpgradeCostDisplay();
        
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 강화 비용 표시 업데이트
    /// </summary>
    private void UpdateUpgradeCostDisplay()
    {
        if (currentCharacter.level >= currentCharacter.characterData.maxLevel)
        {
            upgradeCostText.text = "최대 레벨";
            upgradeButton.interactable = false;
        }
        else
        {
            int cost = CalculateUpgradeCost();
            upgradeCostText.text = $"강화 비용: {cost:N0} G";
            
            // 골드가 부족하면 버튼 비활성화
            bool canAfford = CurrencyManager.Instance.GetGold() >= cost;
            upgradeButton.interactable = canAfford;
            
            // 골드 부족 시 텍스트 색상 변경
            if (!canAfford)
            {
                upgradeCostText.color = Color.red;
            }
            else
            {
                upgradeCostText.color = Color.white;
            }
        }
    }

    /// <summary>
    /// 강화 비용 계산
    /// </summary>
    private int CalculateUpgradeCost()
    {
        int baseCost = currentCharacter.characterData.baseUpgradeCost;
        int levelMultiplier = currentCharacter.level;
        int rarityMultiplier = GetRarityMultiplier(currentCharacter.characterData.rarity);
        
        // 비용 = 기본비용 × 레벨 × 등급배수
        return baseCost * levelMultiplier * rarityMultiplier;
    }

    /// <summary>
    /// 등급별 비용 배수
    /// </summary>
    private int GetRarityMultiplier(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.One: return 1;
            case Rarity.Two: return 2;
            case Rarity.Three: return 3;
            case Rarity.Four: return 5;
            case Rarity.Five: return 10;
            default: return 1;
        }
    }

    public void OnClick_Upgrade()
    {
        if (currentCharacter.level >= currentCharacter.characterData.maxLevel)
        {
            NotiManager.Instance.Show("최대 레벨입니다!");
            return;
        }

        int cost = CalculateUpgradeCost();

        // 골드 차감 시도
        if (!CurrencyManager.Instance.SpendGold(cost))
        {
            NotiManager.Instance.Show("골드가 부족합니다!");
            return;
        }

        // 강화 처리
        int oldLevel = currentCharacter.level;
        int oldPower = currentCharacter.power;
        
        currentCharacter.Upgrade();

        // 강화 저장 (캐릭터와 골드 모두 저장)
        SaveManager.Instance.SaveAllData(characterList);

        // UI 갱신
        levelText.text = $"Lv. {currentCharacter.level}";
        statText.text = $"Power: {currentCharacter.power}";
        
        // 강화 비용 표시 업데이트
        UpdateUpgradeCostDisplay();

        // CharacterListUI 새로고침하여 CharacterSlotUI 업데이트
        if (GachaManager.Instance?.characterListUI != null)
        {
            GachaManager.Instance.characterListUI.ShowOwnedCharacters(characterList);
        }

        // 강화 성공 메시지
        NotiManager.Instance.Show($"강화 성공! Lv.{oldLevel} → Lv.{currentCharacter.level} (Power: {oldPower} → {currentCharacter.power})");

        // DOTween 연출
        levelText.transform.DOKill();
        statText.transform.DOKill();

        levelText.transform.localScale = Vector3.one;
        statText.transform.localScale = Vector3.one;

        levelText.transform.DOScale(1.2f, 0.15f).SetLoops(2, LoopType.Yoyo);
        statText.transform.DOScale(1.2f, 0.15f).SetLoops(2, LoopType.Yoyo);
        AnimateUpgradeText();  // 강화 연출 추가
        PlayUpgradeSuccessEffect();
    }

    public void OnClick_Close()
    {
        gameObject.SetActive(false);
    }

    private void AnimateUpgradeText()
    {
        // DOTween 초기화 (혹시 몰라서)
        levelText.transform.DOKill();
        statText.transform.DOKill();

        // 크기 초기화
        levelText.transform.localScale = Vector3.one;
        statText.transform.localScale = Vector3.one;

        // 확대 후 원상 복귀 애니메이션
        levelText.transform.DOScale(1.3f, 0.15f).SetLoops(2, LoopType.Yoyo);
        statText.transform.DOScale(1.3f, 0.15f).SetLoops(2, LoopType.Yoyo);
    }

    public void PlayUpgradeSuccessEffect()
    {
        upgradeSuccessText.gameObject.SetActive(true);
        upgradeSuccessText.text = "강화 성공!";
        upgradeSuccessText.transform.localScale = Vector3.zero;

        Sequence seq = DOTween.Sequence();
        seq.Join(upgradeSuccessText.transform.DOScale(1, 0.3f).SetEase(Ease.OutBack));
        seq.AppendInterval(0.8f);
        seq.OnComplete(() =>
        {
            upgradeSuccessText.gameObject.SetActive(false);
        });
    }
}
