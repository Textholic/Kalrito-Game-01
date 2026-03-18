using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOptionsSceneManager : MonoBehaviour
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
                    "Apple SD Gothic Neo", "sans-serif" }, 30);
        return sys ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    void Start() => SetupUI();

    // =====================================================================
    private void SetupUI()
    {
        Debug.Log(">>> [GameOptionsSceneManager] SetupUI Started.");

        Canvas canvas = EnsureCanvas();

        // ── 배경 ──────────────────────────────────────────────────────────
        MakeStretch(canvas.transform, "Background", Color.black);

        // ── 중앙 패널 ─────────────────────────────────────────────────────
        var panel = MakeSlicedPanel(canvas.transform, "MainPanel",
            new Vector2(0f, -20f), new Vector2(540f, 560f),
            Theme?.panelDark, new Color(0.08f, 0.07f, 0.12f, 0.95f));

        // ── 헤더 리본 ─────────────────────────────────────────────────────
        MakeHeader(panel.transform, "Header", "메 인 메 뉴",
            new Vector2(0f, 244f), new Vector2(440f, 90f));

        // ── 던전 입장 (Primary) ───────────────────────────────────────────
        CreateMenuButton(panel.transform, "DungeonButton",
            new Vector2(0f, 100f),   new Vector2(440f, 120f),
            "던전 입장", 44,
            Theme?.btnPrimary, Theme?.iconSword,
            new Color(1f, 0.95f, 0.75f), new Color(0.45f, 0.28f, 0.08f),
            () =>
            {
                Debug.Log("[GameOptionsSceneManager] 던전 입장 Clicked.");
                if (GameManager.Instance != null) GameManager.Instance.StartNewGame();
                else SceneManager.LoadScene("GameScene");
            });

        // ── 환경 설정 (Secondary) ─────────────────────────────────────────
        CreateMenuButton(panel.transform, "SettingsButton",
            new Vector2(0f, -40f),   new Vector2(440f, 100f),
            "환경 설정", 38,
            Theme?.btnSecondary, Theme?.iconSettings,
            new Color(0.85f, 0.92f, 1f), new Color(0.15f, 0.2f, 0.35f),
            () =>
            {
                Debug.Log("[GameOptionsSceneManager] 환경설정 Clicked.");
                SceneManager.LoadScene("SettingsScene");
            });

        // ── 게임 이력 (Secondary) ─────────────────────────────────────────
        CreateMenuButton(panel.transform, "HistoryButton",
            new Vector2(0f, -160f),  new Vector2(440f, 100f),
            "게임 이력", 38,
            Theme?.btnSecondary, Theme?.iconStar,
            new Color(0.85f, 0.92f, 1f), new Color(0.15f, 0.2f, 0.35f),
            () =>
            {
                Debug.Log("[GameOptionsSceneManager] 게임 이력 Clicked.");
                SceneManager.LoadScene("GameHistoryScene");
            });

        EnsureEventSystem();
        Debug.Log(">>> [GameOptionsSceneManager] SetupUI Completed.");
    }

    // =====================================================================
    // UI 헬퍼
    // =====================================================================
    private Canvas EnsureCanvas()
    {
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas != null) return canvas;
        var go = new GameObject("Canvas");
        canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var sc = go.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);
        sc.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
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

    // 헤더 리본 (패널 상단)
    private void MakeHeader(Transform parent, string name, string title,
        Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        if (Theme?.headerRibbon != null)
        {
            img.sprite = Theme.headerRibbon; img.type = Image.Type.Sliced; img.color = Color.white;
        }
        else img.color = new Color(0.3f, 0.15f, 0.05f);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos; rt.sizeDelta = size;

        var txtGo = new GameObject("Title");
        txtGo.transform.SetParent(go.transform, false);
        var trt = txtGo.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.sizeDelta = Vector2.zero;
        var t = txtGo.AddComponent<Text>();
        t.text = title; t.font = GetFont(true); t.fontSize = 36;
        t.color = new Color(1f, 0.95f, 0.75f);
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow   = VerticalWrapMode.Overflow;
    }

    private void CreateMenuButton(Transform parent, string name,
        Vector2 pos, Vector2 size, string label, int fontSize,
        Sprite btnSprite, Sprite iconSprite, Color textColor, Color fallback,
        UnityEngine.Events.UnityAction action)
    {
        var btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        var rt = btnObj.AddComponent<RectTransform>();
        rt.anchoredPosition = pos; rt.sizeDelta = size;

        var img = btnObj.AddComponent<Image>();
        if (btnSprite != null) { img.sprite = btnSprite; img.type = Image.Type.Sliced; img.color = Color.white; }
        else                   { img.color = fallback; }

        var btn = btnObj.AddComponent<Button>();
        var cb  = btn.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = new Color(1f, 1f, 0.85f);
        cb.pressedColor     = new Color(0.8f, 0.75f, 0.55f);
        btn.colors = cb;
        btn.onClick.AddListener(action);

        // 아이콘
        float iconW = iconSprite != null ? 65f : 0f;
        if (iconSprite != null)
        {
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(btnObj.transform, false);
            var irt = iconGo.AddComponent<RectTransform>();
            irt.anchorMin = new Vector2(0f, 0.5f); irt.anchorMax = new Vector2(0f, 0.5f);
            irt.pivot     = new Vector2(0f, 0.5f);
            irt.anchoredPosition = new Vector2(20f, 0f);
            irt.sizeDelta = new Vector2(52f, 52f);
            var iimg = iconGo.AddComponent<Image>();
            iimg.sprite = iconSprite; iimg.preserveAspect = true;
            iimg.color  = textColor;
        }

        // 텍스트
        var txtGo = new GameObject("Label");
        txtGo.transform.SetParent(btnObj.transform, false);
        var trt = txtGo.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(iconW, 0f); trt.offsetMax = Vector2.zero;
        var t = txtGo.AddComponent<Text>();
        t.text = label; t.font = GetFont(true); t.fontSize = fontSize;
        t.color = textColor; t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow   = VerticalWrapMode.Overflow;
    }
}
