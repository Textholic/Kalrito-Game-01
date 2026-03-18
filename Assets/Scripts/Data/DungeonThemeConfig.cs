// ============================================================
// DungeonThemeConfig.cs
// 3층마다 변하는 던전 비주얼 테마 ScriptableObject.
// Unity 에디터: Assets > Create > DungeonGame > Dungeon Theme Config
// ============================================================
using UnityEngine;

// ── 층 구간별 비주얼 테마 ─────────────────────────────────────────────────────
[System.Serializable]
public class FloorTheme
{
    [Header("테마 이름 (층 입장 로그에 표시)")]
    public string themeName = "알 수 없는 지대";

    [Header("타일 스프라이트 (null이면 단색 폴백 사용)")]
    [Tooltip("바닥 타일 스프라이트")]
    public Sprite floorSprite;
    [Tooltip("벽 타일 스프라이트")]
    public Sprite wallSprite;
    [Tooltip("계단 스프라이트")]
    public Sprite stairsSprite;
    [Tooltip("배경 스프라이트 (맵 전체 배경)")]
    public Sprite bgSprite;

    [Header("타일 색상 — 시야 내 (밝음)")]
    public Color floorVisibleColor = new Color(0.80f, 0.70f, 0.55f);
    public Color wallVisibleColor  = new Color(0.55f, 0.46f, 0.36f);
    public Color stairsColor       = new Color(0.90f, 0.50f, 1.00f);

    [Header("타일 색상 — 시야 외 (어둠 / 탐색됨)")]
    public Color floorDimColor = new Color(0.30f, 0.26f, 0.20f);
    public Color wallDimColor  = new Color(0.18f, 0.15f, 0.12f);

    [Header("환경 색상")]
    [Tooltip("배경 이미지 색 곱셈 (tint)")]
    public Color bgTint           = new Color(0.15f, 0.14f, 0.18f);
    [Tooltip("카메라 배경색 (Solid Color)")]
    public Color cameraBackground = new Color(0.04f, 0.04f, 0.06f);
}

// ── DungeonThemeConfig ScriptableObject ──────────────────────────────────────
[CreateAssetMenu(fileName = "DungeonThemeConfig", menuName = "DungeonGame/Dungeon Theme Config")]
public class DungeonThemeConfig : ScriptableObject
{
    [Tooltip("3층마다 하나씩 적용 (index 0 = 1~3층, 1 = 4~6층, …). 최소 1개 이상 필요.")]
    public FloorTheme[] themes = new FloorTheme[0];

    /// <summary>해당 층에 맞는 테마를 반환합니다 (없으면 null).</summary>
    public FloorTheme GetThemeForFloor(int floor)
    {
        if (themes == null || themes.Length == 0) return null;
        int idx = Mathf.Clamp((floor - 1) / 3, 0, themes.Length - 1);
        return themes[idx];
    }
}
