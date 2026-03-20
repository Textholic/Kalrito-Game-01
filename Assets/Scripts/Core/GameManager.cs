// ============================================================
// GameManager.cs
// 전역 싱글턴. DontDestroyOnLoad로 씬 간 유지.
// 하위 컴포넌트로 PlayerStats, InventoryManager, 
// EngravingManager, GameHistoryManager, AudioManager 보유.
// ============================================================
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // ── 싱글턴 ──────────────────────────────────────────────────────────────
    public static GameManager Instance { get; private set; }

    // ── 씬 이름 상수 ─────────────────────────────────────────────────────────
    public const string SCENE_TITLE   = "TitleScene";
    public const string SCENE_LOBBY   = "GameOptionsScene";   // 대기 화면
    public const string SCENE_SETTING = "SettingsScene";      // 환경 설정
    public const string SCENE_HISTORY = "GameHistoryScene";   // 게임 이력
    public const string SCENE_GAME    = "GameScene";          // 게임 화면

    // ── 서브시스템 ───────────────────────────────────────────────────────────
    public PlayerStats       Player    { get; private set; }
    public InventoryManager  Inventory { get; private set; }
    public EngravingManager  Engraving { get; private set; }
    public GameHistoryManager History  { get; private set; }
    public AudioManager      Audio     { get; private set; }

    // ── UI 인터페이스 (GameScene 진입 시 HUD가 등록) ──────────────────────────
    public IGameUI UI { get; set; }

    // ── 게임 상태 ─────────────────────────────────────────────────────────────
    public bool IsGameActive  { get; private set; } = false;
    public int  CurrentFloor  { get; private set; } = 1;

    /// <summary>사망 후 로비로 돌아올 때 표시할 새 각인 이름. null이면 표시 없음.</summary>
    public string PendingNewEngravingName { get; set; }
    /// <summary>사망 시 각인 롤이 제실행되었는지 (true이면 보여주기 포하세되어도 팝업 표시).</summary>
    public bool PendingEngravingRolled { get; set; }

    // ── 던전 설정 에셋 (Inspector에서 DungeonFloorConfig을 연결) ──────────────
    [Header("던전 설정 에셋")]
    [Tooltip("Assets/Data/DungeonConfig.asset 을 연결하세요")]
    public DungeonFloorConfig DungeonConfig;

    // ── 초기화 ───────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 서브시스템 컴포넌트 취득 또는 생성
        Player    = GetOrAdd<PlayerStats>();
        Inventory = GetOrAdd<InventoryManager>();
        Engraving = GetOrAdd<EngravingManager>();
        History   = GetOrAdd<GameHistoryManager>();
        Audio     = GetOrAdd<AudioManager>();

        // 달성목표 해제 시 DialogueManager 캐시도 무효화 (다음 대기화면 진입에 반영)
        History.OnAchievementUnlocked += _ => DialogueManager.InvalidateCache();
    }

    private T GetOrAdd<T>() where T : UnityEngine.Component
    {
        T comp = GetComponentInChildren<T>(true);
        return comp != null ? comp : gameObject.AddComponent<T>();
    }

    // ── 게임 흐름 ─────────────────────────────────────────────────────────────

    /// <summary>대기화면에서 [던전 입장] 버튼 클릭 시 호출.</summary>
    public void StartNewGame()
    {
        Player.InitializeNewGame();
        Inventory.Reset();
        Engraving.ApplyAllEngravings(Player);   // 각인 효과 적용

        CurrentFloor = 1;
        IsGameActive = true;
        PendingEngravingRolled = false;
        PendingNewEngravingName = null;
        History.RecordFloor(1);
        SceneManager.LoadScene(SCENE_GAME);
    }

    /// <summary>계단 밟을 때 호출. 다음 층으로 진행.</summary>
    public void AdvanceFloor()
    {
        CurrentFloor++;
        History.RecordFloor(CurrentFloor);
    }

    /// <summary>플레이어 사망 시 호출.</summary>
    public void OnPlayerDeath()
    {
        IsGameActive = false;
        History.RecordDeath();

        // 카르마 수치 기반 각인 해금 시도 (중복 방지 포함)
        PendingEngravingRolled = true;   // 실패해도 팝업을 표시하낵 표시
        var newEng = Engraving.TryUnlockOnDeath(Player.Karma);
        if (newEng != null)
            PendingNewEngravingName = newEng.engravingName;

        SceneManager.LoadScene(SCENE_LOBBY);
    }

    // ── 씬 전환 단축 메서드 ──────────────────────────────────────────────────
    public void GoToLobby()   => SceneManager.LoadScene(SCENE_LOBBY);
    public void GoToSetting() => SceneManager.LoadScene(SCENE_SETTING);
    public void GoToHistory() => SceneManager.LoadScene(SCENE_HISTORY);
    public void GoToTitle()   => SceneManager.LoadScene(SCENE_TITLE);
}
