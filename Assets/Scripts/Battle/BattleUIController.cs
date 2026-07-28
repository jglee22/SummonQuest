using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class BattleUIController
{
    private const string LogSeparator = "────────────────────────";
    private const float ResultPanelWidth = 720f;
    private const float ResultPanelHeight = 260f;
    private const float ResultButtonHeight = 44f;
    private const float ResultButtonBottomPadding = 14f;

    private readonly GameObject battleUI;
    private readonly TextMeshProUGUI battleLogText;
    private readonly ScrollRect battleLogScrollRect;
    private readonly GameObject resultPanel;
    private readonly TextMeshProUGUI resultText;
    private readonly Button battleStartButton;
    private readonly Button battleEndButton;

    private bool layoutInitialized;

    public BattleUIController(
        GameObject battleUI,
        TextMeshProUGUI battleLogText,
        ScrollRect battleLogScrollRect,
        GameObject resultPanel,
        TextMeshProUGUI resultText,
        Button battleStartButton,
        Button battleEndButton)
    {
        this.battleUI = battleUI;
        this.battleLogText = battleLogText;
        this.battleLogScrollRect = battleLogScrollRect;
        this.resultPanel = resultPanel;
        this.resultText = resultText;
        this.battleStartButton = battleStartButton;
        this.battleEndButton = battleEndButton;
    }

    public void BindButtons(Action onBattleStart, Action onBattleEnd)
    {
        if (battleStartButton != null)
        {
            battleStartButton.onClick.RemoveAllListeners();
            battleStartButton.onClick.AddListener(() => onBattleStart?.Invoke());
        }

        if (battleEndButton != null)
        {
            battleEndButton.onClick.RemoveAllListeners();
            battleEndButton.onClick.AddListener(() => onBattleEnd?.Invoke());
        }
    }

    public void InitializeHidden()
    {
        EnsureBattleLayout();
        HideBattleResult();
        HideAll();
    }

    public void ShowBattleScreen()
    {
        EnsureBattleLayout();
        HideBattleResult();
        ApplyLogPanelLayout(false);

        if (battleUI != null)
            battleUI.SetActive(true);

        if (battleLogText != null)
        {
            battleLogText.text = "";
            battleLogText.fontSize = 28;
            battleLogText.lineSpacing = 6f;
            battleLogText.paragraphSpacing = 6f;
            battleLogText.alignment = TextAlignmentOptions.Top;
            battleLogText.enableWordWrapping = true;
            battleLogText.overflowMode = TextOverflowModes.Overflow;
        }

        if (battleStartButton != null)
            battleStartButton.interactable = false;
    }

    public void ShowBattleResult(string message)
    {
        EnsureBattleLayout();
        ApplyLogPanelLayout(true);

        if (battleUI != null)
            battleUI.SetActive(true);

        if (resultText != null)
        {
            resultText.text = message;
            resultText.fontSize = 24;
            resultText.lineSpacing = 8f;
            resultText.paragraphSpacing = 12f;
            resultText.alignment = TextAlignmentOptions.Top;
            resultText.enableWordWrapping = true;
            resultText.overflowMode = TextOverflowModes.Truncate;
        }

        if (resultPanel != null)
            resultPanel.SetActive(true);

        if (battleEndButton != null)
            battleEndButton.gameObject.SetActive(true);

        SetBattleEndButtonEnabled(true);
    }

    public void HideBattleResult()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (battleEndButton != null)
            battleEndButton.gameObject.SetActive(false);

        SetBattleEndButtonEnabled(false);
    }

    public void AppendLog(string log)
    {
        if (battleLogText == null)
            return;

        if (battleLogText.text.Length > 0)
            battleLogText.text += "\n";

        battleLogText.text += log;
        ScrollToBottom();
    }

    public void AppendSectionBreak()
    {
        if (battleLogText == null || battleLogText.text.Length == 0)
            return;

        battleLogText.text += $"\n<color=#999999>{LogSeparator}</color>\n";
        ScrollToBottom();
    }

    public void HideAll()
    {
        HideBattleResult();

        if (battleUI != null)
            battleUI.SetActive(false);

        if (battleStartButton != null)
            battleStartButton.interactable = true;
    }

    private void EnsureBattleLayout()
    {
        if (layoutInitialized)
            return;

        layoutInitialized = true;

        if (resultPanel != null && battleUI != null)
        {
            resultPanel.transform.SetParent(battleUI.transform, false);
            resultPanel.transform.SetAsLastSibling();

            if (resultPanel.GetComponent<RectMask2D>() == null)
                resultPanel.AddComponent<RectMask2D>();

            RectTransform resultRect = resultPanel.GetComponent<RectTransform>();
            resultRect.anchorMin = new Vector2(0.5f, 0.5f);
            resultRect.anchorMax = new Vector2(0.5f, 0.5f);
            resultRect.pivot = new Vector2(0.5f, 0f);
            resultRect.sizeDelta = new Vector2(ResultPanelWidth, ResultPanelHeight);
            resultRect.anchoredPosition = new Vector2(0f, 240f);
        }

        if (battleLogScrollRect != null)
        {
            RectTransform scrollRect = battleLogScrollRect.GetComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRect.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRect.pivot = new Vector2(0.5f, 0.5f);
            scrollRect.sizeDelta = new Vector2(720f, 680f);
            scrollRect.anchoredPosition = new Vector2(0f, -40f);
        }

        if (battleLogText != null)
        {
            RectTransform textRect = battleLogText.rectTransform;
            textRect.anchorMin = new Vector2(0f, 1f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.pivot = new Vector2(0.5f, 1f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = new Vector2(-32f, 0f);
        }

        if (resultText != null && resultPanel != null)
        {
            resultText.transform.SetParent(resultPanel.transform, false);
            resultText.transform.SetAsFirstSibling();

            float buttonAreaHeight = ResultButtonHeight + ResultButtonBottomPadding + 8f;
            float textAreaBottom = buttonAreaHeight / ResultPanelHeight;

            RectTransform textRect = resultText.rectTransform;
            textRect.anchorMin = new Vector2(0.06f, textAreaBottom);
            textRect.anchorMax = new Vector2(0.94f, 0.92f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            textRect.pivot = new Vector2(0.5f, 1f);
        }

        if (battleEndButton != null && resultPanel != null)
        {
            battleEndButton.transform.SetParent(resultPanel.transform, false);
            battleEndButton.transform.SetAsLastSibling();

            RectTransform buttonRect = battleEndButton.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0f);
            buttonRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0f);
            buttonRect.anchoredPosition = new Vector2(0f, ResultButtonBottomPadding);
            buttonRect.sizeDelta = new Vector2(180f, ResultButtonHeight);
            battleEndButton.gameObject.SetActive(false);
        }
    }

    private void ApplyLogPanelLayout(bool showResult)
    {
        if (battleLogScrollRect == null)
            return;

        RectTransform scrollRect = battleLogScrollRect.GetComponent<RectTransform>();
        scrollRect.sizeDelta = showResult ? new Vector2(720f, 470f) : new Vector2(720f, 680f);
        scrollRect.anchoredPosition = showResult ? new Vector2(0f, -135f) : new Vector2(0f, -40f);
    }

    private void ScrollToBottom()
    {
        Canvas.ForceUpdateCanvases();

        if (battleLogScrollRect == null)
            return;

        battleLogScrollRect.verticalNormalizedPosition = 0f;
    }

    public void SetBattleEndButtonEnabled(bool enabled)
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
}
