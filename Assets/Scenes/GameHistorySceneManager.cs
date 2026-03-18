// ============================================================
// GameHistorySceneManager.cs
// 게임 이력 화면 관리 — Knight Fight UI Kit 적용
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Text;

public class GameHistorySceneManager : MonoBehaviour
{
    // ── UIKit 테마 ────────────────────────────────────────────────────────
    private UIKitTheme _theme;
    private UIKitTheme Theme => _theme != null ? _theme : (_theme = Resources.Load<UIKitTheme>("UIKitTheme"));

    private Font GetFont(bool bold = true)
    {
        var t = Theme;
        if (t != null) { var f = bold ? t.titleFont : t.bodyFont; if (f != null) return f; }
        var sys = Font.CreateDynamicFontFromOSFont(
            new[] { "Malgun Gothic", "Malgun Gothic Semilight", "NanumGothic",
                    "Apple SD Gothic Neo", "sans-serif" }, 28);
        return sys ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    void Start() => BuildUI();

    // =====================================================================
    private void BuildUI()
    {
        Debug.Log(">>> [GameHistorySceneManager] BuildUI Started.");

        Canvas canvas = EnsureCanvas();
        EnsureEventSystem();

        // ── 배경 ──────────────────────────────────────────────────────────
        MakeStretch(canvas.transform, "Background", Color.black);

        // ── 메인 레이아웃: 헤더 + 내용 패널 + 버튼 행 ─────────────────────

        // 헤더 리본 (최상단)
        MakeHeader(canvas.transform, "Header", "게 임 이 력",
            new Vector2(0f, 390f), new Vector2(700f, 100f), Theme?.iconList);

        // 이력 스크롤 패널 (중앙)
        var contentPanel = MakeSlicedPanel(canvas.transform, "ContentPanel",
            new Vector2(0f, 30f), new Vector2(1100f, 620f),
            Theme?.panelScroll, new Color(0.08f, 0.06f, 0.04f, 0.96f));

        // 내용 텍스트 (패널 내부)
        MakeHistoryText(contentPanel, BuildHistoryText());

        // ── 버튼 행: 돌아가기(좌) / 이력 초기화(우) ─────────────────────
        CreateStyledButton(canvas.transform, "BackButton",
            new Vector2(-220f, -355f), new Vector2(320f, 90f),
            "돌아가기", 38,
            Theme?.btnSecondary, Theme?.iconBack,
            new Color(0.8f, 0.88f, 1f), new Color(0.12f, 0.18f, 0.32f),
            () => SceneManager.LoadScene(GameManager.SCENE_LOBBY));

        CreateStyledButton(canvas.transform, "ResetButton",
            new Vector2(220f, -355f), new Vector2(320f, 90f),
            "이력 초기화", 38,
            Theme?.btnDanger, Theme?.iconCross,
            new Color(1f, 0.82f, 0.82f), new Color(0.42f, 0.08f, 0.08f),
            OnResetPressed);

        Debug.Log(">>> [GameHistorySceneManager] BuildUI Completed.");
    }

    // ── 이력 텍스트 생성 ─────────────────────────────────────────────────────
    private string BuildHistoryText()
    {
        var h = GameManager.Instance?.History;
        if (h == null) return "(데이터 없음)";

        var sb = new StringBuilder();
        sb.AppendLine($"  ▶ 최대 도달 층수     :  {h.MaxFloorReached} 층");
        sb.AppendLine($"  ▶ 아이템 획득 총수   :  {h.TotalItemsObtained} 개");
        sb.AppendLine($"  ▶ 골드 획득 총수     :  {h.TotalGoldObtained:N0} G");
        sb.AppendLine($"  ▶ 직입당한 누적 수   :  {h.TotalSurpriseAttacks} 회");
        sb.AppendLine();

        sb.AppendLine("  ─── 몬스터별 처치 수 ───────────────────────");
        var kills = h.GetAllMonsterKills();
        if (kills.Count == 0)
        {
            sb.AppendLine("    (기록 없음)");
        }
        else
        {
            var sorted = new List<KeyValuePair<string, int>>(kills);
            sorted.Sort((a, b) => b.Value.CompareTo(a.Value));
            foreach (var kv in sorted)
                sb.AppendLine($"    {kv.Key,-18}:  {kv.Value} 마리");
        }

        sb.AppendLine();
        sb.AppendLine("  ─── 달성 목표 ──────────────────────────────");
        var achievements = new Dictionary<string, string>
        {
            { AchievementID.FIRST_FLOOR,     "첫 번째 층 입장"               },
            { AchievementID.FLOOR_10,        "10층 도달"                     },
            { AchievementID.FLOOR_20,        "20층 도달"                     },
            { AchievementID.FLOOR_30,        "30층 클리어 (최종 보스 격파)"   },
            { AchievementID.KILL_100,        "총 처치 100마리"               },
            { AchievementID.GOLD_1000,       "누적 골드 1,000 G"             },
            { AchievementID.GOLD_10000,      "누적 골드 10,000 G"            },
            { AchievementID.ITEM_50,         "아이템 50개 획득"              },
            { AchievementID.FULL_EQUIP,      "장비 슬롯 만석"                },
            { AchievementID.NO_SURPRISE_RUN, "직입 없이 10층 돌파"           },
        };
        foreach (var kv in achievements)
        {
            string mark = h.IsAchieved(kv.Key) ? "★" : "☆";
            sb.AppendLine($"    {mark}  {kv.Value}");
        }

        return sb.ToString();
    }

    // 이력 초기화
    private void OnResetPressed()
    {
        GameManager.Instance?.History?.ResetHistory();
        SceneManager.LoadScene(GameManager.SCENE_HISTORY);
    }

    // =====================================================================
    // UI 헬퍼
    // =====================================================================
    private Canvas EnsureCanvas()
    {
        var c = FindAnyObjectByType<Canvas>();
        if (c != null) return c;
        var go = new GameObject("Canvas");
        c = go.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        var sc = go.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);
        sc.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return c;
    }

