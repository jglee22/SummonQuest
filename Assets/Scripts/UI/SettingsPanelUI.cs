using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// 게임 설정을 관리하는 UI 패널
/// 음량, 그래픽, 게임 설정 등을 포함
/// </summary>
public class SettingsPanelUI : MonoBehaviour
{
    [Header("음량 설정")]
    public Slider bgmVolumeSlider;
    public Slider sfxVolumeSlider;
    public TextMeshProUGUI bgmVolumeText;
    public TextMeshProUGUI sfxVolumeText;

    [Header("그래픽 설정")]
    public Toggle fullscreenToggle;
    public TMP_Dropdown qualityDropdown;
    public TMP_Dropdown resolutionDropdown;

    [Header("게임 설정")]
    public Toggle autoSaveToggle;
    public Toggle vibrationToggle;
    public TMP_Dropdown languageDropdown;

    [Header("버튼")]
    public Button applyButton;
    public Button resetButton;
    public Button closeButton;

    [Header("설정 텍스트")]
    public TextMeshProUGUI settingsTitleText;
    public TextMeshProUGUI volumeTitleText;
    public TextMeshProUGUI graphicsTitleText;
    public TextMeshProUGUI gameTitleText;

    private void Start()
    {
        InitializeUI();
        LoadSettings();
        SetupEventListeners();
        gameObject.SetActive(false);
    }

    /// <summary>
    /// UI 초기화
    /// </summary>
    private void InitializeUI()
    {
        // 품질 설정 드롭다운 초기화
        qualityDropdown.ClearOptions();
        string[] qualityNames = QualitySettings.names;
        qualityDropdown.AddOptions(new System.Collections.Generic.List<string>(qualityNames));

        // 해상도 드롭다운 초기화
        resolutionDropdown.ClearOptions();
        Resolution[] resolutions = Screen.resolutions;
        System.Collections.Generic.List<string> resolutionOptions = new System.Collections.Generic.List<string>();
        
        for (int i = 0; i < resolutions.Length; i++)
        {
            resolutionOptions.Add($"{resolutions[i].width} x {resolutions[i].height}");
        }
        resolutionDropdown.AddOptions(resolutionOptions);

        // 언어 드롭다운 초기화
        languageDropdown.ClearOptions();
        languageDropdown.AddOptions(new System.Collections.Generic.List<string> { "한국어", "English", "日本語" });
    }

    /// <summary>
    /// 이벤트 리스너 설정
    /// </summary>
    private void SetupEventListeners()
    {
        // 슬라이더 이벤트
        bgmVolumeSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

        // 토글 이벤트
        fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        autoSaveToggle.onValueChanged.AddListener(OnAutoSaveChanged);
        vibrationToggle.onValueChanged.AddListener(OnVibrationChanged);

        // 드롭다운 이벤트
        qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        languageDropdown.onValueChanged.AddListener(OnLanguageChanged);

        // 버튼 이벤트
        applyButton.onClick.AddListener(OnApplyClicked);
        resetButton.onClick.AddListener(OnResetClicked);
        closeButton.onClick.AddListener(OnCloseClicked);
    }

    /// <summary>
    /// 설정 로드
    /// </summary>
    private void LoadSettings()
    {
        // 음량 설정 로드
        float bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 0.7f);
        float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
        
        bgmVolumeSlider.value = bgmVolume;
        sfxVolumeSlider.value = sfxVolume;
        UpdateVolumeTexts();

        // 그래픽 설정 로드
        fullscreenToggle.isOn = Screen.fullScreen;
        qualityDropdown.value = QualitySettings.GetQualityLevel();
        
        // 현재 해상도 찾기
        Resolution currentRes = Screen.currentResolution;
        Resolution[] resolutions = Screen.resolutions;
        for (int i = 0; i < resolutions.Length; i++)
        {
            if (resolutions[i].width == currentRes.width && resolutions[i].height == currentRes.height)
            {
                resolutionDropdown.value = i;
                break;
            }
        }

        // 게임 설정 로드
        autoSaveToggle.isOn = PlayerPrefs.GetInt("AutoSave", 1) == 1;
        vibrationToggle.isOn = PlayerPrefs.GetInt("Vibration", 1) == 1;
        
