using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SettingsSceneManager : MonoBehaviour
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
        Debug.Log(">>> [SettingsSceneManager] SetupUI Started.");
        Canvas canvas = EnsureCanvas();

        // ── 배경 ──────────────────────────────────────────────────────────
        MakeStretch(canvas.transform, "Background", Color.black);

        // ── 중앙 패널 ─────────────────────────────────────────────────────
        var panel = MakeSlicedPanel(canvas.transform, "MainPanel",
            new Vector2(0f, 0f), new Vector2(600f, 520f),
            Theme?.panelMain, new Color(0.12f, 0.09f, 0.05f, 0.97f));

        // ── 헤더 리본 + 설정 아이콘 ───────────────────────────────────────
        MakeHeaderWithIcon(panel.transform, "Header", "환 경 설 정",
            new Vector2(0f, 212f), new Vector2(500f, 90f), Theme?.iconSettings);

        // ── BGM 슬라이더 행 ───────────────────────────────────────────────
        CreateSliderRow(panel, "BGM_Row", "BGM 음량",
            new Vector2(0f, 80f), Theme?.iconMusicOn,
            GameManager.Instance?.Audio?.BgmVolume ?? 0.7f,
            val => GameManager.Instance?.Audio?.SetBgmVolume(val));

        // ── SFX 슬라이더 행 ───────────────────────────────────────────────
        CreateSliderRow(panel, "SFX_Row", "효과음 음량",
            new Vector2(0f, -50f), Theme?.iconSfxOn,
            GameManager.Instance?.Audio?.SfxVolume ?? 0.8f,
            val => GameManager.Instance?.Audio?.SetSfxVolume(val));

        // ── 돌아가기 버튼 ─────────────────────────────────────────────────
        CreateButton(panel.transform, "BackButton",
            new Vector2(0f, -185f), new Vector2(340f, 90f),
            "돌아가기", 36,
            Theme?.btnSecondary, Theme?.iconBack,
            new Color(0.8f, 0.88f, 1f), new Color(0.12f, 0.18f, 0.32f),
            () =>
            {
                Debug.Log("[SettingsSceneManager] 돌아가기 → GameOptionsScene");
                SceneManager.LoadScene("GameOptionsScene");
            });

        EnsureEventSystem();
        Debug.Log(">>> [SettingsSceneManager] SetupUI Completed.");
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

    private void MakeHeaderWithIcon(Transform parent, string name, string title,
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

        // 아이콘
        if (iconSprite != null)
        {
            var ig = new GameObject("Icon");
            ig.transform.SetParent(go.transform, false);
            var irt = ig.AddComponent<RectTransform>();
            irt.anchorMin = new Vector2(0f, 0.5f); irt.anchorMax = new Vector2(0f, 0.5f);
            irt.pivot     = new Vector2(0f, 0.5f);
            irt.anchoredPosition = new Vector2(14f, 0f);
            irt.sizeDelta = new Vector2(52f, 52f);
            var iimg = ig.AddComponent<Image>();
            iimg.sprite = iconSprite; iimg.preserveAspect = true;
            iimg.color  = new Color(1f, 0.92f, 0.65f);
        }

        var txtGo = new GameObject("Title");
        txtGo.transform.SetParent(go.transform, false);
        var trt = txtGo.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.sizeDelta = Vector2.zero;
        var t = txtGo.AddComponent<Text>();
        t.text = title; t.font = GetFont(true); t.fontSize = 34;
        t.color = new Color(1f, 0.95f, 0.75f);
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow   = VerticalWrapMode.Overflow;
    }

    // 슬라이더 행: 아이콘 + 레이블 + 슬라이더
    private void CreateSliderRow(RectTransform parent, string name, string label,
        Vector2 pos, Sprite iconSprite, float initialValue,
        UnityEngine.Events.UnityAction<float> onChange)
    {
        var rowGo = new GameObject(name);
        rowGo.transform.SetParent(parent, false);
        var rt = rowGo.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(520f, 60f);

        // 아이콘
        float cursorX = -240f;
        if (iconSprite != null)
        {
            var ig = new GameObject("Icon");
            ig.transform.SetParent(rowGo.transform, false);
            var irt = ig.AddComponent<RectTransform>();
            irt.anchoredPosition = new Vector2(cursorX + 20f, 0f); irt.sizeDelta = new Vector2(44f, 44f);
            var iimg = ig.AddComponent<Image>();
            iimg.sprite = iconSprite; iimg.preserveAspect = true;
            iimg.color  = new Color(1f, 0.92f, 0.65f);
        }

        // 레이블
        var lblGo = new GameObject("Label");
        lblGo.transform.SetParent(rowGo.transform, false);
        var lrt = lblGo.AddComponent<RectTransform>();
        lrt.anchoredPosition = new Vector2(-130f, 0f); lrt.sizeDelta = new Vector2(180f, 60f);
        var lbl = lblGo.AddComponent<Text>();
        lbl.text = label; lbl.font = GetFont(false); lbl.fontSize = 28;
        lbl.color = new Color(1f, 0.93f, 0.78f); lbl.alignment = TextAnchor.MiddleRight;
        lbl.horizontalOverflow = HorizontalWrapMode.Overflow;
        lbl.verticalOverflow   = VerticalWrapMode.Overflow;

        // 슬라이더
        var slGo = new GameObject("Slider");
        slGo.transform.SetParent(rowGo.transform, false);
        var slRt = slGo.AddComponent<RectTransform>();
        slRt.anchoredPosition = new Vector2(90f, 0f); slRt.sizeDelta = new Vector2(280f, 28f);
        MakeSliderVisual(slGo, initialValue, onChange);
    }

    private static void MakeSliderVisual(GameObject slGo, float init,
        UnityEngine.Events.UnityAction<float> onChange)
    {
        var bg = slGo.AddComponent<Image>();
        bg.color = new Color(0.18f, 0.13f, 0.07f);
        var slider = slGo.AddComponent<Slider>();
        slider.minValue = 0f; slider.maxValue = 1f; slider.value = init;

        // Fill
        var faGo = new GameObject("Fill Area");
        faGo.transform.SetParent(slGo.transform, false);
        var faRt = faGo.AddComponent<RectTransform>();
        faRt.anchorMin = Vector2.zero; faRt.anchorMax = Vector2.one;
        faRt.offsetMin = Vector2.zero; faRt.offsetMax = new Vector2(-10f, 0f);
        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(faGo.transform, false);
        var fillImg = fillGo.AddComponent<Image>();
        fillImg.color = new Color(0.68f, 0.45f, 0.12f);
        var fillRt = fillGo.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one; fillRt.sizeDelta = Vector2.zero;
        slider.fillRect = fillRt;

        // Handle
        var haGo = new GameObject("Handle Slide Area");
        haGo.transform.SetParent(slGo.transform, false);
        var haRt = haGo.AddComponent<RectTransform>();
        haRt.anchorMin = Vector2.zero; haRt.anchorMax = Vector2.one;
        haRt.offsetMin = new Vector2(8f, 0f); haRt.offsetMax = new Vector2(-8f, 0f);
        var hGo = new GameObject("Handle");
        hGo.transform.SetParent(haGo.transform, false);
        var hImg = hGo.AddComponent<Image>();
        hImg.color = new Color(1f, 0.88f, 0.55f);
        var hRt = hGo.GetComponent<RectTransform>();
        hRt.sizeDelta = new Vector2(22f, 0f);
        slider.handleRect = hRt;
        slider.targetGraphic = hImg;

        if (onChange != null) slider.onValueChanged.AddListener(onChange);
    }

    private void CreateButton(Transform parent, string name,
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
        cb.normalColor = Color.white; cb.highlightedColor = new Color(1f, 1f, 0.85f); cb.pressedColor = new Color(0.8f, 0.75f, 0.55f);
        btn.colors = cb;
        btn.onClick.AddListener(action);

        float iconW = iconSprite != null ? 56f : 0f;
        if (iconSprite != null)
        {
            var ig = new GameObject("Icon"); ig.transform.SetParent(btnObj.transform, false);
            var irt = ig.AddComponent<RectTransform>();
            irt.anchorMin = new Vector2(0f, 0.5f); irt.anchorMax = new Vector2(0f, 0.5f);
            irt.pivot = new Vector2(0f, 0.5f); irt.anchoredPosition = new Vector2(16f, 0f); irt.sizeDelta = new Vector2(44f, 44f);
            var iimg = ig.AddComponent<Image>(); iimg.sprite = iconSprite; iimg.preserveAspect = true; iimg.color = textColor;
        }

        var tGo = new GameObject("Label"); tGo.transform.SetParent(btnObj.transform, false);
        var trt = tGo.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(iconW, 0f); trt.offsetMax = Vector2.zero;
        var t = tGo.AddComponent<Text>();
        t.text = label; t.font = GetFont(true); t.fontSize = fontSize;
        t.color = textColor; t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
    }
}
