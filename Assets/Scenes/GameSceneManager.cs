using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class GameSceneManager : MonoBehaviour
{
    // ── 맵 설정 ──────────────────────────────────────────────────────────────
    private const int   MapWidth  = 64;
    private const int   MapHeight = 48;
    private const float TileSize  = 2.0f;

    private char[,] map;
    private bool[,] explored;
    private bool[,] visible;

    // ── 스프라이트 (Inspector / 에디터 스크립트 연결) ─────────────────────────
    [Header("Tile Sprites")]
    [SerializeField] private Sprite floorSprite;
    [SerializeField] private Sprite wallSprite;
    [SerializeField] private Sprite stairsSprite; // Pentagram_Activated.png

    [Header("Entity Sprites")]
    [SerializeField] private Sprite playerSprite;  // 시트 미연결 시 폴백 단일 스프라이트
    [Header("Player Direction Sprites (방향당 1장)")]
    [Tooltip("아래 방향 / 대기 스프라이트")]
    [SerializeField] private Sprite playerSpriteDown;
    [Tooltip("위 방향 스프라이트")]
    [SerializeField] private Sprite playerSpriteUp;
    [Tooltip("왼쪽 방향 스프라이트")]
    [SerializeField] private Sprite playerSpriteLeft;
    [Tooltip("오른쪽 방향 스프라이트")]
    [SerializeField] private Sprite playerSpriteRight;
    [Tooltip("쓰러짐 스프라이트 (사망 시)")]
    [SerializeField] private Sprite playerSpriteDowned;
    [Header("Enemy Slots (최대 30종 — Inspector에서 등록)")]
    [SerializeField] private EnemySlot[] enemySlots = new EnemySlot[0];

    [Header("Item Sprites")]
    [SerializeField] private Sprite potionSprite;  // Potion.png
    [SerializeField] private Sprite goldSmSprite;  // Coin.png  (소형 골드)
    [SerializeField] private Sprite goldLgSprite;  // Diamond.png (대형 골드)

    [Header("Treasure Chest Sprites (lootbox.png)")]
    [Tooltip("닫힌 상자 스프라이트 — 생략 시 Resources/lootbox.png 슬라이스 0 자동 로드")]
    [SerializeField] private Sprite chestClosedSprite;
    [Tooltip("열린 상자 스프라이트 — 생략 시 Resources/lootbox.png 슬라이스 1 자동 로드")]
    [SerializeField] private Sprite chestOpenedSprite;

    [Header("Background")]
    [SerializeField] private Sprite bgSprite;

    [Header("Boss Corridor Sprites")]
    [Tooltip("보스 복도 입구 문 스프라이트 (Gothic/Door.png)")]
    [SerializeField] private Sprite   doorSprite;
    [Tooltip("상점 NPC 스프라이트 두 장 (shop.png 슬라이스) — 람덤 표시")]
    [SerializeField] private Sprite[] shopNpcSprites;

    [Header("HUD Sprites (Brackeys)")]
    [SerializeField] private Sprite inventoryIconSprite;   // Scroll.png
    [SerializeField] private Sprite equipmentIconSprite;   // Iron.png

    // ── 사운드 (Inspector / 에디터 스크립트 연결) ─────────────────────────────
    [Header("Sounds")]
    [SerializeField] private AudioClip sfxHit;       // Hit.wav
    [SerializeField] private AudioClip sfxBonus;     // Bonus.wav
    [SerializeField] private AudioClip sfxGameOver;  // GameOver.wav
    [SerializeField] private AudioClip sfxStairs;    // Spawn.wav
    [SerializeField] private AudioClip sfxEnemyHit;  // GruntVoice01.wav
    [Tooltip("루트박스·문 열기 사운드 (Assets/Sounds/lootbox_open.ogg)")]
    [SerializeField] private AudioClip sfxLootboxOpen;
    [Tooltip("적 사망 사운드 풀 (Assets/Sounds/death_sound01~03.ogg 랜덤)")]
    [SerializeField] private AudioClip[] sfxDeathPool;
    [SerializeField] private AudioClip bgmCombat;    // Click.wav → 배경음으로 루프
    [Tooltip("보스방 전용 BGM 목록 (진입 시 랜덤 재생, 없으면 bgmCombat 유지)")]
    [SerializeField] private AudioClip[] bgmBossPool; // Assets/Sounds/Character/boss_01~10.ogg

    private AudioSource bgmSource;
    private AudioSource sfxSource;
    private AudioClip   _preBossBgm; // 보스방 진입 전 재생 중이던 BGM 저장용

    // ── 에너미 슬롯 정의 (Inspector에서 최대 30종 등록) ─────────────────────
    [System.Serializable]
    public class EnemySlot
    {
        [Tooltip("몬스터 이름")]
        public string name        = "몬스터";
        [Tooltip("적 설명 (최대 50자 권장)")]
        [TextArea(1, 2)]
        public string description = "";
        [Tooltip("보스 몬스터 여부 — 체크 시 3층마다 보스층에만 완정 1마리 등장")]
        public bool   isBoss      = false;
        [Tooltip("오른쪽 바라보는 스프라이트")]
        public Sprite spriteRight;
        [Tooltip("왼쪽 바라보는 스프라이트 (없으면 spriteRight 반전)")]
        public Sprite spriteLeft;
        [Tooltip("스프라이트 틴트 색상")]
        public Color  color       = Color.white;
        [Tooltip("체력 배율 (기준 대비, 기본 1.0)")]
        [Range(0.1f, 10f)] public float hpScale  = 1f;
        [Tooltip("공격력 배율 (기본 1.0)")]
        [Range(0.1f, 10f)] public float atkScale = 1f;
        [Tooltip("경험치 배율 (기본 1.0)")]
        [Range(0.1f, 10f)] public float expScale = 1f;
        [Tooltip("방어력 배율 — 방어력 = floor(층수 × 이 값), 30% 확률로 피해 경감 (기본 0 = 무방어)")]
        [Range(0f, 5f)] public float defScale = 0f;
        [Tooltip("보스 전용 BGM (isBoss=true & bgmBoss 미설정 시 대체로 사용)")]
        public AudioClip bossBattleBgm;
    }

    // ── 엔티티 ─────────────────────────────────────────────────────────────
    private enum PlayerFacing { Down, Up, Left, Right }

    [System.Serializable]
    public class Entity
    {
        public string      name;
        public string      description; // EnemySlot.description에서 복사
        public bool        isBoss;      // EnemySlot.isBoss에서 복사
        public Vector2Int  pos;
        public int         hp, maxHp, attack, defense, exp;
        public int         typeIndex = -1;  // enemySlots 인덱스 (-1 = 플레이어)
        public GameObject  go;
        public bool        isAggro; // 한 번 발견하면 시야 밖에서도 추적

        // ── 스프라이트 방향 ──────────────────────────────────────
        /// <summary>오른쪽 바라보는 스프라이트 (null이면 기본 스프라이트 사용).</summary>
        public Sprite spriteRight;
        /// <summary>왼쪽 바라보는 스프라이트 (null이면 spriteRight를 flipX로 대체).</summary>
        public Sprite spriteLeft;
        /// <summary>true = 오른쪽을 바라보는 중.</summary>
        public bool   facingRight = true;

        // 플레이어용
        public Entity(string name, int hp, int attack)
        { this.name=name; this.hp=hp; this.maxHp=hp; this.attack=attack; }

        // 몬스터용 (슬롯 인덱스)
        public Entity(string name, int hp, int attack, int exp, int typeIdx = -1)
        { this.name=name; this.hp=hp; this.maxHp=hp; this.attack=attack; this.exp=exp; this.typeIndex=typeIdx; }
    }

    // ── 아이템 ─────────────────────────────────────────────────────────────
    public enum ItemType { Potion, GoldSmall, GoldLarge }

    public class Item
    {
        public ItemType   type;
        public Vector2Int pos;
        public GameObject go;
        public int        value; // 회복량 or 골드량
    }

    private Entity        player;
    private int           playerGold = 0;
    private int           playerExp  = 0;

    private List<Entity>  enemies      = new List<Entity>();
    private List<Item>    items        = new List<Item>();
    private List<RectInt> rooms        = new List<RectInt>();
    private Vector2Int    stairsPos;
    private int           currentLevel = 1;

    // ── 맵 오브젝트 ─────────────────────────────────────────────────────────
    private GameObject[,]   tileObjects;
    private GameObject       stairsObject;
    private GameObject       playerObject;
    private PlayerFacing     playerFacing        = PlayerFacing.Down;
    private Coroutine        playerAnimCoroutine;
    private List<GameObject> enemyObjects = new List<GameObject>();
    private List<GameObject> itemObjects  = new List<GameObject>();
    private SpriteRenderer   bgSpriteRenderer;
    private Sprite           _origFloorSprite, _origWallSprite, _origStairsSprite;

    // ── 카메라 / UI ─────────────────────────────────────────────────────────
    private Camera mainCam;
    private Text   logDisplay;
    private Text   statusDisplay;
    private Text   hpDisplay;
    private Text   potionCountText;
    private Font   korFont;

    // ── 패널 (인벤토리 / 장비) ─────────────────────────────────────────────
    private GameObject _inventoryPanel;
    private GameObject _equipmentPanel;

    // ── 미니맵 오버레이 ─────────────────────────────────────────────────────
    private GameObject _minimapOverlay;
    private Texture2D  _minimapTex;
    private bool       _minimapOpen = false;

    // ── 아이템 상자 시스템 ─────────────────────────────────────────────────
    public class TreasureChest
    {
        public Vector2Int pos;
        public GameObject go;    // 하단 레이어 오브젝트 (항상 표시)
        public GameObject topGo; // 상단 레이어 오브젝트 (닫힘↔열림 전환)
        public bool       opened = false;
    }

    private List<TreasureChest> _chests    = new List<TreasureChest>();
    private TreasureChestDatabase _chestDb;
    private TreasureChestDatabase ChestDb => _chestDb != null ? _chestDb
        : (_chestDb = Resources.Load<TreasureChestDatabase>("TreasureChestDatabase"));

    // 아이템 상자 선택 UI 패널
    private GameObject _chestChoicePanel;

    // 플레이어 스탯 (GameSceneManager 내부 추적용) — 장비 누적
    private int   _equipAtk      = 0;
    private int   _equipDef      = 0;
    private float _defChance     = 0f;  // 방어 성공 확률 (0.0~1.0)
    private int   _equipMaxHp    = 0;
    private int   _equipHeal     = 0;
    // 보물상자에서 획득한 장비 목록 (층 이동 후에도 유지) — FIFO 최대 4개
    private readonly List<EquipmentItemDef>  _chestEquipList    = new List<EquipmentItemDef>();
    private const int EQUIP_INV_MAX = 4;

    // ── 메인 아이템 인벤토리 (2D 그리드, 아이템은 이미지로 표시) ─────────────
    // 인덱스: col + row * _mainInvCols
    private ConsumableItemDef[] _mainInvGrid = new ConsumableItemDef[15]; // 초기 5×3
    private int _mainInvCols = 5;
    private int _mainInvRows = 2;

    // ── 플레이어 레벨 ───────────────────────────────────────────────────────
    private int _playerLevel = 1;

    // ── ESC 종료 팝업 ──────────────────────────────────────────────────────
    private bool        _isQuitPopupOpen  = false;
    private GameObject  _quitPopupOverlay = null;

    // ── UIKit 테마 ─────────────────────────────────────────────────────────
    private UIKitTheme _theme;
    private UIKitTheme Theme => _theme != null ? _theme : (_theme = Resources.Load<UIKitTheme>("UIKitTheme"));

    // ── 던전 비주얼 테마 (3층마다 변경) ────────────────────────────────────────
    private DungeonThemeConfig _dungeonThemeCfg;
    private DungeonThemeConfig DungeonThemeCfg => _dungeonThemeCfg != null ? _dungeonThemeCfg
        : (_dungeonThemeCfg = Resources.Load<DungeonThemeConfig>("DungeonThemeConfig"));
    private FloorTheme CurrentTheme => DungeonThemeCfg?.GetThemeForFloor(currentLevel);
    // DungeonThemeConfig 없을 때 tile.png를 직접 사용하기 위한 캐시 + 인덱스
    private Sprite[] _cachedTileSprites;
    private static readonly int[] ThemeTileIndices = { 0, 33, 44, 12, 22, 27, 3, 36, 66, 70 };

    // 층 테마별 색상 (DungeonThemeConfig 없을 때 UpdateVisibility 폴백용)
    private static readonly Color[] ThemeFloorColors =
    {
        new Color(0.80f, 0.70f, 0.55f),  // 0: 고전 던전 (황갈색)
        new Color(0.45f, 0.62f, 0.40f),  // 1: 이끼 석굴 (초록)
        new Color(0.42f, 0.52f, 0.72f),  // 2: 청석 지하 (청색)
        new Color(0.68f, 0.40f, 0.30f),  // 3: 붉은 동굴 (적색)
        new Color(0.35f, 0.56f, 0.52f),  // 4: 심해 유적 (청록)
        new Color(0.72f, 0.64f, 0.34f),  // 5: 모래 폐허 (모래색)
        new Color(0.58f, 0.44f, 0.72f),  // 6: 수정 동굴 (보라)
        new Color(0.64f, 0.56f, 0.36f),  // 7: 고대 석조 (어두운 갈색)
        new Color(0.28f, 0.44f, 0.52f),  // 8: 깊은 심연 (어두운 청)
        new Color(0.22f, 0.20f, 0.24f),  // 9: 흑요석 (거의 검정)
    };
    private Color _currentFloorVisColor = new Color(0.80f, 0.70f, 0.55f);
    private Color _currentFloorDimColor = new Color(0.30f, 0.26f, 0.20f);
    private Color _currentWallVisColor  = new Color(0.55f, 0.46f, 0.36f);
    private Color _currentWallDimColor  = new Color(0.18f, 0.15f, 0.12f);

    private bool          isProcessingTurn = false;

    // ── 방향키 홈드 연속이동 ─────────────────────────────────────
    private Vector2Int _holdDir            = Vector2Int.zero;
    private float      _holdTimer          = 0f;
    private const float HoldInitDelay      = 0.40f;  // 첫 입력 후 반복 시작까지 대기 시간(s)
    private const float HoldRepeatInterval = 0.18f;  // 이후 반복 간격(s)
    private Queue<string> gameLogs = new Queue<string>();

    // ── 사망 통계 추적 ──────────────────────────────────────────────────────
    private int _totalKills     = 0;
    private int potionsPickedUp = 0;
    private int potionInventory = 0;  // 소지 물약 수 (소비 전)

    // ── 보스룸 시스템 ──────────────────────────────────────────────────────
    private RectInt?    _bossRoomRect    = null;   // 보스방 영역 (보스층만 유효)
    private RectInt?    _bossCorridorRect = null;  // 보스 복도 영역 (복도 진입 시 BGM/HUD 활성)
    private RectInt?    _portalRoomRect  = null;   // 포탈룸 영역 (비보스층만 유효)
    private Vector2Int  _corridorDoorPos;           // 복도 시작 타일 (도어 위치)
    private Vector2Int  _shopNpcPos;                // 상점 NPC 위치 (도어 옆)
    private Entity      _bossEntity     = null;    // 현재 층 보스 엔티티 참조
    private bool        _playerInBossRoom = false; // 플레이어 보스방 또는 복도 체류 여부
    private bool        _corridorDoorPassed = false; // 보스 복도 문 첫 통과 여부 (도어 사운드용)
    private bool        _shopActive = false;           // 이번 층 상점 NPC 활성 여부 (보스 처치 후에도 유지)
    // 보스 HUD 위젯
    private GameObject  _bossHudRoot;
    private Text        _bossHudNameText;
    private Text        _bossHudDescText;
    private Text        _bossHudHpText;
    private Image       _bossHudHpBar;
    private RectTransform _bossHudHpBarRt;
    // 상점 패널 위젯
    private GameObject  _shopPanel;
    private GameObject  _bossCorridorDoorGo; // 복도 입구 문 시각 오브젝트
    private GameObject  _bossShopNpcGo;      // 상점 NPC 시각 오브젝트

    // ====================================================================
    void Start()
    {
        Debug.Log(">>> [GameSceneManager] Start.");
        // 테마 폰트 우선 사용, 없으면 시스템 폴백
        var t = Theme;
#if UNITY_EDITOR
        korFont = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>(
            "Assets/UI Kit Pro - Big Bundle/UI Kit Pro - Knight Fight/Fonts/Maplestory OTF Bold.otf");
#endif
        if (korFont == null)
            korFont = (t != null && t.bodyFont != null) ? t.bodyFont
                : Font.CreateDynamicFontFromOSFont(
                    new[] { "Malgun Gothic","Malgun Gothic Semilight","NanumGothic",
                            "Apple SD Gothic Neo","sans-serif" }, 30);

        EnsureSprites();
        SetupAudio();
        SetupCamera();
        SetupBackground();
        SetupUI();
        InitializeLevel();
        StartCoroutine(ShowStartupMessage());
    }

    void Update()
    {
        // 패널 토글 키 (항상 처리)
        if (Input.GetKeyDown(KeyCode.I)) ToggleInventory();
        else if (Input.GetKeyDown(KeyCode.E)) ToggleEquipment();
        else if (Input.GetKeyDown(KeyCode.Tab)) ToggleMinimap();
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_minimapOpen)  { CloseMinimap(); }
            else if (_shopPanel        != null && _shopPanel.activeSelf)        { _shopPanel.SetActive(false); }
            else if (_chestChoicePanel != null && _chestChoicePanel.activeSelf) { Destroy(_chestChoicePanel); _chestChoicePanel = null; }
            else if (_inventoryPanel != null && _inventoryPanel.activeSelf) ToggleInventory();
            else if (_equipmentPanel != null && _equipmentPanel.activeSelf) ToggleEquipment();
            else if (_isQuitPopupOpen)
            {
                // ESC 재입력으로 팝업 닫기
                if (_quitPopupOverlay != null) { Destroy(_quitPopupOverlay); _quitPopupOverlay = null; }
                _isQuitPopupOpen = false;
            }
            else
            {
                // 게임 종료 확인 팝업
                _isQuitPopupOpen = true;
                var canvas = FindAnyObjectByType<Canvas>();
                if (canvas != null)
                {
                    _quitPopupOverlay = EscPopupHelper.ShowPopup(canvas, korFont,
                        "게임을 종료하시겠습니까?",
                        onYes: () =>
                        {
                            _isQuitPopupOpen  = false;
                            _quitPopupOverlay = null;
                            if (GameManager.Instance != null) GameManager.Instance.GoToLobby();
                            else UnityEngine.SceneManagement.SceneManager.LoadScene("GameOptionsScene");
                        },
                        onNo: () =>
                        {
                            _isQuitPopupOpen  = false;
                            _quitPopupOverlay = null;
                        });
                }
            }
        }
        if (isProcessingTurn) return;
        if (_isQuitPopupOpen)  return;  // 팝업 표시 중 게임 입력 차단
        HandleInput();
        FollowCamera();
    }

    // ====================================================================
    // 스프라이트 보장
    // ====================================================================
    private void EnsureSprites()
    {
        // tile.png 자동 로드 (Inspector 미할당 시 Resources에서 로드)
        if (floorSprite == null)
        {
            var tileSprites = Resources.LoadAll<Sprite>("tile");
            if (tileSprites != null && tileSprites.Length > 0)
                floorSprite = tileSprites[0];
            else
                floorSprite = MakeSolid(new Color(0.30f, 0.26f, 0.18f));
        }
        if (wallSprite   == null) wallSprite   = MakeSolid(new Color(0.12f,0.10f,0.08f));
        if (stairsSprite == null) stairsSprite = MakeSolid(new Color(0.8f, 0.3f, 1.0f));
        if (playerSprite == null) playerSprite = MakeSolid(Color.yellow);
        // potion.png 자동 로드
        if (potionSprite == null)
        {
            var s = Resources.Load<Sprite>("potion");
            Debug.Log($"[EnsureSprites] potion load: {(s != null ? s.name : "NULL")}");
            potionSprite = s != null ? s : MakeSolid(new Color(0.4f,1f,0.4f));
        }
        else Debug.Log($"[EnsureSprites] potionSprite already set: {potionSprite.name}");
        // inventory.png 자동 로드
        if (inventoryIconSprite == null)
        {
            inventoryIconSprite = Resources.Load<Sprite>("inventory");
            Debug.Log($"[EnsureSprites] inventory load: {(inventoryIconSprite != null ? inventoryIconSprite.name : "NULL")}");
        }
        else Debug.Log($"[EnsureSprites] inventoryIconSprite already set: {inventoryIconSprite.name}");
        // skills.png 자동 로드
        if (equipmentIconSprite == null)
        {
            equipmentIconSprite = Resources.Load<Sprite>("skills");
            Debug.Log($"[EnsureSprites] skills load: {(equipmentIconSprite != null ? equipmentIconSprite.name : "NULL")}");
        }
        else Debug.Log($"[EnsureSprites] equipmentIconSprite already set: {equipmentIconSprite.name}");
        if (goldSmSprite == null) goldSmSprite = MakeSolid(new Color(1f,0.9f,0.2f));
        if (goldLgSprite == null) goldLgSprite = MakeSolid(new Color(1f,0.7f,0.0f));
        // lootbox.png 스프라이트 자동 로드 (Inspector 미할당 시 Resources에서 첫 2개 슬라이스 사용)
        if (chestClosedSprite == null || chestOpenedSprite == null)
        {
            var lootSprites = Resources.LoadAll<Sprite>("lootbox");
            if (lootSprites != null && lootSprites.Length >= 2)
            {
                if (chestClosedSprite  == null) chestClosedSprite  = lootSprites[0];
                if (chestOpenedSprite  == null) chestOpenedSprite  = lootSprites[1];
            }
        }
        if (chestClosedSprite  == null) chestClosedSprite  = MakeSolid(new Color(0.82f, 0.54f, 0.18f));
        if (chestOpenedSprite  == null) chestOpenedSprite  = MakeSolid(new Color(0.35f, 0.22f, 0.06f));
        // shop.png 슬라이스 자동 로드 (Inspector 미할당 시)
        if (shopNpcSprites == null || shopNpcSprites.Length == 0)
        {
            var shopSprites = Resources.LoadAll<Sprite>("shop");
            if (shopSprites != null && shopSprites.Length > 0)
                shopNpcSprites = shopSprites;
        }
        if (bgSprite     == null) bgSprite     = MakeSolid(new Color(0.05f,0.04f,0.06f));
        // Inspector 원본 보존 (ApplyDungeonTheme이 덮어쓸 때 폴백으로 사용)
        _origFloorSprite  = floorSprite;
        _origWallSprite   = wallSprite;
        _origStairsSprite = stairsSprite;
    }

    private Sprite MakeSolid(Color c)
    {
        var tex = new Texture2D(16,16); var px = new Color[256];
        for (int i=0;i<px.Length;i++) px[i]=c;
        tex.SetPixels(px); tex.Apply(); tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0,0,16,16), new Vector2(0.5f,0.5f), 16f);
    }

    // ====================================================================
    // 오디오 설정
    // ====================================================================
    private void SetupAudio()
    {
        // 저장된 볼륨 읽기 (AudioManager → PlayerPrefs 순)
        var am = GameManager.Instance?.Audio;
        float bgmVol = am != null ? am.BgmVolume : PlayerPrefs.GetFloat("audio_bgm", 0.7f);
        float sfxVol = am != null ? am.SfxVolume : PlayerPrefs.GetFloat("audio_sfx", 0.8f);

        // BGM 소스 (루프)
        var bgmObj = new GameObject("BGM");
        bgmSource = bgmObj.AddComponent<AudioSource>();
        bgmSource.loop   = true;
        bgmSource.volume = bgmVol;
        if (bgmCombat != null) { bgmSource.clip = bgmCombat; bgmSource.Play(); }

        // SFX 소스 (원샷)
        var sfxObj = new GameObject("SFX");
        sfxSource = sfxObj.AddComponent<AudioSource>();
        sfxSource.loop   = false;
        sfxSource.volume = sfxVol;
    }

    private void PlaySFX(AudioClip clip, float pitch = 1f)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip);
    }

    // ====================================================================
    // 카메라
    // ====================================================================
    private void SetupCamera()
    {
        mainCam = Camera.main;
        if (mainCam == null)
        {
            var co = new GameObject("MainCamera"); co.tag = "MainCamera";
            mainCam = co.AddComponent<Camera>();
        }
        mainCam.orthographic     = true;
        mainCam.orthographicSize = 11.52f;
        mainCam.backgroundColor  = new Color(0.04f,0.04f,0.06f);
        mainCam.clearFlags       = CameraClearFlags.SolidColor;
        var p = mainCam.transform.position;
        mainCam.transform.position = new Vector3(p.x,p.y,-10f);
    }

    private void FollowCamera()
    {
        if (player==null) return;
        var t = TileWorldPos(player.pos.x,player.pos.y); t.z = -10f;
        mainCam.transform.position = Vector3.Lerp(mainCam.transform.position, t, Time.deltaTime*12f);
    }

    // ====================================================================
    // 배경
    // ====================================================================
    private void SetupBackground()
    {
        var bg = new GameObject("Background");
        bgSpriteRenderer = bg.AddComponent<SpriteRenderer>();
        bgSpriteRenderer.sprite = bgSprite; bgSpriteRenderer.sortingOrder = -10;
        bgSpriteRenderer.color  = new Color(0.15f,0.14f,0.18f);
        float cx=MapWidth*TileSize/2f, cy=MapHeight*TileSize/2f;
        bg.transform.position   = new Vector3(cx,cy,1f);
        bg.transform.localScale = new Vector3(MapWidth*TileSize*2f,MapHeight*TileSize*2f,1f);
    }

    // ====================================================================
    // UI
    // ====================================================================
    // ====================================================================
    // 시작 메세지 오버레이
    // ====================================================================
    private IEnumerator ShowStartupMessage()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) yield break;

        // 전체화면 반투명 오버레이 패널 (투명도 50%)
        GameObject overlay = MakePanel(
            canvas.transform, "StartupMessageOverlay",
            Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero,
            new Color(0f, 0f, 0f, 0.50f));
        overlay.transform.SetAsLastSibling();

        // 오버레이 중앙 장식 패널 (Knight Fight panelDark)
        var msgPanel = new GameObject("MsgPanel");
        msgPanel.transform.SetParent(overlay.transform, false);
        var mpImg = msgPanel.AddComponent<Image>();
        var themeRef = Theme;
        if (themeRef != null && themeRef.panelDark != null)
        { mpImg.sprite = themeRef.panelDark; mpImg.type = Image.Type.Sliced; mpImg.color = new Color(1f,1f,1f,0.50f); }
        else { mpImg.color = new Color(0.08f, 0.06f, 0.12f, 0.50f); }
        var mpRt = msgPanel.GetComponent<RectTransform>();
        mpRt.anchorMin = new Vector2(0.5f, 0.5f); mpRt.anchorMax = new Vector2(0.5f, 0.5f);
        mpRt.pivot     = new Vector2(0.5f, 0.5f);
        mpRt.anchoredPosition = Vector2.zero;
        mpRt.sizeDelta = new Vector2(900f, 260f);

        // 메세지 텍스트
        Text msgText = MakeText(
            msgPanel.transform, "StartupMessageText",
            Vector2.zero, Vector2.one,
            new Vector2(50f, 30f), new Vector2(-50f, -30f),
            48, new Color(1f, 0.93f, 0.6f), TextAnchor.MiddleCenter);
        if (themeRef != null && themeRef.titleFont != null) msgText.font = themeRef.titleFont;
        msgText.text = StartupMessages.GetRandom();

        yield return new WaitForSeconds(2f);
        Destroy(overlay);
    }

    private void SetupUI()
    {
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            var co = new GameObject("Canvas"); canvas = co.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var cs = co.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920,1080);
            cs.matchWidthOrHeight = 0.5f;
            co.AddComponent<GraphicRaycaster>();
        }

        var thm = Theme;

        // ── 하단 상태 패널 (Knight Fight panel2_midround_night) ───────────
        var panel = MakePanel(canvas.transform,"StatusPanel",
            Vector2.zero,new Vector2(1,0),new Vector2(0.5f,0),
            Vector2.zero,new Vector2(0,120),new Color(0.04f,0.03f,0.07f,0.97f));
        if (thm != null && thm.panelDark != null)
        {
            var pImg = panel.GetComponent<Image>();
            pImg.sprite = thm.panelDark; pImg.type = Image.Type.Sliced; pImg.color = Color.white;
        }

        hpDisplay = MakeText(panel.transform,"HPDisplay",
            new Vector2(0,0),new Vector2(0.30f,1),new Vector2(16,0),Vector2.zero,
            26,new Color(1f,0.93f,0.75f),TextAnchor.MiddleLeft);
        hpDisplay.horizontalOverflow = HorizontalWrapMode.Overflow;  // HP/G 한 줄 유지
        hpDisplay.verticalOverflow   = VerticalWrapMode.Truncate;    // 패널 밖으로 삐져나가지 않음

        statusDisplay = MakeText(panel.transform,"StatusDisplay",
            new Vector2(0.30f,0),new Vector2(0.63f,1),new Vector2(4,0),new Vector2(-4,0),
            20,new Color(0.95f,0.85f,0.55f),TextAnchor.MiddleCenter);

        // ── 물약 슬롯 버튼 (63%–77%) ─────────────────────────────────────────
        var potBtnGo = new GameObject("PotionSlot");
        potBtnGo.transform.SetParent(panel.transform, false);
        var potBtnImg = potBtnGo.AddComponent<Image>();
        potBtnImg.color = new Color(0.06f, 0.20f, 0.06f, 0.95f);
        var potBtnRt = potBtnGo.GetComponent<RectTransform>();
        potBtnRt.anchorMin = new Vector2(0.64f, 0.08f);
        potBtnRt.anchorMax = new Vector2(0.77f, 0.92f);
        potBtnRt.sizeDelta = Vector2.zero;
        var potBtn = potBtnGo.AddComponent<Button>();
        potBtn.onClick.AddListener(UsePotion);
        var potCb = potBtn.colors;
        potCb.normalColor = Color.white; potCb.highlightedColor = new Color(0.8f,1f,0.8f,1f); potCb.pressedColor = new Color(0.5f,0.85f,0.5f,1f);
        potBtn.colors = potCb;
        // 물약 아이콘
        var potIconGo = new GameObject("PotionIcon");
        potIconGo.transform.SetParent(potBtnGo.transform, false);
        var potIconRt = potIconGo.AddComponent<RectTransform>();
        potIconRt.anchorMin = new Vector2(0.04f, 0.12f); potIconRt.anchorMax = new Vector2(0.44f, 0.88f);
        potIconRt.sizeDelta = Vector2.zero;
        var potIconImg = potIconGo.AddComponent<Image>();
        if (potionSprite != null) { potIconImg.sprite = potionSprite; potIconImg.preserveAspect = true; potIconImg.color = Color.white; }
        else potIconImg.color = new Color(0.3f, 0.9f, 0.3f, 0.9f);
        // 수량 텍스트
        potionCountText = MakeText(potBtnGo.transform, "PotionCount",
            new Vector2(0.46f, 0.34f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero,
            20, new Color(0.6f, 1f, 0.6f), TextAnchor.MiddleCenter);
        potionCountText.text = "x 0";
        // 키 힌트
        MakeText(potBtnGo.transform, "PotionKey",
            new Vector2(0.46f, 0f), new Vector2(1f, 0.38f), Vector2.zero, Vector2.zero,
            13, new Color(0.45f, 0.72f, 0.45f), TextAnchor.MiddleCenter).text = "[1]";

        // ── 인벤토리 버튼 (78%–87%) ────────────────────────────────────────
        var invenBtnGo = new GameObject("InvenBtn");
        invenBtnGo.transform.SetParent(panel.transform, false);
        var invenBtnImg = invenBtnGo.AddComponent<Image>();
        if      (inventoryIconSprite != null) { invenBtnImg.sprite = inventoryIconSprite; invenBtnImg.type = Image.Type.Simple; invenBtnImg.preserveAspect = true; invenBtnImg.color = new Color(0.9f, 0.75f, 0.5f); }
        else if (thm?.iconChest     != null) { invenBtnImg.sprite = thm.iconChest;        invenBtnImg.type = Image.Type.Simple; invenBtnImg.preserveAspect = false; invenBtnImg.color = new Color(0.9f, 0.75f, 0.5f); }
        else invenBtnImg.color = new Color(0.36f, 0.20f, 0.07f, 0.95f);
        var invenBtnRt = invenBtnGo.GetComponent<RectTransform>();
        invenBtnRt.anchorMin = new Vector2(0.78f, 0.08f);
        invenBtnRt.anchorMax = new Vector2(0.87f, 0.92f);
        invenBtnRt.sizeDelta = Vector2.zero;
        var invenBtnComp = invenBtnGo.AddComponent<Button>();
        invenBtnComp.onClick.AddListener(ToggleInventory);
        MakeText(invenBtnGo.transform, "Label",
            new Vector2(0f, 0f), new Vector2(1f, 0.42f), Vector2.zero, Vector2.zero,
            13, new Color(1f, 0.92f, 0.65f), TextAnchor.MiddleCenter).text = "[I] 인벤";

        // ── 장비 버튼 (88%–97%) ──────────────────────────────────────────
        var equipBtnGo = new GameObject("EquipBtn");
        equipBtnGo.transform.SetParent(panel.transform, false);
        var equipBtnImg = equipBtnGo.AddComponent<Image>();
        if      (equipmentIconSprite != null) { equipBtnImg.sprite = equipmentIconSprite; equipBtnImg.type = Image.Type.Simple; equipBtnImg.preserveAspect = true; equipBtnImg.color = new Color(0.75f, 0.85f, 1f); }
        else if (thm?.iconArmor     != null) { equipBtnImg.sprite = thm.iconArmor;        equipBtnImg.type = Image.Type.Simple; equipBtnImg.preserveAspect = false; equipBtnImg.color = new Color(0.75f, 0.85f, 1f); }
        else equipBtnImg.color = new Color(0.12f, 0.18f, 0.36f, 0.95f);
        var equipBtnRt = equipBtnGo.GetComponent<RectTransform>();
        equipBtnRt.anchorMin = new Vector2(0.88f, 0.08f);
        equipBtnRt.anchorMax = new Vector2(0.97f, 0.92f);
        equipBtnRt.sizeDelta = Vector2.zero;
        var equipBtnComp = equipBtnGo.AddComponent<Button>();
        equipBtnComp.onClick.AddListener(ToggleEquipment);
        MakeText(equipBtnGo.transform, "Label",
            new Vector2(0f, 0f), new Vector2(1f, 0.42f), Vector2.zero, Vector2.zero,
            13, new Color(0.78f, 0.88f, 1f), TextAnchor.MiddleCenter).text = "[E] 장비";

        // ── 우측 상단 로그 패널 (Knight Fight panelscroll) ────────────────
        var logPanel = MakePanel(canvas.transform,"LogPanel",
            new Vector2(1,1),new Vector2(1,1),new Vector2(1,1),
            new Vector2(-20,-20),new Vector2(460,310),new Color(0.06f,0.04f,0.02f,0.93f));
        if (thm != null && thm.panelScroll != null)
        {
            var lImg = logPanel.GetComponent<Image>();
            lImg.sprite = thm.panelScroll; lImg.type = Image.Type.Sliced; lImg.color = Color.white;
        }
        logDisplay = MakeText(logPanel.transform,"LogDisplay",
            Vector2.zero,Vector2.one,new Vector2(14,14),new Vector2(-14,-14),
            22,new Color(0.93f,0.87f,0.72f),TextAnchor.UpperLeft);
        logDisplay.verticalOverflow = VerticalWrapMode.Truncate;  // 패널 하단 밖으로 overflow 방지

        // ── 좌측 상단 조작 안내 ──────────────────────────────────────────
        var help = MakeText(canvas.transform,"HelpDisplay",
            new Vector2(0,1),new Vector2(0,1),Vector2.zero,Vector2.zero,
            14,new Color(0.55f,0.55f,0.65f),TextAnchor.UpperLeft);
        var hr = help.GetComponent<RectTransform>();
        hr.pivot = new Vector2(0,1); hr.anchoredPosition = new Vector2(20,-20);
        hr.sizeDelta = new Vector2(1000,30);
        help.text = "WASD/방향키: 이동  |  공격: 적 방향  |  아이템: 밟으면 획득  |  [1]: 물약  |  [I]: 인벤토리  |  [E]: 장비  |  [ESC]: 패널 닫기";

        // ── 보스 HUD (기본 숨김 — 보스방 진입 시 표시) ──────────────────────
        BuildBossHud(canvas);

        // ── 상점 패널 (기본 숨김 — 상점 NPC 진입 시 표시) ───────────────────
        BuildShopPanel(canvas);

        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>()==null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

    // ====================================================================
    // 보스 HUD 빌드 (상단 중앙, 기본 숨김)
    // ====================================================================
    private void BuildBossHud(Canvas canvas)
    {
        // 루트 패널 — 상단 중앙 (화면 너비 80% = 1536px 기준, 높이 155px)
        _bossHudRoot = MakePanel(canvas.transform, "BossHUD",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -16f), new Vector2(1536f, 155f),
            new Color(0.06f, 0.02f, 0.02f, 0.93f));

        // 보스 이름 (상단)
        _bossHudNameText = MakeText(_bossHudRoot.transform, "BossName",
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(12f, -52f), new Vector2(-12f, -10f),
            36, new Color(1f, 0.45f, 0.45f), TextAnchor.MiddleCenter);

        // 보스 설명 (이름 아래)
        _bossHudDescText = MakeText(_bossHudRoot.transform, "BossDesc",
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(12f, -84f), new Vector2(-12f, -54f),
            22, new Color(0.8f, 0.6f, 0.6f), TextAnchor.MiddleCenter);

        // HP 바 테두리 (어두운 외곽선 패널)
        var hpBorder = MakePanel(_bossHudRoot.transform, "HpBorder",
            new Vector2(0.05f, 0f), new Vector2(0.95f, 0f), new Vector2(0f, 0f),
            new Vector2(0f, 18f), new Vector2(0f, 40f),
            new Color(0.04f, 0.01f, 0.01f, 1f));

        // HP 바 배경 (테두리 안쪽, 어두운 색)
        var hpBg = MakePanel(hpBorder.transform, "HpBg",
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(-6f, -6f),
            new Color(0.10f, 0.03f, 0.03f, 1f));

        // HP 바 채워지는 부분 — anchorMax.x 로 너비를 비율로 제어 (sprite 없이도 동작)
        var hpFillGo = new GameObject("HpFill");
        hpFillGo.transform.SetParent(hpBg.transform, false);
        _bossHudHpBar   = hpFillGo.AddComponent<Image>();
        _bossHudHpBar.color = new Color(0.6f, 0.1f, 0.9f); // 초기: 보라색 (100%)
        _bossHudHpBarRt = hpFillGo.GetComponent<RectTransform>();
        _bossHudHpBarRt.anchorMin  = Vector2.zero;
        _bossHudHpBarRt.anchorMax  = Vector2.one;   // 초기 100%
        _bossHudHpBarRt.sizeDelta  = Vector2.zero;
        _bossHudHpBarRt.offsetMin  = Vector2.zero;
        _bossHudHpBarRt.offsetMax  = Vector2.zero;

        // HP % 텍스트 (바 위에 중앙 표시)
        _bossHudHpText = MakeText(hpBorder.transform, "HpText",
            Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero,
            20, new Color(1f, 0.92f, 0.92f), TextAnchor.MiddleCenter);

        _bossHudRoot.SetActive(false);
    }

    private void ShowBossHud(Entity boss)
    {
        if (_bossHudRoot == null || boss == null) return;
        if (_bossHudNameText != null) _bossHudNameText.text = $"★  {boss.name}  ★";
        if (_bossHudDescText != null) _bossHudDescText.text = boss.description;
        UpdateBossHud(boss);
        _bossHudRoot.SetActive(true);
    }

    private void UpdateBossHud(Entity boss)
    {
        if (_bossHudRoot == null || !_bossHudRoot.activeSelf || boss == null) return;
        float ratio = boss.maxHp > 0 ? (float)boss.hp / boss.maxHp : 0f;
        if (_bossHudHpBar != null && _bossHudHpBarRt != null)
        {
            // anchorMax.x 로 너비 비율 조절 (왼쪽 고정, 오른쪽이 줄어듦)
            _bossHudHpBarRt.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
            _bossHudHpBarRt.offsetMax = Vector2.zero;
            // 체력 비율에 따른 색상 변경
            if      (ratio > 0.75f) _bossHudHpBar.color = new Color(0.6f, 0.1f, 0.9f);  // 보라 (76~100%)
            else if (ratio > 0.50f) _bossHudHpBar.color = new Color(0.1f, 0.75f, 0.2f); // 초록 (51~75%)
            else if (ratio > 0.25f) _bossHudHpBar.color = new Color(0.9f, 0.75f, 0.05f);// 노랑 (26~50%)
            else                    _bossHudHpBar.color = new Color(0.9f, 0.1f, 0.1f);  // 빨강 (0~25%)
        }
        if (_bossHudHpText != null) _bossHudHpText.text = $"HP  {Mathf.RoundToInt(ratio * 100)} %";
    }

    private void HideBossHud()
    {
        if (_bossHudRoot != null) _bossHudRoot.SetActive(false);
    }

    // ====================================================================
    // 상점 UI 빌드 (보스층 진입 시 창 숨겨둠, 상점 NPC 밟을 때 표시)
    // ====================================================================
    private void BuildShopPanel(Canvas canvas)
    {
        if (_shopPanel != null) return; // 이미 빌드됨

        // 루트 패널 — 화면 중앙 (560×380)
        _shopPanel = MakePanel(canvas.transform, "ShopPanel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(560f, 380f),
            new Color(0.05f, 0.08f, 0.04f, 0.97f));

        // 제목
        var titleText = MakeText(_shopPanel.transform, "ShopTitle",
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(10f, -55f), new Vector2(-10f, -8f),
            32, new Color(0.3f, 1f, 0.5f), TextAnchor.MiddleCenter);
        titleText.text = "🏪  상점 (복도 입구)";

        // 닫기 버튼
        var closeGo = new GameObject("CloseBtn"); closeGo.transform.SetParent(_shopPanel.transform, false);
        var closeImg = closeGo.AddComponent<Image>(); closeImg.color = new Color(0.7f, 0.15f, 0.15f, 0.92f);
        var closeRt = closeGo.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(1f, 1f); closeRt.anchorMax = new Vector2(1f, 1f);
        closeRt.pivot = new Vector2(1f, 1f);
        closeRt.anchoredPosition = new Vector2(-8f, -8f); closeRt.sizeDelta = new Vector2(48f, 40f);
        var closeBtn = closeGo.AddComponent<Button>();
        closeBtn.onClick.AddListener(() => _shopPanel.SetActive(false));
        MakeText(closeGo.transform, "X",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            22, Color.white, TextAnchor.MiddleCenter).text = "✕";

        // 안내 텍스트
        MakeText(_shopPanel.transform, "GoldInfo",
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(10f, -85f), new Vector2(-10f, -56f),
            18, new Color(1f, 0.88f, 0.3f), TextAnchor.MiddleCenter).text =
            "골드가 부족하면 구입할 수 없습니다.";

        // 아이템 목록
        string[] labels  = { "회복 물약\n(HP +40)", "고급 회복 물약\n(HP +100)", "물약 3개 묶음\n(HP +40 ×3)" };
        int[]    basePrc = { 30, 80, 75 };
        System.Action<int>[] buyCbs = new System.Action<int>[3];
        for (int i = 0; i < 3; i++)
        {
            int idx = i;
            float rowY = -110f - idx * 80f;

            // 배경 행
            var rowGo = new GameObject($"ShopRow{idx}");
            rowGo.transform.SetParent(_shopPanel.transform, false);
            var rowImg2 = rowGo.AddComponent<Image>();
            rowImg2.color = new Color(0.08f, 0.16f, 0.08f, 0.85f);
            var rowRt = rowGo.GetComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0.04f, 1f); rowRt.anchorMax = new Vector2(0.96f, 1f);
            rowRt.pivot     = new Vector2(0.5f, 1f);
            rowRt.anchoredPosition = new Vector2(0f, rowY);
            rowRt.sizeDelta = new Vector2(0f, 70f);

            // 아이템 이름 + 설명
            MakeText(rowGo.transform, "ItemLabel",
                new Vector2(0f, 0f), new Vector2(0.60f, 1f),
                new Vector2(12f, 4f), new Vector2(-4f, -4f),
                18, new Color(0.8f, 1f, 0.8f), TextAnchor.MiddleLeft).text = labels[idx];

            // 가격 표시 (층 레벨은 런타임에 업데이트)
            var priceText = MakeText(rowGo.transform, "PriceLabel",
                new Vector2(0.60f, 0f), new Vector2(0.78f, 1f),
                Vector2.zero, Vector2.zero,
                20, new Color(1f, 0.9f, 0.2f), TextAnchor.MiddleCenter);
            priceText.name = $"ShopPrice{idx}";

            // 구입 버튼
            var btnGo = new GameObject("BuyBtn");
            btnGo.transform.SetParent(rowGo.transform, false);
            var btnImg = btnGo.AddComponent<Image>(); btnImg.color = new Color(0.15f, 0.55f, 0.25f, 0.95f);
            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.78f, 0.12f); btnRt.anchorMax = new Vector2(0.97f, 0.88f);
            btnRt.sizeDelta = Vector2.zero;
            MakeText(btnGo.transform, "BuyLabel",
                Vector2.zero, Vector2.one, new Vector2(4f,4f), new Vector2(-4f,-4f),
                18, Color.white, TextAnchor.MiddleCenter).text = "구입";
            var btn = btnGo.AddComponent<Button>();
            int pidx = idx;
            int bprc = basePrc[idx];
            btn.onClick.AddListener(() => OnShopBuy(pidx, bprc, priceText));
        }

        _shopPanel.SetActive(false);
    }

    /// <summary>상점 구입 처리</summary>
    private void OnShopBuy(int itemIdx, int basePrice, Text priceTextRef)
    {
        int price = basePrice + currentLevel * 5;
        if (playerGold < price)
        {
            AddLog($"<color=#FF6666>골드 부족! (필요: {price} G, 소지: {playerGold} G)</color>");
            return;
        }
        playerGold -= price;
        switch (itemIdx)
        {
            case 0:
                potionInventory++;
                potionsPickedUp++;
                AddLog($"<color=#55FF88>물약 구입! (+1개, 소지: {potionInventory}개)</color>");
                UpdatePotionCountUI();
                break;
            case 1:
                player.hp = Mathf.Min(player.maxHp, player.hp + 100);
                AddLog("<color=#55FF88>고급 물약 사용! HP +100</color>");
                break;
            case 2:
                potionInventory += 3;
                potionsPickedUp += 3;
                AddLog($"<color=#55FF88>물약 3개 구입! (소지: {potionInventory}개)</color>");
                UpdatePotionCountUI();
                break;
        }
        UpdateUI();
        // 가격 텍스트 갱신 (구입 후 골드 표시 최신화)
        RefreshShopPrices();
    }

    /// <summary>상점을 열고 가격을 현재 레벨에 맞게 갱신</summary>
    private void ShowShopPanel()
    {
        if (_shopPanel == null) return;
        RefreshShopPrices();
        _shopPanel.SetActive(true);
    }

    private void RefreshShopPrices()
    {
        if (_shopPanel == null) return;
        int[] basePrc = { 30, 80, 75 };
        for (int i = 0; i < 3; i++)
        {
            var pt = _shopPanel.transform.Find($"ShopRow{i}/PriceLabel")?.GetComponent<Text>();
            if (pt == null) pt = FindDeep<Text>(_shopPanel.transform, $"ShopPrice{i}");
            if (pt != null)
                pt.text = $"{basePrc[i] + currentLevel * 5} G";
        }
    }

    private T FindDeep<T>(Transform root, string name) where T : Component
    {
        foreach (Transform child in root)
        {
            if (child.name == name) { var c = child.GetComponent<T>(); if (c != null) return c; }
            var found = FindDeep<T>(child, name);
            if (found != null) return found;
        }
        return null;
    }

    // ====================================================================
    // 보스룸 입/퇴장 감지 헬퍼 — 이동 전·후 어디서든 호출 가능
    // ====================================================================
    private void CheckBossRoomEntry(Vector2Int pos)
    {
        if (!_bossRoomRect.HasValue) return;
        // 보스룸 or 복도 어느 쪽이든 진입 시 BGM/HUD 활성
        bool nowInZone = _bossRoomRect.Value.Contains(pos)
                      || (_bossCorridorRect.HasValue && _bossCorridorRect.Value.Contains(pos));
        if (nowInZone && !_playerInBossRoom)
        {
            _playerInBossRoom = true;
            EnemySlot bSlot = (_bossEntity != null && _bossEntity.typeIndex >= 0 && _bossEntity.typeIndex < enemySlots.Length)
                ? enemySlots[_bossEntity.typeIndex] : null;
            SwitchToBossBgm(bSlot);
            if (_bossEntity != null) ShowBossHud(_bossEntity);
            AddLog("<color=#FF4444>★ 보스가 나타났다!</color>");
        }
        else if (!nowInZone && _playerInBossRoom)
        {
            _playerInBossRoom = false;
            RestoreNormalBgm();
            HideBossHud();
        }
    }

    // ====================================================================
    // 보스 BGM 전환
    // ====================================================================
    private void SwitchToBossBgm(EnemySlot slot)
    {
        if (bgmSource == null) return;
        // 슬롯 개별 BGM → bgmBossPool 랜덤 → bgmCombat 순으로 폴백
        AudioClip bossBgm = null;
        if (slot != null && slot.bossBattleBgm != null)
            bossBgm = slot.bossBattleBgm;
        else if (bgmBossPool != null && bgmBossPool.Length > 0)
            bossBgm = bgmBossPool[Random.Range(0, bgmBossPool.Length)];
        if (bossBgm == null || bossBgm == bgmSource.clip) return;
        _preBossBgm = bgmSource.clip;
        bgmSource.clip = bossBgm;
        bgmSource.Play();
    }

    private void RestoreNormalBgm()
    {
        if (bgmSource == null) return;
        AudioClip restore = _preBossBgm ?? bgmCombat;
        _preBossBgm = null;  // 항상 초기화
        if (restore == null)
        {
            bgmSource.Stop(); // 복원할 BGM 없으면 정지
            return;
        }
        if (restore == bgmSource.clip)
        {
            if (!bgmSource.isPlaying) bgmSource.Play();
            return;
        }
        bgmSource.clip = restore;
        bgmSource.Play();
    }

    // ====================================================================
    private GameObject MakePanel(Transform parent,string name,
        Vector2 aMin,Vector2 aMax,Vector2 pivot,Vector2 aPos,Vector2 sd,Color bg)
    {
        var go = new GameObject(name); go.transform.SetParent(parent,false);
        go.AddComponent<Image>().color = bg;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin=aMin; rt.anchorMax=aMax; rt.pivot=pivot; rt.anchoredPosition=aPos; rt.sizeDelta=sd;
        return go;
    }

    private Text MakeText(Transform parent,string name,
        Vector2 aMin,Vector2 aMax,Vector2 oMin,Vector2 oMax,
        int fs,Color col,TextAnchor anchor)
    {
        var go = new GameObject(name); go.transform.SetParent(parent,false);
        var t = go.AddComponent<Text>();
        t.font = korFont != null ? korFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize=fs; t.color=col; t.alignment=anchor;
        t.supportRichText=true; t.horizontalOverflow=HorizontalWrapMode.Wrap;
        t.verticalOverflow=VerticalWrapMode.Overflow;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin=aMin; rt.anchorMax=aMax; rt.offsetMin=oMin; rt.offsetMax=oMax;
        return t;
    }

    // ====================================================================
    // 던전 테마 적용 (층이 바뀔 때마다 호출)
    // ====================================================================
    private void ApplyDungeonTheme()
    {
        var t = CurrentTheme;

        // ── 바닥 타일: 항상 tile.png 스프라이트시트에서 테마 인덱스로 선택 ─────
        // (DungeonThemeConfig.floorSprite 무시 — tile.png 슬라이스를 직접 사용)
        if (_cachedTileSprites == null || _cachedTileSprites.Length == 0)
        {
            _cachedTileSprites = Resources.LoadAll<Sprite>("tile");
            // 알파벳순 → 숫자 순 정렬: tile_0 < tile_1 < tile_2 … < tile_10 …
            if (_cachedTileSprites != null && _cachedTileSprites.Length > 1)
            {
                System.Array.Sort(_cachedTileSprites, (a, b) =>
                {
                    int ia = 0, ib = 0;
                    int da = a.name.LastIndexOf('_'); if (da >= 0) int.TryParse(a.name.Substring(da + 1), out ia);
                    int db = b.name.LastIndexOf('_'); if (db >= 0) int.TryParse(b.name.Substring(db + 1), out ib);
                    return ia.CompareTo(ib);
                });
            }
            // ── 검증 로그: 총 개수와 정렬 결과 앞 3개 확인 ──
            Debug.Log($"[ApplyDungeonTheme] tile.png 로드: {_cachedTileSprites?.Length ?? 0}개 | " +
                      $"정렬 후 앞3개: {(_cachedTileSprites?.Length > 0 ? _cachedTileSprites[0].name : "없음")}, " +
                      $"{(_cachedTileSprites?.Length > 1 ? _cachedTileSprites[1].name : "")}, " +
                      $"{(_cachedTileSprites?.Length > 2 ? _cachedTileSprites[2].name : "")}");
        }
        if (_cachedTileSprites != null && _cachedTileSprites.Length > 0)
        {
            int themeIdx = Mathf.Clamp((currentLevel - 1) / 3, 0, ThemeTileIndices.Length - 1);
            int tileIdx  = ThemeTileIndices[themeIdx];
            floorSprite  = tileIdx < _cachedTileSprites.Length ? _cachedTileSprites[tileIdx] : _cachedTileSprites[0];
            Debug.Log($"[ApplyDungeonTheme] 층={currentLevel} themeIdx={themeIdx} tileIdx={tileIdx} → {floorSprite?.name ?? "null"}");
        }
        else
        {
            floorSprite = _origFloorSprite;
            Debug.LogWarning("[ApplyDungeonTheme] tile.png 로드 실패 → 원본 스프라이트 폴백");
        }

        // 타일 스프라이트 교체 (폴백은 EnsureSprites가 보장한 _orig* 스프라이트)
        wallSprite   = t?.wallSprite   != null ? t.wallSprite   : _origWallSprite;
        stairsSprite = t?.stairsSprite != null ? t.stairsSprite : _origStairsSprite;

        // ── 층 테마 색상 계산 (DungeonThemeConfig 없을 때 UpdateVisibility 폴백 색으로 사용) ──
        {
            int ti = Mathf.Clamp((currentLevel - 1) / 3, 0, ThemeFloorColors.Length - 1);
            Color fc = ThemeFloorColors[ti];
            _currentFloorVisColor = fc;
            _currentFloorDimColor = Color.Lerp(fc, Color.black, 0.62f);
            _currentWallVisColor  = Color.Lerp(fc, new Color(0.08f, 0.06f, 0.05f), 0.65f);
            _currentWallDimColor  = Color.Lerp(fc, Color.black, 0.82f);
        }

        if (t == null) return;

        // 배경 갱신
        if (bgSpriteRenderer != null)
        {
            bgSpriteRenderer.sprite = t.bgSprite != null ? t.bgSprite : bgSprite;
            bgSpriteRenderer.color  = t.bgTint;
        }

        // 카메라 배경색
        if (mainCam != null) mainCam.backgroundColor = t.cameraBackground;
    }

    // ====================================================================
    // 레벨 초기화
    // ====================================================================
    private void InitializeLevel()
    {
        Debug.Log($">>> InitializeLevel Floor {currentLevel}");
        ClearLevel();
        GenerateDungeon();
        ApplyDungeonTheme();    // ← 층마다 스프라이트·색상 교체
        BuildTileObjects();
        // ── 검증: 첫 바닥 타일의 sprite 이름 출력 ────────────────────────────
        {
            bool found = false;
            for (int vx = 0; vx < MapWidth && !found; vx++)
                for (int vy = 0; vy < MapHeight && !found; vy++)
                    if (tileObjects[vx,vy] != null && map[vx,vy] == '.')
                    {
                        var sr = tileObjects[vx,vy].GetComponent<SpriteRenderer>();
                        Debug.Log($"[BuildTileObjects] 첫 바닥 타일({vx},{vy}) sprite={sr?.sprite?.name ?? "null"}");
                        found = true;
                    }
        }
        SpawnBossRoomSpecials(); // ← 보스층: 문 + 상점 NPC 스폰
        SpawnEntities();
        SpawnItems();
        SpawnTreasureChests();  // ← 아이템 상자 스폰
        UpdateVisibility();
        UpdateUI();
        var tname = CurrentTheme?.themeName;
        string logEntry = $"<color=#88FF88>── {currentLevel}층 진입 ──</color>"
            + (string.IsNullOrEmpty(tname) ? "" : $"  <color=#AADDFF>[{tname}]</color>");
        if (currentLevel % 3 == 0)
            logEntry += "  <color=#FF5555>★ 보스층!</color>";
        AddLog(logEntry);
    }

    private void ClearLevel()
    {
        if (tileObjects!=null)
            for (int x=0;x<MapWidth;x++) for (int y=0;y<MapHeight;y++)
                if (tileObjects[x,y]!=null) Destroy(tileObjects[x,y]);
        if (stairsObject!=null) { Destroy(stairsObject); stairsObject=null; }
        foreach (var go in enemyObjects) if (go!=null) Destroy(go);
        foreach (var go in itemObjects)  if (go!=null) Destroy(go);
        enemyObjects.Clear(); enemies.Clear();
        itemObjects.Clear();  items.Clear();
        // 아이템 상자 정리
        foreach (var chest in _chests) if (chest.go != null) Destroy(chest.go);
        _chests.Clear();
        // 상자 선택 UI 닫기
        if (_chestChoicePanel != null) { Destroy(_chestChoicePanel); _chestChoicePanel = null; }
        // 보스룸 / 포탈룸 상태 초기화
        _bossRoomRect       = null;
        _bossCorridorRect   = null;
        _portalRoomRect     = null;
        _bossEntity         = null;
        _playerInBossRoom   = false;
        _corridorDoorPassed = false;
        _shopActive         = false;
        HideBossHud();
        RestoreNormalBgm();
        if (_shopPanel != null) { _shopPanel.SetActive(false); }
        // 문 / 상점 NPC 정리
        if (_bossCorridorDoorGo != null) { Destroy(_bossCorridorDoorGo); _bossCorridorDoorGo = null; }
        if (_bossShopNpcGo      != null) { Destroy(_bossShopNpcGo);      _bossShopNpcGo      = null; }
    }

    // ====================================================================
    // 던전 생성
    // ====================================================================
    private void GenerateDungeon()
    {
        const int MAX_ATTEMPTS = 15;
        for (int genAttempt = 0; genAttempt < MAX_ATTEMPTS; genAttempt++)
        {
            TryGenerateDungeon();
            if (IsAllRoomsConnected())
            {
                if (genAttempt > 0)
                    Debug.Log($"[Dungeon] {genAttempt + 1}번째 시도에서 완전 연결 맵 생성 완료.");
                return;
            }
            Debug.LogWarning($"[Dungeon] 연결성 검증 실패 (시도 {genAttempt + 1}/{MAX_ATTEMPTS}) — 재생성");
        }
        Debug.LogError("[Dungeon] 최대 재시도 초과. 현재 맵을 사용합니다.");
    }

    /// <summary>BFS로 첫 번째 방에서 모든 방(일반·포탈·보스)이 연결되어 있는지 확인한다.</summary>
    private bool IsAllRoomsConnected()
    {
        if (rooms.Count == 0) return false;

        var start = RoomCenter(rooms[0]);
        if (GetT(start.x, start.y) != '.') return false;

        var visited = new bool[MapWidth, MapHeight];
        var queue   = new Queue<Vector2Int>();
        queue.Enqueue(start);
        visited[start.x, start.y] = true;

        int[] dx = { 0, 0, 1, -1 };
        int[] dy = { 1, -1, 0, 0 };
        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();
            for (int d = 0; d < 4; d++)
            {
                int nx = cur.x + dx[d], ny = cur.y + dy[d];
                if (!IB(nx, ny) || visited[nx, ny] || map[nx, ny] == '#') continue;
                visited[nx, ny] = true;
                queue.Enqueue(new Vector2Int(nx, ny));
            }
        }

        // 모든 일반 방 중심이 도달 가능한지 확인
        foreach (var r in rooms)
        {
            var c = RoomCenter(r);
            if (!visited[c.x, c.y]) return false;
        }
        // 포탈룸 연결 확인
        if (_portalRoomRect.HasValue)
        {
            var c = RoomCenter(_portalRoomRect.Value);
            if (!visited[c.x, c.y]) return false;
        }
        // 보스룸 연결 확인
        if (_bossRoomRect.HasValue)
        {
            var c = RoomCenter(_bossRoomRect.Value);
            if (!visited[c.x, c.y]) return false;
        }
        return true;
    }

    private void TryGenerateDungeon()
    {
        // 이전 시도의 상태 완전 초기화
        _bossRoomRect     = null;
        _bossCorridorRect = null;
        _portalRoomRect   = null;

        map=new char[MapWidth,MapHeight];
        explored=new bool[MapWidth,MapHeight];
        visible=new bool[MapWidth,MapHeight];
        rooms.Clear();
        for (int x=0;x<MapWidth;x++) for (int y=0;y<MapHeight;y++) map[x,y]='#';

        // 보스층: 상단을 보스룸 예약 영역으로 확보 (일반 방 생성 상단 제한)
        const int BOSS_ROOM_H_RSV  = 12;
        const int BOSS_CORRIDOR_RSV = 8;
        const int BOSS_MARGIN_RSV   = 3;
        int normalRoomMaxY = (currentLevel % 3 == 0)
            ? MapHeight - BOSS_MARGIN_RSV - BOSS_ROOM_H_RSV - BOSS_CORRIDOR_RSV - 4
            : MapHeight - 1;
        normalRoomMaxY = Mathf.Max(normalRoomMaxY, 16); // 최소 공간 보장

        int attempts=0;
        while (rooms.Count<12 && attempts<300)
        {
            attempts++;
            int w=Random.Range(5,10),h=Random.Range(4,8);
            int x=Random.Range(1,MapWidth-w-1);
            int y=Random.Range(1, Mathf.Max(2, normalRoomMaxY - h));
            var nr=new RectInt(x,y,w,h);
            bool ov=false;
            foreach (var r in rooms) if (Overlaps(nr,r)){ov=true;break;}
            if (ov) continue;
            for (int rx=x;rx<x+w;rx++) for (int ry=y;ry<y+h;ry++) map[rx,ry]='.';
            if (rooms.Count>0) ConnectRooms(rooms[rooms.Count-1],nr);
            rooms.Add(nr);
        }
        if (rooms.Count==0)
        {
            var fb=new RectInt(5,5,8,6); rooms.Add(fb);
            for (int rx=5;rx<13;rx++) for (int ry=5;ry<11;ry++) map[rx,ry]='.';
        }

        // 보스층: 전용 보스룸 + 복도 / 비보스층: 격리 포탈룸 + 복도
        if (currentLevel % 3 == 0)
        {
            GenerateBossRoom();
            // 보스층: 일반 방 재연결 (보스룸 SealRoomPerimeter가 복도 덮지 않도록
            // 일반 방은 하단에 있으므로 안전; 연결 누락만 보완)
            for (int i = 1; i < rooms.Count; i++)
                ConnectRooms(rooms[i - 1], rooms[i]);
        }
        else
        {
            GeneratePortalRoom();
            // 비보스층: SealRoomPerimeter가 일반 방 복도를 덮을 수 있으므로 재연결
            for (int i = 1; i < rooms.Count; i++)
                ConnectRooms(rooms[i - 1], rooms[i]);
        }
    }

    // ====================================================================
    // 보스룸 생성 — 항상 맵 최상단 중앙, 하단 수직 복도 하나만, 입구에 문+상점
    // 구조: [보스룸 12×12] ← 최상단
    //              ↑ 수직 복도 (8타일)
    //        [문][상점] ← 복도 입구 (플레이어 진입 지점)
    //              ↑ L자 복도
    //       [던전 일반 방들]
    // ====================================================================
    private void GenerateBossRoom()
    {
        const int CORRIDOR_LEN = 8;   // 수직 복도 길이
        const int BOSS_ROOM_W  = 12;
        const int BOSS_ROOM_H  = 12;
        const int MARGIN       = 3;

        // ── 보스룸 위치: 항상 맵 최상단 중앙 ──────────────────────────────────
        int bossX = (MapWidth  - BOSS_ROOM_W) / 2;
        int bossY = MapHeight  - MARGIN - BOSS_ROOM_H;  // 상단 여백 3타일
        var bossRoom = new RectInt(bossX, bossY, BOSS_ROOM_W, BOSS_ROOM_H);

        // 보스룸 바닥 파기
        for (int rx = bossRoom.xMin; rx < bossRoom.xMax; rx++)
            for (int ry = bossRoom.yMin; ry < bossRoom.yMax; ry++)
                map[rx, ry] = '.';
        _bossRoomRect = bossRoom;

        // 외곽 완전봉인 (4면 — 단일 복도만 허용)
        SealRoomPerimeter(bossRoom);

        // ── 하단 중앙 수직 복도 ────────────────────────────────────────────────
        int corridorX      = bossRoom.xMin + BOSS_ROOM_W / 2;   // 방 중앙 X
        int corridorTopY   = bossRoom.yMin - 1;                  // 보스룸 바닥 바로 아래
        int corridorBotY   = corridorTopY  - (CORRIDOR_LEN - 1); // 복도 끝 (입구)
        for (int ry = corridorBotY; ry <= corridorTopY; ry++)
            if (IB(corridorX, ry)) map[corridorX, ry] = '.';

        // 문: 복도 입구 최하단 (플레이어가 처음 진입하는 타일)
        _corridorDoorPos = new Vector2Int(corridorX, corridorBotY);
        // 상점: 문 바로 오른쪽
        _shopNpcPos = new Vector2Int(corridorX + 1, corridorBotY);
        if (IB(_shopNpcPos.x, _shopNpcPos.y)) map[_shopNpcPos.x, _shopNpcPos.y] = '.';
        _shopActive = true;  // 이번 층 상점 활성 (보스 처치 후에도 유지)

        // 복도 영역 저장: 입구(corridorBotY) ~ 보스룸 바닥 바로 아래(corridorTopY)
        // 너비 3타일(corridorX-1 ~ corridorX+1)로 여유 있게 잡아 이동 중 감지 보장
        _bossCorridorRect = new RectInt(corridorX - 1, corridorBotY, 3, corridorTopY - corridorBotY + 1);

        // 계단: 보스룸 상단 반대편 코너 (입구와 가장 먼 위치)
        stairsPos = new Vector2Int(bossRoom.xMax - 2, bossRoom.yMax - 2);
        map[stairsPos.x, stairsPos.y] = '>';

        // ── 복도 입구 → 가장 가까운 일반 방을 L자 복도로 연결 ────────────────
        if (rooms.Count > 0)
        {
            // 맨해튼 거리 기준 가장 가까운 방 탐색
            var nearestRoom = rooms[0];
            float bestDist  = float.MaxValue;
            foreach (var r in rooms)
            {
                var rc2 = RoomCenter(r);
                float d = Mathf.Abs(rc2.x - corridorX) + Mathf.Abs(rc2.y - corridorBotY);
                if (d < bestDist) { bestDist = d; nearestRoom = r; }
            }
            var rc = RoomCenter(nearestRoom);
            // 수직: 복도 입구 ~ 방 중심 Y
            int minRy = Mathf.Min(corridorBotY, rc.y);
            int maxRy = Mathf.Max(corridorBotY, rc.y);
            for (int ry = minRy; ry <= maxRy; ry++)
                if (IB(corridorX, ry)) map[corridorX, ry] = '.';
            // 수평: 복도 X ~ 방 중심 X (방 중심 Y행)
            int minRx = Mathf.Min(corridorX, rc.x);
            int maxRx = Mathf.Max(corridorX, rc.x);
            for (int rx = minRx; rx <= maxRx; rx++)
                if (IB(rx, rc.y)) map[rx, rc.y] = '.';
        }
    }

    /// <summary>보스방 입구 반대편 코너를 계단 위치로 반환</summary>
    private Vector2Int GetBossRoomFarCorner(RectInt room, Vector2Int corridorDir)
    {
        // 복도가 아래에서 오면(up) → 방 상단, 위에서 오면(down) → 방 하단
        // 좌우도 반대
        int cx = (corridorDir.x >= 0) ? room.xMax - 2 : room.xMin + 1;
        int cy = (corridorDir.y >= 0) ? room.yMax - 2 : room.yMin + 1;
        return new Vector2Int(cx, cy);
    }

    // ====================================================================
    // 방 외곽 봉인 (1타일 테두리를 '#'으로 — 단일 복도 보장용)
    // 호출 후 반드시 지정 복도를 재개척해야 합니다.
    // ====================================================================
    private void SealRoomPerimeter(RectInt room)
    {
        // 무조건 봉인 — 기존 복도(.)도 덮어써서 단일 복도를 완전히 보장
        for (int x = room.xMin - 1; x <= room.xMax; x++)
        {
            if (IB(x, room.yMin - 1)) map[x, room.yMin - 1] = '#';
            if (IB(x, room.yMax))     map[x, room.yMax]     = '#';
        }
        for (int y = room.yMin; y < room.yMax; y++)
        {
            if (IB(room.xMin - 1, y)) map[room.xMin - 1, y] = '#';
            if (IB(room.xMax, y))     map[room.xMax, y]     = '#';
        }
    }

    // ====================================================================
    // 포탈룸 생성 (비보스층: 8×8 격리 방 + 1×6 복도 + 계단)
    // GenerateDungeon에서 비보스층의 마지막 방 대신 호출
    // ====================================================================
    private void GeneratePortalRoom()
    {
        const int CORRIDOR_LEN   = 6;   // 복도 길이
        const int PORTAL_ROOM_W  = 8;   // 포탈룸 너비
        const int PORTAL_ROOM_H  = 8;   // 포탈룸 높이
        const int MARGIN         = 3;   // 맵 가장자리 여백

        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        // ── 1단계: 모든 방 × 4방향 탐색 (lastRoom 우선, 이후 역순) ──────────────
        for (int ri = rooms.Count - 1; ri >= 0; ri--)
        {
            var anchorRoom = rooms[ri];
            var fromCenter = RoomCenter(anchorRoom);

            foreach (var d in dirs)
            {
                Vector2Int corridorStart;
                if      (d == Vector2Int.right) corridorStart = new Vector2Int(anchorRoom.xMax,     fromCenter.y);
                else if (d == Vector2Int.left)  corridorStart = new Vector2Int(anchorRoom.xMin - 1, fromCenter.y);
                else if (d == Vector2Int.up)    corridorStart = new Vector2Int(fromCenter.x,         anchorRoom.yMax);
                else                            corridorStart = new Vector2Int(fromCenter.x,         anchorRoom.yMin - 1);

                // 복도 경계 검사 (맵 범위 및 기존 방 통과 여부)
                bool corridorOk = true;
                for (int k = 0; k < CORRIDOR_LEN; k++)
                {
                    var ct = corridorStart + d * k;
                    if (!IB(ct.x, ct.y) || ct.x < MARGIN || ct.y < MARGIN ||
                        ct.x >= MapWidth - MARGIN || ct.y >= MapHeight - MARGIN)
                    { corridorOk = false; break; }
                    foreach (var r in rooms)
                        if (r.Contains(ct)) { corridorOk = false; break; }
                    if (!corridorOk) break;
                }
                if (!corridorOk) continue;

                Vector2Int portalEntry = corridorStart + d * CORRIDOR_LEN;
                RectInt candidate;
                if      (d == Vector2Int.right) candidate = new RectInt(portalEntry.x,                 portalEntry.y - PORTAL_ROOM_H / 2, PORTAL_ROOM_W, PORTAL_ROOM_H);
                else if (d == Vector2Int.left)  candidate = new RectInt(portalEntry.x - PORTAL_ROOM_W, portalEntry.y - PORTAL_ROOM_H / 2, PORTAL_ROOM_W, PORTAL_ROOM_H);
                else if (d == Vector2Int.up)    candidate = new RectInt(portalEntry.x - PORTAL_ROOM_W / 2, portalEntry.y,                PORTAL_ROOM_W, PORTAL_ROOM_H);
                else                            candidate = new RectInt(portalEntry.x - PORTAL_ROOM_W / 2, portalEntry.y - PORTAL_ROOM_H, PORTAL_ROOM_W, PORTAL_ROOM_H);

                if (candidate.xMin < MARGIN || candidate.yMin < MARGIN ||
                    candidate.xMax >= MapWidth - MARGIN || candidate.yMax >= MapHeight - MARGIN) continue;

                bool overlaps = false;
                foreach (var r in rooms) if (Overlaps(candidate, r)) { overlaps = true; break; }
                if (overlaps) continue;

                // ── 검사 통과: 포탈룸 배치 ──────────────────────────────────
                PlacePortalRoom(candidate, corridorStart, d, CORRIDOR_LEN);
                return;
            }
        }

        // ── 2단계: 전체 탐색 실패 → 맵 코너에 강제 배치 ────────────────────────
        Debug.LogWarning("[PortalRoom] 모든 방 × 방향 탐색 실패. 맵 코너 강제 배치.");
        ForcePortalRoomAtCorner(MARGIN, PORTAL_ROOM_W, PORTAL_ROOM_H);
    }

    /// <summary>포탈룸·복도·계단을 맵에 반영한다.</summary>
    private void PlacePortalRoom(RectInt candidate, Vector2Int corridorStart, Vector2Int d, int corridorLen)
    {
        for (int rx = candidate.xMin; rx < candidate.xMax; rx++)
            for (int ry = candidate.yMin; ry < candidate.yMax; ry++)
                map[rx, ry] = '.';
        _portalRoomRect = candidate;

        // 외곽 봉인: 단일 복도 보장
        SealRoomPerimeter(candidate);

        // 복도 — 봉인 후 재개척
        for (int k = 0; k < corridorLen; k++)
        {
            var ct = corridorStart + d * k;
            if (IB(ct.x, ct.y)) map[ct.x, ct.y] = '.';
        }

        stairsPos = GetBossRoomFarCorner(candidate, d);
        map[stairsPos.x, stairsPos.y] = '>';
    }

    /// <summary>포탈룸 공간 부족 폴백: 맵 코너 중 lastRoom에서 가장 먼 곳에 강제 배치</summary>
    private void ForcePortalRoomAtCorner(int margin, int w, int h)
    {
        var corners = new[]
        {
            new RectInt(margin,                margin,                  w, h),  // 좌하
            new RectInt(MapWidth - margin - w, margin,                  w, h),  // 우하
            new RectInt(margin,                MapHeight - margin - h,  w, h),  // 좌상
            new RectInt(MapWidth - margin - w, MapHeight - margin - h,  w, h),  // 우상
        };

        // 마지막 방과 가장 먼 코너 선택 (가장 고립된 위치)
        var lastCenter = RoomCenter(rooms[rooms.Count - 1]);
        RectInt chosen = corners[0];
        float worst = 0f;
        foreach (var c in corners)
        {
            float dist = Vector2Int.Distance(RoomCenter(c), lastCenter);
            if (dist > worst) { worst = dist; chosen = c; }
        }

        for (int rx = chosen.xMin; rx < chosen.xMax; rx++)
            for (int ry = chosen.yMin; ry < chosen.yMax; ry++)
                map[rx, ry] = '.';
        _portalRoomRect = chosen;
        SealRoomPerimeter(chosen);

        // L자 복도로 마지막 방과 연결
        var lc = lastCenter;
        var bc = RoomCenter(chosen);
        for (int x = Mathf.Min(lc.x, bc.x); x <= Mathf.Max(lc.x, bc.x); x++)
            if (IB(x, lc.y)) map[x, lc.y] = '.';
        for (int y = Mathf.Min(lc.y, bc.y); y <= Mathf.Max(lc.y, bc.y); y++)
            if (IB(bc.x, y)) map[bc.x, y] = '.';

        stairsPos = RoomCenter(chosen);
        map[stairsPos.x, stairsPos.y] = '>';
    }

    // ====================================================================
    // 보스룸 특수 오브젝트 스폰 (문 + 상점 NPC)
    // GenerateDungeon → BuildTileObjects 이후에 호출
    // ====================================================================
    private void SpawnBossRoomSpecials()
    {
        if (!_bossRoomRect.HasValue) return;

        // ── 문 (복도 입구 타일 위에 오버레이 — 1타일 크기에 맞춤) ──────────────
        if (IB(_corridorDoorPos.x, _corridorDoorPos.y))
        {
            _bossCorridorDoorGo = new GameObject("BossDoor");
            _bossCorridorDoorGo.transform.position = TileWorldPos(_corridorDoorPos.x, _corridorDoorPos.y);
            var doorSr = _bossCorridorDoorGo.AddComponent<SpriteRenderer>();
            Sprite doorSpr = doorSprite != null ? doorSprite : MakeSolid(new Color(0.85f, 0.62f, 0.10f));
            doorSr.sprite       = doorSpr;
            doorSr.color        = doorSprite != null ? Color.white : new Color(0.85f, 0.62f, 0.10f, 0.88f);
            doorSr.sortingOrder = 3;
            // 스프라이트 bounds 기반으로 정확히 1타일(TileSize)에 맞게 스케일
            float dsx = TileSize / doorSpr.bounds.size.x;
            float dsy = TileSize / doorSpr.bounds.size.y;
            _bossCorridorDoorGo.transform.localScale = new Vector3(dsx * 0.92f, dsy * 0.92f, 1f);
            _bossCorridorDoorGo.SetActive(false);
        }

        // ── 상점 (문 옆 타일에 1타일 크기에 맞춤) ─────────────────────────────
        if (IB(_shopNpcPos.x, _shopNpcPos.y))
        {
            _bossShopNpcGo = new GameObject("ShopNpc");
            _bossShopNpcGo.transform.position = TileWorldPos(_shopNpcPos.x, _shopNpcPos.y);
            var shopSr = _bossShopNpcGo.AddComponent<SpriteRenderer>();
            Sprite shopSpr = (shopNpcSprites != null && shopNpcSprites.Length > 0)
                ? shopNpcSprites[Random.Range(0, shopNpcSprites.Length)]
                : MakeSolid(new Color(0.20f, 0.82f, 0.38f));
            shopSr.sprite       = shopSpr;
            shopSr.color        = Color.white;
            shopSr.sortingOrder = 4;
            // 스프라이트 bounds 기반으로 정확히 1타일(TileSize)에 맞게 스케일
            float ssx = TileSize / shopSpr.bounds.size.x;
            float ssy = TileSize / shopSpr.bounds.size.y;
            _bossShopNpcGo.transform.localScale = new Vector3(ssx * 0.90f, ssy * 0.90f, 1f);
            _bossShopNpcGo.SetActive(false);
        }
    }

    private bool Overlaps(RectInt a,RectInt b)
        =>a.x<b.x+b.width+1&&a.x+a.width+1>b.x&&a.y<b.y+b.height+1&&a.y+a.height+1>b.y;

    private void ConnectRooms(RectInt a,RectInt b)
    {
        var p1=RoomCenter(a);var p2=RoomCenter(b);
        for (int x=Mathf.Min(p1.x,p2.x);x<=Mathf.Max(p1.x,p2.x);x++) SetT(x,p1.y,'.');
        for (int y=Mathf.Min(p1.y,p2.y);y<=Mathf.Max(p1.y,p2.y);y++) SetT(p2.x,y,'.');
    }

    private Vector2Int RoomCenter(RectInt r)=>new Vector2Int(r.x+r.width/2,r.y+r.height/2);
    private void SetT(int x,int y,char c){if(IB(x,y))map[x,y]=c;}
    private char GetT(int x,int y)=>IB(x,y)?map[x,y]:'#';
    private bool IB(int x,int y)=>x>=0&&x<MapWidth&&y>=0&&y<MapHeight;

    // ====================================================================
    // 타일 빌드
    // ====================================================================
    private void BuildTileObjects()
    {
        tileObjects=new GameObject[MapWidth,MapHeight];
        for (int x=0;x<MapWidth;x++)
        {
            for (int y=0;y<MapHeight;y++)
            {
                char c=map[x,y];
                Sprite spr=(c=='#')?wallSprite:floorSprite;
                tileObjects[x,y]=MakeTile($"T_{x}_{y}",x,y,spr,Color.white,0);
            }
        }
        // 계단: 틴트 없이 원본 스프라이트 그대로 표시
        stairsObject=MakeTile("Stairs",stairsPos.x,stairsPos.y,
            stairsSprite,Color.white,2);

        for (int x=0;x<MapWidth;x++) for (int y=0;y<MapHeight;y++)
            if (tileObjects[x,y]!=null) tileObjects[x,y].SetActive(false);
        stairsObject.SetActive(false);
    }

    private GameObject MakeTile(string n,int x,int y,Sprite spr,Color col,int order)
    {
        var go=new GameObject(n); go.transform.position=TileWorldPos(x,y);
        var sr=go.AddComponent<SpriteRenderer>(); sr.sprite=spr;sr.color=col;sr.sortingOrder=order;
        // 스프라이트 원본 크기와 무관하게 항상 TileSize(1×1 unit) 격자에 딱 맞게 스케일
        if (spr != null)
        {
            float sx = TileSize / spr.bounds.size.x;
            float sy = TileSize / spr.bounds.size.y;
            go.transform.localScale = new Vector3(sx, sy, 1f);
        }
        return go;
    }

    private Vector3 TileWorldPos(int x,int y)=>new Vector3(x*TileSize,y*TileSize,0);

    // Y 기반 정렬 순서: 낮은 Y(화면 하단)일수록 앞에 렌더링
    private int EntitySortOrder(int tileY) => 1000 - tileY * 2;

    // ====================================================================
    // 엔티티 스폰 (3종 몬스터)
    // ====================================================================
    private void SpawnEntities()
    {
        if (player==null) player=new Entity("PLAYER",100,15);
        player.pos=RoomCenter(rooms[0]);

        if (playerObject==null)
        {
            playerObject=new GameObject("Player");
            var sr=playerObject.AddComponent<SpriteRenderer>();
            sr.color=Color.white;
        }
        SetPlayerIdleSprite();
        player.go=playerObject;
        playerObject.transform.position=TileWorldPos(player.pos.x,player.pos.y);
        playerObject.GetComponent<SpriteRenderer>().sortingOrder = EntitySortOrder(player.pos.y) + 1;
        // 스프라이트를 타일 크기에 맞게 정규화 (ppu 차이로 인한 시각적 겹침 방지)
        var refSprite = playerSpriteDown != null ? playerSpriteDown : playerSprite;
        if (refSprite!=null)
        {
            float ps=TileSize/Mathf.Max(refSprite.bounds.size.x,refSprite.bounds.size.y);
            playerObject.transform.localScale=new Vector3(ps,ps,1f);
        }
        playerObject.SetActive(true);

        // 에너미 슬롯 기반 스폰 (일반/보스 분리)
        int slotCount    = (enemySlots != null) ? enemySlots.Length : 0;
        int totalEnemies = 4 + currentLevel * 2;
        bool isBossFloor = currentLevel % 3 == 0;

        // 슬롯을 보스 / 일반 풀로 분리
        var bossPool   = new System.Collections.Generic.List<int>();
        var normalPool = new System.Collections.Generic.List<int>();
        for (int s = 0; s < slotCount; s++)
            (enemySlots[s].isBoss ? bossPool : normalPool).Add(s);
        if (normalPool.Count == 0) normalPool.AddRange(bossPool); // 일반 없으면 보스로 대체
        if (bossPool.Count == 0) isBossFloor = false;            // 보스 없으면 보스층 없음

        int bossSpawned = 0;
        for (int i = 0; i < totalEnemies; i++)
        {
            // 보스층 첫 번째 엔티티는 반드시 보스
            int typeIdx;
            if (isBossFloor && bossSpawned == 0 && bossPool.Count > 0)
            {
                typeIdx = bossPool[Random.Range(0, bossPool.Count)];
                bossSpawned++;
            }
            else
                typeIdx = normalPool.Count > 0 ? normalPool[Random.Range(0, normalPool.Count)] : (slotCount > 0 ? Random.Range(0, slotCount) : -1);

            string eName; string eDesc; bool eIsBoss;
            int eHp, eAtk, eExp;
            Sprite eSprRight, eSprLeft; Color eColor;

            // 각 적의 스탯 기준 레벨: 플레이어 레벨 ±1 범위에서 무작위 결정
            int eLevel = Mathf.Max(1, _playerLevel + Random.Range(-1, 2));

            if (typeIdx >= 0)
            {
                var slot  = enemySlots[typeIdx];
                eName     = string.IsNullOrEmpty(slot.name) ? $"몬스터{typeIdx+1}" : slot.name;
                eDesc     = slot.description ?? "";
                eIsBoss   = slot.isBoss;
                eHp       = Mathf.RoundToInt((15 + eLevel * 8)  * slot.hpScale);
                eAtk      = Mathf.RoundToInt((3  + eLevel)      * slot.atkScale);
                eExp      = Mathf.RoundToInt((5  + eLevel)      * slot.expScale);
                eSprRight = slot.spriteRight;
                eSprLeft  = slot.spriteLeft;
                eColor    = slot.color;
            }
            else
            {
                eName = "몬스터"; eDesc = ""; eIsBoss = false;
                eHp   = 15 + eLevel * 8;
                eAtk  = 3  + eLevel; eExp = 5 + eLevel;
                eSprRight = null; eSprLeft = null;
                eColor    = new Color(1f, 0.4f, 0.4f);
            }

            var e = new Entity(eName, eHp, eAtk, eExp, typeIdx);
            e.description = eDesc;
            e.isBoss      = eIsBoss;
            e.defense     = (typeIdx >= 0) ? Mathf.RoundToInt(eLevel * enemySlots[typeIdx].defScale) : 0;
            // 보스는 보스방 중앙에, 일반 몬스터는 랜덤 빈 바닥에 배치
            if (eIsBoss && _bossRoomRect.HasValue)
                e.pos = RoomCenter(_bossRoomRect.Value);
            else
                e.pos = FindEmptyFloor(6);
            e.spriteRight = eSprRight;
            e.spriteLeft  = eSprLeft;
            e.facingRight = true;
            if (e.pos.x < 0) continue;

            var eSprRef = eSprRight ?? eSprLeft;
            var eGo = new GameObject($"Enemy_{i}_{eName}");
            eGo.transform.position = TileWorldPos(e.pos.x, e.pos.y);
            var esr = eGo.AddComponent<SpriteRenderer>();
            esr.sprite = eSprRef; esr.color = Color.white;
            esr.sortingOrder = EntitySortOrder(e.pos.y);
            if (eSprRef != null)
            {
                float es = TileSize / Mathf.Max(eSprRef.bounds.size.x, eSprRef.bounds.size.y);
                if (eIsBoss) es *= 1.7f;
                eGo.transform.localScale = new Vector3(es, es, 1f);
            }
            eGo.SetActive(false);
            e.go = eGo; enemies.Add(e); enemyObjects.Add(eGo);
            // 보스 엔티티 캐싱 (HUD·BGM 전환에 사용)
            if (eIsBoss && _bossEntity == null) _bossEntity = e;
        }
    }

    // ====================================================================
    // 아이템 스폰 (회복약 / 골드 소 / 골드 대)
    // ====================================================================
    private void SpawnItems()
    {
        // 방 당 0~2개 아이템 (첫 번째 방 제외)
        for (int ri=1;ri<rooms.Count;ri++)
        {
            int count = Random.Range(0,3);
            for (int j=0;j<count;j++)
            {
                var pos = FindEmptyFloorInRoom(rooms[ri]);
                if (pos.x<0) continue;

                float roll=Random.value;
                ItemType itype; Sprite spr; int val;
                if (roll<0.40f)
                {
                    itype=ItemType.Potion;    spr=potionSprite; val=25+currentLevel*5;
                }
                else if (roll<0.75f)
                {
                    itype=ItemType.GoldSmall; spr=goldSmSprite; val=5+currentLevel*3;
                }
                else
                {
                    itype=ItemType.GoldLarge; spr=goldLgSprite; val=15+currentLevel*8;
                }

                var item=new Item{type=itype,pos=pos,value=val};
                var iGo=MakeTile($"Item_{ri}_{j}",pos.x,pos.y,spr,Color.white,3);
                // 종횡비 유지하며 타일의 82% 크기로 표시
                if (spr != null)
                {
                    float fs = TileSize * 0.82f / Mathf.Max(spr.bounds.size.x, spr.bounds.size.y);
                    iGo.transform.localScale = new Vector3(fs, fs, 1f);
                }
                else
                    iGo.transform.localScale = Vector3.one * 0.82f;
                iGo.SetActive(false);
                item.go=iGo;
                items.Add(item); itemObjects.Add(iGo);
            }
        }
    }

    private Vector2Int FindEmptyFloorInRoom(RectInt room)
    {
        for (int i=0;i<50;i++)
        {
            int x=Random.Range(room.xMin,room.xMax);
            int y=Random.Range(room.yMin,room.yMax);
            if (GetT(x,y)=='.' && GetEnemyAt(x,y)==null &&
                GetItemAt(x,y)==null && (player==null||player.pos!=new Vector2Int(x,y)))
                return new Vector2Int(x,y);
        }
        return new Vector2Int(-1,-1);
    }

    // minDistFromPlayer > 0 이면 플레이어로부터 해당 거리 이상인 칸만 선택
    private Vector2Int FindEmptyFloor(int minDistFromPlayer=0)
    {
        for (int i=0;i<500;i++)
        {
            int x=Random.Range(1,MapWidth-1),y=Random.Range(1,MapHeight-1);
            var p=new Vector2Int(x,y);
            if (GetT(x,y)!='.'||GetEnemyAt(x,y)!=null||GetItemAt(x,y)!=null) continue;
            if (player!=null&&player.pos==p) continue;
            if (minDistFromPlayer>0&&player!=null&&
                Vector2Int.Distance(p,player.pos)<=minDistFromPlayer) continue;
            // 보스룸 / 포탈룸 내부에는 일반 엔티티 배치 금지
            if (_bossRoomRect.HasValue   && _bossRoomRect.Value.Contains(p))   continue;
            if (_portalRoomRect.HasValue && _portalRoomRect.Value.Contains(p)) continue;
            return p;
        }
        return new Vector2Int(-1,-1);
    }

    // 플레이어 또는 적이 점유 중인 칸 여부
    private bool IsEntityAt(Vector2Int pos)
        => (player!=null&&player.pos==pos)||
           enemies.Exists(e=>e.pos==pos);

    // ====================================================================
    // 입력
    // ====================================================================
    private void HandleInput()
    {
        // 패널이 열린 동안은 이동/전투 차단
        bool panelOpen = (_inventoryPanel  != null && _inventoryPanel.activeSelf)
                      || (_equipmentPanel  != null && _equipmentPanel.activeSelf)
                      || (_chestChoicePanel != null && _chestChoicePanel.activeSelf)
                      || (_shopPanel       != null && _shopPanel.activeSelf);
        if (panelOpen) return;

        // 물약 사용 (1 키)
        if (Input.GetKeyDown(KeyCode.Alpha1)) { UsePotion(); return; }

        // 방향 교체 또는 키 해제 시 타이머 리셋
        Vector2Int move = Vector2Int.zero;
        bool isKeyDown  = false;

        if      (Input.GetKeyDown(KeyCode.W)||Input.GetKeyDown(KeyCode.UpArrow))    { move=Vector2Int.up;    isKeyDown=true; }
        else if (Input.GetKeyDown(KeyCode.S)||Input.GetKeyDown(KeyCode.DownArrow))  { move=Vector2Int.down;  isKeyDown=true; }
        else if (Input.GetKeyDown(KeyCode.A)||Input.GetKeyDown(KeyCode.LeftArrow))  { move=Vector2Int.left;  isKeyDown=true; }
        else if (Input.GetKeyDown(KeyCode.D)||Input.GetKeyDown(KeyCode.RightArrow)) { move=Vector2Int.right; isKeyDown=true; }
        else
        {
            if      (Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow))    move=Vector2Int.up;
            else if (Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow))  move=Vector2Int.down;
            else if (Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow))  move=Vector2Int.left;
            else if (Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)) move=Vector2Int.right;
        }

        if (move != _holdDir) { _holdDir = move; _holdTimer = 0f; }

        bool doMove = false;
        if (isKeyDown)
        {
            doMove     = true;
            _holdTimer = 0f;
        }
        else if (move != Vector2Int.zero)
        {
            _holdTimer += Time.deltaTime;
            if (_holdTimer >= HoldInitDelay)
            {
                doMove     = true;
                _holdTimer -= HoldRepeatInterval;
            }
        }

        if (doMove&&move!=Vector2Int.zero&&ProcessPlayerTurn(move))
        {
            TriggerPlayerWalkAnim(move);
            isProcessingTurn=true;
            ProcessEnemyTurn();
            UpdateVisibility();
            if (_minimapOpen && _minimapTex != null) RenderMinimapTexture(4);
            UpdateUI();
            if (player.hp > 0) isProcessingTurn=false;
        }
    }

    // ====================================================================
    // 플레이어 턴
    // ====================================================================
    private bool ProcessPlayerTurn(Vector2Int move)
    {
        // ── 보스룸 입/퇴장 판정 (이동·공격 전 현재 위치 기준으로 먼저 체크) ────
        CheckBossRoomEntry(player.pos);

        // 같은 칸에 적이 있으면 즉시 밀어내기 (비정상 겹침 복구)
        var sameTile=GetEnemyAt(player.pos.x,player.pos.y);
        if (sameTile!=null)
        {
            // 빈 인접 칸으로 적을 밀어내고 로그
            foreach (var d in new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right})
            {
                var kick=sameTile.pos+d;
                if (GetT(kick.x,kick.y)!='#'&&!IsEntityAt(kick))
                {
                    sameTile.pos=kick;
                    if (sameTile.go!=null)
                    {
                        sameTile.go.transform.position=TileWorldPos(kick.x,kick.y);
                        sameTile.go.GetComponent<SpriteRenderer>().sortingOrder=EntitySortOrder(kick.y);
                    }
                    break;
                }
            }
        }

        var np=player.pos+move;
        if (!IB(np.x,np.y)) return false;

        // 적 공격 — 가시성 무관하게 인접 적은 항상 공격 가능
        var target=GetEnemyAt(np.x,np.y);
        if (target!=null)
        {
            int rawDmg = player.attack + Random.Range(-3, 4);
            int dmg;
            if (target.defense > 0 && Random.value < 0.30f)
            {
                dmg = Mathf.Max(1, rawDmg - target.defense);
                AddLog($"<color=#88AAFF>{target.name}이(가) 방어! ({rawDmg}-{target.defense}={dmg})</color>");
            }
            else
            {
                dmg = Mathf.Max(1, rawDmg);
            }
            target.hp-=dmg;
            PlaySFX(sfxHit, Random.Range(0.9f,1.1f));
            AddLog($"<color=#FFCC44>{target.name}에게 <b>{dmg}</b> 데미지!</color>");
            // 보스 HUD 체력바 즉시 갱신
            if (target.isBoss && target.hp > 0) UpdateBossHud(target);
            if (target.hp<=0)
            {
                playerExp+=target.exp;
                if (target.isBoss)
                {
                    AddLog($"<color=#FFD700>★ 보스 {target.name} 처치! +{target.exp} EXP</color>");
                    GameManager.Instance?.Player?.AddKarmaOnBossKill();
                    HideBossHud();
                    RestoreNormalBgm();
                    _bossRoomRect     = null;  // 보스 처치 후 보스룸 감지 비활성화 (BGM 재호출 방지)
                    _playerInBossRoom = false;
                    if (_bossEntity == target) _bossEntity = null;
                }
                else
                {
                    AddLog($"<color=#88FF88>{target.name} 처치! +{target.exp} EXP</color>");
                    GameManager.Instance?.Player?.AddKarmaOnNormalKill();
                }
                // 사망 사운드: sfxDeathPool에서 랜덤, 없으면 sfxEnemyHit 폴백
                if (sfxDeathPool != null && sfxDeathPool.Length > 0)
                    PlaySFX(sfxDeathPool[Random.Range(0, sfxDeathPool.Length)], Random.Range(0.9f, 1.1f));
                else
                    PlaySFX(sfxEnemyHit);
                // 처치 통계 기록
                _totalKills++;
                GameManager.Instance?.History?.RecordMonsterKill(target.name);
                if (target.go!=null) Destroy(target.go);
                enemyObjects.Remove(target.go); enemies.Remove(target);
                CheckLevelUp();
            }
            return true;
        }

        // 이동 (적이 없는 빈 바닥으로만 이동 가능)
        if (GetT(np.x,np.y)!='#'&&GetEnemyAt(np.x,np.y)==null)
        {
            player.pos=np;
            playerObject.transform.position=TileWorldPos(player.pos.x,player.pos.y);
            playerObject.GetComponent<SpriteRenderer>().sortingOrder = EntitySortOrder(player.pos.y) + 1;

            // 보스룸 입/퇴장 감지 (이동 후 새 위치 기준)
            CheckBossRoomEntry(player.pos);
            // 보스 복도 문 첫 통과 시 도어 사운드
            if (!_corridorDoorPassed && player.pos == _corridorDoorPos)
            {
                _corridorDoorPassed = true;
                PlaySFX(sfxLootboxOpen);
            }
            // 보스 HUD HP 실시간 갱신
            if (_bossEntity != null && _playerInBossRoom) UpdateBossHud(_bossEntity);

            // 아이템 획득
            var it=GetItemAt(np.x,np.y);
            if (it!=null) PickupItem(it);

            // 상점 NPC 상호작용 (보스층 복도 입구 옆 상점)
            if (_shopActive && np == _shopNpcPos)
            {
                ShowShopPanel();
                AddLog("<color=#55FF99>상점 주인: 어서오세요! (ESC 또는 ✕로 닫기)</color>");
                return true;
            }

            // 아이템 상자 충돌 체크
            var chest = GetChestAt(np.x, np.y);
            if (chest != null && !chest.opened)
            {
                OpenTreasureChest(chest);
                return true;
            }

            // 계단
            if (player.pos==stairsPos)
            {
                PlaySFX(sfxStairs);
                AddLog("<color=#88FFCC>다음 층으로 내려간다...</color>");
                currentLevel++;
                GameManager.Instance?.AdvanceFloor();
                InitializeLevel();
                StartCoroutine(ShowStartupMessage());
                return false;
            }
            return true;
        }
        return false;
    }

    private void PickupItem(Item it)
    {
        PlaySFX(sfxBonus);
        switch (it.type)
        {
            case ItemType.Potion:
                potionInventory++;
                potionsPickedUp++;
                GameManager.Instance?.History?.RecordItemObtained();
                UpdatePotionCountUI();
                AddLog($"<color=#55FF88>물약 획득! (소지: {potionInventory}개)</color>");
                break;
            case ItemType.GoldSmall:
                playerGold+=it.value;
                GameManager.Instance?.History?.RecordGoldObtained(it.value);
                AddLog($"<color=#FFD700>골드 +{it.value} G (소형)</color>");
                break;
            case ItemType.GoldLarge:
                playerGold+=it.value;
                GameManager.Instance?.History?.RecordGoldObtained(it.value);
                AddLog($"<color=#FFA500>골드 +{it.value} G (대형)</color>");
                break;
        }
        if (it.go!=null) Destroy(it.go);
        itemObjects.Remove(it.go); items.Remove(it);
    }

    private void CheckLevelUp()
    {
        // exp 30마다 레벨업
        int newLevel = 1 + playerExp / 30;
        if (newLevel <= _playerLevel) return;

        int gained = newLevel - _playerLevel;
        _playerLevel = newLevel;
        player.attack += 1 * gained;
        player.maxHp  += 10 * gained;
        player.hp     = Mathf.Min(player.hp + 10 * gained, player.maxHp);
        AddLog($"<color=#AAFFFF>레벨 업! (Lv.{_playerLevel}) 공격력 +{1*gained}, 최대 HP +{10*gained}</color>");

    }

    // ====================================================================
    // 미니맵 오버레이 (Tab = 열기/닫기, ESC = 닫기)
    // explored[] 기반으로 타일 색상을 Texture2D에 그려 RawImage로 표시
    // ====================================================================
    private void ToggleMinimap()
    {
        if (_minimapOpen) CloseMinimap();
        else              OpenMinimap();
    }

    private void OpenMinimap()
    {
        if (map == null || player == null) return;
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        // 전체화면 반투명 배경
        _minimapOverlay = MakePanel(canvas.transform, "MinimapOverlay",
            Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero,
            new Color(0f, 0f, 0f, 0.78f));
        _minimapOverlay.transform.SetAsLastSibling();

        // 한 타일 = 4×4 픽셀
        const int scale = 4;
        int tw = MapWidth  * scale;   // 256
        int th = MapHeight * scale;   // 192
        _minimapTex = new Texture2D(tw, th, TextureFormat.RGBA32, false);
        _minimapTex.filterMode = FilterMode.Point;
        RenderMinimapTexture(scale);

        var rawGo = new GameObject("MinimapRawImage");
        rawGo.transform.SetParent(_minimapOverlay.transform, false);
        var raw = rawGo.AddComponent<RawImage>();
        raw.texture = _minimapTex;
        var rt = rawGo.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = new Vector2(tw * 2f, th * 2f); // 2배 확대 표시

        // 제목 텍스트
        var titleGo = new GameObject("MinimapTitle");
        titleGo.transform.SetParent(_minimapOverlay.transform, false);
        var title = titleGo.AddComponent<Text>();
        title.font      = korFont != null ? korFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        title.fontSize  = 24;
        title.color     = new Color(1f, 0.93f, 0.6f);
        title.alignment = TextAnchor.MiddleCenter;
        title.text      = "미니맵  [ Tab / ESC : 닫기 ]";
        var titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin        = new Vector2(0.5f, 1f);
        titleRt.anchorMax        = new Vector2(0.5f, 1f);
        titleRt.pivot            = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -16f);
        titleRt.sizeDelta        = new Vector2(500f, 40f);

        _minimapOpen = true;
    }

    private void RenderMinimapTexture(int scale)
    {
        // 전체 투명 초기화
        int tw = MapWidth * scale;
        int th = MapHeight * scale;
        var transparent = new Color(0f, 0f, 0f, 0f);
        Color[] buf = new Color[tw * th];
        for (int i = 0; i < buf.Length; i++) buf[i] = transparent;
        _minimapTex.SetPixels(buf);

        // 타일별 색상 정의
        var floorVis  = new Color(0.75f, 0.68f, 0.50f, 1f);
        var floorDim  = new Color(0.28f, 0.24f, 0.18f, 1f);
        var wallVis   = new Color(0.50f, 0.44f, 0.38f, 1f);
        var wallDim   = new Color(0.16f, 0.14f, 0.12f, 1f);
        var stairCol  = new Color(0.80f, 0.30f, 1.00f, 1f);
        var playerCol = Color.yellow;

        for (int x = 0; x < MapWidth; x++)
        {
            for (int y = 0; y < MapHeight; y++)
            {
                if (!explored[x, y]) continue;

                Color col;
                if (x == player.pos.x && y == player.pos.y)
                    col = playerCol;
                else if (map[x, y] == '>' || map[x, y] == '<')
                    col = stairCol;
                else if (map[x, y] == '#')
                    col = visible[x, y] ? wallVis : wallDim;
                else
                    col = visible[x, y] ? floorVis : floorDim;

                // scale×scale 블록으로 채우기
                for (int px = 0; px < scale; px++)
                    for (int py = 0; py < scale; py++)
                        _minimapTex.SetPixel(x * scale + px, y * scale + py, col);
            }
        }
        _minimapTex.Apply();
    }

    private void CloseMinimap()
    {
        if (_minimapOverlay != null) { Destroy(_minimapOverlay); _minimapOverlay = null; }
        if (_minimapTex     != null) { Destroy(_minimapTex);     _minimapTex     = null; }
        _minimapOpen = false;
    }

    // ====================================================================
    // BFS 경로탐색: 적(e)에서 goal까지의 최단 경로 첫 번째 방향 반환.
    // 벽('#')을 우회하며, 보스는 _bossRoomRect 내부로만 이동
    // (단, 목표 타일 자체는 예외로 허용).
    // 경로가 없으면 Vector2Int.zero 반환.
    // ====================================================================
    private Vector2Int BFSNextStep(Entity e, Vector2Int goal)
    {
        var start = e.pos;
        if (start == goal) return Vector2Int.zero;

        var queue    = new Queue<Vector2Int>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        queue.Enqueue(start);
        cameFrom[start] = start;

        var dirs = new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();
            if (cur == goal)
            {
                // 경로를 역추적하여 start 바로 다음 한 칸을 반환
                var step = cur;
                while (cameFrom[step] != start) step = cameFrom[step];
                return step - start;
            }
            foreach (var d in dirs)
            {
                var next = cur + d;
                if (cameFrom.ContainsKey(next)) continue;
                if (GetT(next.x, next.y) == '#') continue;
                // 보스는 보스방 경계 내부만 이동 가능 (목표 타일은 예외)
                // 귀환 중(목표=방 중앙)일 때는 보스방 내부라면 항상 통과
                if (e.isBoss && _bossRoomRect.HasValue && next != goal
                    && !_bossRoomRect.Value.Contains(next)) continue;
                cameFrom[next] = cur;
                queue.Enqueue(next);
            }
        }
        return Vector2Int.zero; // 경로 없음
    }

    // ====================================================================
    // 적 턴
    // ====================================================================
    private void ProcessEnemyTurn()
    {
        foreach (var e in new List<Entity>(enemies))
        {
            // 시야에 들어오면 어그로 활성화 (이후 복도로 도망쳐도 계속 추적)
            if (IsVis(e.pos.x,e.pos.y)) e.isAggro = true;

            // 어그로 상태가 아니면 행동하지 않음
            if (!e.isAggro) continue;
            // 너무 멀면 추적 포기 (어그로 해제)
            if (Vector2Int.Distance(e.pos,player.pos)>16) { e.isAggro=false; continue; }

            // ── 보스 귀환 처리 ─────────────────────────────────────────────
            // 보스가 어그로 상태이지만 플레이어가 시야 밖이면 방 중앙으로 귀환
            if (e.isBoss && _bossRoomRect.HasValue && !IsVis(e.pos.x, e.pos.y))
            {
                var center = RoomCenter(_bossRoomRect.Value);
                if (e.pos == center)
                {
                    // 방 중앙 도달 → 어그로 해제
                    e.isAggro = false;
                    continue;
                }
                var retMv = BFSNextStep(e, center);
                if (retMv != Vector2Int.zero)
                {
                    var retNp = e.pos + retMv;
                    if (GetT(retNp.x, retNp.y) != '#' && !IsEntityAt(retNp))
                    {
                        e.pos = retNp;
                        if (e.go != null)
                        {
                            e.go.transform.position = TileWorldPos(e.pos.x, e.pos.y);
                            var retSr = e.go.GetComponent<SpriteRenderer>();
                            retSr.sortingOrder = EntitySortOrder(e.pos.y);
                            if (retMv.x != 0) { e.facingRight = retMv.x > 0; UpdateEnemyFacing(e, retSr); }
                        }
                    }
                }
                continue; // 귀환 중에는 플레이어 추적 건너뜀
            }

            // BFS로 벽을 우회하는 최단 경로 첫 번째 방향을 구한다
            var mv = BFSNextStep(e, player.pos);
            if (mv == Vector2Int.zero) continue; // 경로 없음 (완전 차단)

            var np=e.pos+mv;
            if (np==player.pos)
            {
                int rawDmg = e.attack + Random.Range(-2, 3);
                int dmg;
                if (_equipDef > 0 && _defChance > 0f && Random.value < _defChance)
                {
                    dmg = Mathf.Max(1, rawDmg - _equipDef);
                    PlaySFX(sfxHit, 0.7f);
                    AddLog($"<color=#88AAFF>방어 성공! ({rawDmg}-{_equipDef}={dmg}) <color=#FF6666>{e.name}에게 <b>{dmg}</b> 데미지를 받았다!</color></color>");
                }
                else
                {
                    dmg = Mathf.Max(1, rawDmg);
                    PlaySFX(sfxHit, 0.7f);
                    AddLog($"<color=#FF6666>{e.name}에게 <b>{dmg}</b> 데미지를 받았다!</color>");
                }
                player.hp-=dmg;
            }
            else if (GetT(np.x,np.y)!='#' && !IsEntityAt(np) &&
                     !(e.isBoss && _bossRoomRect.HasValue && !_bossRoomRect.Value.Contains(np)))
            {
                e.pos=np;
                if (e.go!=null)
                {
                    e.go.transform.position=TileWorldPos(e.pos.x,e.pos.y);
                    var esr = e.go.GetComponent<SpriteRenderer>();
                    esr.sortingOrder = EntitySortOrder(e.pos.y);
                    // 좌우 이동 시 스프라이트 방향 전환
                    if (mv.x != 0)
                    {
                        e.facingRight = mv.x > 0;
                        UpdateEnemyFacing(e, esr);
                    }
                }
            }
        }
        if (player.hp<=0)
        {
            ShowDeathPanel();
        }
    }

    // ====================================================================
    // 적 스프라이트 방향 전환
    // spriteLeft 설정 시 실제 스프라이트 교체, 미설정 시 flipX 사용.
    // ====================================================================
    private void UpdateEnemyFacing(Entity e, SpriteRenderer sr)
    {
        if (e.spriteLeft != null)
        {
            sr.sprite = e.facingRight ? e.spriteRight : e.spriteLeft;
            sr.flipX  = false;
        }
        else
        {
            // 왼쪽 스프라이트 미설정: 오른쪽 스프라이트를 flipX로 사용
            sr.flipX = !e.facingRight;
        }
    }

    // ====================================================================
    // 사망 결과 패널
    // ====================================================================
    private void ShowDeathPanel()
    {
        isProcessingTurn = true;  // 즉시 모든 조작 차단
        if (playerAnimCoroutine != null) StopCoroutine(playerAnimCoroutine);
        PlaySFX(sfxGameOver);
        AddLog("<color=#FF4444>사망하였습니다...</color>");
        HideBossHud();

        // 사망 처리는 GameManager.OnPlayerDeath()에서 일괄 수행
        // (History.RecordDeath, 카르마 기반 각인 해금, 씬 전환 포함)

        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas==null) { GameManager.Instance?.OnPlayerDeath(); return; }

        // 전체화면 반투명 오버레이
        var overlay = MakePanel(canvas.transform,"DeathOverlay",
            Vector2.zero,Vector2.one,new Vector2(0.5f,0.5f),
            Vector2.zero,Vector2.zero,new Color(0f,0f,0f,0.82f));

        // 중앙 결과 박스 — 높이 640px
        var box = MakePanel(overlay.transform,"DeathBox",
            new Vector2(0.5f,0.5f),new Vector2(0.5f,0.5f),new Vector2(0.5f,0.5f),
            Vector2.zero,new Vector2(560,640),new Color(0.10f,0.04f,0.06f,0.97f));

        // ── 상단: 쾐러지는 애니메이션 이미지 (88×88px) ──
        var deathAnimGo = new GameObject("DeathAnimImg");
        deathAnimGo.transform.SetParent(box.transform, false);
        var deathAnimImg = deathAnimGo.AddComponent<Image>();
        deathAnimImg.preserveAspect = true;
        deathAnimImg.color = Color.white;
        deathAnimImg.sprite = GetPlayerDownedSprite();
        var deathAnimRt = deathAnimGo.GetComponent<RectTransform>();
        deathAnimRt.anchorMin = new Vector2(0.5f, 1f);
        deathAnimRt.anchorMax = new Vector2(0.5f, 1f);
        deathAnimRt.pivot     = new Vector2(0.5f, 1f);
        deathAnimRt.anchoredPosition = new Vector2(0f, -6f);
        deathAnimRt.sizeDelta = new Vector2(88f, 88f);
        playerAnimCoroutine = StartCoroutine(PlayDeathAnimOnUI(deathAnimImg));

        // ── 제목: 사망하였습니다 (y −94 ≤ y ≤ −140) ──
        var titleTxt = MakeText(box.transform,"DeathTitle",
            new Vector2(0,1),new Vector2(1,1),
            new Vector2(16,-140),new Vector2(-16,-94),
            38,new Color(1f,0.2f,0.2f),TextAnchor.UpperCenter);
        titleTxt.text = "사망하였습니다";

        // 제목 아래 구분선
        var line = MakePanel(box.transform,"Divider",
            new Vector2(0,1),new Vector2(1,1),new Vector2(0.5f,1),
            new Vector2(0,-148),new Vector2(-40,2),new Color(0.6f,0.15f,0.15f,1f));

        // ── 통계 (top-anchored, 상단 156px ~ 하단 94px) ──
        string statsText =
            $"<color=#FFD966>도달한 층    :  {currentLevel}층</color>\n\n" +
            $"<color=#FF9090>쓰러뜨린 적  :  {_totalKills}마리</color>\n\n" +
            $"<color=#88FF88>회복약 사용  :  {potionsPickedUp}개</color>\n\n" +
            $"<color=#FFD700>획득 골드    :  {playerGold} G</color>";

        var statsTxt = MakeText(box.transform,"DeathStats",
            new Vector2(0,1),new Vector2(1,1),
            new Vector2(46,-546),new Vector2(-46,-156),
            26,new Color(0.9f,0.85f,0.85f),TextAnchor.UpperLeft);
        statsTxt.text = statsText;

        // ── 확인 버튼 (하단에서 30px 위, 240×62) ──
        var btnGo = new GameObject("ConfirmBtn");
        btnGo.transform.SetParent(box.transform,false);
        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = new Color(0.55f,0.10f,0.10f,1f);
        var btnRt = btnGo.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f,0f); btnRt.anchorMax = new Vector2(0.5f,0f);
        btnRt.pivot     = new Vector2(0.5f,0f);
        btnRt.anchoredPosition = new Vector2(0f,30f);
        btnRt.sizeDelta = new Vector2(240,62);
        var btn = btnGo.AddComponent<Button>();
        var cb  = btn.colors;
        cb.normalColor      = new Color(0.55f,0.10f,0.10f,1f);
        cb.highlightedColor = new Color(0.75f,0.18f,0.18f,1f);
        cb.pressedColor     = new Color(0.35f,0.05f,0.05f,1f);
        btn.colors = cb;
        btn.onClick.AddListener(()=>GameManager.Instance?.OnPlayerDeath());
        MakeText(btnGo.transform,"BtnText",
            Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,
            34,Color.white,TextAnchor.MiddleCenter).text = "확인";
    }

    private IEnumerator DelayLoad(string scene,float delay)
    {
        isProcessingTurn=true;
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(scene);
    }

    // ====================================================================
    // 시야
    // ====================================================================
    private void UpdateVisibility()
    {
        for (int x=0;x<MapWidth;x++) for (int y=0;y<MapHeight;y++) visible[x,y]=false;

        int vr=5;
        for (int x=player.pos.x-vr;x<=player.pos.x+vr;x++)
            for (int y=player.pos.y-vr;y<=player.pos.y+vr;y++)
                if (Vector2Int.Distance(player.pos,new Vector2Int(x,y))<vr) SetVis(x,y);

        foreach (var room in rooms)
            if (room.Contains(player.pos))
                for (int x=room.xMin;x<room.xMax;x++)
                    for (int y=room.yMin;y<room.yMax;y++) SetVis(x,y);

        // 보스방에 있을 때 보스방 전체 공개
        if (_bossRoomRect.HasValue && _bossRoomRect.Value.Contains(player.pos))
            for (int x = _bossRoomRect.Value.xMin; x < _bossRoomRect.Value.xMax; x++)
                for (int y = _bossRoomRect.Value.yMin; y < _bossRoomRect.Value.yMax; y++)
                    SetVis(x, y);

        // 포탈룸에 있을 때 포탈룸 전체 공개
        if (_portalRoomRect.HasValue && _portalRoomRect.Value.Contains(player.pos))
            for (int x = _portalRoomRect.Value.xMin; x < _portalRoomRect.Value.xMax; x++)
                for (int y = _portalRoomRect.Value.yMin; y < _portalRoomRect.Value.yMax; y++)
                    SetVis(x, y);

        var visTheme = CurrentTheme; // 루프 안에서 매번 조회하지 않도록 캐시
        for (int x=0;x<MapWidth;x++)
        {
            for (int y=0;y<MapHeight;y++)
            {
                if (tileObjects[x,y]==null) continue;
                var sr=tileObjects[x,y].GetComponent<SpriteRenderer>();
                if (!explored[x,y])        { tileObjects[x,y].SetActive(false); }
                else if (visible[x,y])
                {
                    tileObjects[x,y].SetActive(true);
                    sr.color = Color.white;
                }
                else
                {
                    tileObjects[x,y].SetActive(true);
                    sr.color = new Color(0.35f, 0.35f, 0.35f); // 탐색됐지만 시야 밖: 무채색 어둠
                }
            }
        }

        // 계단
        bool sv=explored[stairsPos.x,stairsPos.y];
        stairsObject.SetActive(sv);
        if (sv)
        {
            var ssr=stairsObject.GetComponent<SpriteRenderer>();
            ssr.color = IsVis(stairsPos.x, stairsPos.y) ? Color.white : new Color(0.35f, 0.35f, 0.35f);
        }

        // 적
        foreach (var e in enemies) if (e.go!=null) e.go.SetActive(IsVis(e.pos.x,e.pos.y));

        // 아이템
        foreach (var it in items)
            if (it.go!=null) it.go.SetActive(IsVis(it.pos.x,it.pos.y)||explored[it.pos.x,it.pos.y]);

        // 아이템 상자
        foreach (var chest in _chests)
            if (chest.go != null)
                chest.go.SetActive(IsVis(chest.pos.x, chest.pos.y) || explored[chest.pos.x, chest.pos.y]);

        // 보스룸 문 & 상점 NPC 가시성
        if (_bossCorridorDoorGo != null)
        {
            bool dv = IB(_corridorDoorPos.x, _corridorDoorPos.y) &&
                      explored[_corridorDoorPos.x, _corridorDoorPos.y];
            _bossCorridorDoorGo.SetActive(dv);
        }
        if (_bossShopNpcGo != null)
        {
            bool sv2 = IB(_shopNpcPos.x, _shopNpcPos.y) &&
                       explored[_shopNpcPos.x, _shopNpcPos.y];
            _bossShopNpcGo.SetActive(sv2);
        }
    }

    private void SetVis(int x,int y){if(IB(x,y)){visible[x,y]=true;explored[x,y]=true;}}
    private bool IsVis(int x,int y)=>IB(x,y)&&visible[x,y];

    // ====================================================================
    // UI 갱신
    // ====================================================================
    private void UpdateUI()
    {
        if (player==null) return;
        float r=(float)player.hp/player.maxHp;
        Color hc=Color.Lerp(new Color(1f,0.3f,0.3f),new Color(0.4f,1f,0.4f),r);
        if (hpDisplay!=null)
            hpDisplay.text=$"<color=#{ColorUtility.ToHtmlStringRGB(hc)}>HP {player.hp}/{player.maxHp}</color>   " +
                            $"<color=#FFD700>G {playerGold}</color>";
        if (statusDisplay!=null)
        {
            float karma = GameManager.Instance?.Player?.Karma ?? 0f;
            statusDisplay.text=$"<color=#FFD966>{currentLevel}F</color>  ATK {player.attack}" +
                                $"  EXP {playerExp}  적 {enemies.Count}" +
                                $"  <color=#CC99FF>카르마 {karma:F1}%</color>";
        }
        UpdatePotionCountUI();
    }

    private void AddLog(string msg)
    {
        if (logDisplay==null) return;
        gameLogs.Enqueue(msg);
        while (gameLogs.Count>7) gameLogs.Dequeue();
        logDisplay.text=string.Join("\n",gameLogs);
    }

    // ====================================================================
    // 유틸
    // ====================================================================
    private Entity GetEnemyAt(int x,int y) => enemies.Find(e=>e.pos.x==x&&e.pos.y==y);
    private Item   GetItemAt(int x,int y)  => items.Find(i=>i.pos.x==x&&i.pos.y==y);

    // ====================================================================
    // 플레이어 애니메이션 (방향당 1장 스프라이트)
    // ====================================================================
    private Sprite GetPlayerDirectionSprite(PlayerFacing facing)
    {
        return facing == PlayerFacing.Up    ? (playerSpriteUp     ?? playerSprite) :
               facing == PlayerFacing.Left  ? (playerSpriteLeft   ?? playerSprite) :
               facing == PlayerFacing.Right ? (playerSpriteRight  ?? playerSprite) :
                                              (playerSpriteDown   ?? playerSprite);
    }

    private Sprite GetPlayerDownedSprite()
        => playerSpriteDowned != null ? playerSpriteDowned : playerSprite;

    private void SetPlayerIdleSprite()
    {
        if (playerObject == null) return;
        var sr = playerObject.GetComponent<SpriteRenderer>();
        if (sr == null) return;
        sr.sprite = GetPlayerDirectionSprite(playerFacing);
    }

    private void TriggerPlayerWalkAnim(Vector2Int move)
    {
        PlayerFacing facing = move.y > 0 ? PlayerFacing.Up   :
                              move.y < 0 ? PlayerFacing.Down :
                              move.x < 0 ? PlayerFacing.Left : PlayerFacing.Right;
        playerFacing = facing;
        if (playerAnimCoroutine != null) { StopCoroutine(playerAnimCoroutine); playerAnimCoroutine = null; }
        // 단일 스프라이트: 즉시 방향 전환
        if (playerObject != null)
        {
            var sr = playerObject.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sprite = GetPlayerDirectionSprite(facing);
        }
    }

    private IEnumerator PlayPlayerDownedAnim(float frameTime = 0.12f)
    {
        if (playerObject != null)
        {
            var sr = playerObject.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sprite = GetPlayerDownedSprite();
        }
        yield return null;
    }

    // 사망 패널 상단 UI Image에 쓰러짐 스프라이트 표시
    private IEnumerator PlayDeathAnimOnUI(Image img, float frameTime = 0.14f)
    {
        if (img != null) img.sprite = GetPlayerDownedSprite();
        yield return null;
    }

    // ====================================================================
    // 물약 사용
    // ====================================================================
    private void UsePotion()
    {
        if (player == null || player.hp <= 0) return;
        if (potionInventory <= 0) { AddLog("<color=#FF8888>물약이 없습니다!</color>"); return; }
        int healAmt = 25 + currentLevel * 5;
        int heal = Mathf.Min(healAmt, player.maxHp - player.hp);
        if (heal <= 0) { AddLog("<color=#FFCC44>HP가 이미 가득 찼습니다.</color>"); return; }
        player.hp += heal;
        potionInventory--;
        PlaySFX(sfxBonus);
        UpdatePotionCountUI();
        UpdateUI();
        AddLog($"<color=#55FF88>물약 사용! HP +{heal}  [남은 물약: {potionInventory}개]</color>");
    }

    private void UpdatePotionCountUI()
    {
        if (potionCountText != null)
            potionCountText.text = $"x {potionInventory}";
    }

    // ====================================================================
    // 인벤토리 / 장비 패널 토글
    // ====================================================================
    private void ToggleInventory()
    {
        if (_inventoryPanel == null)
        {
            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;
            BuildInventoryPanel(canvas);
        }
        if (_equipmentPanel != null && _equipmentPanel.activeSelf)
        { _equipmentPanel.SetActive(false); ItemSlotUI.HideTooltip(); }
        bool open = !_inventoryPanel.activeSelf;
        _inventoryPanel.SetActive(open);
        if (open) RefreshInventoryPanel();
        else ItemSlotUI.HideTooltip();
    }

    private void ToggleEquipment()
    {
        if (_equipmentPanel == null)
        {
            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;
            BuildEquipmentPanel(canvas);
        }
        if (_inventoryPanel != null && _inventoryPanel.activeSelf)
        { _inventoryPanel.SetActive(false); ItemSlotUI.HideTooltip(); }
        bool open = !_equipmentPanel.activeSelf;
        _equipmentPanel.SetActive(open);
        if (open) RefreshEquipmentPanel();
        else ItemSlotUI.HideTooltip();
    }

    // ── 인벤토리 패널 빌드 ────────────────────────────────────────────────────
    private void BuildInventoryPanel(Canvas canvas)
    {
        _inventoryPanel = new GameObject("InventoryPanel");
        _inventoryPanel.transform.SetParent(canvas.transform, false);
        var overlay = _inventoryPanel.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.76f);
        var overlayRt = _inventoryPanel.GetComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero; overlayRt.anchorMax = Vector2.one; overlayRt.sizeDelta = Vector2.zero;

        var thm = Theme;
        var box = new GameObject("InvenBox"); box.transform.SetParent(_inventoryPanel.transform, false);
        var boxImg = box.AddComponent<Image>();
        if (thm?.panelMain != null) { boxImg.sprite = thm.panelMain; boxImg.type = Image.Type.Sliced; boxImg.color = Color.white; }
        else boxImg.color = new Color(0.12f, 0.08f, 0.04f, 0.98f);
        var boxRt = box.GetComponent<RectTransform>();
        // 앵커: 화면 중앙 고정 (sizeDelta는 RefreshInventoryPanel에서 재설정)
        boxRt.anchorMin = new Vector2(0.5f, 0.5f); boxRt.anchorMax = new Vector2(0.5f, 0.5f);
        boxRt.pivot = new Vector2(0.5f, 0.5f); boxRt.anchoredPosition = new Vector2(0, 20f);
        boxRt.sizeDelta = new Vector2(600f, 420f); // 초기값 (Refresh에서 재설정됨)

        // 제목 (상단 고정)
        var titleGo = new GameObject("Title"); titleGo.transform.SetParent(box.transform, false);
        var titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f); titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f); titleRt.anchoredPosition = new Vector2(0f, -8f); titleRt.sizeDelta = new Vector2(0f, 48f);
        var titleTxt = titleGo.AddComponent<Text>();
        titleTxt.text = "인벤토리";
        titleTxt.font = korFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleTxt.fontSize = 27; titleTxt.color = new Color(1f, 0.92f, 0.65f);
        titleTxt.alignment = TextAnchor.MiddleCenter;
        titleTxt.horizontalOverflow = HorizontalWrapMode.Overflow; titleTxt.verticalOverflow = VerticalWrapMode.Overflow;

        // 조작 힌트 (제목 아래)
        var hintGo = new GameObject("Hint"); hintGo.transform.SetParent(box.transform, false);
        var hintRt = hintGo.AddComponent<RectTransform>();
        hintRt.anchorMin = new Vector2(0f, 1f); hintRt.anchorMax = new Vector2(1f, 1f);
        hintRt.pivot = new Vector2(0.5f, 1f); hintRt.anchoredPosition = new Vector2(0f, -60f); hintRt.sizeDelta = new Vector2(0f, 22f);
        var hintTxt = hintGo.AddComponent<Text>();
        hintTxt.text = "좌클릭: 사용 │ 우클릭: 버리기 │ 드래그: 이동 │ [I] / [ESC]: 닫기";
        hintTxt.font = korFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hintTxt.fontSize = 14; hintTxt.color = new Color(0.58f, 0.52f, 0.38f);
        hintTxt.alignment = TextAnchor.MiddleCenter;
        hintTxt.horizontalOverflow = HorizontalWrapMode.Overflow; hintTxt.verticalOverflow = VerticalWrapMode.Overflow;

        // X 닫기 버튼
        var closeBtnGo = new GameObject("InvenCloseBtn"); closeBtnGo.transform.SetParent(box.transform, false);
        var closeBtnImg = closeBtnGo.AddComponent<Image>(); closeBtnImg.color = new Color(0.65f, 0.10f, 0.10f, 0.95f);
        var closeBtnRt = closeBtnGo.GetComponent<RectTransform>();
        closeBtnRt.anchorMin = new Vector2(1f, 1f); closeBtnRt.anchorMax = new Vector2(1f, 1f); closeBtnRt.pivot = new Vector2(1f, 1f);
        closeBtnRt.anchoredPosition = new Vector2(-8f, -6f); closeBtnRt.sizeDelta = new Vector2(44f, 44f);
        var closeBtn = closeBtnGo.AddComponent<Button>();
        closeBtn.onClick.AddListener(() => { _inventoryPanel.SetActive(false); ItemSlotUI.HideTooltip(); });
        var cbX = closeBtn.colors; cbX.normalColor = Color.white; cbX.highlightedColor = new Color(1f, 0.6f, 0.6f, 1f); cbX.pressedColor = new Color(0.7f, 0.2f, 0.2f, 1f); closeBtn.colors = cbX;
        MakeText(closeBtnGo.transform, "X", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 22, Color.white, TextAnchor.MiddleCenter).text = "✕";

        // InventorySlots 컨테이너 (위치와 크기는 RefreshInventoryPanel에서 결정)
        var slotsGo = new GameObject("InventorySlots"); slotsGo.transform.SetParent(box.transform, false);
        var slotsRt = slotsGo.AddComponent<RectTransform>();
        slotsRt.anchorMin = new Vector2(0.5f, 1f); slotsRt.anchorMax = new Vector2(0.5f, 1f);
        slotsRt.pivot = new Vector2(0.5f, 1f); slotsRt.anchoredPosition = new Vector2(0f, -88f); slotsRt.sizeDelta = new Vector2(400f, 240f);
        // RectMask2D: 컨테이너 경계 밖으로 아이템이 삐져나오지 않도록 마스크
        slotsGo.AddComponent<RectMask2D>();

        // 공유 툴팁 초기화 (ItemSlotUI.cs에서 사용)
        ItemSlotUI.EnsureSharedUI(canvas, korFont);

        _inventoryPanel.SetActive(false);
    }

    private void RefreshInventoryPanel()
    {
        if (_inventoryPanel == null) return;
        var canvas = FindAnyObjectByType<Canvas>();
        // 인벤토리가 파괴된 뒤 재빌드된 경우를 위해 공유 UI 재보장
        if (canvas != null) ItemSlotUI.EnsureSharedUI(canvas, korFont);

        var slotsRoot = _inventoryPanel.transform.Find("InvenBox/InventorySlots");
        if (slotsRoot == null) return;

        // 기존 슬롯 자식 제거
        for (int i = slotsRoot.childCount - 1; i >= 0; i--)
            Destroy(slotsRoot.GetChild(i).gameObject);

        int cols = _mainInvCols;
        int rows = _mainInvRows;

        // 슬롯 크기 (정사각형 아이콘 기반)
        const float slotW  = 90f;
        const float slotH  = 90f;
        const float gapX   = 10f;
        const float gapY   = 10f;
        float gridW = cols * slotW + (cols - 1) * gapX;
        float gridH = rows * slotH + (rows - 1) * gapY;

        // ── InvenBox 및 InventorySlots 컨테이너 크기 갱신 ─────────────────────
        var boxRt    = _inventoryPanel.transform.Find("InvenBox")?.GetComponent<RectTransform>();
        var slotsRt  = slotsRoot.GetComponent<RectTransform>();
        if (boxRt   != null) boxRt.sizeDelta   = new Vector2(gridW + 80f, gridH + 126f);
        if (slotsRt != null) slotsRt.sizeDelta  = new Vector2(gridW, gridH);

        // ── 슬롯 생성 ────────────────────────────────────────────────────────
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int idx = c + r * cols;
                var item = (idx < _mainInvGrid.Length) ? _mainInvGrid[idx] : null;

                // 슬롯 배경 (포인트 앵커 → sizeDelta가 절대 크기가 됨)
                var slotGo  = new GameObject($"Slot_{c}_{r}");
                slotGo.transform.SetParent(slotsRoot, false);
                var slotImg = slotGo.AddComponent<Image>();
                slotImg.color = item != null
                    ? new Color(0.28f, 0.18f, 0.07f, 0.97f)
                    : new Color(0.07f, 0.05f, 0.03f, 0.97f);
                var slotRt = slotGo.GetComponent<RectTransform>();
                // ★ 반드시 포인트 앵커로 설정해야 sizeDelta가 절대 크기로 동작
                slotRt.anchorMin = new Vector2(0.5f, 0.5f);
                slotRt.anchorMax = new Vector2(0.5f, 0.5f);
                slotRt.pivot     = new Vector2(0.5f, 0.5f);
                float posX = c * (slotW + gapX) - gridW * 0.5f + slotW * 0.5f;
                float posY = -r * (slotH + gapY) + gridH * 0.5f - slotH * 0.5f;
                slotRt.anchoredPosition = new Vector2(posX, posY);
                slotRt.sizeDelta        = new Vector2(slotW, slotH);

                if (item != null)
                {
                    // 아이템 아이콘 이미지 (아이템이 있을 때만)
                    var iconGo  = new GameObject("Icon");
                    iconGo.transform.SetParent(slotGo.transform, false);
                    var iconImg = iconGo.AddComponent<Image>();
                    iconImg.preserveAspect = true;
                    if (item.icon != null)
                    {
                        iconImg.sprite = item.icon;
                        iconImg.color  = Color.white;
                    }
                    else
                    {
                        // 아이콘 없으면 색상 사각형 대체
                        iconImg.color = new Color(0.85f, 0.65f, 0.25f, 0.9f);
                    }
                    var iconRt = iconGo.GetComponent<RectTransform>();
                    iconRt.anchorMin = new Vector2(0.08f, 0.08f);
                    iconRt.anchorMax = new Vector2(0.92f, 0.92f);
                    iconRt.sizeDelta = Vector2.zero;

                    // 저주 아이템 표시 (우측 하단 작은 마크)
                    if (item.id == "cursed" || item.id.Contains("_cursed"))
                    {
                        var markGo  = new GameObject("CurseMark");
                        markGo.transform.SetParent(slotGo.transform, false);
                        var markTxt = markGo.AddComponent<Text>();
                        markTxt.text      = "⚠";
                        markTxt.font      = korFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                        markTxt.fontSize  = 12;
                        markTxt.color     = new Color(1f, 0.3f, 0.1f, 1f);
                        markTxt.alignment = TextAnchor.LowerRight;
                        markTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
                        markTxt.verticalOverflow   = VerticalWrapMode.Overflow;
                        var markRt = markGo.GetComponent<RectTransform>();
                        markRt.anchorMin = Vector2.zero; markRt.anchorMax = Vector2.one;
                        markRt.offsetMin = new Vector2(2f, 2f); markRt.offsetMax = new Vector2(-2f, -2f);
                    }

                    // ItemSlotUI 컴포넌트 부착 (드래그&드롭 / 툴팁 / 클릭)
                    var slotUI = slotGo.AddComponent<ItemSlotUI>();
                    slotUI.col = c;
                    slotUI.row = r;

                    // 툴팁 텍스트 생성 함수 캡처
                    var capturedItem = item;
                    slotUI.GetTooltip     = () => BuildItemTooltip(capturedItem);
                    slotUI.GetTooltipIcon = () => capturedItem?.icon;
                    slotUI.GetTooltipTitle = () => capturedItem?.displayName;

                    // 좌클릭 → 사용
                    int capC = c, capR = r;
                    slotUI.OnUse = (uc, ur) =>
                    {
                        int useIdx = uc + ur * _mainInvCols;
                        if (useIdx >= _mainInvGrid.Length || _mainInvGrid[useIdx] == null) return;
                        var used = _mainInvGrid[useIdx];
                        ApplyConsumable(used);
                        _mainInvGrid[useIdx] = null;
                        AddLog($"<color=#88FF88><b>{used.displayName}</b> 사용!</color>");
                        UpdateUI();
                        RefreshInventoryPanel();
                    };

                    // 우클릭 → 버리기
                    slotUI.OnDiscard = (dc, dr) =>
                    {
                        int discIdx = dc + dr * _mainInvCols;
                        if (discIdx >= _mainInvGrid.Length || _mainInvGrid[discIdx] == null) return;
                        var discarded = _mainInvGrid[discIdx];
                        _mainInvGrid[discIdx] = null;
                        AddLog($"<color=#AAAAAA>{discarded.displayName} 버렸습니다.</color>");
                        RefreshInventoryPanel();
                    };

                    // 드롭 → 슬롯 이동 (빈 슬롯으로만 이동 가능, 스택 없음)
                    slotUI.OnDropped = (fromC, fromR, toC, toR) =>
                    {
                        int fromIdx = fromC + fromR * _mainInvCols;
                        int toIdx   = toC   + toR   * _mainInvCols;
                        if (fromIdx == toIdx) return;
                        if (fromIdx < 0 || fromIdx >= _mainInvGrid.Length) return;
                        if (toIdx   < 0 || toIdx   >= _mainInvGrid.Length) return;
                        // 목적지가 비어있을 때만 이동 (아이템 중복 스택 불가)
                        if (_mainInvGrid[toIdx] != null) return;
                        _mainInvGrid[toIdx]   = _mainInvGrid[fromIdx];
                        _mainInvGrid[fromIdx] = null;
                        RefreshInventoryPanel();
                    };
                }
                else
                {
                    // 빈 슬롯: 드롭 대상이 될 수 있도록 ItemSlotUI 부착 (드롭 수신용)
                    var slotUI = slotGo.AddComponent<ItemSlotUI>();
                    slotUI.col = c;
                    slotUI.row = r;
                    slotUI.GetTooltip = null;
                    slotUI.OnUse      = null;
                    slotUI.OnDiscard  = null;
                    slotUI.OnDropped  = (fromC, fromR, toC, toR) =>
                    {
                        int fromIdx = fromC + fromR * _mainInvCols;
                        int toIdx   = toC   + toR   * _mainInvCols;
                        if (fromIdx == toIdx) return;
                        if (fromIdx < 0 || fromIdx >= _mainInvGrid.Length) return;
                        if (toIdx   < 0 || toIdx   >= _mainInvGrid.Length) return;
                        if (_mainInvGrid[toIdx] != null) return;
                        _mainInvGrid[toIdx]   = _mainInvGrid[fromIdx];
                        _mainInvGrid[fromIdx] = null;
                        RefreshInventoryPanel();
                    };
                }
            }
        }
    }

    /// <summary>아이템 툴팁 텍스트 생성.</summary>
    private string BuildItemTooltip(ConsumableItemDef item)
    {
        if (item == null) return "";
        var sb = new System.Text.StringBuilder();

        // 아이템 설명 (제목은 타이틀바에 별도 표시)
        if (!string.IsNullOrEmpty(item.description))
        {
            sb.AppendLine($"<color=#CCBBDD>{item.description}</color>");
            sb.AppendLine("<color=#443344>------------------------------</color>");
        }

        // 합닥 효과 (항목별 별도 줄)
        bool hasEffect = false;
        if (item.levelUp)
        {
            sb.AppendLine("<color=#AAFFAA>▲ 레벨 업  (ATK+3 / MaxHP+15)</color>");
            hasEffect = true;
        }
        if (item.hpHeal == 9999)
        {
            sb.AppendLine("<color=#66FF88>+ HP 완전 회복</color>");
            hasEffect = true;
        }
        else if (item.hpHeal > 0)
        {
            sb.AppendLine($"<color=#66FF88>+ HP 회복  +{item.hpHeal}</color>");
            hasEffect = true;
        }
        if (item.maxHpBonus > 0)
        {
            sb.AppendLine($"<color=#FF8888>+ 최대 HP  +{item.maxHpBonus}</color>");
            hasEffect = true;
        }
        if (item.attackBonus > 0)
        {
            sb.AppendLine($"<color=#FFAA44>+ 공격력  +{item.attackBonus}</color>");
            hasEffect = true;
        }
        if (item.defenseBonus > 0)
        {
            sb.AppendLine($"<color=#88AAFF>+ 방어력  +{item.defenseBonus}</color>");
            hasEffect = true;
        }
        if (item.defChanceBonus > 0f)
        {
            sb.AppendLine($"<color=#AADDFF>+ 방어 확률  +{Mathf.RoundToInt(item.defChanceBonus * 100)}%</color>");
            hasEffect = true;
        }
        if (item.goldGain > 0)
        {
            sb.AppendLine($"<color=#FFD700>+ 골드  +{item.goldGain}</color>");
            hasEffect = true;
        }
        if (item.cureEffect != StatusEffectType.None)
        {
            string effectName = GetStatusEffectName(item.cureEffect);
            sb.AppendLine($"<color=#88FFFF>* {effectName} 상태이상 해제</color>");
            hasEffect = true;
        }
        if (item.id == "cure_all")
        {
            sb.AppendLine("<color=#88FFFF>* 모든 상태이상 해제</color>");
            hasEffect = true;
        }
        if (!hasEffect)
            sb.AppendLine("<color=#666677>효과 없음</color>");

        // 조작 가이드
        sb.AppendLine("<color=#443344>------------------------------</color>");

        // 서명글
        if (!string.IsNullOrEmpty(item.flavorText))
        {
            sb.AppendLine("<color=#443344>------------------------------</color>");
            sb.AppendLine($"<color=#887799><i>{item.flavorText}</i></color>");
        }

        sb.Append("<color=#665566><size=11>[L] 사용   [R] 상자에 다시 넣기</size></color>");
        return sb.ToString().TrimEnd();
    }

    private static string GetStatusEffectName(StatusEffectType t)
    {
        switch (t)
        {
            case StatusEffectType.Burn:      return "화상";
            case StatusEffectType.Poison:    return "중독";
            case StatusEffectType.Charm:     return "매료";
            case StatusEffectType.Confusion: return "혼란";
            case StatusEffectType.Fatigue:   return "피로";
            default:                         return t.ToString();
        }
    }

    // ── 장비 패널 빌드 ────────────────────────────────────────────────────────
    private void BuildEquipmentPanel(Canvas canvas)
    {
        _equipmentPanel = new GameObject("EquipmentPanel");
        _equipmentPanel.transform.SetParent(canvas.transform, false);
        var overlay = _equipmentPanel.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.76f);
        var overlayRt = _equipmentPanel.GetComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero; overlayRt.anchorMax = Vector2.one; overlayRt.sizeDelta = Vector2.zero;

        var thm = Theme;
        var box = new GameObject("EquipBox"); box.transform.SetParent(_equipmentPanel.transform, false);
        var boxImg = box.AddComponent<Image>();
        if (thm?.panelMain != null) { boxImg.sprite = thm.panelMain; boxImg.type = Image.Type.Sliced; boxImg.color = Color.white; }
        else boxImg.color = new Color(0.10f, 0.06f, 0.14f, 0.98f);
        var boxRt = box.GetComponent<RectTransform>();
        boxRt.anchorMin = new Vector2(0.5f, 0.5f); boxRt.anchorMax = new Vector2(0.5f, 0.5f);
        boxRt.pivot = new Vector2(0.5f, 0.5f); boxRt.anchoredPosition = new Vector2(0f, 20f);
        boxRt.sizeDelta = new Vector2(900f, 230f); // Refresh에서 재설정됨

        // 제목
        var titleGo = new GameObject("Title"); titleGo.transform.SetParent(box.transform, false);
        var titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f); titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f); titleRt.anchoredPosition = new Vector2(0f, -8f); titleRt.sizeDelta = new Vector2(0f, 48f);
        var titleTxt = titleGo.AddComponent<Text>();
        titleTxt.text = "장비 인벤토리  [E / ESC 닫기]";
        titleTxt.font = korFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleTxt.fontSize = 27; titleTxt.color = new Color(0.85f, 0.76f, 1f);
        titleTxt.alignment = TextAnchor.MiddleCenter;
        titleTxt.horizontalOverflow = HorizontalWrapMode.Overflow; titleTxt.verticalOverflow = VerticalWrapMode.Overflow;

        // 조작 힌트
        var hintGo = new GameObject("Hint"); hintGo.transform.SetParent(box.transform, false);
        var hintRt = hintGo.AddComponent<RectTransform>();
        hintRt.anchorMin = new Vector2(0f, 1f); hintRt.anchorMax = new Vector2(1f, 1f);
        hintRt.pivot = new Vector2(0.5f, 1f); hintRt.anchoredPosition = new Vector2(0f, -60f); hintRt.sizeDelta = new Vector2(0f, 22f);
        var hintTxt = hintGo.AddComponent<Text>();
        hintTxt.text = "1×4 FIFO — 가득 차면 가장 오래된 장비가 자동으로 제거됩니다 │ [E] / [ESC]: 닫기";
        hintTxt.font = korFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hintTxt.fontSize = 14; hintTxt.color = new Color(0.5f, 0.46f, 0.62f);
        hintTxt.alignment = TextAnchor.MiddleCenter;
        hintTxt.horizontalOverflow = HorizontalWrapMode.Overflow; hintTxt.verticalOverflow = VerticalWrapMode.Overflow;

        // X 닫기 버튼
        var closeBtnGo = new GameObject("EquipCloseBtn"); closeBtnGo.transform.SetParent(box.transform, false);
        var closeBtnImg = closeBtnGo.AddComponent<Image>(); closeBtnImg.color = new Color(0.65f, 0.10f, 0.10f, 0.95f);
        var closeBtnRt = closeBtnGo.GetComponent<RectTransform>();
        closeBtnRt.anchorMin = new Vector2(1f, 1f); closeBtnRt.anchorMax = new Vector2(1f, 1f); closeBtnRt.pivot = new Vector2(1f, 1f);
        closeBtnRt.anchoredPosition = new Vector2(-8f, -6f); closeBtnRt.sizeDelta = new Vector2(44f, 44f);
        var closeBtn = closeBtnGo.AddComponent<Button>();
        closeBtn.onClick.AddListener(() => { _equipmentPanel.SetActive(false); ItemSlotUI.HideTooltip(); });
        var cbA = closeBtn.colors; cbA.normalColor = Color.white; cbA.highlightedColor = new Color(1f, 0.6f, 0.6f, 1f); cbA.pressedColor = new Color(0.7f, 0.2f, 0.2f, 1f); closeBtn.colors = cbA;
        MakeText(closeBtnGo.transform, "X", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 22, Color.white, TextAnchor.MiddleCenter).text = "✕";

        // EquipSlots 컨테이너 (1×4, Refresh에서 크기 재설정)
        var slotsGo = new GameObject("EquipSlots"); slotsGo.transform.SetParent(box.transform, false);
        var slotsRt = slotsGo.AddComponent<RectTransform>();
        slotsRt.anchorMin = new Vector2(0.5f, 1f); slotsRt.anchorMax = new Vector2(0.5f, 1f);
        slotsRt.pivot = new Vector2(0.5f, 1f); slotsRt.anchoredPosition = new Vector2(0f, -88f); slotsRt.sizeDelta = new Vector2(860f, 100f);
        slotsGo.AddComponent<RectMask2D>();

        // 공유 툴팁 초기화 (인벤토리 패널보다 먼저 열 수도 있으므로 여기서도 보장)
        ItemSlotUI.EnsureSharedUI(canvas, korFont);

        _equipmentPanel.SetActive(false);
        RefreshEquipmentPanel();
    }

    private void RefreshEquipmentPanel()
    {
        if (_equipmentPanel == null) return;

        var slotsRoot = _equipmentPanel.transform.Find("EquipBox/EquipSlots");
        if (slotsRoot == null) return;

        for (int i = slotsRoot.childCount - 1; i >= 0; i--)
            Destroy(slotsRoot.GetChild(i).gameObject);

        // 슬롯 크기 (1×8 가로 배열, 정사각형 아이콘)
        const int   totalSlots = EQUIP_INV_MAX;
        const float slotW  = 110f;
        const float slotH  = 110f;
        const float gapX   = 12f;
        float gridW = totalSlots * slotW + (totalSlots - 1) * gapX;

        // EquipBox와 EquipSlots 컨테이너 크기 갱신
        var boxRt   = _equipmentPanel.transform.Find("EquipBox")?.GetComponent<RectTransform>();
        var slotsRt = slotsRoot.GetComponent<RectTransform>();
        if (boxRt   != null) boxRt.sizeDelta   = new Vector2(gridW + 70f, slotH + 148f);
        if (slotsRt != null) slotsRt.sizeDelta  = new Vector2(gridW, slotH);

        for (int i = 0; i < totalSlots; i++)
        {
            // _chestEquipList의 i번째 장비 (없으면 null → 빈 슬롯)
            var eq = (i < _chestEquipList.Count) ? _chestEquipList[i] : null;

            // ★ 포인트 앵커 설정 → sizeDelta가 절대 크기
            var slotGo  = new GameObject($"EQ_{i}");
            slotGo.transform.SetParent(slotsRoot, false);
            var slotImg = slotGo.AddComponent<Image>();
            slotImg.color = eq != null
                ? new Color(0.24f, 0.12f, 0.36f, 0.97f)
                : new Color(0.07f, 0.04f, 0.12f, 0.97f);
            var slotRt = slotGo.GetComponent<RectTransform>();
            slotRt.anchorMin = new Vector2(0.5f, 0.5f);
            slotRt.anchorMax = new Vector2(0.5f, 0.5f);
            slotRt.pivot     = new Vector2(0.5f, 0.5f);
            float posX = i * (slotW + gapX) - gridW * 0.5f + slotW * 0.5f;
            slotRt.anchoredPosition = new Vector2(posX, 0f);
            slotRt.sizeDelta        = new Vector2(slotW, slotH);

            // 슬롯 번호 라벨 (좌측 상단, 작게)
            var numGo  = new GameObject("Num");
            numGo.transform.SetParent(slotGo.transform, false);
            var numTxt = numGo.AddComponent<Text>();
            numTxt.text      = (i + 1).ToString();
            numTxt.font      = korFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            numTxt.fontSize  = 14;
            numTxt.color     = new Color(0.5f, 0.45f, 0.6f, 0.8f);
            numTxt.alignment = TextAnchor.UpperLeft;
            numTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            numTxt.verticalOverflow   = VerticalWrapMode.Overflow;
            var numRt = numGo.GetComponent<RectTransform>();
            numRt.anchorMin = Vector2.zero; numRt.anchorMax = Vector2.one;
            numRt.offsetMin = new Vector2(3f, 3f); numRt.offsetMax = new Vector2(-3f, -3f);

            if (eq != null)
            {
                // 아이템 아이콘 이미지
                var iconGo  = new GameObject("Icon");
                iconGo.transform.SetParent(slotGo.transform, false);
                var iconImg = iconGo.AddComponent<Image>();
                iconImg.preserveAspect = true;
                if (eq.icon != null)
                {
                    iconImg.sprite = eq.icon;
                    iconImg.color  = Color.white;
                }
                else
                {
                    iconImg.color = new Color(0.65f, 0.45f, 0.9f, 0.9f);
                }
                var iconRt = iconGo.GetComponent<RectTransform>();
                iconRt.anchorMin = new Vector2(0.10f, 0.14f);
                iconRt.anchorMax = new Vector2(0.90f, 0.86f);
                iconRt.sizeDelta = Vector2.zero;

                // 마우스오버 툴팁 (이름 + 스탯)
                var tooltipComp = slotGo.AddComponent<EquipSlotTooltip>();
                tooltipComp.SetEquip(eq, korFont);
            }
        }
    }

    // ====================================================================
    // 아이템 상자 스폰
    // ====================================================================
    private void SpawnTreasureChests()
    {
        if (ChestDb == null)
        {
            Debug.LogWarning("[Chest] TreasureChestDatabase 를 찾을 수 없습니다. Tools > 6. Create Treasure Chest Database 를 실행하세요.");
            return;
        }
        // 방 1개당 최대 1개 상자 (방이 3개 이상일 때부터 배치, 첫 번째 방 제외)
        int chestCount = Mathf.Clamp(rooms.Count / 3, 1, 4);
        // 상자를 배치할 방 랜덤 선택 (첫 번째 방, 계단 있는 마지막 방 제외)
        var roomPool = new List<int>();
        for (int i = 1; i < rooms.Count - 1; i++) roomPool.Add(i);

        int placed = 0;
        for (int attempt = 0; attempt < 50 && placed < chestCount && roomPool.Count > 0; attempt++)
        {
            int poolIdx = Random.Range(0, roomPool.Count);
            int roomIdx = roomPool[poolIdx];
            roomPool.RemoveAt(poolIdx);

            var pos = FindEmptyFloorInRoomNoChest(rooms[roomIdx]);
            if (pos.x < 0) continue;

            var chest = new TreasureChest { pos = pos };

            // 단일 SpriteRenderer — lootbox.png 닫힌 스프라이트로 초기화
            var go = new GameObject($"Chest_{placed}");
            go.transform.position = TileWorldPos(pos.x, pos.y);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = chestClosedSprite;
            sr.color        = Color.white;
            sr.sortingOrder = 3;

            // 크기 정규화 (타일 88% 채움)
            if (chestClosedSprite != null)
            {
                float fs = TileSize * 0.88f / Mathf.Max(chestClosedSprite.bounds.size.x, chestClosedSprite.bounds.size.y);
                go.transform.localScale = new Vector3(fs, fs, 1f);
            }
            else
                go.transform.localScale = Vector3.one * 0.88f;

            go.SetActive(false);
            chest.go    = go;
            chest.topGo = null; // 단일 레이어 — topGo 미사용
            _chests.Add(chest);
            placed++;
        }
    }

    private Vector2Int FindEmptyFloorInRoomNoChest(RectInt room)
    {
        for (int i = 0; i < 60; i++)
        {
            int x = Random.Range(room.xMin, room.xMax);
            int y = Random.Range(room.yMin, room.yMax);
            if (GetT(x, y) != '.') continue;
            if (GetEnemyAt(x, y) != null) continue;
            if (GetItemAt(x, y) != null) continue;
            if (GetChestAt(x, y) != null) continue;
            if (player != null && player.pos == new Vector2Int(x, y)) continue;
            return new Vector2Int(x, y);
        }
        return new Vector2Int(-1, -1);
    }

    private TreasureChest GetChestAt(int x, int y)
        => _chests.Find(c => c.pos.x == x && c.pos.y == y);

    // 낙개 스프라이트 적용 — 너비가 하단과 일치하도록 스케일 부여,
    // 낙개 하단이 상자 상단에 딥도록 Y 위치 자동 계산
    // 상자의 단일 SpriteRenderer 스프라이트를 교체 (닫힘↔열림 전환)
    private void ApplyChestTopSprite(TreasureChest chest, Sprite sprite)
    {
        if (chest?.go == null) return;
        var sr = chest.go.GetComponent<SpriteRenderer>();
        if (sr != null) sr.sprite = sprite;
    }

    // ====================================================================
    // 아이템 상자 열기 → 선택 UI 표시
    // ====================================================================
    private void OpenTreasureChest(TreasureChest chest)
    {
        if (chest == null || chest.opened) return;
        PlaySFX(sfxLootboxOpen);
        AddLog("<color=#FFD700>✦ 아이템 상자를 발견했습니다! 아이템을 선택하세요.</color>");

        // 뚜껑을 열린 스프라이트로 교체 (위치/스케일 자동 재계산)
        ApplyChestTopSprite(chest, chestOpenedSprite ?? chestClosedSprite);

        // 층 기반 랜덤 아이템 3개 생성
        var db = ChestDb;
        List<ChestItemWrapper> choices;
        if (db != null)
            choices = db.GetRandomMixed(currentLevel, 3);
        else
        {
            choices = new List<ChestItemWrapper>(); // 폴백: 빈 리스트
            AddLog("<color=#FF8888>데이터베이스 없음: Tools > 4 실행 필요</color>");
        }

        // 선택 UI 표시 (패널이 열리면 이동 차단됨)
        ShowChestChoiceUI(chest, choices);
    }

    // ====================================================================
    // 아이템 상자 선택 UI
    // ====================================================================
    private void ShowChestChoiceUI(TreasureChest chest, List<ChestItemWrapper> choices)
    {
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        // 기존 패널 제거
        if (_chestChoicePanel != null) Destroy(_chestChoicePanel);

        // ── 전체 오버레이 ─────────────────────────────────────────────────────
        _chestChoicePanel = new GameObject("ChestChoicePanel");
        _chestChoicePanel.transform.SetParent(canvas.transform, false);
        var overlay = _chestChoicePanel.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.72f);
        var overlayRt = _chestChoicePanel.GetComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero; overlayRt.anchorMax = Vector2.one; overlayRt.sizeDelta = Vector2.zero;

        var thm = Theme;

        // ── 메인 박스 ─────────────────────────────────────────────────────────
        var box = new GameObject("ChestBox"); box.transform.SetParent(_chestChoicePanel.transform, false);
        var boxImg = box.AddComponent<Image>();
        if (thm?.panelDark != null) { boxImg.sprite = thm.panelDark; boxImg.type = Image.Type.Sliced; boxImg.color = Color.white; }
        else boxImg.color = new Color(0.10f, 0.07f, 0.02f, 0.98f);
        var boxRt = box.GetComponent<RectTransform>();
        boxRt.anchorMin = new Vector2(0.5f, 0.5f); boxRt.anchorMax = new Vector2(0.5f, 0.5f);
        boxRt.pivot     = new Vector2(0.5f, 0.5f);
        boxRt.anchoredPosition = Vector2.zero;
        boxRt.sizeDelta = new Vector2(960f, 460f);

        // ── 타이틀 ────────────────────────────────────────────────────────────
        var titleTxt = MakeText(box.transform, "ChestTitle",
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(16f, -52f), new Vector2(-16f, -8f),
            32, new Color(1f, 0.88f, 0.38f), TextAnchor.UpperCenter);
        if (thm?.titleFont != null) titleTxt.font = thm.titleFont;
        titleTxt.text = "✦  아이템 상자  ✦\n<size=18><color=#AAAAAA>하나를 선택하세요. 취소할 수 없습니다.</color></size>";

        // ── 닫기(건너뜀) 버튼 ────────────────────────────────────────────────
        var skipGo = new GameObject("SkipBtn"); skipGo.transform.SetParent(box.transform, false);
        var skipImg = skipGo.AddComponent<Image>(); skipImg.color = new Color(0.35f, 0.10f, 0.05f, 0.95f);
        var skipRt = skipGo.GetComponent<RectTransform>();
        skipRt.anchorMin = new Vector2(1f, 1f); skipRt.anchorMax = new Vector2(1f, 1f); skipRt.pivot = new Vector2(1f, 1f);
        skipRt.anchoredPosition = new Vector2(-8f, -8f); skipRt.sizeDelta = new Vector2(38f, 38f);
        var skipBtn = skipGo.AddComponent<Button>();
        // 상자 건너뜀: 상자를 열림 처리하고 UI 닫기
        skipBtn.onClick.AddListener(() =>
        {
            chest.opened = true;
            if (_chestChoicePanel != null) { Destroy(_chestChoicePanel); _chestChoicePanel = null; }
            AddLog("<color=#888888>아이템 상자를 건너뜁니다.</color>");
        });
        MakeText(skipGo.transform, "X", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            18, Color.white, TextAnchor.MiddleCenter).text = "✕";

        // ── 아이템 카드 3개 ───────────────────────────────────────────────────
        float cardW = 270f, cardH = 330f, cardGap = 20f;
        float totalW = choices.Count * cardW + (choices.Count - 1) * cardGap;
        float startX = -totalW * 0.5f + cardW * 0.5f;

        for (int i = 0; i < choices.Count; i++)
        {
            var choice = choices[i];
            int capturedIdx = i;

            // 카드 배경
            var card = new GameObject($"Card_{i}"); card.transform.SetParent(box.transform, false);
            var cardImg = card.AddComponent<Image>();
            // 테두리 색으로 소비형/장비 구분
            // 소비형: 녹황색 테두리, 장비: 파란-보라 테두리
            cardImg.color = choice.isEquipment
                ? new Color(0.18f, 0.10f, 0.35f, 0.97f)
                : new Color(0.08f, 0.25f, 0.08f, 0.97f);
            var cardRt = card.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f); cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot     = new Vector2(0.5f, 0.5f);
            cardRt.anchoredPosition = new Vector2(startX + i * (cardW + cardGap), -20f);
            cardRt.sizeDelta = new Vector2(cardW, cardH);

            // 테두리 강조선 (상단)
            var borderTop = new GameObject("BorderTop"); borderTop.transform.SetParent(card.transform, false);
            var btImg = borderTop.AddComponent<Image>();
            btImg.color = choice.isEquipment ? new Color(0.55f, 0.38f, 1f) : new Color(0.38f, 1f, 0.55f);
            var btRt = borderTop.GetComponent<RectTransform>();
            btRt.anchorMin = new Vector2(0f, 1f); btRt.anchorMax = new Vector2(1f, 1f); btRt.pivot = new Vector2(0.5f, 1f);
            btRt.anchoredPosition = Vector2.zero; btRt.sizeDelta = new Vector2(0f, 4f);

            // 종류 라벨
            var kindGo = new GameObject("KindLabel"); kindGo.transform.SetParent(card.transform, false);
            var kindRt = kindGo.AddComponent<RectTransform>();
            kindRt.anchorMin = new Vector2(0f, 1f); kindRt.anchorMax = new Vector2(1f, 1f); kindRt.pivot = new Vector2(0.5f, 1f);
            kindRt.anchoredPosition = new Vector2(0f, -6f); kindRt.sizeDelta = new Vector2(0f, 26f);
            var kindTxt = kindGo.AddComponent<Text>();
            kindTxt.text = choice.isEquipment ? "[ 장비 ]" : "[ 소비형 ]";
            kindTxt.font = korFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            kindTxt.fontSize = 16;
            kindTxt.color = choice.isEquipment ? new Color(0.78f, 0.62f, 1f) : new Color(0.55f, 1f, 0.65f);
            kindTxt.alignment = TextAnchor.MiddleCenter;
            kindTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            kindTxt.verticalOverflow   = VerticalWrapMode.Overflow;

            // 아이콘
            var iconGo = new GameObject("Icon"); iconGo.transform.SetParent(card.transform, false);
            var iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 1f); iconRt.anchorMax = new Vector2(0.5f, 1f);
            iconRt.pivot     = new Vector2(0.5f, 1f);
            iconRt.anchoredPosition = new Vector2(0f, -36f);
            iconRt.sizeDelta = new Vector2(72f, 72f);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = choice.Icon;
            iconImg.color  = iconImg.sprite != null ? Color.white : (choice.isEquipment ? new Color(0.6f, 0.5f, 1f) : new Color(0.5f, 1f, 0.6f));
            iconImg.preserveAspect = true;

            // 아이템 이름
            var nameTxt = MakeText(card.transform, "Name",
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(8f, -118f), new Vector2(-8f, -86f),
                20, Color.white, TextAnchor.MiddleCenter);
            nameTxt.text = choice.isEquipment ? "???" : choice.DisplayName;
            nameTxt.horizontalOverflow = HorizontalWrapMode.Wrap;

            // 설명 (소비형: 전체, 장비: ???)
            var descTxt = MakeText(card.transform, "Desc",
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(10f, -270f), new Vector2(-10f, -126f),
                16, new Color(0.82f, 0.82f, 0.82f), TextAnchor.UpperCenter);
            descTxt.text = choice.GetDescription(revealed: false);
            descTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            descTxt.verticalOverflow   = VerticalWrapMode.Overflow;

            // 선택 버튼
            var btnGo = new GameObject("SelectBtn"); btnGo.transform.SetParent(card.transform, false);
            var btnImg2 = btnGo.AddComponent<Image>();
            btnImg2.color = choice.isEquipment ? new Color(0.30f, 0.14f, 0.55f) : new Color(0.10f, 0.40f, 0.14f);
            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.1f, 0f); btnRt.anchorMax = new Vector2(0.9f, 0f);
            btnRt.pivot     = new Vector2(0.5f, 0f);
            btnRt.anchoredPosition = new Vector2(0f, 14f);
            btnRt.sizeDelta = new Vector2(0f, 44f);
            var btn = btnGo.AddComponent<Button>();
            var bc  = btn.colors;
            if (choice.isEquipment) { bc.normalColor = new Color(0.30f,0.14f,0.55f); bc.highlightedColor = new Color(0.5f,0.3f,0.8f); bc.pressedColor = new Color(0.18f,0.08f,0.38f); }
            else                    { bc.normalColor = new Color(0.10f,0.40f,0.14f); bc.highlightedColor = new Color(0.2f,0.65f,0.25f); bc.pressedColor = new Color(0.06f,0.25f,0.08f); }
            btn.colors = bc;
            // 클로저 캡처
            var capturedChoice = choice;
            var capturedChest  = chest;
            btn.onClick.AddListener(() => OnChestItemSelected(capturedChest, capturedChoice));
            MakeText(btnGo.transform, "BtnTxt",
                Vector2.zero, Vector2.one, new Vector2(4f, 2f), new Vector2(-4f, -2f),
                18, Color.white, TextAnchor.MiddleCenter).text = "선택";
        }

        // 아이템이 없을 때 안내
        if (choices.Count == 0)
        {
            var emptyTxt = MakeText(box.transform, "EmptyMsg",
                new Vector2(0.1f, 0.2f), new Vector2(0.9f, 0.7f), Vector2.zero, Vector2.zero,
                24, new Color(0.8f, 0.5f, 0.3f), TextAnchor.MiddleCenter);
            emptyTxt.text = "아이템을 찾을 수 없습니다.\n(Tools > 4. Create Treasure Chest Database 실행 필요)";
        }
    }

    // ====================================================================
    // 아이템 선택 처리
    // ====================================================================
    private void OnChestItemSelected(TreasureChest chest, ChestItemWrapper choice)
    {
        chest.opened = true;

        // UI 닫기
        if (_chestChoicePanel != null) { Destroy(_chestChoicePanel); _chestChoicePanel = null; }

        PlaySFX(sfxLootboxOpen);

        if (!choice.isEquipment)
        {
            // ── 소비형 아이템 → _mainInvGrid 에 보관 ──────────────────────────
            var c = choice.consumable;
            bool added = false;
            for (int i = 0; i < _mainInvGrid.Length && !added; i++)
            {
                if (_mainInvGrid[i] == null)
                {
                    _mainInvGrid[i] = c;
                    added = true;
                }
            }
            if (added)
                AddLog($"<color=#88FF88>아이템 획득: <b>{c.displayName}</b> — 인벤토리에 추가됨 (I키로 사용)</color>");
            else
                AddLog("<color=#FF8888>더 이상 아이템을 얻을 수 없습니다. (인벤토리가 가득 찼습니다)</color>");
        }
        else
        {
            // ── 장비 아이템 → 장비 인벤토리에 추가 및 스탯 즉시 적용 ──────────
            var e = choice.equipment;
            ApplyEquipmentDef(e);
            AddLog($"<color=#AAAAFF>장비 획득: <b>{e.displayName}</b>\n[장비 인벤토리에 추가됨]</color>");
        }

        UpdateUI();
    }

    private void ApplyConsumable(ConsumableItemDef c)
    {
        // 골드
        if (c.goldGain > 0)
        {
            playerGold += c.goldGain;
            GameManager.Instance?.History?.RecordGoldObtained(c.goldGain);
        }
        // 레벨업
        if (c.levelUp)
        {
            player.attack += 3;
            player.maxHp  += 15;
            player.hp      = Mathf.Min(player.hp + 15, player.maxHp);
            AddLog("<color=#AAFFFF>레벨 업! 공격력 +3, 최대 HP +15</color>");
        }
        // HP 회복
        if (c.hpHeal > 0)
        {
            int heal = Mathf.Min(c.hpHeal, player.maxHp - player.hp);
            player.hp += heal;
        }
        // 최대 HP
        if (c.maxHpBonus > 0)
        {
            player.maxHp += c.maxHpBonus;
            player.hp     = Mathf.Min(player.hp, player.maxHp);
        }
        // 공격력
        if (c.attackBonus > 0)
            player.attack += c.attackBonus;
        // 방어력 (내부 추적)
        if (c.defenseBonus > 0)
            _equipDef += c.defenseBonus;
        // 방어 확률
        if (c.defChanceBonus > 0f)
            _defChance = Mathf.Clamp01(_defChance + c.defChanceBonus);
        // 상태이상 해제
        if (c.cureEffect != StatusEffectType.None)
        {
            GameManager.Instance?.Player?.RemoveStatusEffect(c.cureEffect);
            AddLog($"<color=#88FFFF>{c.cureEffect} 상태이상이 해제되었습니다!</color>");
        }
        else if (c.id == "cure_all")
        {
            // 만능 해독제: 모든 상태이상 해제
            GameManager.Instance?.Player?.ClearAllStatusEffects();
            AddLog("<color=#88FFFF>모든 상태이상이 해제되었습니다!</color>");
        }

        GameManager.Instance?.History?.RecordItemObtained();
    }

    private void ApplyEquipmentDef(EquipmentItemDef e)
    {
        // FIFO: 가득 찼으면 가장 오래된 장비 제거
        if (_chestEquipList.Count >= EQUIP_INV_MAX)
        {
            var oldest = _chestEquipList[0];
            _chestEquipList.RemoveAt(0);
            AddLog($"<color=#DDAAFF>[{oldest.displayName}] 이(가) 장비 목록에서 밀려났습니다.</color>");
        }

        // 목록에 추가 → 패널에 표시
        _chestEquipList.Add(e);

        // 모든 스탯: 동일 능력치는 최고 버프 + 최저 디버프 기준으로 재계산
        RecalcEquipCombatStats();

        GameManager.Instance?.History?.RecordItemObtained();

        // 장비 패널 갱신 (열려 있을 경우)
        if (_equipmentPanel != null && _equipmentPanel.activeSelf)
            RefreshEquipmentPanel();
    }

    // 장착된 모든 장비의 스탯을 재계산한다.
    // 동일 종류 능력치는 (최고 버프) + (최저 디버프) 만 적용.
    // 예: [+5, +3, -3] → 최고 버프 +5, 최저 디버프 -3 → 합계 +2
    private void RecalcEquipCombatStats()
    {
        // ── 이전 장비 보정 제거 ────────────────────────────────────────────
        player.attack -= _equipAtk;
        player.maxHp  -= _equipMaxHp;
        player.hp      = Mathf.Clamp(player.hp, 1, Mathf.Max(1, player.maxHp));

        // ── 장비 목록 순회: 능력치별 최고 버프 / 최저 디버프 추출 ──────────
        int   bestAtkBuff   = 0, worstAtkDebuff   = 0;
        int   bestDefBuff   = 0, worstDefDebuff   = 0;
        int   bestHpBuff    = 0, worstHpDebuff    = 0;
        float bestDcBuff    = 0, worstDcDebuff    = 0;
        int   bestHealBuff  = 0, worstHealDebuff  = 0;

        foreach (var eq in _chestEquipList)
        {
            if      (eq.attackMod    > 0) bestAtkBuff    = Mathf.Max(bestAtkBuff,    eq.attackMod);
            else if (eq.attackMod    < 0) worstAtkDebuff = Mathf.Min(worstAtkDebuff, eq.attackMod);

            if      (eq.defenseMod   > 0) bestDefBuff    = Mathf.Max(bestDefBuff,    eq.defenseMod);
            else if (eq.defenseMod   < 0) worstDefDebuff = Mathf.Min(worstDefDebuff, eq.defenseMod);

            if      (eq.maxHpMod     > 0) bestHpBuff     = Mathf.Max(bestHpBuff,     eq.maxHpMod);
            else if (eq.maxHpMod     < 0) worstHpDebuff  = Mathf.Min(worstHpDebuff,  eq.maxHpMod);

            if      (eq.defChanceMod > 0) bestDcBuff     = Mathf.Max(bestDcBuff,     eq.defChanceMod);
            else if (eq.defChanceMod < 0) worstDcDebuff  = Mathf.Min(worstDcDebuff,  eq.defChanceMod);

            if      (eq.healMod      > 0) bestHealBuff   = Mathf.Max(bestHealBuff,   eq.healMod);
            else if (eq.healMod      < 0) worstHealDebuff= Mathf.Min(worstHealDebuff, eq.healMod);
        }

        // ── 재계산 결과 저장 및 적용 ────────────────────────────────────────
        _equipAtk   = bestAtkBuff  + worstAtkDebuff;
        _equipDef   = bestDefBuff  + worstDefDebuff;
        _equipMaxHp = bestHpBuff   + worstHpDebuff;
        _defChance  = Mathf.Clamp01(bestDcBuff + worstDcDebuff);
        _equipHeal  = bestHealBuff + worstHealDebuff;

        player.attack  = Mathf.Max(0, player.attack + _equipAtk);
        player.maxHp  += _equipMaxHp;
        player.hp      = Mathf.Clamp(player.hp, 1, player.maxHp);
    }

