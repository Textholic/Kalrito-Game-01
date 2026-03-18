using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class GameSceneManager : MonoBehaviour
{
    // ── 맵 설정 ──────────────────────────────────────────────────────────────
    private const int   MapWidth  = 40;
    private const int   MapHeight = 25;
    private const float TileSize  = 1.0f;

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
    [Header("Player Animation Sprites")]
    [Tooltip("IDLE 4프레임 (아래 방향 대기)")]
    [SerializeField] private Sprite[] playerIdleSprites;
    [Tooltip("WALK DOWN 4프레임 (없으면 Idle 사용)")]
    [SerializeField] private Sprite[] playerWalkDownSprites;
    [Tooltip("WALK UP 4프레임")]
    [SerializeField] private Sprite[] playerWalkUpSprites;
    [Tooltip("WALK LEFT 4프레임")]
    [SerializeField] private Sprite[] playerWalkLeftSprites;
    [Tooltip("WALK RIGHT 4프레임")]
    [SerializeField] private Sprite[] playerWalkRightSprites;
    [Tooltip("DOWNED 4프레임 — 사망 시에만 재생")]
    [SerializeField] private Sprite[] playerDownedSprites;
    // 몬스터 3종
    [SerializeField] private Sprite enemy1Sprite;  // GothicEnemy01
    [SerializeField] private Sprite enemy2Sprite;  // GothicEnemy02
    [SerializeField] private Sprite enemy3Sprite;  // FireheadEnemy

    [Header("Item Sprites")]
    [SerializeField] private Sprite potionSprite;  // Potion.png
    [SerializeField] private Sprite goldSmSprite;  // Coin.png  (소형 골드)
    [SerializeField] private Sprite goldLgSprite;  // Diamond.png (대형 골드)

    [Header("Treasure Chest Sprites")]
    [Tooltip("아이템 상자 스프라이트 (닫힌 상태)")]
    [SerializeField] private Sprite chestClosedSprite;
    [Tooltip("아이템 상자 스프라이트 (열린 상태)")]
    [SerializeField] private Sprite chestOpenedSprite;

    [Header("Background")]
    [SerializeField] private Sprite bgSprite;

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
    [SerializeField] private AudioClip bgmCombat;    // Click.wav → 배경음으로 루프
    
    private AudioSource bgmSource;
    private AudioSource sfxSource;

    // ── 엔티티 ─────────────────────────────────────────────────────────────
    public enum MonsterType { Goblin, Orc, FireDemon }
    private enum PlayerFacing { Down, Up, Left, Right }

    [System.Serializable]
    public class Entity
    {
        public string      name;
        public Vector2Int  pos;
        public int         hp, maxHp, attack, exp;
        public MonsterType type;
        public GameObject  go;
        public bool        isAggro; // 한 번 발견하면 시야 밖에서도 추적

        // 플레이어용
        public Entity(string name, int hp, int attack)
        { this.name=name; this.hp=hp; this.maxHp=hp; this.attack=attack; }

        // 몬스터용
        public Entity(string name, int hp, int attack, int exp, MonsterType type)
        { this.name=name; this.hp=hp; this.maxHp=hp; this.attack=attack; this.exp=exp; this.type=type; }
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

    // ── 아이템 상자 시스템 ─────────────────────────────────────────────────
    public class TreasureChest
    {
        public Vector2Int pos;
        public GameObject go;
        public bool       opened = false;
    }

    private List<TreasureChest> _chests    = new List<TreasureChest>();
    private TreasureChestDatabase _chestDb;
    private TreasureChestDatabase ChestDb => _chestDb != null ? _chestDb
        : (_chestDb = Resources.Load<TreasureChestDatabase>("TreasureChestDatabase"));

    // 아이템 상자 선택 UI 패널
    private GameObject _chestChoicePanel;

    // 플레이어 스탯 (GameSceneManager 내부 추적용) — 장비 누적
    private int _equipAtk = 0;
    private int _equipDef = 0;
    private int _equipMaxHp = 0;
    private int _equipHeal  = 0;
    // 보물상자에서 획득한 장비 목록 (층 이동 후에도 유지) — FIFO 최대 8개
    private readonly List<EquipmentItemDef>  _chestEquipList    = new List<EquipmentItemDef>();
    private const int EQUIP_INV_MAX = 8;

    // ── 메인 아이템 인벤토리 (2D 그리드, 아이템은 이미지로 표시) ─────────────
    // 인덱스: col + row * _mainInvCols
    private ConsumableItemDef[] _mainInvGrid = new ConsumableItemDef[15]; // 초기 5×3
    private int _mainInvCols = 5;
    private int _mainInvRows = 3;

    // ── 플레이어 레벨 (인벤토리 확장 기준) ───────────────────────────────────
    private int _playerLevel = 1;

    // ── UIKit 테마 ─────────────────────────────────────────────────────────
    private UIKitTheme _theme;
    private UIKitTheme Theme => _theme != null ? _theme : (_theme = Resources.Load<UIKitTheme>("UIKitTheme"));

    // ── 던전 비주얼 테마 (3층마다 변경) ────────────────────────────────────────
    private DungeonThemeConfig _dungeonThemeCfg;
    private DungeonThemeConfig DungeonThemeCfg => _dungeonThemeCfg != null ? _dungeonThemeCfg
        : (_dungeonThemeCfg = Resources.Load<DungeonThemeConfig>("DungeonThemeConfig"));
    private FloorTheme CurrentTheme => DungeonThemeCfg?.GetThemeForFloor(currentLevel);

    private bool          isProcessingTurn = false;
    private Queue<string> gameLogs = new Queue<string>();

    // ── 사망 통계 추적 ──────────────────────────────────────────────────────
    private int killsGoblin     = 0;
    private int killsOrc        = 0;
    private int killsFireDemon  = 0;
    private int potionsPickedUp = 0;
    private int potionInventory = 0;  // 소지 물약 수 (소비 전)

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
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_inventoryPanel != null && _inventoryPanel.activeSelf) ToggleInventory();
            else if (_equipmentPanel != null && _equipmentPanel.activeSelf) ToggleEquipment();
        }
        if (isProcessingTurn) return;
        HandleInput();
        FollowCamera();
    }

    // ====================================================================
    // 스프라이트 보장
    // ====================================================================
    private void EnsureSprites()
    {
        if (floorSprite  == null) floorSprite  = MakeSolid(new Color(0.30f,0.26f,0.18f));
        if (wallSprite   == null) wallSprite   = MakeSolid(new Color(0.12f,0.10f,0.08f));
        if (stairsSprite == null) stairsSprite = MakeSolid(new Color(0.8f, 0.3f, 1.0f));
        if (playerSprite == null) playerSprite = MakeSolid(Color.yellow);
        if (enemy1Sprite == null) enemy1Sprite = MakeSolid(new Color(1f,0.4f,0.4f));
        if (enemy2Sprite == null) enemy2Sprite = MakeSolid(new Color(1f,0.3f,0.2f));
        if (enemy3Sprite == null) enemy3Sprite = MakeSolid(new Color(1f,0.1f,0.0f));
        if (potionSprite == null) potionSprite = MakeSolid(new Color(0.4f,1f,0.4f));
        if (goldSmSprite == null) goldSmSprite = MakeSolid(new Color(1f,0.9f,0.2f));
        if (goldLgSprite == null) goldLgSprite = MakeSolid(new Color(1f,0.7f,0.0f));
        if (chestClosedSprite == null) chestClosedSprite = MakeSolid(new Color(0.65f, 0.42f, 0.12f));
        if (chestOpenedSprite == null) chestOpenedSprite = MakeSolid(new Color(0.35f, 0.22f, 0.06f));
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
        mainCam.orthographicSize = 8f;
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

        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>()==null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

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
        if (t == null) return;

        // 타일 스프라이트 교체 (폴백은 EnsureSprites가 보장한 _orig* 스프라이트)
        floorSprite  = t.floorSprite  != null ? t.floorSprite  : _origFloorSprite;
        wallSprite   = t.wallSprite   != null ? t.wallSprite   : _origWallSprite;
        stairsSprite = t.stairsSprite != null ? t.stairsSprite : _origStairsSprite;

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
        SpawnEntities();
        SpawnItems();
        SpawnTreasureChests();  // ← 아이템 상자 스폰
        UpdateVisibility();
        UpdateUI();
        var tname = CurrentTheme?.themeName;
        string logEntry = $"<color=#88FF88>── {currentLevel}층 진입 ──</color>"
            + (string.IsNullOrEmpty(tname) ? "" : $"  <color=#AADDFF>[{tname}]</color>");
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
    }

    // ====================================================================
    // 던전 생성
    // ====================================================================
    private void GenerateDungeon()
    {
        map=new char[MapWidth,MapHeight];
        explored=new bool[MapWidth,MapHeight];
        visible=new bool[MapWidth,MapHeight];
        rooms.Clear();
        for (int x=0;x<MapWidth;x++) for (int y=0;y<MapHeight;y++) map[x,y]='#';

        int attempts=0;
        while (rooms.Count<12 && attempts<300)
        {
            attempts++;
            int w=Random.Range(5,10),h=Random.Range(4,8);
            int x=Random.Range(1,MapWidth-w-1),y=Random.Range(1,MapHeight-h-1);
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
        stairsPos=RoomCenter(rooms[rooms.Count-1]);
        map[stairsPos.x,stairsPos.y]='>';
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
                Color col=(c=='#')?new Color(0.32f,0.27f,0.22f):new Color(0.60f,0.52f,0.40f);
                tileObjects[x,y]=MakeTile($"T_{x}_{y}",x,y,spr,col,0);
            }
        }
        // 계단: Pentagram_Activated (보라색 마법진 느낌)
        stairsObject=MakeTile("Stairs",stairsPos.x,stairsPos.y,
            stairsSprite,new Color(0.9f,0.5f,1.0f),2);

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
        var refSprite = (playerIdleSprites!=null&&playerIdleSprites.Length>0) ? playerIdleSprites[0] : playerSprite;
        if (refSprite!=null)
        {
            float ps=TileSize/Mathf.Max(refSprite.bounds.size.x,refSprite.bounds.size.y);
            playerObject.transform.localScale=new Vector3(ps,ps,1f);
        }
        playerObject.SetActive(true);

        // 몬스터 종류별 파라미터
        var monsterDefs = new (string name, int hp, int atk, int exp, MonsterType type, Sprite spr, Color col)[]
        {
            ("고블린",  15+currentLevel*8,  3+currentLevel,   5+currentLevel,   MonsterType.Goblin,   enemy1Sprite, new Color(0.5f,1.0f,0.5f)),
            ("오크",    30+currentLevel*15, 6+currentLevel*2, 12+currentLevel*2, MonsterType.Orc,      enemy2Sprite, new Color(1.0f,0.6f,0.2f)),
            ("화염마",  20+currentLevel*12, 8+currentLevel*2, 20+currentLevel*3, MonsterType.FireDemon,enemy3Sprite, new Color(1.0f,0.3f,0.1f)),
        };

        int totalEnemies = 4 + currentLevel * 2;
        for (int i=0;i<totalEnemies;i++)
        {
            // 층이 낮을수록 고블린 위주, 높을수록 다양
            int typeIdx;
            float roll = Random.value;
            if      (roll < 0.50f) typeIdx = 0; // 고블린 50%
            else if (roll < 0.80f) typeIdx = 1; // 오크   30%
            else                   typeIdx = 2; // 화염마 20%

            var def = monsterDefs[typeIdx];
            var e   = new Entity(def.name,def.hp,def.atk,def.exp,def.type);
            e.pos   = FindEmptyFloor(6); // 플레이어로부터 6칸 이상 떨어진 빈 칸
            if (e.pos.x < 0) continue;

            var eGo = new GameObject($"Enemy_{i}_{def.name}");
            eGo.transform.position=TileWorldPos(e.pos.x,e.pos.y);
            var esr=eGo.AddComponent<SpriteRenderer>();
            esr.sprite=def.spr; esr.color=def.col; esr.sortingOrder=EntitySortOrder(e.pos.y);
            // 스프라이트를 타일 크기에 맞게 정규화 (시각적 겹침 방지)
            if (def.spr!=null)
            {
                float es=TileSize/Mathf.Max(def.spr.bounds.size.x,def.spr.bounds.size.y);
                eGo.transform.localScale=new Vector3(es,es,1f);
            }
            eGo.SetActive(false);
            e.go=eGo; enemies.Add(e); enemyObjects.Add(eGo);
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
                ItemType itype; Sprite spr; int val; Color col;
                if (roll<0.40f)
                {
                    itype=ItemType.Potion;   spr=potionSprite; val=25+currentLevel*5;
                    col=new Color(0.3f,1f,0.4f);
                }
                else if (roll<0.75f)
                {
                    itype=ItemType.GoldSmall; spr=goldSmSprite; val=5+currentLevel*3;
                    col=new Color(1f,0.95f,0.3f);
                }
                else
                {
                    itype=ItemType.GoldLarge; spr=goldLgSprite; val=15+currentLevel*8;
                    col=new Color(0.95f,0.7f,0.1f);
                }

                var item=new Item{type=itype,pos=pos,value=val};
                var iGo=MakeTile($"Item_{ri}_{j}",pos.x,pos.y,spr,col,3);
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
        bool panelOpen = (_inventoryPanel != null && _inventoryPanel.activeSelf)
                      || (_equipmentPanel != null && _equipmentPanel.activeSelf)
                      || (_chestChoicePanel != null && _chestChoicePanel.activeSelf);
        if (panelOpen) return;

        // 물약 사용 (1 키)
        if (Input.GetKeyDown(KeyCode.Alpha1)) { UsePotion(); return; }

        Vector2Int move=Vector2Int.zero;
        if      (Input.GetKeyDown(KeyCode.W)||Input.GetKeyDown(KeyCode.UpArrow))    move=Vector2Int.up;
        else if (Input.GetKeyDown(KeyCode.S)||Input.GetKeyDown(KeyCode.DownArrow))  move=Vector2Int.down;
        else if (Input.GetKeyDown(KeyCode.A)||Input.GetKeyDown(KeyCode.LeftArrow))  move=Vector2Int.left;
        else if (Input.GetKeyDown(KeyCode.D)||Input.GetKeyDown(KeyCode.RightArrow)) move=Vector2Int.right;

        if (move!=Vector2Int.zero&&ProcessPlayerTurn(move))
        {
            TriggerPlayerWalkAnim(move);
            isProcessingTurn=true;
            ProcessEnemyTurn();
            UpdateVisibility();
            UpdateUI();
            if (player.hp > 0) isProcessingTurn=false;
        }
    }

    // ====================================================================
    // 플레이어 턴
    // ====================================================================
    private bool ProcessPlayerTurn(Vector2Int move)
    {
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
            int dmg=Mathf.Max(1,player.attack+Random.Range(-3,4));
            target.hp-=dmg;
            PlaySFX(sfxHit, Random.Range(0.9f,1.1f));
            AddLog($"<color=#FFCC44>{target.name}에게 <b>{dmg}</b> 데미지!</color>");
            if (target.hp<=0)
            {
                playerExp+=target.exp;
                AddLog($"<color=#88FF88>{target.name} 처치! +{target.exp} EXP</color>");
                PlaySFX(sfxEnemyHit);
                // 처치 통계 기록
                if      (target.type==MonsterType.Goblin)   killsGoblin++;
                else if (target.type==MonsterType.Orc)      killsOrc++;
                else                                        killsFireDemon++;
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

            // 아이템 획득
            var it=GetItemAt(np.x,np.y);
            if (it!=null) PickupItem(it);

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
        player.attack += 2 * gained;
        player.maxHp  += 10 * gained;
        player.hp     = Mathf.Min(player.hp + 10 * gained, player.maxHp);
        AddLog($"<color=#AAFFFF>레벨 업! (Lv.{_playerLevel}) 공격력 +{2*gained}, 최대 HP +{10*gained}</color>");

        // 5 레벨마다 인벤토리 확장 (+1열 +1행)
        if (_playerLevel % 5 == 0)
        {
            int oldCols = _mainInvCols;
            int oldRows = _mainInvRows;
            _mainInvCols++;
            _mainInvRows++;
            var oldGrid = _mainInvGrid;
            _mainInvGrid = new ConsumableItemDef[_mainInvCols * _mainInvRows];
            for (int r = 0; r < oldRows; r++)
                for (int c = 0; c < oldCols; c++)
                    _mainInvGrid[c + r * _mainInvCols] = oldGrid[c + r * oldCols];
            // 패널 재빌드
            if (_inventoryPanel != null) { Destroy(_inventoryPanel); _inventoryPanel = null; }
            AddLog($"<color=#FFD700>인벤토리 확장! {_mainInvCols}×{_mainInvRows}</color>");
        }
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

            var diff=player.pos-e.pos;
            var mv=Vector2Int.zero;
            if (Mathf.Abs(diff.x)>=Mathf.Abs(diff.y)) mv.x=(int)Mathf.Sign(diff.x);
            else                                        mv.y=(int)Mathf.Sign(diff.y);

            var np=e.pos+mv;
            if (np==player.pos)
            {
                int dmg=Mathf.Max(1,e.attack+Random.Range(-2,3)-_equipDef);
                // 화염마는 추가 데미지
                if (e.type==MonsterType.FireDemon) dmg=Mathf.RoundToInt(dmg*1.3f);
                player.hp-=dmg;
                PlaySFX(sfxHit, 0.7f);
                string defInfo = _equipDef > 0 ? $" (방어 -{_equipDef})" : "";
                AddLog($"<color=#FF6666>{e.name}에게 <b>{dmg}</b> 데미지를 받았다{defInfo}!</color>");
            }
            else if (GetT(np.x,np.y)!='#'&&!IsEntityAt(np))
            {
                e.pos=np;
                if (e.go!=null)
                {
                    e.go.transform.position=TileWorldPos(e.pos.x,e.pos.y);
                    e.go.GetComponent<SpriteRenderer>().sortingOrder = EntitySortOrder(e.pos.y);
                }
            }
        }
        if (player.hp<=0)
        {
            ShowDeathPanel();
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

        // 게임 이력 저장
        GameManager.Instance?.History?.RecordDeath();

        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas==null) { SceneManager.LoadScene("GameOptionsScene"); return; }

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
        var downedFrames = GetPlayerDownedFrames();
        if (downedFrames != null && downedFrames.Length > 0 && downedFrames[0] != null)
            deathAnimImg.sprite = downedFrames[0];
        else if (playerSprite != null)
            deathAnimImg.sprite = playerSprite;
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
        int totalKills = killsGoblin+killsOrc+killsFireDemon;
        string statsText =
            $"<color=#FFD966>도달한 층    :  {currentLevel}층</color>\n\n" +
            $"<color=#FF9090>쓰러뜨린 적  :  {totalKills}마리</color>\n" +
            $"    고블린  {killsGoblin}마리\n" +
            $"    오크    {killsOrc}마리\n" +
            $"    화염마  {killsFireDemon}마리\n\n" +
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
        btn.onClick.AddListener(()=>SceneManager.LoadScene("GameOptionsScene"));
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
                    sr.color = map[x,y]=='#'
                        ? (visTheme != null ? visTheme.wallVisibleColor  : new Color(0.55f,0.46f,0.36f))
                        : (visTheme != null ? visTheme.floorVisibleColor : new Color(0.80f,0.70f,0.55f));
                }
                else
                {
                    tileObjects[x,y].SetActive(true);
                    sr.color = map[x,y]=='#'
                        ? (visTheme != null ? visTheme.wallDimColor  : new Color(0.18f,0.15f,0.12f))
                        : (visTheme != null ? visTheme.floorDimColor : new Color(0.30f,0.26f,0.20f));
                }
            }
        }

        // 계단
        bool sv=explored[stairsPos.x,stairsPos.y];
        stairsObject.SetActive(sv);
        if (sv)
        {
            var ssr=stairsObject.GetComponent<SpriteRenderer>();
            Color stairsVis = visTheme != null ? visTheme.stairsColor : new Color(0.9f,0.5f,1.0f);
            Color stairsDim = visTheme != null ? Color.Lerp(visTheme.stairsColor, Color.black, 0.55f) : new Color(0.4f,0.2f,0.5f);
            ssr.color = IsVis(stairsPos.x,stairsPos.y) ? stairsVis : stairsDim;
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
            statusDisplay.text=$"<color=#FFD966>{currentLevel}F</color>  ATK {player.attack}" +
                                $"  EXP {playerExp}  적 {enemies.Count}";
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
    // 플레이어 애니메이션
    // ====================================================================
    private Sprite[] GetPlayerFrames(PlayerFacing facing)
    {
        Sprite[] arr = facing == PlayerFacing.Up    ? playerWalkUpSprites    :
                       facing == PlayerFacing.Left  ? playerWalkLeftSprites  :
                       facing == PlayerFacing.Right ? playerWalkRightSprites :
                       (playerWalkDownSprites != null && playerWalkDownSprites.Length > 0
                           ? playerWalkDownSprites : playerIdleSprites);
        return (arr != null && arr.Length > 0) ? arr : null;
    }

    private Sprite[] GetPlayerDownedFrames()
    {
        return (playerDownedSprites != null && playerDownedSprites.Length > 0)
            ? playerDownedSprites : null;
    }

    private void SetPlayerIdleSprite()
    {
        if (playerObject == null) return;
        var sr = playerObject.GetComponent<SpriteRenderer>();
        if (sr == null) return;
        var frames = GetPlayerFrames(playerFacing);
        sr.sprite = (frames != null && frames[0] != null) ? frames[0] : playerSprite;
    }

    private void TriggerPlayerWalkAnim(Vector2Int move)
    {
        // TileWorldPos y*TileSize 구조: tileY 켜지면 화면 위로 이동
        // 하지만 스프라이트 시트 배치 기준으로 Up/Down 스왕
        PlayerFacing facing = move.y > 0 ? PlayerFacing.Up   :
                              move.y < 0 ? PlayerFacing.Down :
                              move.x < 0 ? PlayerFacing.Left : PlayerFacing.Right;
        playerFacing = facing;
        if (playerAnimCoroutine != null) StopCoroutine(playerAnimCoroutine);
        playerAnimCoroutine = StartCoroutine(PlayPlayerWalkAnim(facing));
    }

    private IEnumerator PlayPlayerWalkAnim(PlayerFacing facing, float frameTime = 0.08f)
    {
        var frames = GetPlayerFrames(facing);
        if (frames == null) yield break;
        var sr = playerObject.GetComponent<SpriteRenderer>();
        int count = Mathf.Min(4, frames.Length); // 배열이 4개 이상이라도 반드시 4프레임만 재생
        for (int i = 0; i < count; i++)
        {
            if (frames[i] != null) sr.sprite = frames[i];
            yield return new WaitForSeconds(frameTime);
        }
        if (frames[0] != null) sr.sprite = frames[0];
    }

    private IEnumerator PlayPlayerDownedAnim(float frameTime = 0.12f)
    {
        var frames = GetPlayerDownedFrames();
        if (frames == null) { SetPlayerIdleSprite(); yield break; }
        var sr = playerObject.GetComponent<SpriteRenderer>();
        int count = Mathf.Min(4, frames.Length); // 반드시 4프레임만 재생
        for (int i = 0; i < count; i++)
        {
            if (frames[i] != null) sr.sprite = frames[i];
            yield return new WaitForSeconds(frameTime);
        }
    }

    // 사망 패널 상단 UI Image에 쓰러지는 애니메이션 루프 재생
    private IEnumerator PlayDeathAnimOnUI(Image img, float frameTime = 0.14f)
    {
        var frames = GetPlayerDownedFrames();
        if (frames == null || frames.Length == 0) yield break;
        int count = Mathf.Min(4, frames.Length);
        while (img != null)
        {
            for (int i = 0; i < count; i++)
            {
                if (img == null) yield break;
                if (frames[i] != null) img.sprite = frames[i];
                yield return new WaitForSeconds(frameTime);
            }
        }
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
        titleRt.pivot = new Vector2(0.5f, 1f); titleRt.anchoredPosition = new Vector2(0f, -8f); titleRt.sizeDelta = new Vector2(0f, 38f);
        var titleTxt = titleGo.AddComponent<Text>();
        titleTxt.text = "인벤토리";
        titleTxt.font = korFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleTxt.fontSize = 22; titleTxt.color = new Color(1f, 0.92f, 0.65f);
        titleTxt.alignment = TextAnchor.MiddleCenter;
        titleTxt.horizontalOverflow = HorizontalWrapMode.Overflow; titleTxt.verticalOverflow = VerticalWrapMode.Overflow;

        // 조작 힌트 (제목 아래)
        var hintGo = new GameObject("Hint"); hintGo.transform.SetParent(box.transform, false);
        var hintRt = hintGo.AddComponent<RectTransform>();
        hintRt.anchorMin = new Vector2(0f, 1f); hintRt.anchorMax = new Vector2(1f, 1f);
        hintRt.pivot = new Vector2(0.5f, 1f); hintRt.anchoredPosition = new Vector2(0f, -48f); hintRt.sizeDelta = new Vector2(0f, 18f);
        var hintTxt = hintGo.AddComponent<Text>();
        hintTxt.text = "좌클릭: 사용 │ 우클릭: 버리기 │ 드래그: 이동 │ [I] / [ESC]: 닫기";
        hintTxt.font = korFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hintTxt.fontSize = 11; hintTxt.color = new Color(0.58f, 0.52f, 0.38f);
        hintTxt.alignment = TextAnchor.MiddleCenter;
        hintTxt.horizontalOverflow = HorizontalWrapMode.Overflow; hintTxt.verticalOverflow = VerticalWrapMode.Overflow;

        // X 닫기 버튼
        var closeBtnGo = new GameObject("InvenCloseBtn"); closeBtnGo.transform.SetParent(box.transform, false);
        var closeBtnImg = closeBtnGo.AddComponent<Image>(); closeBtnImg.color = new Color(0.65f, 0.10f, 0.10f, 0.95f);
        var closeBtnRt = closeBtnGo.GetComponent<RectTransform>();
        closeBtnRt.anchorMin = new Vector2(1f, 1f); closeBtnRt.anchorMax = new Vector2(1f, 1f); closeBtnRt.pivot = new Vector2(1f, 1f);
        closeBtnRt.anchoredPosition = new Vector2(-8f, -6f); closeBtnRt.sizeDelta = new Vector2(36f, 36f);
        var closeBtn = closeBtnGo.AddComponent<Button>();
        closeBtn.onClick.AddListener(() => { _inventoryPanel.SetActive(false); ItemSlotUI.HideTooltip(); });
        var cbX = closeBtn.colors; cbX.normalColor = Color.white; cbX.highlightedColor = new Color(1f, 0.6f, 0.6f, 1f); cbX.pressedColor = new Color(0.7f, 0.2f, 0.2f, 1f); closeBtn.colors = cbX;
        MakeText(closeBtnGo.transform, "X", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 18, Color.white, TextAnchor.MiddleCenter).text = "✕";

        // InventorySlots 컨테이너 (위치와 크기는 RefreshInventoryPanel에서 결정)
        var slotsGo = new GameObject("InventorySlots"); slotsGo.transform.SetParent(box.transform, false);
        var slotsRt = slotsGo.AddComponent<RectTransform>();
        slotsRt.anchorMin = new Vector2(0.5f, 1f); slotsRt.anchorMax = new Vector2(0.5f, 1f);
        slotsRt.pivot = new Vector2(0.5f, 1f); slotsRt.anchoredPosition = new Vector2(0f, -70f); slotsRt.sizeDelta = new Vector2(400f, 240f);
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
        const float slotW  = 72f;
        const float slotH  = 72f;
        const float gapX   = 8f;
        const float gapY   = 8f;
        float gridW = cols * slotW + (cols - 1) * gapX;
        float gridH = rows * slotH + (rows - 1) * gapY;

        // ── InvenBox 및 InventorySlots 컨테이너 크기 갱신 ─────────────────────
        var boxRt    = _inventoryPanel.transform.Find("InvenBox")?.GetComponent<RectTransform>();
        var slotsRt  = slotsRoot.GetComponent<RectTransform>();
        if (boxRt   != null) boxRt.sizeDelta   = new Vector2(gridW + 60f, gridH + 100f);
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
        titleRt.pivot = new Vector2(0.5f, 1f); titleRt.anchoredPosition = new Vector2(0f, -8f); titleRt.sizeDelta = new Vector2(0f, 38f);
        var titleTxt = titleGo.AddComponent<Text>();
        titleTxt.text = "장비 인벤토리  [E / ESC 닫기]";
        titleTxt.font = korFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleTxt.fontSize = 22; titleTxt.color = new Color(0.85f, 0.76f, 1f);
        titleTxt.alignment = TextAnchor.MiddleCenter;
        titleTxt.horizontalOverflow = HorizontalWrapMode.Overflow; titleTxt.verticalOverflow = VerticalWrapMode.Overflow;

        // 조작 힌트
        var hintGo = new GameObject("Hint"); hintGo.transform.SetParent(box.transform, false);
        var hintRt = hintGo.AddComponent<RectTransform>();
        hintRt.anchorMin = new Vector2(0f, 1f); hintRt.anchorMax = new Vector2(1f, 1f);
        hintRt.pivot = new Vector2(0.5f, 1f); hintRt.anchoredPosition = new Vector2(0f, -48f); hintRt.sizeDelta = new Vector2(0f, 18f);
        var hintTxt = hintGo.AddComponent<Text>();
        hintTxt.text = "1×8 FIFO — 가득 차면 가장 오래된 장비가 자동으로 제거됩니다 │ [E] / [ESC]: 닫기";
        hintTxt.font = korFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hintTxt.fontSize = 11; hintTxt.color = new Color(0.5f, 0.46f, 0.62f);
        hintTxt.alignment = TextAnchor.MiddleCenter;
        hintTxt.horizontalOverflow = HorizontalWrapMode.Overflow; hintTxt.verticalOverflow = VerticalWrapMode.Overflow;

        // X 닫기 버튼
        var closeBtnGo = new GameObject("EquipCloseBtn"); closeBtnGo.transform.SetParent(box.transform, false);
        var closeBtnImg = closeBtnGo.AddComponent<Image>(); closeBtnImg.color = new Color(0.65f, 0.10f, 0.10f, 0.95f);
        var closeBtnRt = closeBtnGo.GetComponent<RectTransform>();
        closeBtnRt.anchorMin = new Vector2(1f, 1f); closeBtnRt.anchorMax = new Vector2(1f, 1f); closeBtnRt.pivot = new Vector2(1f, 1f);
        closeBtnRt.anchoredPosition = new Vector2(-8f, -6f); closeBtnRt.sizeDelta = new Vector2(36f, 36f);
        var closeBtn = closeBtnGo.AddComponent<Button>();
        closeBtn.onClick.AddListener(() => { _equipmentPanel.SetActive(false); ItemSlotUI.HideTooltip(); });
        var cbA = closeBtn.colors; cbA.normalColor = Color.white; cbA.highlightedColor = new Color(1f, 0.6f, 0.6f, 1f); cbA.pressedColor = new Color(0.7f, 0.2f, 0.2f, 1f); closeBtn.colors = cbA;
        MakeText(closeBtnGo.transform, "X", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 18, Color.white, TextAnchor.MiddleCenter).text = "✕";

        // EquipSlots 컨테이너 (1×8, Refresh에서 크기 재설정)
        var slotsGo = new GameObject("EquipSlots"); slotsGo.transform.SetParent(box.transform, false);
        var slotsRt = slotsGo.AddComponent<RectTransform>();
        slotsRt.anchorMin = new Vector2(0.5f, 1f); slotsRt.anchorMax = new Vector2(0.5f, 1f);
        slotsRt.pivot = new Vector2(0.5f, 1f); slotsRt.anchoredPosition = new Vector2(0f, -70f); slotsRt.sizeDelta = new Vector2(860f, 100f);
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
        const float slotW  = 90f;
        const float slotH  = 90f;
        const float gapX   = 10f;
        float gridW = totalSlots * slotW + (totalSlots - 1) * gapX;

        // EquipBox와 EquipSlots 컨테이너 크기 갱신
        var boxRt   = _equipmentPanel.transform.Find("EquipBox")?.GetComponent<RectTransform>();
        var slotsRt = slotsRoot.GetComponent<RectTransform>();
        if (boxRt   != null) boxRt.sizeDelta   = new Vector2(gridW + 60f, slotH + 120f);
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
            numTxt.fontSize  = 11;
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
            var go = MakeTile($"Chest_{placed}", pos.x, pos.y, chestClosedSprite, new Color(0.82f, 0.54f, 0.18f), 3);
            if (chestClosedSprite != null)
            {
                float fs = TileSize * 0.88f / Mathf.Max(chestClosedSprite.bounds.size.x, chestClosedSprite.bounds.size.y);
                go.transform.localScale = new Vector3(fs, fs, 1f);
            }
            else
                go.transform.localScale = Vector3.one * 0.88f;
            go.SetActive(false);
            chest.go = go;
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

    // ====================================================================
    // 아이템 상자 열기 → 선택 UI 표시
    // ====================================================================
    private void OpenTreasureChest(TreasureChest chest)
    {
        if (chest == null || chest.opened) return;
        PlaySFX(sfxBonus);
        AddLog("<color=#FFD700>✦ 아이템 상자를 발견했습니다! 아이템을 선택하세요.</color>");

        // 상자 스프라이트 열림으로 교체
        if (chest.go != null)
        {
            var sr = chest.go.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = chestOpenedSprite != null ? chestOpenedSprite : chestClosedSprite;
                sr.color  = new Color(0.4f, 0.25f, 0.08f);
            }
        }

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

        PlaySFX(sfxBonus);

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
        // FIFO: 8개 가득 찼으면 가장 오래된 장비 제거
        if (_chestEquipList.Count >= EQUIP_INV_MAX)
        {
            var oldest = _chestEquipList[0];
            player.attack  -= oldest.attackMod;
            _equipDef      -= oldest.defenseMod;
            player.maxHp   -= oldest.maxHpMod;
            player.hp       = Mathf.Clamp(player.hp, 1, player.maxHp);
            _equipHeal     -= oldest.healMod;
            _chestEquipList.RemoveAt(0);
            AddLog($"<color=#DDAAFF>[{oldest.displayName}] 이(가) 장비 목록에서 밀려났습니다.</color>");
        }

        // 스탯 즉시 적용
        player.attack  += e.attackMod;
        _equipDef      += e.defenseMod;
        player.maxHp   += e.maxHpMod;
        player.hp       = Mathf.Clamp(player.hp, 1, player.maxHp);
        _equipHeal     += e.healMod;

        // 목록에 추가 → 패널에 표시
        _chestEquipList.Add(e);

        GameManager.Instance?.History?.RecordItemObtained();

        // 장비 패널 갱신 (열려 있을 경우)
        if (_equipmentPanel != null && _equipmentPanel.activeSelf)
            RefreshEquipmentPanel();
    }
}

