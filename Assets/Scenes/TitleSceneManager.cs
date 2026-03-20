using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TitleSceneManager : MonoBehaviour
{
    // ── UIKit 테마 (Resources/UIKitTheme.asset) ──────────────────────────
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

    void Start() => SetupTitleUI();

    // =====================================================================
    private void SetupTitleUI()
    {
        Debug.Log(">>> [TitleSceneManager] SetupTitleUI Started.");

        Canvas canvas = EnsureCanvas();

        // ── 배경 (완전 검정) ─────────────────────────────────────────────
        MakeStretch(canvas.transform, "Background", Color.black);

        // ── 제목 로고 이미지 ──────────────────────────────────────────────
        Sprite logo = Resources.Load<Sprite>("TitleLogo");
        if (logo != null)
        {
            var logoObj = MakeImage(canvas.transform, "Logo",
                new Vector2(0f, 140f), new Vector2(900f, 340f), Color.white, logo, true);
        }

        // ── 게임 시작 버튼 ────────────────────────────────────────────────
        Sprite startBtnSpr = Resources.Load<Sprite>("btn_start_game");
        CreateButtonWithIcon(canvas, "StartButton",
            pos: new Vector2(0f, -120f), size: new Vector2(480f, 130f),
            label: startBtnSpr != null ? "" : "게임 시작", fontSize: 46,
            sprite: startBtnSpr ?? Theme?.btnPrimary, iconSprite: null,
            textColor: new Color(1f, 0.95f, 0.8f),
            fallbackColor: new Color(0.45f, 0.28f, 0.08f),
            action: () => {
                Debug.Log("[TitleSceneManager] Start → GameOptionsScene");
                SceneManager.LoadScene("GameOptionsScene");
            });

        EnsureEventSystem();
        Debug.Log(">>> [TitleSceneManager] SetupTitleUI Completed.");
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

    private static GameObject MakeStretch(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = color;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
        return go;
    }

    private static GameObject MakeImage(Transform parent, string name,
        Vector2 pos, Vector2 size, Color color, Sprite sprite = null, bool preserve = false)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        if (sprite != null) { img.sprite = sprite; img.preserveAspect = preserve; }
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        return go;
    }

    private Text MakeText(Transform parent, string name, string text,
        Vector2 pos, Vector2 size, int fontSize, Color color,
        bool bold = false, TextAnchor anchor = TextAnchor.MiddleLeft)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        var t = go.AddComponent<Text>();
        t.text = text; t.font = GetFont(bold); t.fontSize = fontSize;
        t.color = color; t.alignment = anchor;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow   = VerticalWrapMode.Overflow;
        return t;
    }

    private void CreateButtonWithIcon(Canvas canvas, string name,
        Vector2 pos, Vector2 size, string label, int fontSize,
        Sprite sprite, Sprite iconSprite, Color textColor, Color fallbackColor,
        UnityEngine.Events.UnityAction action)
    {
        var btnObj = new GameObject(name);
        btnObj.transform.SetParent(canvas.transform, false);
        var rt = btnObj.AddComponent<RectTransform>();
        rt.anchoredPosition = pos; rt.sizeDelta = size;

        var img = btnObj.AddComponent<Image>();
        if (sprite != null) { img.sprite = sprite; img.type = Image.Type.Simple; img.preserveAspect = true; img.color = Color.white; }
        else                { img.color = fallbackColor; }

        var btn = btnObj.AddComponent<Button>();
        var cb  = btn.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = new Color(1f, 1f, 0.85f);
        cb.pressedColor     = new Color(0.8f, 0.7f, 0.5f);
        btn.colors = cb;
        btn.onClick.AddListener(action);

        // 아이콘
        float iconW = iconSprite != null ? 70f : 0f;
        if (iconSprite != null)
        {
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(btnObj.transform, false);
            var irt = iconGo.AddComponent<RectTransform>();
            irt.anchorMin = new Vector2(0f, 0.5f); irt.anchorMax = new Vector2(0f, 0.5f);
            irt.pivot     = new Vector2(0f, 0.5f);
            irt.anchoredPosition = new Vector2(24f, 0f);
            irt.sizeDelta = new Vector2(60f, 60f);
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
