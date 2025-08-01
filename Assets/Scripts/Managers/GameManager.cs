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
    public int totalGachaPulls = 0;      // 총 가챠 뽑기 횟수
    public string playerName = "플레이어"; // 플레이어 이름

    [Header("UI 참조")]
    public GameObject pausePanel;        // 일시정지 패널
    public GameObject gameOverPanel;     // 게임 오버 패널
    public GameObject loadingPanel;      // 로딩 패널

    [Header("설정")]
    public float autoSaveInterval = 60f; // 자동 저장 간격 (초)
    public bool enableAutoSave = true;   // 자동 저장 활성화

    // 게임 시작 시간
    private float gameStartTime;
    private float lastAutoSaveTime;

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
        // 게임 시작 시간 기록
        gameStartTime = Time.time;
        lastAutoSaveTime = Time.time;

        // 초기 게임 상태 설정
        SetGameState(GameState.Playing); // MainMenu 대신 Playing으로 시작

        // UI 초기화
        InitializeUI();

        // 저장된 게임 데이터 불러오기
        LoadGameData();
        
        // AudioManager가 있다면 기본 BGM 재생
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBGM("gameplay");
        }
    }

    private void Update()
    {
        // 플레이 시간 업데이트
        if (currentState == GameState.Playing)
        {
            totalPlayTime = Mathf.FloorToInt(Time.time - gameStartTime);
        }

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

        Debug.Log($"게임 상태 변경: {previousState} -> {newState}");

        // 상태별 처리
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

        SetGameState(GameState.Paused);
        OnPauseStateChanged?.Invoke(true);
        
        Debug.Log("게임 일시정지");
    }

    /// <summary>
    /// 게임 재개
    /// </summary>
    public void ResumeGame()
    {
        if (!isPaused) return;

        // 이전 상태로 복원
        if (currentState == GameState.Paused)
        {
            SetGameState(GameState.Playing);
        }

        OnPauseStateChanged?.Invoke(false);
        
        // 일시정지 패널 숨기기
        if (pausePanel != null)
            pausePanel.SetActive(false);
        
        Debug.Log("게임 재개");
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
        PlayerPrefs.SetInt("TotalPlayTime", totalPlayTime);
        PlayerPrefs.SetInt("TotalBattles", totalBattles);
        PlayerPrefs.SetInt("TotalGachaPulls", totalGachaPulls);
        PlayerPrefs.SetString("PlayerName", playerName);
        PlayerPrefs.Save();

        Debug.Log("게임 데이터 저장 완료");
    }

    /// <summary>
    /// 게임 데이터 불러오기
    /// </summary>
    public void LoadGameData()
    {
        totalPlayTime = PlayerPrefs.GetInt("TotalPlayTime", 0);
        totalBattles = PlayerPrefs.GetInt("TotalBattles", 0);
        totalGachaPulls = PlayerPrefs.GetInt("TotalGachaPulls", 0);
        playerName = PlayerPrefs.GetString("PlayerName", "플레이어");

        Debug.Log("게임 데이터 불러오기 완료");
    }

    /// <summary>
    /// 자동 저장
    /// </summary>
    private void AutoSave()
    {
        SaveGameData();
        
        // 다른 매니저들도 저장
        if (SaveManager.Instance != null)
        {
            // SaveManager의 저장 메서드 호출
        }
        
        Debug.Log("자동 저장 완료");
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
    /// 가챠 뽑기 횟수 증가
    /// </summary>
    public void IncrementGachaCount()
    {
        totalGachaPulls++;
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
        // 게임 데이터 저장
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
        // 게임 데이터 초기화
        totalPlayTime = 0;
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