    private static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
        var go = new GameObject("EventSystem");
        go.AddComponent<UnityEngine.EventSystems.EventSystem>();
        go.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }

    private static void MakeStretch(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = color;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
    }

    private static RectTransform MakeSlicedPanel(Transform parent, string name,
        Vector2 pos, Vector2 size, Sprite sprite, Color fallback)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        if (sprite != null) { img.sprite = sprite; img.type = Image.Type.Sliced; img.color = Color.white; }
        else                { img.color = fallback; }
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        return rt;
    }

    private void MakeHeader(Transform parent, string name, string title,
        Vector2 pos, Vector2 size, Sprite iconSprite)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        if (Theme?.headerRibbon != null)
        {
            img.sprite = Theme.headerRibbon; img.type = Image.Type.Sliced; img.color = Color.white;
        }
        else img.color = new Color(0.28f, 0.14f, 0.04f);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos; rt.sizeDelta = size;

        if (iconSprite != null)
        {
            var ig = new GameObject("Icon"); ig.transform.SetParent(go.transform, false);
            var irt = ig.AddComponent<RectTransform>();
            irt.anchorMin = new Vector2(0f, 0.5f); irt.anchorMax = new Vector2(0f, 0.5f);
            irt.pivot = new Vector2(0f, 0.5f); irt.anchoredPosition = new Vector2(16f, 0f); irt.sizeDelta = new Vector2(56f, 56f);
            var iimg = ig.AddComponent<Image>(); iimg.sprite = iconSprite; iimg.preserveAspect = true; iimg.color = new Color(1f, 0.92f, 0.65f);
        }

        var tGo = new GameObject("Title"); tGo.transform.SetParent(go.transform, false);
        var trt = tGo.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.sizeDelta = Vector2.zero;
        var t = tGo.AddComponent<Text>();
        t.text = title; t.font = GetFont(true); t.fontSize = 30;
        t.color = new Color(1f, 0.95f, 0.75f); t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
    }

    // 이력 텍스트를 패널 내부에 배치 (좌상단 정렬, 줄바꿈 허용)
    private void MakeHistoryText(RectTransform panel, string content)
    {
        var go = new GameObject("HistoryText");
        go.transform.SetParent(panel, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(24f, 16f); rt.offsetMax = new Vector2(-24f, -16f);
        var t = go.AddComponent<Text>();
        t.text = content; t.font = GetFont(false); t.fontSize = 22;
        t.color = new Color(0.93f, 0.88f, 0.78f); t.alignment = TextAnchor.UpperLeft;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow   = VerticalWrapMode.Overflow;
    }

    private void CreateStyledButton(Transform parent, string name,
        Vector2 pos, Vector2 size, string label, int fontSize,
        Sprite sprite, Sprite iconSprite, Color textColor, Color fallback,
        UnityEngine.Events.UnityAction action)
    {
        var btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        var rt = btnObj.AddComponent<RectTransform>();
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        var img = btnObj.AddComponent<Image>();
        if (sprite != null) { img.sprite = sprite; img.type = Image.Type.Sliced; img.color = Color.white; }
        else                { img.color = fallback; }
        var btn = btnObj.AddComponent<Button>();
        var cb  = btn.colors;
        cb.normalColor = Color.white; cb.highlightedColor = new Color(1f, 1f, 0.85f); cb.pressedColor = new Color(0.75f, 0.7f, 0.55f);
        btn.colors = cb;
        btn.onClick.AddListener(action);

        float iconW = iconSprite != null ? 58f : 0f;
        if (iconSprite != null)
        {
            var ig = new GameObject("Icon"); ig.transform.SetParent(btnObj.transform, false);
            var irt = ig.AddComponent<RectTransform>();
            irt.anchorMin = new Vector2(0f, 0.5f); irt.anchorMax = new Vector2(0f, 0.5f);
            irt.pivot = new Vector2(0f, 0.5f); irt.anchoredPosition = new Vector2(16f, 0f); irt.sizeDelta = new Vector2(46f, 46f);
            var iimg = ig.AddComponent<Image>(); iimg.sprite = iconSprite; iimg.preserveAspect = true; iimg.color = textColor;
        }

        var tGo = new GameObject("Label"); tGo.transform.SetParent(btnObj.transform, false);
        var trt = tGo.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(iconW, 0f); trt.offsetMax = Vector2.zero;
        var t = tGo.AddComponent<Text>();
        t.text = label; t.font = GetFont(true); t.fontSize = Mathf.Min(fontSize, 30);
        t.color = textColor; t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
    }
}
