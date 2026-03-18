// ============================================================
// UIKitTheme.cs
// UI Kit Pro - Knight Fight 에셋 레퍼런스 ScriptableObject.
// Assets/Resources/UIKitTheme.asset 에 배치하여 런타임에서
// Resources.Load<UIKitTheme>("UIKitTheme") 으로 로드합니다.
//
// 설정: Tools > 5. UI 테마 설정 (Knight Fight)
// ============================================================
using UnityEngine;

[CreateAssetMenu(fileName = "UIKitTheme", menuName = "DungeonGame/UI Kit Theme")]
public class UIKitTheme : ScriptableObject
{
    // ── 폰트 ──────────────────────────────────────────────────────────────
    [Header("폰트")]
    [Tooltip("타이틀·버튼 등 강조용 굵은 폰트 (Maplestory OTF Bold)")]
    public Font titleFont;

    [Tooltip("본문·설명용 폰트 (Maplestory OTF Light)")]
    public Font bodyFont;

    // ── 버튼 스프라이트 ───────────────────────────────────────────────────
    [Header("버튼 스프라이트 (9-Sliced)")]
    [Tooltip("주요 동작 버튼 (던전 입장 등) — btn7_arcround_brown")]
    public Sprite btnPrimary;

    [Tooltip("보조 버튼 (설정·이력 등) — btn7_arcround_night")]
    public Sprite btnSecondary;

    [Tooltip("위험 동작 버튼 (초기화 등) — btn7_arcround_red")]
    public Sprite btnDanger;

    [Tooltip("뒤로 가기 등 소형 버튼 — btn11_round_black")]
    public Sprite btnBack;

    [Tooltip("비활성화 상태 버튼 — btn7_arcround_greyscale")]
    public Sprite btnDisabled;

    // ── 패널 스프라이트 ───────────────────────────────────────────────────
    [Header("패널 스프라이트 (9-Sliced)")]
    [Tooltip("메인 콘텐츠 패널 — panel2_midround_brown")]
    public Sprite panelMain;

    [Tooltip("스크롤 스타일 패널 (이력·로그용) — panelscroll1_brown")]
    public Sprite panelScroll;

    [Tooltip("아이템 슬롯 패널 — panelslot1_brown")]
    public Sprite panelSlot;

    [Tooltip("어두운 HUD 패널 — panel2_midround_night")]
    public Sprite panelDark;

    [Tooltip("투명 오버레이 패널 — panel2_midround_transparent")]
    public Sprite panelTransparent;

    // ── 헤더 스프라이트 ───────────────────────────────────────────────────
    [Header("헤더 스프라이트 (9-Sliced)")]
    [Tooltip("리본 스타일 섹션 헤더 — head1_ribbon_brown")]
    public Sprite headerRibbon;

    [Tooltip("아웃라인 리본 헤더 — head2_outribbon_brown")]
    public Sprite headerRibbonOutline;

    // ── 게임 아이콘 ──────────────────────────────────────────────────────
    [Header("게임 아이콘 (Icons/Flat)")]
    public Sprite iconSword;       // icon_weapon_sword1
    public Sprite iconShield;      // icon_weapon_shield1
    public Sprite iconSkull;       // icon_skull
    public Sprite iconChest;       // icon_chest
    public Sprite iconSettings;    // icon_settings1
    public Sprite iconStar;        // icon_ranking_star
    public Sprite iconCoin;        // icon_coin_crown
    public Sprite iconMusicOn;     // icon_music1_on
    public Sprite iconMusicOff;    // icon_music1_off
    public Sprite iconSfxOn;       // icon_sfx_on
    public Sprite iconSfxOff;      // icon_sfx_off
    public Sprite iconBack;        // icon_arrow1_left
    public Sprite iconCastle;      // icon_castle
    public Sprite iconPotion;      // icon_elixir
    public Sprite iconList;        // icon_list
    public Sprite iconCheckmark;   // icon_checkmark
    public Sprite iconCross;       // icon_cross
    public Sprite iconArmor;       // icon_armor
    public Sprite iconBone;        // icon_bone

    // ── 편의 프로퍼티 ─────────────────────────────────────────────────────
    /// <summary>titleFont이 있으면 반환, 없으면 시스템 폴백 폰트 반환.</summary>
    public Font GetTitleFont()
    {
        if (titleFont != null) return titleFont;
        return Font.CreateDynamicFontFromOSFont(
            new[] { "Malgun Gothic", "Malgun Gothic Semilight", "NanumGothic",
                    "Apple SD Gothic Neo", "sans-serif" }, 30)
            ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    /// <summary>bodyFont이 있으면 반환, 없으면 시스템 폴백 폰트 반환.</summary>
    public Font GetBodyFont()
    {
        if (bodyFont != null) return bodyFont;
        return GetTitleFont();
    }
}
