// ============================================================
// EscPopupHelper.cs
// ESC 키 확인 팝업(예/아니오)을 동적으로 생성하는 헬퍼.
// ============================================================
using System;
using UnityEngine;
using UnityEngine.UI;

public static class EscPopupHelper
{
    /// <summary>
    /// 확인 팝업을 Canvas 위에 생성하고 루트 GameObject를 반환.
    /// 예: onYes, 아니오: onNo (nullable) 를 클릭하면 팝업이 자동 제거됨.
    /// </summary>
    public static GameObject ShowPopup(Canvas canvas, Font font, string message,
        Action onYes, Action onNo = null)
    {
        // ── 전체 차단 반투명 오버레이 ─────────────────────────────────────
        var overlayGo = new GameObject("EscPopupOverlay");
        overlayGo.transform.SetParent(canvas.transform, false);
        var overlayImg = overlayGo.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.70f);
        var overlayRt = overlayGo.GetComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.sizeDelta = Vector2.zero;
        overlayGo.transform.SetAsLastSibling();

        // ── 팝업 패널 ───────────────────────────────────────────────────
        var panelGo = new GameObject("EscPopupPanel");
        panelGo.transform.SetParent(overlayGo.transform, false);
        var panelImg = panelGo.AddComponent<Image>();
        panelImg.color = new Color(0.08f, 0.06f, 0.14f, 0.97f);
        var panelRt = panelGo.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot     = new Vector2(0.5f, 0.5f);
        panelRt.anchoredPosition = Vector2.zero;
        panelRt.sizeDelta = new Vector2(560f, 230f);

        // ── 상단 구분선 (황금빛) ─────────────────────────────────────────
        var lineGo = new GameObject("TopLine");
        lineGo.transform.SetParent(panelGo.transform, false);
        var lineImg = lineGo.AddComponent<Image>();
        lineImg.color = new Color(0.85f, 0.65f, 0.15f, 1f);
        var lineRt = lineGo.GetComponent<RectTransform>();
        lineRt.anchorMin = new Vector2(0f, 1f);
        lineRt.anchorMax = new Vector2(1f, 1f);
        lineRt.pivot     = new Vector2(0.5f, 1f);
        lineRt.anchoredPosition = Vector2.zero;
        lineRt.sizeDelta = new Vector2(0f, 4f);

        // ── 메시지 텍스트 ────────────────────────────────────────────────
        var textGo = new GameObject("Message");
        textGo.transform.SetParent(panelGo.transform, false);
        var textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0f, 1f);
        textRt.anchorMax = new Vector2(1f, 1f);
        textRt.pivot     = new Vector2(0.5f, 1f);
        textRt.anchoredPosition = new Vector2(0f, -35f);
        textRt.sizeDelta = new Vector2(-40f, 75f);
        var t = textGo.AddComponent<Text>();
        t.text      = message;
        t.font      = font;
        t.fontSize  = 32;
        t.color     = new Color(1f, 0.95f, 0.80f);
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow   = VerticalWrapMode.Overflow;

        // ── 예 버튼 (왼쪽) ──────────────────────────────────────────────
        MakeButton(panelGo.transform, "YesButton", "예", font,
            new Vector2(-100f, -155f), new Vector2(160f, 62f),
            new Color(0.45f, 0.28f, 0.06f),
            () => { UnityEngine.Object.Destroy(overlayGo); onYes?.Invoke(); });

        // ── 아니오 버튼 (오른쪽) ────────────────────────────────────────
        MakeButton(panelGo.transform, "NoButton", "아니오", font,
            new Vector2(100f, -155f), new Vector2(160f, 62f),
            new Color(0.14f, 0.10f, 0.28f),
            () => { UnityEngine.Object.Destroy(overlayGo); onNo?.Invoke(); });

        return overlayGo;
    }

    // ── 내부 버튼 생성 ─────────────────────────────────────────────────────
    private static void MakeButton(Transform parent, string name, string label,
        Font font, Vector2 pos, Vector2 size, Color bgColor,
        UnityEngine.Events.UnityAction action)
    {
        var btnGo = new GameObject(name);
        btnGo.transform.SetParent(parent, false);
        var rt = btnGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var img = btnGo.AddComponent<Image>();
        img.color = bgColor;

        var btn = btnGo.AddComponent<Button>();
        var cb  = btn.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = new Color(1f, 1f, 0.80f);
        cb.pressedColor     = new Color(0.7f, 0.6f, 0.45f);
        btn.colors = cb;
        btn.onClick.AddListener(action);

        var txtGo = new GameObject("Label");
        txtGo.transform.SetParent(btnGo.transform, false);
        var trt = txtGo.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.sizeDelta = Vector2.zero;
        var t = txtGo.AddComponent<Text>();
        t.text      = label;
        t.font      = font;
        t.fontSize  = 28;
        t.color     = new Color(1f, 0.93f, 0.70f);
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow   = VerticalWrapMode.Overflow;
    }
}
