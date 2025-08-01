using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// 다국어 지원을 위한 언어 관리 시스템
/// </summary>
public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance;

    [Header("언어 설정")]
    public Language currentLanguage = Language.Korean;

    // 언어별 텍스트 데이터
    private Dictionary<string, Dictionary<Language, string>> textData = new Dictionary<string, Dictionary<Language, string>>();

    // 언어 변경 이벤트
    public Action<Language> OnLanguageChanged;

    public enum Language
    {
        Korean = 0,
        English = 1,
        Japanese = 2
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeLanguageData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 저장된 언어 설정 로드
        LoadLanguageSetting();
    }

    /// <summary>
    /// 언어 데이터 초기화
    /// </summary>
    private void InitializeLanguageData()
    {
        // UI 텍스트들
        AddText("settings_title", "설정", "Settings", "設定");
        AddText("volume_title", "볼륨", "Volume", "音量");
        AddText("graphics_title", "그래픽 설정", "Graphics", "グラフィック設定");
        AddText("game_title", "게임 세팅", "Game Settings", "ゲーム設定");
        AddText("fullscreen", "전체화면", "Fullscreen", "フルスクリーン");
        AddText("quality_setting", "퀄리티 설정", "Quality Setting", "品質設定");
        AddText("resolution", "해상도", "Resolution", "解像度");
        AddText("auto_save", "자동저장", "Auto Save", "自動保存");
        AddText("vibration", "진동", "Vibration", "振動");
        AddText("language_setting", "언어 설정", "Language Setting", "言語設定");
        AddText("apply", "적용", "Apply", "適用");
        AddText("reset", "초기화", "Reset", "リセット");
        AddText("close", "닫기", "Close", "閉じる");
        AddText("settings_saved", "설정이 저장되었습니다!", "Settings saved!", "設定が保存されました！");

        // 게임 텍스트들
        AddText("battle", "전투", "Battle", "戦闘");
        AddText("gacha", "가챠", "Gacha", "ガチャ");
        AddText("character", "캐릭터", "Character", "キャラクター");
        AddText("level", "레벨", "Level", "レベル");
        AddText("power", "파워", "Power", "パワー");
        AddText("upgrade", "강화", "Upgrade", "強化");
        AddText("upgrade_success", "강화 성공!", "Upgrade Success!", "強化成功！");
        AddText("max_level", "최대 레벨입니다!", "Max Level!", "最大レベルです！");
        AddText("not_enough_gold", "골드가 부족합니다!", "Not enough gold!", "ゴールドが不足しています！");
    }

    /// <summary>
    /// 텍스트 데이터 추가
    /// </summary>
    private void AddText(string key, string korean, string english, string japanese)
    {
        textData[key] = new Dictionary<Language, string>
        {
            { Language.Korean, korean },
            { Language.English, english },
            { Language.Japanese, japanese }
        };
    }

    /// <summary>
    /// 언어별 텍스트 가져오기
    /// </summary>
    public string GetText(string key)
    {
        if (textData.TryGetValue(key, out var languageData))
        {
            if (languageData.TryGetValue(currentLanguage, out var text))
            {
                return text;
            }
        }
        
        // 키가 없으면 키 자체를 반환
        Debug.LogWarning($"텍스트 키를 찾을 수 없습니다: {key}");
        return key;
    }

    /// <summary>
    /// 언어 변경
    /// </summary>
    public void ChangeLanguage(Language newLanguage)
    {
        if (currentLanguage != newLanguage)
        {
            currentLanguage = newLanguage;
            SaveLanguageSetting();
            OnLanguageChanged?.Invoke(currentLanguage);
            
            Debug.Log($"언어가 변경되었습니다: {currentLanguage}");
        }
    }

    /// <summary>
    /// 언어 설정 저장
    /// </summary>
    private void SaveLanguageSetting()
    {
        PlayerPrefs.SetInt("Language", (int)currentLanguage);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 언어 설정 로드
    /// </summary>
    private void LoadLanguageSetting()
    {
        int savedLanguage = PlayerPrefs.GetInt("Language", 0);
        currentLanguage = (Language)savedLanguage;
    }

    /// <summary>
    /// 현재 언어 인덱스 가져오기
    /// </summary>
    public int GetCurrentLanguageIndex()
    {
        return (int)currentLanguage;
    }

    /// <summary>
    /// 언어 인덱스로 언어 변경
    /// </summary>
    public void ChangeLanguageByIndex(int index)
    {
        if (index >= 0 && index < Enum.GetValues(typeof(Language)).Length)
        {
            ChangeLanguage((Language)index);
        }
    }
} 