        // 언어 설정 로드
        if (LocalizationManager.Instance != null)
        {
            languageDropdown.value = LocalizationManager.Instance.GetCurrentLanguageIndex();
        }
        else
        {
            languageDropdown.value = PlayerPrefs.GetInt("Language", 0);
        }
    }

    /// <summary>
    /// 설정 저장
    /// </summary>
    private void SaveSettings()
    {
        // 음량 설정 저장
        PlayerPrefs.SetFloat("BGMVolume", bgmVolumeSlider.value);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolumeSlider.value);

        // 게임 설정 저장
        PlayerPrefs.SetInt("AutoSave", autoSaveToggle.isOn ? 1 : 0);
        PlayerPrefs.SetInt("Vibration", vibrationToggle.isOn ? 1 : 0);
        PlayerPrefs.SetInt("Language", languageDropdown.value);

        PlayerPrefs.Save();
    }

    /// <summary>
    /// BGM 볼륨 변경
    /// </summary>
    private void OnBGMVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetBGMVolume(value);
        }
        UpdateVolumeTexts();
    }

    /// <summary>
    /// SFX 볼륨 변경
    /// </summary>
    private void OnSFXVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(value);
        }
        UpdateVolumeTexts();
    }

    /// <summary>
    /// 볼륨 텍스트 업데이트
    /// </summary>
    private void UpdateVolumeTexts()
    {
        bgmVolumeText.text = $"BGM: {(int)(bgmVolumeSlider.value * 100)}%";
        sfxVolumeText.text = $"SFX: {(int)(sfxVolumeSlider.value * 100)}%";
    }

    /// <summary>
    /// 전체화면 토글 변경
    /// </summary>
    private void OnFullscreenChanged(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }

    /// <summary>
    /// 품질 설정 변경
    /// </summary>
    private void OnQualityChanged(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
    }

    /// <summary>
    /// 해상도 변경
    /// </summary>
    private void OnResolutionChanged(int resolutionIndex)
    {
        Resolution[] resolutions = Screen.resolutions;
        if (resolutionIndex < resolutions.Length)
        {
            Resolution newResolution = resolutions[resolutionIndex];
            Screen.SetResolution(newResolution.width, newResolution.height, Screen.fullScreen);
        }
    }

    /// <summary>
    /// 자동저장 토글 변경
    /// </summary>
    private void OnAutoSaveChanged(bool isEnabled)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.enableAutoSave = isEnabled;
        }
    }

    /// <summary>
    /// 진동 토글 변경
    /// </summary>
    private void OnVibrationChanged(bool isEnabled)
    {
        // 진동 기능 구현 (필요시)
        if (isEnabled)
        {
            Handheld.Vibrate();
        }
    }

    /// <summary>
    /// 언어 변경
    /// </summary>
    private void OnLanguageChanged(int languageIndex)
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.ChangeLanguageByIndex(languageIndex);
            UpdateAllTexts(); // 모든 텍스트 업데이트
        }
    }

    /// <summary>
    /// 모든 텍스트 업데이트
    /// </summary>
    private void UpdateAllTexts()
    {
        if (LocalizationManager.Instance == null) return;

        // 제목들 업데이트
        if (settingsTitleText != null)
            settingsTitleText.text = LocalizationManager.Instance.GetText("settings_title");
        if (volumeTitleText != null)
            volumeTitleText.text = LocalizationManager.Instance.GetText("volume_title");
        if (graphicsTitleText != null)
            graphicsTitleText.text = LocalizationManager.Instance.GetText("graphics_title");
        if (gameTitleText != null)
            gameTitleText.text = LocalizationManager.Instance.GetText("game_title");

        // 버튼 텍스트 업데이트
        if (applyButton != null)
            applyButton.GetComponentInChildren<TextMeshProUGUI>().text = LocalizationManager.Instance.GetText("apply");
        if (resetButton != null)
            resetButton.GetComponentInChildren<TextMeshProUGUI>().text = LocalizationManager.Instance.GetText("reset");
        if (closeButton != null)
            closeButton.GetComponentInChildren<TextMeshProUGUI>().text = LocalizationManager.Instance.GetText("close");
    }

    /// <summary>
    /// 적용 버튼 클릭
    /// </summary>
    private void OnApplyClicked()
    {
        SaveSettings();
        
        // 적용 효과음 재생
        if (UIManager.Instance != null)
        {
            UIManager.Instance.PlayButtonClickSound();
        }

        // 적용 완료 알림
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowNotification("설정이 저장되었습니다!");
        }

        Debug.Log("설정이 적용되었습니다.");
    }

    /// <summary>
    /// 초기화 버튼 클릭
    /// </summary>
    private void OnResetClicked()
    {
        // 기본값으로 초기화
        bgmVolumeSlider.value = 0.7f;
        sfxVolumeSlider.value = 0.8f;
        fullscreenToggle.isOn = true;
        qualityDropdown.value = QualitySettings.GetQualityLevel();
        autoSaveToggle.isOn = true;
        vibrationToggle.isOn = true;
        languageDropdown.value = 0;

        UpdateVolumeTexts();
        
        // 초기화 효과음 재생
        if (UIManager.Instance != null)
        {
            UIManager.Instance.PlayButtonClickSound();
        }

        Debug.Log("설정이 초기화되었습니다.");
    }

    /// <summary>
    /// 닫기 버튼 클릭
    /// </summary>
    private void OnCloseClicked()
    {
        // 닫기 효과음 재생
        if (UIManager.Instance != null)
        {
            UIManager.Instance.PlayButtonClickSound();
        }

        // 설정 패널 숨기기
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HidePanel(UIPanel.Settings);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 설정 패널 표시
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
        LoadSettings();
        UpdateAllTexts(); // 텍스트 업데이트
        
        // 애니메이션 효과 (CanvasGroup 제거)
        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
    }

    /// <summary>
    /// 설정 패널 숨기기
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
} 