#if UNITY_EDITOR
    // ====================================================================
    // 에디터 전용: 적 30종 초기값 설정
    // Inspector 기어 아이콘 (⋮) > "적 30종 초기값 설정" 실행
    // ====================================================================
    [ContextMenu("적 30종 초기값 설정 (일반 20 + 보스 10)")]
    private void InitDefaultEnemySlots()
    {
        const string B = "Assets/Brackeys/2D Mega Pack/";
        Sprite s1 = LoadFirstSub(B + "Enemies/Gothic/GothicEnemy01.png");
        Sprite s2 = LoadFirstSub(B + "Enemies/Gothic/GothicEnemy02.png");
        Sprite s3 = LoadFirstSub(B + "Enemies/Gothic/FireheadEnemy.png");

        UnityEditor.Undo.RecordObject(this, "Init Enemy Slots");
        enemySlots = new EnemySlot[]
        {
            // ─ 일반 몬스터 20종 ────────────────────────────────────────────
            S("슬라임",        "끌적한 점액으로 공격하는 기초 던전 몬스터",   false, s2, new Color(0.4f,0.9f,0.4f), 0.6f, 0.5f, 0.5f),
            S("고블린",        "날카로운 단검을 휘두르는 작은 녹색 도적", false, s1, new Color(0.7f,1.0f,0.3f), 0.8f, 0.7f, 0.7f),
            S("해골 전사",     "고대 마법으로 깨어난 언데드 해골 전사",  false, s2, new Color(0.9f,0.9f,0.9f), 0.9f, 0.8f, 0.8f),
            S("동굴 박쥐",     "어둠 속을 날며 혼란을 주는 흡혁 박쥐",  false, s1, new Color(0.5f,0.3f,0.6f), 0.5f, 0.6f, 0.6f),
            S("오크",          "거대한 몸집과 완력을 자랑하는 녹색 전사",  false, s2, new Color(0.4f,0.8f,0.4f), 1.2f, 1.1f, 1.0f),
            S("마법 고블린",   "간단한 주문을 구사하는 지능적인 고블린", false, s1, new Color(0.8f,0.4f,1.0f), 0.7f, 1.0f, 0.9f),
            S("독거지",        "독이 있는 실크로 먹이를 포박하는 거설 거지",  false, s3, new Color(0.5f,0.7f,0.2f), 0.8f, 0.9f, 0.8f),
            S("좀비",          "느리지만 끝질기게 첨아오는 썾은 언데드",   false, s2, new Color(0.5f,0.7f,0.4f), 1.1f, 0.7f, 0.7f),
            S("스켈레톤 궁수", "뉴로 된 활을 싸는 원거리 언데드 사수",   false, s1, new Color(0.8f,0.8f,0.7f), 0.7f, 1.0f, 0.9f),
            S("오크 전사",     "전투 훈련을 받은 베테랑 오크 병사",      false, s2, new Color(0.2f,0.6f,0.2f), 1.3f, 1.2f, 1.1f),
            S("화염 슬라임",   "몸 전체에서 불꽃을 내뛰는 위험한 슬라임",  false, s3, new Color(1.0f,0.5f,0.1f), 0.8f, 1.1f, 1.0f),
            S("독 거인",       "독성 안개를 뒤에서 접근하는 거대 생명체",  false, s1, new Color(0.3f,0.8f,0.1f), 1.4f, 1.0f, 1.1f),
            S("냉기 골렇",    "얼음 결정으로 이루어진 감각 없는 거인",     false, s2, new Color(0.5f,0.8f,1.0f), 1.5f, 1.1f, 1.2f),
            S("범파이어",      "피에 굴주린 어둠의 불멸 존재",           false, s3, new Color(0.8f,0.0f,0.2f), 1.0f, 1.3f, 1.3f),
            S("반란군 기사",   "왕국을 배신하고 던전에 숨어든 저주받은 기사", false, s1, new Color(0.5f,0.6f,0.8f), 1.3f, 1.3f, 1.3f),
            S("악마 전사",     "지옥에서 소환된 붉은 갑옵의 전사",      false, s2, new Color(1.0f,0.2f,0.1f), 1.4f, 1.4f, 1.4f),
            S("마녀",          "저주와 독 마법을 구사하는 사악한 마법사",   false, s3, new Color(0.7f,0.2f,0.9f), 1.0f, 1.5f, 1.5f),
            S("어둠 기사",     "어두운 힘으로 강화된 죽음의 기사",      false, s1, new Color(0.2f,0.1f,0.3f), 1.6f, 1.5f, 1.5f),
            S("고대 골렁",    "고대 마법 유적으로 만들어진 돌 거인",      false, s2, new Color(0.6f,0.5f,0.4f), 2.0f, 1.4f, 1.6f),
            S("카오스 오크",   "광기에 사로잡혀 모든 것을 파괴하는 오크",    false, s3, new Color(0.9f,0.3f,0.1f), 1.8f, 1.8f, 1.8f),
            // ─ 보스 몬스터 10종 (3층마다 출현) ─────────────────────────────────
            S("고블린 왕",     "수백의 고블린을 지배하는 잔혹한 부족 족장",  true,  s1, new Color(0.6f,1.0f,0.2f), 3.0f, 2.0f, 3.0f),
            S("해골 군주",     "언데드 군단을 이끊는 죽음의 선봉",      true,  s2, new Color(0.8f,1.0f,0.8f), 3.5f, 2.5f, 4.0f),
            S("독거지 여왕",  "치명적 독을 뒤곳걳 슬 군단의 무자비한 여왕", true,  s3, new Color(0.5f,0.9f,0.0f), 4.0f, 2.5f, 5.0f),
            S("오크 대족장",  "수천의 오크를 통솔하는 전설의 지도자",     true,  s1, new Color(0.1f,0.7f,0.1f), 4.5f, 3.0f, 6.0f),
            S("화염 군주",     "불꽃을 자유로이 다루는 불의 절대 지배자",   true,  s3, new Color(1.0f,0.4f,0.0f), 5.0f, 3.5f, 7.0f),
            S("저주 마녀왕",  "고대 저주 마법으로 던전 전체를 통제하는 자",   true,  s2, new Color(0.8f,0.0f,0.9f), 5.5f, 3.5f, 8.0f),
            S("어둠의 용",     "암흐 속에서 탄생한 전설의 고대 용",      true,  s3, new Color(0.1f,0.0f,0.4f), 6.0f, 4.0f,10.0f),
            S("악마 대공",     "지옥의 여섯 왕자 중 한 명인 절대 악마",  true,  s1, new Color(0.8f,0.0f,0.1f), 6.5f, 4.5f,12.0f),
            S("고대 드리치",  "불멸을 갈망하다 타락한 최강위 언데드 마법사", true,  s2, new Color(0.5f,0.0f,0.8f), 7.0f, 5.0f,15.0f),
            S("던전 군주",     "이 던전 전체를 지배하는 절대 악의 화신",   true,  s3, new Color(1.0f,0.8f,0.0f), 8.0f, 6.0f,20.0f),
        };
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log("[Setup] 적 30종 초기 슬롯 설정 완료 (일반 20종 + 보스 10종)");
    }

    private static EnemySlot S(string name, string desc, bool boss, Sprite spr, Color col,
                               float hp, float atk, float exp)
        => new EnemySlot { name=name, description=desc, isBoss=boss, spriteRight=spr, color=col,
                           hpScale=hp, atkScale=atk, expScale=exp };

    private static Sprite LoadFirstSub(string path)
    {
        foreach (var a in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path))
            if (a is Sprite s) return s;
        return null;
    }
#endif
}

