using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// 캐릭터 상세 정보 및 강화/각성/출전 지정 기능을 제공하는 UI 컨트롤러
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
    public Button selectBattleButton;
    public Button awakenButton;
    public TextMeshProUGUI awakenText;

    private OwnedCharacter currentCharacter;
    private List<OwnedCharacter> characterList;
    private RectTransform actionArea;
    private bool layoutBuilt;

    private void Start()
    {
        EnsureLayout();
        gameObject.SetActive(false);

        if (selectBattleButton != null)
            selectBattleButton.onClick.AddListener(OnClick_SelectForBattle);

        if (awakenButton != null)
            awakenButton.onClick.AddListener(OnClick_Awaken);
    }

    private void EnsureLayout()
    {
        if (layoutBuilt)
            return;

        layoutBuilt = true;
        EnsureActionArea();
        EnsureActionButtons();
        LayoutInfoSection();
        LayoutActionSection();
        LayoutUtilityButtons();
    }

    private void EnsureActionArea()
    {
        if (actionArea != null)
            return;

        GameObject areaObject = new GameObject("ActionArea", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        areaObject.transform.SetParent(transform, false);

        actionArea = areaObject.GetComponent<RectTransform>();
        actionArea.anchorMin = new Vector2(0.5f, 0.16f);
        actionArea.anchorMax = new Vector2(0.5f, 0.16f);
        actionArea.pivot = new Vector2(0.5f, 0f);
        actionArea.anchoredPosition = Vector2.zero;
        actionArea.sizeDelta = new Vector2(300f, 0f);

        VerticalLayoutGroup layout = areaObject.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = areaObject.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void LayoutInfoSection()
    {
        if (portraitImage != null)
            SetFixedAnchor(portraitImage.rectTransform, new Vector2(0.10f, 0.54f), new Vector2(120f, 120f));

        if (nameText != null)
        {
            SetLeftAnchor(nameText.rectTransform, new Vector2(0.22f, 0.70f), new Vector2(360f, 40f));
            nameText.fontSize = 28;
            nameText.alignment = TextAlignmentOptions.Left;
        }

        if (levelText != null)
        {
            SetLeftAnchor(levelText.rectTransform, new Vector2(0.22f, 0.62f), new Vector2(360f, 36f));
            levelText.fontSize = 24;
            levelText.alignment = TextAlignmentOptions.Left;
        }

        if (statText != null)
        {
            SetLeftAnchor(statText.rectTransform, new Vector2(0.22f, 0.40f), new Vector2(360f, 150f));
            statText.fontSize = 22;
            statText.alignment = TextAlignmentOptions.TopLeft;
            statText.enableWordWrapping = true;
        }
    }

    private void LayoutActionSection()
    {
        if (actionArea == null)
            return;

        int index = 0;

        if (upgradeCostText != null)
        {
            ReparentToActionArea(upgradeCostText.rectTransform, 30f);
            upgradeCostText.transform.SetSiblingIndex(index++);
            upgradeCostText.alignment = TextAlignmentOptions.Center;
            upgradeCostText.fontSize = 20;
        }

        if (upgradeButton != null)
        {
            ReparentToActionArea(upgradeButton.GetComponent<RectTransform>(), 44f);
            upgradeButton.transform.SetSiblingIndex(index++);
        }

        if (selectBattleButton != null)
        {
            ReparentToActionArea(selectBattleButton.GetComponent<RectTransform>(), 44f);
            selectBattleButton.transform.SetSiblingIndex(index++);
        }

        if (awakenButton != null)
        {
            ReparentToActionArea(awakenButton.GetComponent<RectTransform>(), 52f);
            awakenButton.transform.SetSiblingIndex(index++);
        }
    }

    private void LayoutUtilityButtons()
    {
        Transform closeButton = transform.Find("Close_Btn");
        if (closeButton != null)
            SetFixedAnchor(closeButton as RectTransform, new Vector2(0.5f, 0.06f), new Vector2(180f, 40f));

        if (upgradeSuccessText != null)
            SetFixedAnchor(upgradeSuccessText.rectTransform, new Vector2(0.5f, 0.82f), new Vector2(320f, 48f));
    }

    private void EnsureActionButtons()
    {
        if (upgradeButton == null)
            return;

        if (selectBattleButton == null)
            selectBattleButton = CreateActionButton("SelectBattleButton", "출전 지정");

        if (awakenButton == null)
            awakenButton = CreateActionButton("AwakenButton", "각성");

        if (awakenText == null && awakenButton != null)
            awakenText = awakenButton.GetComponentInChildren<TextMeshProUGUI>();
    }

    private Button CreateActionButton(string objectName, string label)
    {
        Transform parent = actionArea != null ? actionArea : upgradeButton.transform.parent;

        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, 44f);

        LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
        layoutElement.preferredHeight = 44f;
        layoutElement.minHeight = 44f;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.2f, 0.45f, 0.85f, 1f);

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonObject.transform, false);

        TextMeshProUGUI labelText = textObject.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI referenceText = upgradeButton.GetComponentInChildren<TextMeshProUGUI>();
        if (referenceText != null)
            labelText.font = referenceText.font;

        labelText.text = label;
        labelText.fontSize = 22;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = Color.white;
        labelText.enableWordWrapping = true;

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8f, 4f);
        textRect.offsetMax = new Vector2(-8f, -4f);

        return buttonObject.GetComponent<Button>();
    }

    private static void SetFixedAnchor(RectTransform rect, Vector2 anchor, Vector2 size)
    {
        if (rect == null)
            return;

        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;
    }

    private static void SetLeftAnchor(RectTransform rect, Vector2 anchor, Vector2 size)
    {
        if (rect == null)
            return;

        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;
    }

    private void ReparentToActionArea(RectTransform rect, float preferredHeight)
    {
        if (rect == null || actionArea == null)
            return;

        rect.SetParent(actionArea, false);

        LayoutElement layoutElement = rect.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = rect.gameObject.AddComponent<LayoutElement>();

        layoutElement.preferredHeight = preferredHeight;
        layoutElement.minHeight = preferredHeight;

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, preferredHeight);
        rect.anchoredPosition = Vector2.zero;
    }

    public void SetCharacter(OwnedCharacter owned, List<OwnedCharacter> list)
    {
        currentCharacter = owned;
        characterList = list;
        RefreshDisplay();
    }

    public void Show(OwnedCharacter character, List<OwnedCharacter> list)
    {
        EnsureLayout();
        currentCharacter = character;
        characterList = list;
        RefreshDisplay();
        gameObject.SetActive(true);
    }

    private void RefreshDisplay()
    {
        portraitImage.sprite = currentCharacter.characterData.portrait;
        nameText.text = currentCharacter.characterData.characterName;
        levelText.text = $"Lv. {currentCharacter.level} / {currentCharacter.EffectiveMaxLevel}";

        bool isSelected = PlayerInventory.Instance != null && PlayerInventory.Instance.IsSelected(currentCharacter);
        string battleStatus = isSelected ? "출전 중" : "미출전";
        statText.text = $"Power: {currentCharacter.power}\n공격: {currentCharacter.AttackPower}\n속성: {currentCharacter.element}\n{battleStatus}";

        UpdateUpgradeDisplay();
        UpdateAwakenDisplay();
        UpdateSelectBattleDisplay();
    }

    private void UpdateUpgradeDisplay()
    {
        if (currentCharacter.level >= currentCharacter.EffectiveMaxLevel)
        {
            upgradeCostText.text = "최대 레벨";
            upgradeButton.interactable = false;
            return;
        }

        int cost = CalculateUpgradeCost();
        upgradeCostText.text = $"강화 비용: {cost:N0} G";
        bool canAfford = CurrencyManager.Instance.GetGold() >= cost;
        upgradeButton.interactable = canAfford;
        upgradeCostText.color = canAfford ? Color.white : Color.red;
    }

    private void UpdateAwakenDisplay()
    {
        if (awakenText == null)
            return;

        GameConfig config = GameConfig.Instance;
        if (currentCharacter.awakeningLevel >= config.maxAwakeningLevel)
        {
            awakenText.text = "최대 각성";
            if (awakenButton != null)
                awakenButton.interactable = false;
            return;
        }

        awakenText.text = $"각성 {currentCharacter.awakeningLevel}/{config.maxAwakeningLevel}\n중복 {config.duplicatesPerAwakening}개 소모";
        if (awakenButton != null)
            awakenButton.interactable = currentCharacter.CanAwaken();
    }

    private void UpdateSelectBattleDisplay()
    {
        if (selectBattleButton == null)
            return;

        bool isSelected = PlayerInventory.Instance != null && PlayerInventory.Instance.IsSelected(currentCharacter);
        selectBattleButton.interactable = !isSelected;

        TextMeshProUGUI buttonLabel = selectBattleButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonLabel != null)
            buttonLabel.text = isSelected ? "출전 중" : "출전 지정";
    }

    private int CalculateUpgradeCost()
    {
        int baseCost = currentCharacter.characterData.baseUpgradeCost;
        int levelMultiplier = currentCharacter.level;
        int rarityMultiplier = GetRarityMultiplier(currentCharacter.characterData.rarity);
        return baseCost * levelMultiplier * rarityMultiplier;
    }

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
        if (currentCharacter.level >= currentCharacter.EffectiveMaxLevel)
        {
            NotiManager.Instance.Show("최대 레벨입니다!");
            return;
        }

        int cost = CalculateUpgradeCost();
        if (!CurrencyManager.Instance.SpendGold(cost))
        {
            NotiManager.Instance.Show("골드가 부족합니다!");
            return;
        }

        int oldLevel = currentCharacter.level;
        int oldPower = currentCharacter.power;
        currentCharacter.Upgrade();

        SaveManager.Instance.SaveAllData(PlayerInventory.Instance.Characters);
        RefreshDisplay();

        if (GachaManager.Instance?.characterListUI != null)
            GachaManager.Instance.characterListUI.ShowOwnedCharacters(PlayerInventory.Instance.Characters);

        NotiManager.Instance.Show($"강화 성공! Lv.{oldLevel} → Lv.{currentCharacter.level} (Power: {oldPower} → {currentCharacter.power})");
        AnimateUpgradeText();
        PlayUpgradeSuccessEffect();
    }

    public void OnClick_SelectForBattle()
    {
        if (PlayerInventory.Instance == null)
            return;

        PlayerInventory.Instance.SelectCharacter(currentCharacter);
        NotiManager.Instance.Show($"{currentCharacter.characterData.characterName} 출전 지정!");
        RefreshDisplay();

        if (GachaManager.Instance?.characterListUI != null)
            GachaManager.Instance.characterListUI.ShowOwnedCharacters(PlayerInventory.Instance.Characters);
    }

    public void OnClick_Awaken()
    {
        if (!currentCharacter.TryAwaken(out string message))
        {
            NotiManager.Instance.Show(message);
            RefreshDisplay();
            return;
        }

        SaveManager.Instance.SaveAllData(PlayerInventory.Instance.Characters);
        NotiManager.Instance.Show(message);
        RefreshDisplay();

        if (GachaManager.Instance?.characterListUI != null)
            GachaManager.Instance.characterListUI.ShowOwnedCharacters(PlayerInventory.Instance.Characters);
    }

    public void OnClick_Close()
    {
        gameObject.SetActive(false);
    }

    private void AnimateUpgradeText()
    {
        levelText.transform.DOKill();
        statText.transform.DOKill();
        levelText.transform.localScale = Vector3.one;
        statText.transform.localScale = Vector3.one;
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
        seq.OnComplete(() => upgradeSuccessText.gameObject.SetActive(false));
    }
}
