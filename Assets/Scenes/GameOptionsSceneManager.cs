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

        // ── 배경 이미지 ──────────────────────────────────────────────────
        {
            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(canvas.transform, false);
            Texture2D bgTex = Resources.Load<Texture2D>("TitleBackground");
            if (bgTex != null)
            {
                var rawImg = bgGo.AddComponent<RawImage>();
                rawImg.texture = bgTex;
                rawImg.color = Color.white;
            }
            else
            {
                var fallback = bgGo.AddComponent<Image>();
                fallback.color = Color.black;
                Debug.LogWarning("[GameOptionsSceneManager] TitleBackground 이미지를 찾을 수 없습니다. Assets/Resources/TitleBackground.png 경로를 확인하세요.");
            }
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.sizeDelta = Vector2.zero;
        }

        // ── 배경 위 어두운 오버레이 (UI 가독성 확보) ─────────────────────
        MakeStretch(canvas.transform, "DimOverlay", new Color(0f, 0f, 0f, 0.35f));

        // ── 버튼 패널 (우측 상단) ─────────────────────────────────────────
        // 1920×1080 기준: x=680(우측), y=300(상단)
        var panel = MakeSlicedPanel(canvas.transform, "MenuPanel",
            new Vector2(680f, 300f), new Vector2(380f, 300f),
            Theme?.panelDark, new Color(0.04f, 0.03f, 0.06f, 0.80f));

        // 공통 버튼 크기
        var btnSize = new Vector2(340f, 80f);

        // ── 던전 입장 (황금빛) ────────────────────────────────────────────
        CreateMenuButton(panel.transform, "DungeonButton",
            new Vector2(0f, 95f), btnSize,
            "던전 입장", 34,
            Theme?.btnPrimary, Theme?.iconSword,
            new Color(1f, 0.95f, 0.65f), new Color(0.50f, 0.30f, 0.05f),
            () =>
            {
                Debug.Log("[GameOptionsSceneManager] 던전 입장 Clicked.");
                if (GameManager.Instance != null) GameManager.Instance.StartNewGame();
                else SceneManager.LoadScene("GameScene");
            });

        // ── 게임 이력 (청록빛) ────────────────────────────────────────────
        CreateMenuButton(panel.transform, "HistoryButton",
            new Vector2(0f, 0f), btnSize,
            "게임 이력", 34,
            Theme?.btnSecondary, Theme?.iconStar,
            new Color(0.75f, 1f, 0.95f), new Color(0.05f, 0.28f, 0.30f),
            () =>
            {
                Debug.Log("[GameOptionsSceneManager] 게임 이력 Clicked.");
                SceneManager.LoadScene("GameHistoryScene");
            });

        // ── 환경 설정 (보랏빛) ────────────────────────────────────────────
        CreateMenuButton(panel.transform, "SettingsButton",
            new Vector2(0f, -95f), btnSize,
            "환경 설정", 34,
            Theme?.btnSecondary, Theme?.iconSettings,
            new Color(0.88f, 0.78f, 1f), new Color(0.18f, 0.08f, 0.35f),
            () =>
            {
                Debug.Log("[GameOptionsSceneManager] 환경설정 Clicked.");
                SceneManager.LoadScene("SettingsScene");
            });

        // ── 하단 대화창 ──────────────────────────────────────────────────────
        DialogueBoxUI.Create(canvas.transform, GetFont(), Theme);

        // ── 좌측 상단 각인 패널 ───────────────────────────────────────────
        BuildEngravingPanel(canvas.transform);

        // ── 사망 후 새 각인 해금 알림 ─────────────────────────────────────
        ShowNewEngravingNotification(canvas.transform);

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

    // =====================================================================
    // 각인 패널 (좌측 상단)
    // =====================================================================
    private void BuildEngravingPanel(Transform canvasTransform)
    {
        var em = GameManager.Instance?.Engraving;
        var unlocked = em != null ? em.Unlocked : new System.Collections.Generic.List<EngravingData>();

        // 루트 패널
        var panelGo = new GameObject("EngravingPanel");
        panelGo.transform.SetParent(canvasTransform, false);
        var panelImg = panelGo.AddComponent<Image>();
        var thm = Theme;
        if (thm != null && thm.panelDark != null)
        { panelImg.sprite = thm.panelDark; panelImg.type = Image.Type.Sliced; panelImg.color = Color.white; }
        else panelImg.color = new Color(0.05f, 0.03f, 0.08f, 0.90f);
        var panelRt = panelGo.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0f, 1f);
        panelRt.anchorMax = new Vector2(0f, 1f);
        panelRt.pivot     = new Vector2(0f, 1f);
        panelRt.anchoredPosition = new Vector2(20f, -20f);
        panelRt.sizeDelta = new Vector2(280f, unlocked.Count > 0 ? 50f + unlocked.Count * 42f : 110f);

        // 제목
        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(panelGo.transform, false);
        var titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f); titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot     = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -8f);
        titleRt.sizeDelta = new Vector2(0f, 34f);
        var titleTxt = titleGo.AddComponent<Text>();
        titleTxt.text = "슬레이어 각인";
        titleTxt.font = GetFont(true);
        titleTxt.fontSize = 22;
        titleTxt.color = new Color(1f, 0.85f, 0.4f);
        titleTxt.alignment = TextAnchor.MiddleCenter;
        titleTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
        titleTxt.verticalOverflow   = VerticalWrapMode.Overflow;

        if (unlocked.Count == 0)
        {
            var noneGo = new GameObject("NoEngravings");
            noneGo.transform.SetParent(panelGo.transform, false);
            var noneRt = noneGo.AddComponent<RectTransform>();
            noneRt.anchorMin = new Vector2(0f, 0f); noneRt.anchorMax = new Vector2(1f, 1f);
            noneRt.offsetMin = new Vector2(10f, 10f); noneRt.offsetMax = new Vector2(-10f, -44f);
            var noneTxt = noneGo.AddComponent<Text>();
            noneTxt.text = "아직 각인이 없습니다.\n던전을 클리어하면\n새겨질지도...";
            noneTxt.font = GetFont(false);
            noneTxt.fontSize = 16;
            noneTxt.color = new Color(0.6f, 0.55f, 0.7f);
            noneTxt.alignment = TextAnchor.MiddleCenter;
            noneTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            noneTxt.verticalOverflow   = VerticalWrapMode.Overflow;
        }
        else
        {
            for (int i = 0; i < unlocked.Count; i++)
            {
                var eng = unlocked[i];
                var rowGo = new GameObject($"Engraving_{i}");
                rowGo.transform.SetParent(panelGo.transform, false);
                var rowRt = rowGo.AddComponent<RectTransform>();
                rowRt.anchorMin = new Vector2(0f, 1f); rowRt.anchorMax = new Vector2(1f, 1f);
                rowRt.pivot     = new Vector2(0.5f, 1f);
                rowRt.anchoredPosition = new Vector2(0f, -(46f + i * 42f));
                rowRt.sizeDelta = new Vector2(-20f, 38f);
                var rowImg = rowGo.AddComponent<Image>();
                rowImg.color = new Color(0.12f, 0.08f, 0.18f, 0.85f);

                var rowTxt = new GameObject("RowText");
                rowTxt.transform.SetParent(rowGo.transform, false);
                var rtRt = rowTxt.AddComponent<RectTransform>();
                rtRt.anchorMin = Vector2.zero; rtRt.anchorMax = Vector2.one;
                rtRt.offsetMin = new Vector2(6f, 0f); rtRt.offsetMax = new Vector2(-6f, 0f);
                var txt = rowTxt.AddComponent<Text>();
                bool isDebuff = eng != null && eng.isDebuff;
                string prefix = isDebuff ? "<color=#FF8888>▼</color>" : "<color=#88FF88>▲</color>";
                txt.text = $"{prefix} {(eng != null ? eng.engravingName : "??")}";
                txt.font = GetFont(false);
                txt.fontSize = 18;
                txt.color = isDebuff ? new Color(1f, 0.7f, 0.7f) : new Color(0.7f, 1f, 0.75f);
                txt.alignment = TextAnchor.MiddleLeft;
                txt.supportRichText = true;
                txt.horizontalOverflow = HorizontalWrapMode.Overflow;
                txt.verticalOverflow   = VerticalWrapMode.Overflow;
            }
        }
    }

    // =====================================================================
    // 사망 후 새 각인 해금 알림
    // =====================================================================
    private void ShowNewEngravingNotification(Transform canvasTransform)
    {
        var gm = GameManager.Instance;
        // PendingEngravingRolled 가 true 일 때만 표시 (실패해도 팝업을 보여줘야 함)
        if (gm == null || !gm.PendingEngravingRolled) return;

        string newName = gm.PendingNewEngravingName;
        bool success   = !string.IsNullOrEmpty(newName);

        // 플래그 초기화 (한 번만 표시)
        gm.PendingEngravingRolled   = false;
        gm.PendingNewEngravingName  = null;

        // ── 전체화면 반투명 차단 오버레이 ─────────────────────────────────
        var overlayGo = new GameObject("EngravingOverlay");
        overlayGo.transform.SetParent(canvasTransform, false);
        var overlayImg = overlayGo.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.72f);
        var overlayRt = overlayGo.GetComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero; overlayRt.anchorMax = Vector2.one; overlayRt.sizeDelta = Vector2.zero;
        overlayGo.transform.SetAsLastSibling();

        // 클릭 차단용 Raycaster 추가
        overlayGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // ── 중앙 팝업 박스 ────────────────────────────────────────────────
        var boxGo = new GameObject("EngravingBox");
        boxGo.transform.SetParent(overlayGo.transform, false);
        var boxImg = boxGo.AddComponent<Image>();
        boxImg.color = new Color(0.04f, 0.02f, 0.08f, 0.97f);
        var boxRt = boxGo.GetComponent<RectTransform>();
        boxRt.anchorMin = new Vector2(0.5f, 0.5f); boxRt.anchorMax = new Vector2(0.5f, 0.5f);
        boxRt.pivot     = new Vector2(0.5f, 0.5f);
        boxRt.anchoredPosition = Vector2.zero;
        boxRt.sizeDelta = new Vector2(540f, 320f);

        // ── 테두리 강조 ───────────────────────────────────────────────────
        var borderGo = new GameObject("Border");
        borderGo.transform.SetParent(boxGo.transform, false);
        var borderImg = borderGo.AddComponent<Image>();
        borderImg.color = success ? new Color(0.8f, 0.6f, 1f, 0.7f) : new Color(0.4f, 0.35f, 0.5f, 0.5f);
        var borderRt = borderGo.GetComponent<RectTransform>();
        borderRt.anchorMin = Vector2.zero; borderRt.anchorMax = Vector2.one;
        borderRt.offsetMin = new Vector2(-3f, -3f); borderRt.offsetMax = new Vector2(3f, 3f);
        borderGo.transform.SetAsFirstSibling();

        // ── 1단계 텍스트: "당신의 업보가 정산되고있다..." ─────────────────
        var phase1Go = new GameObject("Phase1");
        phase1Go.transform.SetParent(boxGo.transform, false);
        var p1Rt = phase1Go.AddComponent<RectTransform>();
        p1Rt.anchorMin = Vector2.zero; p1Rt.anchorMax = Vector2.one; p1Rt.sizeDelta = Vector2.zero;
        var p1TxtGo = new GameObject("Txt1");
        p1TxtGo.transform.SetParent(phase1Go.transform, false);
        var p1TxtRt = p1TxtGo.AddComponent<RectTransform>();
        p1TxtRt.anchorMin = Vector2.zero; p1TxtRt.anchorMax = Vector2.one;
        p1TxtRt.offsetMin = new Vector2(20f, 20f); p1TxtRt.offsetMax = new Vector2(-20f, -20f);
        var p1Txt = p1TxtGo.AddComponent<Text>();
        p1Txt.text      = "당신의 업보가\n정산되고있다...";
        p1Txt.font      = GetFont(true);
        p1Txt.fontSize  = 38;
        p1Txt.color     = new Color(0.85f, 0.75f, 1f);
        p1Txt.alignment = TextAnchor.MiddleCenter;
        p1Txt.supportRichText = true;
        p1Txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        p1Txt.verticalOverflow   = VerticalWrapMode.Overflow;

        // ── 2단계: 결과 패널 (초기 숨김) ─────────────────────────────────
        var phase2Go = new GameObject("Phase2");
        phase2Go.transform.SetParent(boxGo.transform, false);
        var p2Rt = phase2Go.AddComponent<RectTransform>();
        p2Rt.anchorMin = Vector2.zero; p2Rt.anchorMax = Vector2.one; p2Rt.sizeDelta = Vector2.zero;
        phase2Go.SetActive(false);

        // 결과 텍스트 (성공: 각인 이름, 실패: "아무 일도 없었다")
        var resultTxtGo = new GameObject("ResultTxt");
        resultTxtGo.transform.SetParent(phase2Go.transform, false);
        var rTxtRt = resultTxtGo.AddComponent<RectTransform>();
        rTxtRt.anchorMin = Vector2.zero; rTxtRt.anchorMax = new Vector2(1f, 1f);
        rTxtRt.offsetMin = new Vector2(20f, 70f); rTxtRt.offsetMax = new Vector2(-20f, -20f);
        var rTxt = resultTxtGo.AddComponent<Text>();

        if (success)
        {
            rTxt.text = $"<color=#FFD700>★</color>  각인이 새겨졌다!\n\n<size=32><color=#E8C0FF>{newName}</color></size>";
        }
        else
        {
            rTxt.text = "<color=#888888>아무 일도 없었다...</color>";
        }
        rTxt.font        = GetFont(true);
        rTxt.fontSize    = 28;
        rTxt.color       = Color.white;
        rTxt.alignment   = TextAnchor.MiddleCenter;
        rTxt.supportRichText = true;
        rTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
        rTxt.verticalOverflow   = VerticalWrapMode.Overflow;

        // 확인 버튼
        var confirmGo = new GameObject("ConfirmBtn");
        confirmGo.transform.SetParent(phase2Go.transform, false);
        var confirmImg = confirmGo.AddComponent<Image>();
        confirmImg.color = success
            ? new Color(0.35f, 0.15f, 0.55f, 0.95f)
            : new Color(0.20f, 0.18f, 0.24f, 0.95f);
        var confirmRt = confirmGo.GetComponent<RectTransform>();
        confirmRt.anchorMin = new Vector2(0.5f, 0f); confirmRt.anchorMax = new Vector2(0.5f, 0f);
        confirmRt.pivot     = new Vector2(0.5f, 0f);
        confirmRt.anchoredPosition = new Vector2(0f, 16f);
        confirmRt.sizeDelta = new Vector2(200f, 52f);
        var confirmBtn = confirmGo.AddComponent<Button>();
        confirmBtn.onClick.AddListener(() => Destroy(overlayGo));
        var confirmTxtGo = new GameObject("BtnTxt");
        confirmTxtGo.transform.SetParent(confirmGo.transform, false);
        var ctRt = confirmTxtGo.AddComponent<RectTransform>();
        ctRt.anchorMin = Vector2.zero; ctRt.anchorMax = Vector2.one; ctRt.sizeDelta = Vector2.zero;
        var ctTxt = confirmTxtGo.AddComponent<Text>();
        ctTxt.text      = "확인";
        ctTxt.font      = GetFont(true);
        ctTxt.fontSize  = 26;
        ctTxt.color     = Color.white;
        ctTxt.alignment = TextAnchor.MiddleCenter;
        ctTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
        ctTxt.verticalOverflow   = VerticalWrapMode.Overflow;

        // ── 코루틴: 1.8초 후 phase1 → phase2 전환 ────────────────────────
        StartCoroutine(EngravingRevealSequence(phase1Go, phase2Go));
    }

    private System.Collections.IEnumerator EngravingRevealSequence(
        GameObject phase1, GameObject phase2)
    {
        yield return new WaitForSeconds(1.8f);
        if (phase1 != null) phase1.SetActive(false);
        if (phase2 != null) phase2.SetActive(true);
    }

    // =====================================================================
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
