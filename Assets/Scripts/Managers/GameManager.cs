using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 게임의 전체 상태를 관리하는 매니저 클래스
/// 게임 상태, 일시정지, 씬 전환, 게임 데이터 등을 담당
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("게임 상태")]
    public GameState currentState = GameState.MainMenu;
    public bool isPaused = false;
    public bool isGameOver = false;

    [Header("게임 데이터")]
    public int totalPlayTime = 0;        // 총 플레이 시간 (초)
    public int totalBattles = 0;         // 총 전투 횟수
    public int totalGachaPulls = 0;      // 가챠 실행 횟수 (1연/10연 각 1회)
    public string playerName = "플레이어"; // 플레이어 이름

    [Header("UI 참조")]
    public GameObject pausePanel;        // 일시정지 패널
    public GameObject gameOverPanel;     // 게임 오버 패널
    public GameObject loadingPanel;      // 로딩 패널

    [Header("설정")]
    public float autoSaveInterval = 60f; // 자동 저장 간격 (초)
    public bool enableAutoSave = true;   // 자동 저장 활성화

    // 게임 시작 시간
    private int accumulatedPlayTime;
    private float sessionStartTime;
    private float lastAutoSaveTime;
    private GameState stateBeforePause;

    // 이벤트
    public System.Action<GameState> OnGameStateChanged;
    public System.Action<bool> OnPauseStateChanged;

    private void Awake()
    {
        // 싱글톤 패턴
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeGameManager();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        sessionStartTime = Time.time;
        lastAutoSaveTime = Time.time;

        // 초기 게임 상태 설정
        SetGameState(GameState.Playing); // MainMenu 대신 Playing으로 시작

        // UI 초기화
        InitializeUI();

        // 저장된 게임 데이터 불러오기
        LoadGameData();
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBGM("gameplay");
        }
    }

    private void Update()
    {
        if (IsGamePlaying())
            totalPlayTime = accumulatedPlayTime + Mathf.FloorToInt(Time.time - sessionStartTime);

        // 자동 저장 체크
        if (enableAutoSave && Time.time - lastAutoSaveTime >= autoSaveInterval)
        {
            AutoSave();
            lastAutoSaveTime = Time.time;
        }

        // ESC 키로 일시정지
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    /// <summary>
    /// 게임 매니저 초기화
    /// </summary>
    private void InitializeGameManager()
    {
        // 게임 품질 설정
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;

        // 화면 설정
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    /// <summary>
    /// UI 초기화
    /// </summary>
    private void InitializeUI()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }

    #region 게임 상태 관리

    /// <summary>
    /// 게임 상태 설정
    /// </summary>
    public void SetGameState(GameState newState)
    {
        GameState previousState = currentState;
        currentState = newState;

        switch (newState)
        {
            case GameState.MainMenu:
                HandleMainMenuState();
                break;
            case GameState.Playing:
                HandlePlayingState();
                break;
            case GameState.Paused:
                HandlePausedState();
                break;
            case GameState.Battle:
                HandleBattleState();
                break;
            case GameState.Gacha:
                HandleGachaState();
                break;
            case GameState.GameOver:
                HandleGameOverState();
                break;
        }

        // UIManager 업데이트
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateUIForGameState(newState);
        }

        // 이벤트 호출
        OnGameStateChanged?.Invoke(newState);
    }

    private void HandleMainMenuState()
    {
        Time.timeScale = 1f;
        isPaused = false;
        
        // 메인 메뉴 BGM 재생
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBGM("main_menu");
        }
    }

    private void HandlePlayingState()
    {
        Time.timeScale = 1f;
        isPaused = false;
        
        // 게임 플레이 BGM 재생
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBGM("gameplay");
        }
    }

    private void HandlePausedState()
    {
        Time.timeScale = 0f;
        isPaused = true;
        
        // 일시정지 패널 표시
        if (pausePanel != null)
            pausePanel.SetActive(true);
    }

    private void HandleBattleState()
    {
        Time.timeScale = 1f;
        isPaused = false;
        
        // 전투 BGM 재생
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBGM("battle");
        }
    }

    private void HandleGachaState()
    {
        Time.timeScale = 1f;
        isPaused = false;
        
        // 가챠 BGM 재생
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBGM("gacha");
        }
    }

    private void HandleGameOverState()
    {
        Time.timeScale = 0f;
        isGameOver = true;
        
        // 게임 오버 패널 표시
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }

    #endregion

    #region 일시정지 관리

    /// <summary>
    /// 일시정지 토글
    /// </summary>
    public void TogglePause()
    {
        if (currentState == GameState.Playing || currentState == GameState.Battle || currentState == GameState.Gacha)
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    /// <summary>
    /// 게임 일시정지
    /// </summary>
    public void PauseGame()
    {
        if (isPaused) return;

        FlushSessionPlayTime();
        stateBeforePause = currentState;
        SetGameState(GameState.Paused);
        OnPauseStateChanged?.Invoke(true);
    }

    /// <summary>
    /// 게임 재개
    /// </summary>
    public void ResumeGame()
    {
        if (!isPaused) return;

        sessionStartTime = Time.time;

        GameState resumeState = stateBeforePause;
        if (resumeState == GameState.Paused || resumeState == GameState.MainMenu || resumeState == GameState.GameOver)
            resumeState = GameState.Playing;

        SetGameState(resumeState);
        OnPauseStateChanged?.Invoke(false);

        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    #endregion

    #region 씬 전환

    /// <summary>
    /// 메인 메뉴로 이동
    /// </summary>
    public void LoadMainMenu()
    {
        StartCoroutine(LoadSceneAsync("MainMenu"));
    }

    /// <summary>
    /// 게임 씬으로 이동
    /// </summary>
    public void LoadGameScene()
    {
        StartCoroutine(LoadSceneAsync("GameScene"));
    }

    /// <summary>
    /// 씬 비동기 로드
    /// </summary>
    private IEnumerator LoadSceneAsync(string sceneName)
    {
        // 로딩 패널 표시
        if (loadingPanel != null)
            loadingPanel.SetActive(true);

        // 현재 씬 언로드
        AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene());
        while (!unloadOperation.isDone)
        {
            yield return null;
        }

        // 새 씬 로드
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);
        while (!loadOperation.isDone)
        {
            yield return null;
        }

        // 로딩 패널 숨기기
        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        // 게임 상태 설정
        if (sceneName == "MainMenu")
            SetGameState(GameState.MainMenu);
        else
            SetGameState(GameState.Playing);
    }

    #endregion

    #region 게임 데이터 관리

    /// <summary>
    /// 게임 데이터 저장
    /// </summary>
    public void SaveGameData()
    {
        if (SaveManager.Instance == null)
            return;

        var ownedCharacters = PlayerInventory.Instance != null
            ? PlayerInventory.Instance.Characters
            : new System.Collections.Generic.List<OwnedCharacter>();

        SaveManager.Instance.SaveAllData(ownedCharacters);
    }

    public void LoadGameData()
    {
        if (SaveManager.Instance == null)
            return;

        SaveWrapper saveData = SaveManager.Instance.GetSaveData();
        ApplySaveStatistics(
            saveData.totalPlayTime,
            saveData.totalBattles,
            saveData.totalGachaPulls,
            saveData.playerName
        );
    }

    public void ApplySaveStatistics(int playTime, int battles, int gachaPulls, string name)
    {
        accumulatedPlayTime = Mathf.Max(0, playTime);
        totalPlayTime = accumulatedPlayTime;
        sessionStartTime = Time.time;
        totalBattles = battles;
        totalGachaPulls = gachaPulls;
        playerName = string.IsNullOrEmpty(name) ? "플레이어" : name;
    }

    public void WriteSaveStatistics(SaveWrapper wrapper)
    {
        FlushSessionPlayTime();
        wrapper.totalPlayTime = accumulatedPlayTime;
        wrapper.totalBattles = totalBattles;
        wrapper.totalGachaPulls = totalGachaPulls;
        wrapper.playerName = playerName;
    }

    private void FlushSessionPlayTime()
    {
        if (!IsGamePlaying())
            return;

        accumulatedPlayTime += Mathf.FloorToInt(Time.time - sessionStartTime);
        sessionStartTime = Time.time;
        totalPlayTime = accumulatedPlayTime;
    }

    /// <summary>
    /// 자동 저장
    /// </summary>
    private void AutoSave()
    {
        SaveGameData();
    }

    #endregion

    #region 게임 통계

    /// <summary>
    /// 전투 횟수 증가
    /// </summary>
    public void IncrementBattleCount()
    {
        totalBattles++;
        SaveGameData();
    }

    /// <summary>
    /// 가챠 실행 횟수 증가 (1연/10연 각 1회)
    /// </summary>
    public void IncrementGachaSessionCount(bool persist = true)
    {
        totalGachaPulls++;
        if (persist)
            SaveGameData();
    }

    /// <summary>
    /// 게임 통계 가져오기
    /// </summary>
    public GameStatistics GetGameStatistics()
    {
        return new GameStatistics
        {
            totalPlayTime = totalPlayTime,
            totalBattles = totalBattles,
            totalGachaPulls = totalGachaPulls,
            playerName = playerName
        };
    }

    #endregion

    #region 게임 종료

    /// <summary>
    /// 게임 종료
    /// </summary>
    public void QuitGame()
    {
        FlushSessionPlayTime();
        SaveGameData();
        
        // 오디오 정지
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopAllAudio();
        }
        
        Debug.Log("게임 종료");
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    /// <summary>
    /// 게임 재시작
    /// </summary>
    public void RestartGame()
    {
        accumulatedPlayTime = 0;
        totalPlayTime = 0;
        sessionStartTime = Time.time;
        totalBattles = 0;
        totalGachaPulls = 0;
        
        // 게임 상태 초기화
        isGameOver = false;
        isPaused = false;
        
        // 게임 오버 패널 숨기기
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        
        // 게임 상태를 플레이로 설정
        SetGameState(GameState.Playing);
        
        Debug.Log("게임 재시작");
    }

    #endregion

    #region 유틸리티

    /// <summary>
    /// 게임이 일시정지 상태인지 확인
    /// </summary>
    public bool IsGamePaused()
    {
        return isPaused || currentState == GameState.Paused;
    }

    /// <summary>
    /// 게임이 플레이 중인지 확인
    /// </summary>
    public bool IsGamePlaying()
    {
        return currentState == GameState.Playing || currentState == GameState.Battle || currentState == GameState.Gacha;
    }

    #endregion
}

/// <summary>
/// 게임 상태 열거형
/// </summary>
public enum GameState
{
    MainMenu,    // 메인 메뉴
    Playing,     // 게임 플레이 중
    Paused,      // 일시정지
    Battle,      // 전투 중
    Gacha,       // 가챠 중
    GameOver     // 게임 오버
}

/// <summary>
/// 게임 통계 데이터 구조
/// </summary>
[System.Serializable]
public struct GameStatistics
{
    public int totalPlayTime;
    public int totalBattles;
    public int totalGachaPulls;
    public string playerName;
} 