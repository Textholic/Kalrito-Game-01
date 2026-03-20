#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class ProjectInitializer
{
    // ── 스프라이트 경로 ────────────────────────────────────────────────────
    private const string BASE    = "Assets/Brackeys/2D Mega Pack/";
    private const string FloorP  = "Assets/Resources/tile.png";
    private const string WallP   = BASE+"Environment/Gothic/Stone.png";
    private const string StairsP = BASE+"Environment/Gothic/Pentagram_Activated.png"; // 계단 변경
    private const string PlayerP      = BASE+"Characters/Fantasy/LightWizard.png";
    private const string PlayerSheetP = "Assets/Characters/PlayerSheet.png";  // 플레이어 스프라이트 시트
    // Down=0, Up=4, Left=8, Right=12, Downed=16 (시트 인덱스)
    private static readonly (string field, int index)[] PlayerSingleSpriteFields =
    {
        ("playerSpriteDown",    0),
        ("playerSpriteUp",      4),
        ("playerSpriteLeft",    8),
        ("playerSpriteRight",  12),
        ("playerSpriteDowned", 16),
    };
    // 에너미 슬롯은 Inspector에서 수동등록
    private const string PotionP = BASE+"Items & Icons/Pixel Art/Potion.png";
    private const string GoldSmP = BASE+"Items & Icons/Pixel Art/Coin.png";
    private const string GoldLgP   = BASE+"Items & Icons/Pixel Art/Diamond.png";
    private const string InvenIconP = BASE+"Items & Icons/Pixel Art/Scroll.png";
    private const string EquipIconP = BASE+"Items & Icons/Pixel Art/Iron.png";
    private const string BgP        = BASE+"Backgrounds/DarkBackground.png";
    // 보스 복도 스프라이트
    private const string DoorP      = BASE+"Environment/Gothic/Door.png";
    private const string ShopNpcP   = "Assets/Resources/shop.png";
    // 아이템 상자 스프라이트 — lootbox 파일명 기준 (스프라이트 임포트 후 자동 할당됨)
    private const string ChestBottomP   = BASE+"Items & Icons/Lootbox/lootbox_bronze_bottom.png";
    private const string ChestTopCloseP = BASE+"Items & Icons/Lootbox/lootbox_bronze_topclose.png";
    private const string ChestTopOpenP  = BASE+"Items & Icons/Lootbox/lootbox_bronze_topopen.png";

    // ── 사운드 경로 ───────────────────────────────────────────────────────
    private const string SND = BASE+"Sounds/";
    private const string SfxHitP      = SND+"Hit.wav";
    private const string SfxBonusP    = SND+"Bonus.wav";
    private const string SfxGameOverP = SND+"GameOver.wav";
    private const string SfxStairsP   = SND+"Spawn.wav";
    private const string SfxEnemyHitP    = SND+"GruntVoice01.wav";
    private const string SfxLootboxOpenP = "Assets/Sounds/lootbox_open.ogg";
    private const string SfxDeath01P     = "Assets/Sounds/death_sound01.ogg";
    private const string SfxDeath02P     = "Assets/Sounds/death_sound02.ogg";
    private const string SfxDeath03P     = "Assets/Sounds/death_sound03.ogg";
    private const string BgmP         = "Assets/Sounds/Theme/main_01.ogg"; // 배경음

    // ====================================================================
    [MenuItem("Tools/1. Initialize Project (Force Create)")]
    public static void Initialize()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("오류", "플레이 모드에서는 실행할 수 없습니다.\n플레이를 중지한 후 다시 시도하세요.", "확인");
            return;
        }

        if (!Directory.Exists(Application.dataPath+"/Scenes"))
        {
            Directory.CreateDirectory(Application.dataPath+"/Scenes");
            AssetDatabase.Refresh();
        }

        CreateScene("TitleScene",       typeof(TitleSceneManager),       false);
        CreateScene("GameOptionsScene", typeof(GameOptionsSceneManager), false);
        CreateScene("SettingsScene",    typeof(SettingsSceneManager),       false);
        CreateScene("GameHistoryScene",  typeof(GameHistorySceneManager),    false);
        CreateScene("GameScene",        typeof(GameSceneManager),        true);

        CreateGameManagerPrefab();
        SetupBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(">>> All scenes created.");
        EditorUtility.DisplayDialog("완료","씬이 생성되었습니다.\nTools > 2. Open Title Scene 으로 시작하세요.","확인");
    }

    [MenuItem("Tools/2. Open Title Scene")]
    public static void OpenTitleScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene("Assets/Scenes/TitleScene.unity");
    }

    [MenuItem("Tools/3. Setup Game Scene Sprites")]
    public static void SetupGameSceneSprites()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity", OpenSceneMode.Single);

        var gsm = Object.FindAnyObjectByType<GameSceneManager>();
        if (gsm == null)
        {
            EditorUtility.DisplayDialog("오류",
                "GameSceneManager를 찾을 수 없습니다.\n먼저 Tools/1을 실행하세요.","확인");
            return;
        }
        AssignAll(gsm);
        EditorUtility.SetDirty(gsm);
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("완료","스프라이트 & 사운드가 연결되었습니다!","확인");
    }

    // ====================================================================
    private static void CreateScene(string name, System.Type type, bool assign)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[ProjectInitializer] 플레이 모드에서는 씬을 생성할 수 없습니다.");
            return;
        }
        string path = $"Assets/Scenes/{name}.unity";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var obj   = new GameObject(name+"Manager");
        var comp  = obj.AddComponent(type);
        if (assign && comp is GameSceneManager gsm) AssignAll(gsm);
        EditorSceneManager.SaveScene(scene, path);
    }

    public static void AssignAll(GameSceneManager gsm)
    {
        var so = new SerializedObject(gsm);

        // 타일
        Set(so, "floorSprite",  SubSprIdx(FloorP, 0));
        Set(so, "wallSprite",   Spr(WallP));
        Set(so, "stairsSprite", Spr(StairsP));

        // 엔티티
        Set(so, "playerSprite", Spr(PlayerP));
        SetPlayerSingleSprites(so, PlayerSheetP);
        // 에너미 슬롯은 Inspector에서 수동 등록 (GameSceneManager > Enemy Slots)

        // 아이템
        Set(so, "potionSprite", Spr(PotionP));
        Set(so, "goldSmSprite", Spr(GoldSmP));
        Set(so, "goldLgSprite", Spr(GoldLgP));

        // 아이템 상자 스프라이트 (Assets/Resources/lootbox.png 의 [0]=닫힘, [1]=열림)
        // EnsureSprites() 에서 자동 로드되므로 Inspector 값이 없어도 동작함
        // Set(so, "chestClosedSprite", ...);
        // Set(so, "chestOpenedSprite", ...);

        // HUD 아이콘
        Set(so, "inventoryIconSprite", Spr(InvenIconP));
        Set(so, "equipmentIconSprite", Spr(EquipIconP));

        // 배경
        Set(so, "bgSprite", Spr(BgP));

        // 보스 복도 스프라이트
        Set(so, "doorSprite", Spr(DoorP));
        SetSpriteArray(so, "shopNpcSprites", ShopNpcP);

        // 사운드
        SetClip(so, "sfxHit",      Clip(SfxHitP));
        SetClip(so, "sfxBonus",    Clip(SfxBonusP));
        SetClip(so, "sfxGameOver", Clip(SfxGameOverP));
        SetClip(so, "sfxStairs",   Clip(SfxStairsP));
        SetClip(so, "sfxEnemyHit",    Clip(SfxEnemyHitP));
        SetClip(so, "sfxLootboxOpen", Clip(SfxLootboxOpenP));
        SetClipArray(so, "sfxDeathPool", new[]{ SfxDeath01P, SfxDeath02P, SfxDeath03P });
        SetClip(so, "bgmCombat",      Clip(BgmP));

        // 보스 BGM 배열 (Assets/Sounds/Character/boss_01~10.ogg)
        SetClipArray(so, "bgmBossPool", BossBgmPaths());

        so.ApplyModifiedProperties();
        Debug.Log("[Setup] All assets assigned to GameSceneManager.");
    }

    // ── 헬퍼 ──────────────────────────────────────────────────────────────
    private static void Set(SerializedObject so, string field, Sprite spr)
    {
        if (spr==null){Debug.LogWarning($"[Setup] 스프라이트 없음: {field}");return;}
        var p=so.FindProperty(field);
        if (p==null){Debug.LogWarning($"[Setup] 필드 없음: {field}");return;}
        p.objectReferenceValue=spr;
    }

    // 슬라이스된 PNG의 모든 서브 스프라이트를 배열 필드에 할당
    private static void SetSpriteArray(SerializedObject so, string field, string path)
    {
        var p = so.FindProperty(field);
        if (p == null) { Debug.LogWarning($"[Setup] 필드 없음: {field}"); return; }
        var all = AssetDatabase.LoadAllAssetsAtPath(path);
        var sprites = new System.Collections.Generic.List<Sprite>();
        foreach (var o in all)
            if (o is Sprite s) sprites.Add(s);
        if (sprites.Count == 0) { Debug.LogWarning($"[Setup] 스프라이트 없음: {path}"); return; }
        p.ClearArray();
        p.arraySize = sprites.Count;
        for (int i = 0; i < sprites.Count; i++)
            p.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        Debug.Log($"[Setup] {field}: {sprites.Count}개 슬라이스 등록");
    }

    private static void SetClip(SerializedObject so,string field,AudioClip clip)
    {
        if (clip==null){Debug.LogWarning($"[Setup] 클립 없음: {field}");return;}
        var p=so.FindProperty(field);
        if (p==null){Debug.LogWarning($"[Setup] 필드 없음: {field}");return;}
        p.objectReferenceValue=clip;
    }

    private static void SetClipArray(SerializedObject so, string field, string[] paths)
    {
        var p = so.FindProperty(field);
        if (p == null) { Debug.LogWarning($"[Setup] 필드 없음: {field}"); return; }
        var clips = new List<AudioClip>();
        foreach (var path in paths)
        {
            var c = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (c != null) clips.Add(c);
            else Debug.LogWarning($"[Setup] 클립 없음: {path}");
        }
        p.ClearArray();
        p.arraySize = clips.Count;
        for (int i = 0; i < clips.Count; i++)
            p.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
        Debug.Log($"[Setup] {field}: {clips.Count}개 등록");
    }

    private static string[] BossBgmPaths()
    {
        const string BASE_DIR = "Assets/Sounds/Character/";
        var paths = new List<string>();
        for (int i = 1; i <= 10; i++)
            paths.Add($"{BASE_DIR}boss_{i:D2}.ogg");
        return paths.ToArray();
    }

    private static void SetPlayerSingleSprites(SerializedObject so, string path)
    {
        var all = AssetDatabase.LoadAllAssetsAtPath(path);
        var sprites = new List<Sprite>();
        foreach (var a in all)
            if (a is Sprite s) sprites.Add(s);
        if (sprites.Count == 0)
        {
            Debug.LogWarning($"[Setup] 플레이어 시트 없음: {path} — Inspector에서 수동 할당 필요");
            return;
        }
        sprites.Sort((a, b) => {
            var aParts = a.name.Split('_'); var bParts = b.name.Split('_');
            int ai = int.TryParse(aParts[aParts.Length - 1], out var aVal) ? aVal : 0;
            int bi = int.TryParse(bParts[bParts.Length - 1], out var bVal) ? bVal : 0;
            return ai.CompareTo(bi);
        });
        foreach (var (field, index) in PlayerSingleSpriteFields)
        {
            if (index >= sprites.Count) { Debug.LogWarning($"[Setup] 실제 스프라이트 수 부족 ({sprites.Count}개)으로 {field} 할당 실패"); continue; }
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogWarning($"[Setup] 필드 없음: {field}"); continue; }
            p.objectReferenceValue = sprites[index];
        }
        Debug.Log($"[Setup] 플레이어 방향 스프라이트 {PlayerSingleSpriteFields.Length}장 자동 할당 완료");
    }

    private static void SetSprArr(SerializedObject so, string field, string path)
    {
        var all = AssetDatabase.LoadAllAssetsAtPath(path);
        var sprites = new List<Sprite>();
        foreach (var a in all)
            if (a is Sprite s) sprites.Add(s);
        if (sprites.Count == 0) { Debug.LogWarning($"[Setup] 스프라이트 시트 없음: {path}"); return; }
        sprites.Sort((a, b) => {
            var aParts = a.name.Split('_'); var bParts = b.name.Split('_');
            int ai = int.TryParse(aParts[aParts.Length - 1], out var aVal) ? aVal : 0;
            int bi = int.TryParse(bParts[bParts.Length - 1], out var bVal) ? bVal : 0;
            return ai.CompareTo(bi);
        });
        var p = so.FindProperty(field);
        if (p == null) { Debug.LogWarning($"[Setup] 필드 없음: {field}"); return; }
        p.arraySize = sprites.Count;
        for (int i = 0; i < sprites.Count; i++)
            p.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
    }

    private static Sprite Spr(string path)
        => AssetDatabase.LoadAssetAtPath<Sprite>(path);

    private static Sprite SubSpr(string path,string name)
    {
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
            if (o is Sprite s && s.name==name) return s;
        return AssetDatabase.LoadAssetAtPath<Sprite>(path); // fallback: 메인
    }

    // 슬라이스된 스프라이트 시트에서 인덱스로 하나 가져오기
    private static Sprite SubSprIdx(string path, int index)
    {
        var all = AssetDatabase.LoadAllAssetsAtPath(path);
        var sprites = new System.Collections.Generic.List<Sprite>();
        foreach (var o in all)
            if (o is Sprite s) sprites.Add(s);
        if (sprites.Count > index) return sprites[index];
        if (sprites.Count > 0) return sprites[0];
        return null;
    }

    private static AudioClip Clip(string path)
        => AssetDatabase.LoadAssetAtPath<AudioClip>(path);

    private static void SetupBuildSettings()
    {
        var paths=new[]{
            "Assets/Scenes/TitleScene.unity",
            "Assets/Scenes/GameOptionsScene.unity",
            "Assets/Scenes/SettingsScene.unity",
            "Assets/Scenes/GameHistoryScene.unity",
            "Assets/Scenes/GameScene.unity"};
        var list=new List<EditorBuildSettingsScene>();
        foreach (var p in paths) list.Add(new EditorBuildSettingsScene(p,true));
        EditorBuildSettings.scenes=list.ToArray();
    }

    // ── GameManager 프리팸 생성 ────────────────────────────────────────────────
    [MenuItem("Tools/4. Create GameManager Prefab")]
    public static void CreateGameManagerPrefab()
    {
        const string prefabPath = "Assets/Resources/GameManager.prefab";
        if (!Directory.Exists(Application.dataPath + "/Resources"))
        {
            Directory.CreateDirectory(Application.dataPath + "/Resources");
            AssetDatabase.Refresh();
        }
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
        {
            Debug.Log("[Setup] GameManager 프리팸이 이미 존재합니다.");
            return;
        }
        var go = new GameObject("GameManager");
        go.AddComponent<GameManager>();
        // 서브시스템 컴포넌트는 GameManager.Awake()에서 자동 추가되므로 여기에는 비움
        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Setup] GameManager 프리팹 생성: {prefabPath}");
    }

    // ── UI 테마 설정 (Knight Fight) ──────────────────────────────────────────
    [MenuItem("Tools/5. UI 테마 설정 (Knight Fight)")]
    public static void SetupUIKitTheme()
    {
        const string KNIGHT = "Assets/UI Kit Pro - Big Bundle/UI Kit Pro - Knight Fight";
        const string SPRITE_BTN  = KNIGHT + "/Sprites/Buttons/1x";
        const string SPRITE_PANEL = KNIGHT + "/Sprites/Panels/1x";
        const string SPRITE_HEAD  = KNIGHT + "/Sprites/Headers/1x";
        const string SPRITE_ICON  = KNIGHT + "/Sprites/Icons/Flat";
        const string FONT_PATH    = KNIGHT + "/Fonts";

        if (!Directory.Exists(Application.dataPath + "/Resources"))
        {
            Directory.CreateDirectory(Application.dataPath + "/Resources");
            AssetDatabase.Refresh();
        }

        const string assetPath = "Assets/Resources/UIKitTheme.asset";
        UIKitTheme theme = AssetDatabase.LoadAssetAtPath<UIKitTheme>(assetPath);
        if (theme == null)
        {
            theme = ScriptableObject.CreateInstance<UIKitTheme>();
            AssetDatabase.CreateAsset(theme, assetPath);
        }

        // 폰트
        theme.titleFont = AssetDatabase.LoadAssetAtPath<Font>($"{FONT_PATH}/Maplestory OTF Bold.otf");
        theme.bodyFont  = AssetDatabase.LoadAssetAtPath<Font>($"{FONT_PATH}/Maplestory OTF Light.otf");

        // 버튼 스프라이트
        theme.btnPrimary   = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_BTN}/btn7_arcround_brown.png");
        theme.btnSecondary = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_BTN}/btn7_arcround_night.png");
        theme.btnDanger    = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_BTN}/btn7_arcround_red.png");
        theme.btnBack      = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_BTN}/btn11_round_black.png");
        theme.btnDisabled  = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_BTN}/btn7_arcround_greyscale.png");

        // 패널 스프라이트
        theme.panelMain        = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_PANEL}/panel2_midround_brown.png");
        theme.panelScroll      = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_PANEL}/panelscroll1_brown.png");
        theme.panelSlot        = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_PANEL}/panelslot1_brown.png");
        theme.panelDark        = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_PANEL}/panel2_midround_night.png");
        theme.panelTransparent = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_PANEL}/panel2_midround_transparent.png");

        // 헤더 스프라이트
        theme.headerRibbon        = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_HEAD}/head1_ribbon_brown.png");
        theme.headerRibbonOutline = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_HEAD}/head2_outribbon_brown.png");

        // 게임 아이콘
        theme.iconSword     = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_ICON}/icon_weapon_sword1.png");
        theme.iconShield    = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_ICON}/icon_weapon_shield1.png");
        theme.iconSkull     = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_ICON}/icon_skull.png");
        theme.iconChest     = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_ICON}/icon_chest.png");
        theme.iconSettings  = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_ICON}/icon_settings1.png");
        theme.iconStar      = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_ICON}/icon_ranking_star.png");
        theme.iconCoin      = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_ICON}/icon_coin_crown.png");
        theme.iconMusicOn   = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_ICON}/icon_music1_on.png");
        theme.iconMusicOff  = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_ICON}/icon_music1_off.png");
        theme.iconSfxOn     = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_ICON}/icon_sfx_on.png");
        theme.iconSfxOff    = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_ICON}/icon_sfx_off.png");
        theme.iconBack      = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_ICON}/icon_arrow1_left.png");
        theme.iconCastle    = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_ICON}/icon_castle.png");
        theme.iconPotion    = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_ICON}/icon_elixir.png");
        theme.iconList      = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_ICON}/icon_list.png");
        theme.iconCheckmark = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_ICON}/icon_checkmark.png");
        theme.iconCross     = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_ICON}/icon_cross.png");
        theme.iconArmor     = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_ICON}/icon_armor.png");
        theme.iconBone      = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_ICON}/icon_bone.png");

        EditorUtility.SetDirty(theme);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        int loaded = CountLoaded(theme);
        Debug.Log($"[UIKitThemeSetup] UIKitTheme 설정 완료! ({loaded}개 에셋 연결)");
        Selection.activeObject = theme;
        EditorUtility.DisplayDialog("완료",
            $"UIKitTheme 설정 완료!\n{loaded}개 에셋이 연결되었습니다.\n\nAssets/Resources/UIKitTheme.asset 을 확인하세요.", "확인");
    }

    private static int CountLoaded(UIKitTheme t)
    {
        int c = 0;
        if (t.titleFont        != null) c++;
        if (t.bodyFont         != null) c++;
        if (t.btnPrimary       != null) c++;
        if (t.btnSecondary     != null) c++;
        if (t.btnDanger        != null) c++;
        if (t.btnBack          != null) c++;
        if (t.panelMain        != null) c++;
        if (t.panelScroll      != null) c++;
        if (t.panelDark        != null) c++;
        if (t.headerRibbon     != null) c++;
        if (t.iconSword        != null) c++;
        if (t.iconSettings     != null) c++;
        if (t.iconStar         != null) c++;
        if (t.iconCoin         != null) c++;
        if (t.iconPotion       != null) c++;
        if (t.iconBack         != null) c++;
        if (t.iconCastle       != null) c++;
        return c;
    }

    // ── 던전 비주얼 테마 설정 (Brackeys 2D Mega Pack) ────────────────────────
    [MenuItem("Tools/6. 던전 테마 설정 (Brackeys 2D)")]
    public static void SetupDungeonTheme()
    {
        const string BRACKEYS = "Assets/Brackeys/2D Mega Pack";
        const string ENV    = BRACKEYS + "/Environment";
        const string BG_DIR = BRACKEYS + "/Backgrounds";
        const string TILES  = ENV + "/Tiles";
        const string GOTHIC = ENV + "/Gothic";

        // 스프라이트 로드
        // tile.png 슬라이스 로드 (11열×7행=77장, 인덱스 0부터)
        const string TILE_PATH = "Assets/Resources/tile.png";
        var tileObjs = AssetDatabase.LoadAllAssetsAtPath(TILE_PATH);
        var tileList = new System.Collections.Generic.List<Sprite>();
        foreach (var o in tileObjs)
            if (o is Sprite s) tileList.Add(s);
        // 인덱스 범위 초과 시 0번 폴백
        System.Func<int, Sprite> T = i => tileList.Count > i ? tileList[i]
                                        : (tileList.Count > 0 ? tileList[0] : null);

        Sprite stoneWall    = AssetDatabase.LoadAssetAtPath<Sprite>(GOTHIC + "/Stone.png");
        Sprite stairsSpr    = AssetDatabase.LoadAssetAtPath<Sprite>(GOTHIC + "/Pentagram_Activated.png");
        Sprite darkBg       = AssetDatabase.LoadAssetAtPath<Sprite>(BG_DIR + "/DarkBackground.png");
        Sprite blueBg       = AssetDatabase.LoadAssetAtPath<Sprite>(BG_DIR + "/BlueBackground.png");
        Sprite brickBg      = AssetDatabase.LoadAssetAtPath<Sprite>(BG_DIR + "/BrickBackground.png");
        Sprite dustyBg      = AssetDatabase.LoadAssetAtPath<Sprite>(BG_DIR + "/DustyBackground.png");
        Sprite greyBg       = AssetDatabase.LoadAssetAtPath<Sprite>(BG_DIR + "/ArcadeGreyBackground.png");

        // 에셋 생성 또는 로드
        if (!Directory.Exists(Application.dataPath + "/Resources"))
        {
            Directory.CreateDirectory(Application.dataPath + "/Resources");
            AssetDatabase.Refresh();
        }
        const string assetPath = "Assets/Resources/DungeonThemeConfig.asset";
        DungeonThemeConfig config = AssetDatabase.LoadAssetAtPath<DungeonThemeConfig>(assetPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<DungeonThemeConfig>();
            AssetDatabase.CreateAsset(config, assetPath);
        }

        // 10개 테마 정의 (3층마다 하나: 0=1~3층, 1=4~6층, … 9=28~30층)
        config.themes = new FloorTheme[10];

        // ── 0: 1~3층   석조 지하실 (Stone Dungeon) ──────────────────────────
        config.themes[0] = new FloorTheme
        {
            themeName          = "석조 지하실",
            floorSprite        = T(0),  // 밝은 회색 크랙 돌
            wallSprite         = stoneWall,
            stairsSprite       = stairsSpr,
            bgSprite           = darkBg,
            floorVisibleColor  = new Color(0.78f, 0.67f, 0.50f),
            wallVisibleColor   = new Color(0.52f, 0.42f, 0.32f),
            stairsColor        = new Color(1.00f, 0.85f, 0.20f),
            floorDimColor      = new Color(0.28f, 0.23f, 0.16f),
            wallDimColor       = new Color(0.16f, 0.13f, 0.09f),
            bgTint             = new Color(0.12f, 0.10f, 0.08f),
            cameraBackground   = new Color(0.05f, 0.04f, 0.03f),
        };

        // ── 1: 4~6층   음산한 지하묘지 (Gloomy Crypt) ──────────────────────
        config.themes[1] = new FloorTheme
        {
            themeName          = "음산한 지하묘지",
            floorSprite        = T(33), // 어두운 크랙 석판
            wallSprite         = stoneWall,
            stairsSprite       = stairsSpr,
            bgSprite           = darkBg,
            floorVisibleColor  = new Color(0.52f, 0.57f, 0.70f),
            wallVisibleColor   = new Color(0.30f, 0.34f, 0.44f),
            stairsColor        = new Color(0.55f, 0.90f, 1.00f),
            floorDimColor      = new Color(0.18f, 0.20f, 0.28f),
            wallDimColor       = new Color(0.10f, 0.12f, 0.18f),
            bgTint             = new Color(0.08f, 0.09f, 0.16f),
            cameraBackground   = new Color(0.03f, 0.04f, 0.08f),
        };

        // ── 2: 7~9층   마법 비전실 (Arcane Chamber) ─────────────────────────
        config.themes[2] = new FloorTheme
        {
            themeName          = "마법 비전실",
            floorSprite        = T(44), // 코블스톤 포장돌
            wallSprite         = stoneWall,
            stairsSprite       = stairsSpr,
            bgSprite           = blueBg,
            floorVisibleColor  = new Color(0.64f, 0.52f, 0.88f),
            wallVisibleColor   = new Color(0.38f, 0.28f, 0.58f),
            stairsColor        = new Color(0.90f, 0.38f, 1.00f),
            floorDimColor      = new Color(0.22f, 0.16f, 0.36f),
            wallDimColor       = new Color(0.12f, 0.08f, 0.22f),
            bgTint             = new Color(0.08f, 0.04f, 0.18f),
            cameraBackground   = new Color(0.04f, 0.02f, 0.10f),
        };

        // ── 3: 10~12층  용암 지대 (Lava Zone) ──────────────────────────────
        config.themes[3] = new FloorTheme
        {
            themeName          = "용암 지대",
            floorSprite        = T(12), // 어두운 갈색 흔흥 흐응
            wallSprite         = stoneWall,
            stairsSprite       = stairsSpr,
            bgSprite           = darkBg,
            floorVisibleColor  = new Color(0.88f, 0.46f, 0.16f),
            wallVisibleColor   = new Color(0.58f, 0.18f, 0.06f),
            stairsColor        = new Color(1.00f, 0.35f, 0.05f),
            floorDimColor      = new Color(0.36f, 0.16f, 0.05f),
            wallDimColor       = new Color(0.20f, 0.07f, 0.02f),
            bgTint             = new Color(0.20f, 0.05f, 0.02f),
            cameraBackground   = new Color(0.10f, 0.02f, 0.01f),
        };

        // ── 4: 13~15층  수몰 동굴 (Flooded Cave) ───────────────────────────
        config.themes[4] = new FloorTheme
        {
            themeName          = "수몰 동굴",
            floorSprite        = T(22), // 회색빛 젖은 돌
            wallSprite         = stoneWall,
            stairsSprite       = stairsSpr,
            bgSprite           = blueBg,
            floorVisibleColor  = new Color(0.36f, 0.78f, 0.88f),
            wallVisibleColor   = new Color(0.16f, 0.44f, 0.62f),
            stairsColor        = new Color(0.38f, 1.00f, 0.90f),
            floorDimColor      = new Color(0.12f, 0.28f, 0.40f),
            wallDimColor       = new Color(0.06f, 0.14f, 0.24f),
            bgTint             = new Color(0.04f, 0.11f, 0.22f),
            cameraBackground   = new Color(0.02f, 0.05f, 0.12f),
        };

        // ── 5: 16~18층  고대 유적 (Ancient Ruins) ───────────────────────────
        config.themes[5] = new FloorTheme
        {
            themeName          = "고대 유적",
            floorSprite        = T(27), // 모래/자갈 돌코스
            wallSprite         = stoneWall,
            stairsSprite       = stairsSpr,
            bgSprite           = dustyBg,
            floorVisibleColor  = new Color(0.90f, 0.78f, 0.50f),
            wallVisibleColor   = new Color(0.62f, 0.52f, 0.30f),
            stairsColor        = new Color(1.00f, 0.88f, 0.24f),
            floorDimColor      = new Color(0.34f, 0.28f, 0.16f),
            wallDimColor       = new Color(0.20f, 0.17f, 0.10f),
            bgTint             = new Color(0.18f, 0.15f, 0.06f),
            cameraBackground   = new Color(0.07f, 0.06f, 0.02f),
        };

        // ── 6: 19~21층  수정 동굴 (Crystal Cave) ────────────────────────────
        config.themes[6] = new FloorTheme
        {
            themeName          = "수정 동굴",
            floorSprite        = T(3),  // 연한 회색 크랙 돌
            wallSprite         = stoneWall,
            stairsSprite       = stairsSpr,
            bgSprite           = brickBg,
            floorVisibleColor  = new Color(0.70f, 0.96f, 1.00f),
            wallVisibleColor   = new Color(0.26f, 0.64f, 0.84f),
            stairsColor        = new Color(0.40f, 1.00f, 0.85f),
            floorDimColor      = new Color(0.22f, 0.36f, 0.46f),
            wallDimColor       = new Color(0.10f, 0.22f, 0.32f),
            bgTint             = new Color(0.04f, 0.12f, 0.20f),
            cameraBackground   = new Color(0.02f, 0.05f, 0.10f),
        };

        // ── 7: 22~24층  화산 지하 (Volcanic Depths) ─────────────────────────
        config.themes[7] = new FloorTheme
        {
            themeName          = "화산 지하",
            floorSprite        = T(36), // 어두운 아스팔트/섘어진 돌
            wallSprite         = stoneWall,
            stairsSprite       = stairsSpr,
            bgSprite           = darkBg,
            floorVisibleColor  = new Color(0.84f, 0.33f, 0.08f),
            wallVisibleColor   = new Color(0.32f, 0.10f, 0.03f),
            stairsColor        = new Color(1.00f, 0.55f, 0.08f),
            floorDimColor      = new Color(0.30f, 0.11f, 0.03f),
            wallDimColor       = new Color(0.16f, 0.05f, 0.01f),
            bgTint             = new Color(0.15f, 0.03f, 0.01f),
            cameraBackground   = new Color(0.06f, 0.01f, 0.01f),
        };

        // ── 8: 25~27층  심연의 그림자 (Shadow Abyss) ────────────────────────
        config.themes[8] = new FloorTheme
        {
            themeName          = "심연의 그림자",
            floorSprite        = T(66), // 짙은 슬레이트 석판
            wallSprite         = stoneWall,
            stairsSprite       = stairsSpr,
            bgSprite           = greyBg,
            floorVisibleColor  = new Color(0.50f, 0.36f, 0.72f),
            wallVisibleColor   = new Color(0.20f, 0.14f, 0.34f),
            stairsColor        = new Color(0.85f, 0.34f, 1.00f),
            floorDimColor      = new Color(0.18f, 0.12f, 0.26f),
            wallDimColor       = new Color(0.08f, 0.05f, 0.14f),
            bgTint             = new Color(0.06f, 0.03f, 0.12f),
            cameraBackground   = new Color(0.03f, 0.01f, 0.06f),
        };

        // ── 9: 28~30층  최종 심연 (Final Abyss) ─────────────────────────────
        config.themes[9] = new FloorTheme
        {
            themeName          = "최종 심연",
            floorSprite        = T(70), // 매우 어두운 크랙 돌
            wallSprite         = stoneWall,
            stairsSprite       = stairsSpr,
            bgSprite           = darkBg,
            floorVisibleColor  = new Color(0.90f, 0.78f, 0.26f),
            wallVisibleColor   = new Color(0.12f, 0.08f, 0.16f),
            stairsColor        = new Color(1.00f, 0.92f, 0.18f),
            floorDimColor      = new Color(0.28f, 0.23f, 0.08f),
            wallDimColor       = new Color(0.06f, 0.04f, 0.08f),
            bgTint             = new Color(0.03f, 0.02f, 0.05f),
            cameraBackground   = new Color(0.01f, 0.01f, 0.02f),
        };

        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 연결된 스프라이트 수 계산
        int loaded = 0;
        foreach (var th in config.themes)
        {
            if (th == null) continue;
            if (th.floorSprite  != null) loaded++;
            if (th.wallSprite   != null) loaded++;
            if (th.stairsSprite != null) loaded++;
            if (th.bgSprite     != null) loaded++;
        }

        Debug.Log($"[DungeonThemeSetup] DungeonThemeConfig 설정 완료! ({loaded}개 스프라이트 연결)");
        Selection.activeObject = config;
        EditorUtility.DisplayDialog("완료",
            $"던전 테마 설정 완료!\n{loaded}개 스프라이트가 연결되었습니다.\n\n" +
            "▶ 3층마다 분위기가 변경됩니다:\n" +
            "  1-3층: 석조 지하실  /  4-6층: 음산한 지하묘지\n" +
            "  7-9층: 마법 비전실  /  10-12층: 용암 지대\n" +
            "  13-15층: 수몰 동굴  /  16-18층: 고대 유적\n" +
            "  19-21층: 수정 동굴  /  22-24층: 화산 지하\n" +
            "  25-27층: 심연의 그림자  /  28-30층: 최종 심연\n\n" +
            "Assets/Resources/DungeonThemeConfig.asset 을 확인하세요.", "확인");
    }
}
#endif
