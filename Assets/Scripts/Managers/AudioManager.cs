using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

/// <summary>
/// 게임의 모든 오디오를 관리하는 매니저 클래스
/// 배경음악, 효과음, 볼륨 조절 등을 담당
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    public AudioSource bgmSource;        // 배경음악용
    public AudioSource sfxSource;        // 효과음용
    public AudioSource uiSource;         // UI 효과음용

    [Header("Audio Mixer")]
    public AudioMixer audioMixer;        // 오디오 믹서

    [Header("Audio Clips")]
    public AudioClip[] bgmClips;         // 배경음악 클립들
    public AudioClip[] sfxClips;         // 효과음 클립들
    public AudioClip[] uiClips;          // UI 효과음 클립들

    [Header("Settings")]
    public float bgmVolume = 0.7f;       // 배경음악 볼륨
    public float sfxVolume = 0.8f;       // 효과음 볼륨
    public float uiVolume = 0.6f;        // UI 효과음 볼륨
    public bool isBGMEnabled = true;     // 배경음악 활성화 여부
    public bool isSFXEnabled = true;     // 효과음 활성화 여부

    // 오디오 클립 딕셔너리 (빠른 검색용)
    private Dictionary<string, AudioClip> bgmDictionary = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> sfxDictionary = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> uiDictionary = new Dictionary<string, AudioClip>();

    private void Awake()
    {
        // 싱글톤 패턴
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioManager();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 저장된 설정 불러오기
        LoadAudioSettings();
        
        // 게임 시작 시 기본 BGM 재생
        StartDefaultBGM();
    }

    /// <summary>
    /// 오디오 매니저 초기화
    /// </summary>
    private void InitializeAudioManager()
    {
        // AudioSource가 없으면 자동 생성
        if (bgmSource == null)
        {
            GameObject bgmObj = new GameObject("BGM Source");
            bgmObj.transform.SetParent(transform);
            bgmSource = bgmObj.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
        }

        if (sfxSource == null)
        {
            GameObject sfxObj = new GameObject("SFX Source");
            sfxObj.transform.SetParent(transform);
            sfxSource = sfxObj.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }

        if (uiSource == null)
        {
            GameObject uiObj = new GameObject("UI Source");
            uiObj.transform.SetParent(transform);
            uiSource = uiObj.AddComponent<AudioSource>();
            uiSource.loop = false;
            uiSource.playOnAwake = false;
        }

        // 딕셔너리 초기화
        InitializeAudioDictionaries();
    }

    /// <summary>
    /// 오디오 클립 딕셔너리 초기화
    /// </summary>
    private void InitializeAudioDictionaries()
    {
        // BGM 딕셔너리
        foreach (var clip in bgmClips)
        {
            if (clip != null)
                bgmDictionary[clip.name] = clip;
        }

        // SFX 딕셔너리
        foreach (var clip in sfxClips)
        {
            if (clip != null)
                sfxDictionary[clip.name] = clip;
        }

        // UI 딕셔너리
        foreach (var clip in uiClips)
        {
            if (clip != null)
                uiDictionary[clip.name] = clip;
        }
    }

    #region BGM (배경음악) 메서드들

    /// <summary>
    /// 배경음악 재생
    /// </summary>
    public void PlayBGM(string clipName)
    {
        if (!isBGMEnabled) return;

        if (bgmDictionary.TryGetValue(clipName, out AudioClip clip))
        {
            bgmSource.clip = clip;
            bgmSource.volume = bgmVolume;
            bgmSource.Play();
            Debug.Log($"BGM 재생: {clipName}");
        }
        else
        {
            Debug.LogWarning($"BGM 클립을 찾을 수 없습니다: {clipName}");
            
            // 기본 BGM 재생 (bgmClips가 비어있지 않다면 첫 번째 클립 사용)
            if (bgmClips != null && bgmClips.Length > 0 && bgmClips[0] != null)
            {
                bgmSource.clip = bgmClips[0];
                bgmSource.volume = bgmVolume;
                bgmSource.Play();
                Debug.Log($"기본 BGM 재생: {bgmClips[0].name}");
            }
            else
            {
                Debug.LogWarning("BGM 클립이 설정되지 않았습니다. AudioManager의 bgmClips 배열에 오디오 파일을 추가해주세요.");
            }
        }
    }

    /// <summary>
    /// 배경음악 정지
    /// </summary>
    public void StopBGM()
    {
        bgmSource.Stop();
        Debug.Log("BGM 정지");
    }

    /// <summary>
    /// 배경음악 일시정지
    /// </summary>
    public void PauseBGM()
    {
        bgmSource.Pause();
        Debug.Log("BGM 일시정지");
    }

    /// <summary>
    /// 배경음악 재개
    /// </summary>
    public void ResumeBGM()
    {
        bgmSource.UnPause();
        Debug.Log("BGM 재개");
    }

    /// <summary>
    /// 배경음악 페이드 인
    /// </summary>
    public void FadeInBGM(float duration = 1f)
    {
        StartCoroutine(FadeBGM(0f, bgmVolume, duration));
    }

    /// <summary>
    /// 배경음악 페이드 아웃
    /// </summary>
    public void FadeOutBGM(float duration = 1f)
    {
        StartCoroutine(FadeBGM(bgmVolume, 0f, duration));
    }

    #endregion

    #region SFX (효과음) 메서드들

    /// <summary>
    /// 효과음 재생
    /// </summary>
    public void PlaySFX(string clipName)
    {
        if (!isSFXEnabled) return;

        if (sfxDictionary.TryGetValue(clipName, out AudioClip clip))
        {
            sfxSource.PlayOneShot(clip, sfxVolume);
            Debug.Log($"SFX 재생: {clipName}");
        }
        else
        {
            Debug.LogWarning($"SFX 클립을 찾을 수 없습니다: {clipName}");
        }
    }

    /// <summary>
    /// UI 효과음 재생
    /// </summary>
    public void PlayUISound(string clipName)
    {
        if (!isSFXEnabled) return;

        if (uiDictionary.TryGetValue(clipName, out AudioClip clip))
        {
            uiSource.PlayOneShot(clip, uiVolume);
            Debug.Log($"UI Sound 재생: {clipName}");
        }
        else
        {
            Debug.LogWarning($"UI Sound 클립을 찾을 수 없습니다: {clipName}");
        }
    }

    #endregion

    #region 볼륨 설정 메서드들

    /// <summary>
    /// 배경음악 볼륨 설정
    /// </summary>
    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        bgmSource.volume = bgmVolume;
        PlayerPrefs.SetFloat("BGMVolume", bgmVolume);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 효과음 볼륨 설정
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        uiVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 배경음악 활성화/비활성화
    /// </summary>
    public void SetBGMEnabled(bool enabled)
    {
        isBGMEnabled = enabled;
        if (!enabled)
            StopBGM();
        PlayerPrefs.SetInt("BGMEnabled", enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 효과음 활성화/비활성화
    /// </summary>
    public void SetSFXEnabled(bool enabled)
    {
        isSFXEnabled = enabled;
        PlayerPrefs.SetInt("SFXEnabled", enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    #endregion

    #region 설정 저장/불러오기

    /// <summary>
    /// 오디오 설정 저장
    /// </summary>
    public void SaveAudioSettings()
    {
        PlayerPrefs.SetFloat("BGMVolume", bgmVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.SetInt("BGMEnabled", isBGMEnabled ? 1 : 0);
        PlayerPrefs.SetInt("SFXEnabled", isSFXEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 오디오 설정 불러오기
    /// </summary>
    public void LoadAudioSettings()
    {
        bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 0.7f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
        uiVolume = sfxVolume;
        isBGMEnabled = PlayerPrefs.GetInt("BGMEnabled", 1) == 1;
        isSFXEnabled = PlayerPrefs.GetInt("SFXEnabled", 1) == 1;

        // 현재 재생 중인 BGM에 볼륨 적용
        if (bgmSource.isPlaying)
            bgmSource.volume = bgmVolume;
    }

    #endregion

    #region 유틸리티 메서드들

    /// <summary>
    /// BGM 페이드 코루틴
    /// </summary>
    private System.Collections.IEnumerator FadeBGM(float startVolume, float endVolume, float duration)
    {
        float currentTime = 0f;
        bgmSource.volume = startVolume;

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(startVolume, endVolume, currentTime / duration);
            yield return null;
        }

        bgmSource.volume = endVolume;
    }

    /// <summary>
    /// 모든 오디오 정지
    /// </summary>
    public void StopAllAudio()
    {
        StopBGM();
        sfxSource.Stop();
        uiSource.Stop();
    }

    /// <summary>
    /// 게임 시작 시 기본 BGM 재생
    /// </summary>
    private void StartDefaultBGM()
    {
        if (isBGMEnabled && bgmClips != null && bgmClips.Length > 0 && bgmClips[0] != null)
        {
            bgmSource.clip = bgmClips[0];
            bgmSource.volume = bgmVolume;
            bgmSource.Play();
            Debug.Log($"게임 시작 - 기본 BGM 재생: {bgmClips[0].name}");
        }
        else
        {
            Debug.Log("BGM 클립이 설정되지 않았습니다. AudioManager의 bgmClips 배열에 오디오 파일을 추가해주세요.");
        }
    }

    /// <summary>
    /// 테스트용 BGM 재생 (개발 중 확인용)
    /// </summary>
    [ContextMenu("테스트 BGM 재생")]
    public void TestBGM()
    {
        if (bgmClips != null && bgmClips.Length > 0 && bgmClips[0] != null)
        {
            PlayBGM(bgmClips[0].name);
        }
        else
        {
            Debug.LogWarning("테스트할 BGM 클립이 없습니다!");
        }
    }

    #endregion
} 