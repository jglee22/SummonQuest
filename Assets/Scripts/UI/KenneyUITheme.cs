using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class KenneyUITheme
{
    private const string PanelSpritePath = "UI/Sprites/tile_0013";
    private const string ButtonSpritePath = "UI/Sprites/tile_0082";
    private const string SlotFrameSpritePath = "UI/Sprites/tile_0027";

    private static Sprite panelSprite;
    private static Sprite buttonSprite;
    private static Sprite slotFrameSprite;
    private static bool applied;

    public static Sprite PanelSprite => panelSprite ??= LoadSprite(PanelSpritePath);
    public static Sprite ButtonSprite => buttonSprite ??= LoadSprite(ButtonSpritePath);
    public static Sprite SlotFrameSprite => slotFrameSprite ??= LoadSprite(SlotFrameSpritePath);

    public static bool IsReady =>
        PanelSprite != null && ButtonSprite != null;

    public static void Configure(Sprite panel, Sprite button, Sprite slotFrame)
    {
        if (panel != null)
            panelSprite = panel;
        if (button != null)
            buttonSprite = button;
        if (slotFrame != null)
            slotFrameSprite = slotFrame;
    }

    public static void ApplyAll(UIManager uiManager = null)
    {
        if (!IsReady)
        {
            Debug.LogWarning("Kenney UI 스프라이트를 Resources에서 찾지 못했습니다. Assets/Resources/UI/Sprites 경로를 확인하세요.");
            return;
        }

        ApplyCanvasBackground();
        ApplyManagerPanels(uiManager);
        ApplyNamedPanels();
        ApplyButtons();
        ApplyPanelTextColors();
        RefreshCharacterSlots();
        applied = true;
    }

    public static void ApplyPanel(Image image)
    {
        if (image == null || PanelSprite == null)
            return;

        image.sprite = PanelSprite;
        image.type = Image.Type.Sliced;
        image.color = Color.white;
        image.preserveAspect = false;
    }

    public static void ApplyButton(Button button)
    {
        if (button == null || ButtonSprite == null)
            return;

        Image image = button.targetGraphic as Image;
        if (image == null)
            return;

        image.sprite = ButtonSprite;
        image.type = Image.Type.Sliced;
        image.color = Color.white;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.65f, 0.65f, 0.65f, 0.65f);
        button.colors = colors;
    }

    public static void ApplySlotFrame(Image image)
    {
        if (image == null || SlotFrameSprite == null)
            return;

        image.sprite = SlotFrameSprite;
        image.type = Image.Type.Sliced;
        image.color = Color.white;
    }

    private static Sprite LoadSprite(string resourcePath)
    {
        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite != null)
            return sprite;

        Object[] assets = Resources.LoadAll(resourcePath, typeof(Sprite));
        foreach (Object asset in assets)
        {
            if (asset is Sprite loadedSprite)
                return loadedSprite;
        }

        string spriteName = resourcePath.Substring(resourcePath.LastIndexOf('/') + 1);
        Sprite[] folderSprites = Resources.LoadAll<Sprite>("UI/Sprites");
        foreach (Sprite folderSprite in folderSprites)
        {
            if (folderSprite.name.StartsWith(spriteName))
                return folderSprite;
        }

        return null;
    }

    private static void ApplyCanvasBackground()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
            return;

        Image canvasBackground = canvas.GetComponent<Image>();
        if (canvasBackground == null)
            canvasBackground = canvas.gameObject.AddComponent<Image>();

        canvasBackground.color = new Color(0.17f, 0.18f, 0.22f, 1f);
        canvasBackground.raycastTarget = false;
    }

    private static void ApplyManagerPanels(UIManager uiManager)
    {
        if (uiManager == null)
            return;

        ApplyPanelObject(uiManager.battlePanel);
        ApplyPanelObject(uiManager.gachaPanel);
        ApplyPanelObject(uiManager.characterPanel);
        ApplyPanelObject(uiManager.settingsPanel);
        ApplyPanelObject(uiManager.stageSelectionPanel);
        ApplyPanelObject(uiManager.notificationPanel);
        ApplyPanelObject(uiManager.confirmPanel);
    }

    private static void ApplyNamedPanels()
    {
        Image[] images = Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Image image in images)
        {
            if (ShouldSkipImage(image))
                continue;

            string objectName = image.gameObject.name;
            if (objectName.Contains("Viewport") || objectName.Contains("Handle"))
                continue;

            if (objectName.Contains("Panel") || objectName.Contains("Scroll View"))
                ApplyPanel(image);
        }
    }

    private static void ApplyPanelObject(GameObject panel)
    {
        if (panel == null)
            return;

        Image panelImage = panel.GetComponent<Image>();
        if (panelImage != null)
            ApplyPanel(panelImage);

        foreach (Image childImage in panel.GetComponentsInChildren<Image>(true))
        {
            if (ShouldSkipImage(childImage))
                continue;

            if (childImage.gameObject.name.Contains("Panel"))
                ApplyPanel(childImage);
        }
    }

    private static void ApplyButtons()
    {
        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Button button in buttons)
            ApplyButton(button);
    }

    private static void ApplyPanelTextColors()
    {
        TextMeshProUGUI[] texts = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Color panelTextColor = new Color(0.22f, 0.16f, 0.12f, 1f);
        Color goldTextColor = new Color(0.62f, 0.42f, 0.05f, 1f);

        foreach (TextMeshProUGUI text in texts)
        {
            if (text.GetComponentInParent<CharacterSlotUI>() != null)
                continue;

            if (text.gameObject.name == "Gold_Text")
            {
                text.color = goldTextColor;
                continue;
            }

            if (IsInsideThemedPanel(text.transform))
                text.color = panelTextColor;
        }
    }

    private static bool IsInsideThemedPanel(Transform transform)
    {
        Transform current = transform;
        while (current != null)
        {
            string name = current.name;
            if (name.Contains("Panel") || name.Contains("Scroll View"))
                return true;

            current = current.parent;
        }

        return false;
    }

    private static bool ShouldSkipImage(Image image)
    {
        if (image.sprite != null && image.sprite.name.Contains("portrait"))
            return true;

        if (image.GetComponentInParent<CharacterSlotUI>() != null)
            return true;

        if (image.gameObject.name.Contains("Portrait"))
            return true;

        if (image.gameObject.name.Contains("Icon") || image.gameObject.name.Contains("Favorite"))
            return true;

        return false;
    }

    private static void RefreshCharacterSlots()
    {
        CharacterSlotUI[] slots = Object.FindObjectsByType<CharacterSlotUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (CharacterSlotUI slot in slots)
            slot.RefreshSlotFrame();
    }
}
