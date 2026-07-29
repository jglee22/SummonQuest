using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 캐릭터 하나의 정보를 UI에 표시해주는 역할 (이름, 이미지, 등급 등)
/// </summary>
public class CharacterSlotUI : MonoBehaviour
{
    private const float SlotWidth = 160f;
    private const float SlotHeight = 220f;
    private const float PortraitSize = 88f;
    private const float PortraitTopPadding = 6f;
    private const float StatsGapBelowPortrait = 6f;
    private const float NameAreaHeight = 28f;
    private const float NameBottomPadding = 8f;

    [Header("UI 요소")]
    public Image portraitImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI starText;
    public string element;

    public Button favoriteButton;
    public GameObject favoriteOnIcon;
    public GameObject favoriteOffIcon;
    public GameObject selectedIndicator;

    private OwnedCharacter ownedRef;
    private CharacterData characterData;
    private bool layoutInitialized;
    private Image portraitDisplay;
    private Image slotFrameImage;

    private void Start()
    {
        favoriteButton.onClick.RemoveAllListeners();
        favoriteButton.onClick.AddListener(() =>
        {
            ownedRef.isFavorite = !ownedRef.isFavorite;
            UpdateFavoriteIcon();

            if (GachaManager.Instance?.characterListUI != null)
                GachaManager.Instance.characterListUI.ShowOwnedCharacters(PlayerInventory.Instance.Characters);

            PlayerInventory.Instance?.Save();
        });
    }

    public void SetCharacter(OwnedCharacter ownedCharacter, int totalCount = 1, bool isSelected = false)
    {
        EnsureSlotLayout();

        characterData = ownedCharacter.characterData;
        string selectedPrefix = isSelected ? "[출전] " : string.Empty;
        nameText.text = selectedPrefix + characterData.characterName;

        string awakeningText = ownedCharacter.awakeningLevel > 0 ? $"\n각성 {ownedCharacter.awakeningLevel}" : string.Empty;

        if (totalCount > 1)
            starText.text = $"Lv. {ownedCharacter.level}\nPower: {ownedCharacter.power}\n수량: {totalCount}{awakeningText}";
        else
            starText.text = $"Lv. {ownedCharacter.level}\nPower: {ownedCharacter.power}{awakeningText}";

        if (portraitDisplay != null)
            portraitDisplay.sprite = characterData.portrait;
        else if (portraitImage != null)
            portraitImage.sprite = characterData.portrait;

        element = ownedCharacter.element;

        ownedRef = ownedCharacter;
        UpdateFavoriteIcon();
        UpdateSelectedIndicator(isSelected);
        ApplySlotFrame();
    }

    private void UpdateFavoriteIcon()
    {
        favoriteOnIcon.SetActive(ownedRef.isFavorite);
        favoriteOffIcon.SetActive(!ownedRef.isFavorite);
        ownedRef.isFavorite = favoriteOnIcon.activeSelf;
    }

    private void UpdateSelectedIndicator(bool isSelected)
    {
        if (selectedIndicator != null)
            selectedIndicator.SetActive(isSelected);
    }

    private void EnsureSlotLayout()
    {
        if (layoutInitialized)
            return;

        layoutInitialized = true;

        RectTransform root = (RectTransform)transform;
        root.sizeDelta = new Vector2(SlotWidth, SlotHeight);

        EnsurePortraitDisplay(root);

        float statsTop = PortraitTopPadding + PortraitSize + StatsGapBelowPortrait;
        float statsHeight = SlotHeight - statsTop - NameAreaHeight - NameBottomPadding;

        if (starText != null)
        {
            RectTransform statsRect = starText.rectTransform;
            statsRect.anchorMin = new Vector2(0f, 1f);
            statsRect.anchorMax = new Vector2(1f, 1f);
            statsRect.pivot = new Vector2(0.5f, 1f);
            statsRect.anchoredPosition = new Vector2(0f, -statsTop);
            statsRect.sizeDelta = new Vector2(-8f, statsHeight);

            starText.fontSize = 20;
            starText.lineSpacing = 0f;
            starText.paragraphSpacing = 0f;
            starText.alignment = TextAlignmentOptions.Top;
            starText.enableWordWrapping = false;
            starText.overflowMode = TextOverflowModes.Overflow;
        }

        if (nameText != null)
        {
            RectTransform nameRect = nameText.rectTransform;
            nameRect.anchorMin = new Vector2(0f, 0f);
            nameRect.anchorMax = new Vector2(1f, 0f);
            nameRect.pivot = new Vector2(0.5f, 0f);
            nameRect.anchoredPosition = new Vector2(0f, NameBottomPadding);
            nameRect.sizeDelta = new Vector2(-8f, NameAreaHeight);

            nameText.fontSize = 20;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.overflowMode = TextOverflowModes.Ellipsis;
        }

        if (favoriteButton != null)
        {
            RectTransform favoriteRect = favoriteButton.GetComponent<RectTransform>();
            favoriteRect.anchorMin = new Vector2(1f, 1f);
            favoriteRect.anchorMax = new Vector2(1f, 1f);
            favoriteRect.pivot = new Vector2(1f, 1f);
            favoriteRect.anchoredPosition = new Vector2(-4f, -PortraitTopPadding);
            favoriteRect.sizeDelta = new Vector2(36f, 36f);
        }
    }

    private void EnsurePortraitDisplay(RectTransform root)
    {
        if (portraitDisplay != null)
            return;

        if (portraitImage != null)
        {
            portraitImage.color = new Color(1f, 1f, 1f, 0f);
            portraitImage.raycastTarget = true;
        }

        GameObject portraitObject = new GameObject("PortraitDisplay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        portraitObject.transform.SetParent(root, false);
        portraitObject.transform.SetAsFirstSibling();

        portraitDisplay = portraitObject.GetComponent<Image>();
        portraitDisplay.preserveAspect = true;
        portraitDisplay.raycastTarget = false;

        RectTransform portraitRect = portraitDisplay.rectTransform;
        portraitRect.anchorMin = new Vector2(0.5f, 1f);
        portraitRect.anchorMax = new Vector2(0.5f, 1f);
        portraitRect.pivot = new Vector2(0.5f, 1f);
        portraitRect.anchoredPosition = new Vector2(0f, -PortraitTopPadding);
        portraitRect.sizeDelta = new Vector2(PortraitSize, PortraitSize);
    }

    public void RefreshSlotFrame()
    {
        ApplySlotFrame();
    }

    private void ApplySlotFrame()
    {
        if (!KenneyUITheme.IsReady)
            return;

        if (slotFrameImage == null)
        {
            GameObject frameObject = new GameObject("SlotFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            frameObject.transform.SetParent(transform, false);
            frameObject.transform.SetAsFirstSibling();

            slotFrameImage = frameObject.GetComponent<Image>();
            slotFrameImage.raycastTarget = false;

            RectTransform frameRect = slotFrameImage.rectTransform;
            frameRect.anchorMin = new Vector2(0.5f, 1f);
            frameRect.anchorMax = new Vector2(0.5f, 1f);
            frameRect.pivot = new Vector2(0.5f, 1f);
            frameRect.anchoredPosition = new Vector2(0f, -2f);
            frameRect.sizeDelta = new Vector2(96f, 96f);
        }

        KenneyUITheme.ApplySlotFrame(slotFrameImage);
    }
}
