// ============================================================
// ItemSlotUI.cs
// 메인 인벤토리 슬롯에 붙는 UI 핸들러.
// 드래그 & 드롭 이동, 마우스오버 툴팁, 클릭(사용/버리기)을 처리한다.
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

/// <summary>
/// 아이템 인벤토리 슬롯 하나에 붙는 컴포넌트.
/// 인덱스는 col + row * totalCols 방식으로 계산.
/// </summary>
public class ItemSlotUI : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerClickHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    // ── 슬롯 좌표 ─────────────────────────────────────────────────────────────
    public int col;
    public int row;

    // ── 콜백 ─────────────────────────────────────────────────────────────────
    /// <summary>좌클릭 → 아이템 사용 호출 (col, row)</summary>
    public Action<int, int> OnUse;
    /// <summary>우클릭 → 아이템 버리기 호출 (col, row)</summary>
    public Action<int, int> OnDiscard;
    /// <summary>드롭 → 이동 호출 (fromCol, fromRow, toCol, toRow)</summary>
    public Action<int, int, int, int> OnDropped;
    /// <summary>마우스오버 시 표시할 텍스트 반환 함수</summary>
    public Func<string> GetTooltip;    /// <summary>마우스오버 툴팁 카드 상단에 표시할 아이콘 스프라이트 반환 함수</summary>
    public Func<Sprite> GetTooltipIcon;
    /// <summary>마우스오버 툴팁 카드 타이틀 바에 표시할 이름 반환 함수</summary>
    public Func<string> GetTooltipTitle;
    // ── 공유 드래그 상태 (static) ─────────────────────────────────────────────
    private static GameObject _dragProxy;
    private static ItemSlotUI _draggingSlot;
    private static Canvas     _rootCanvas;

    // ── 공유 툴팁 (static) ────────────────────────────────────────────────────
    private static GameObject    _tooltip;
    private static Text          _tooltipText;
    private static GameObject    _tooltipTitleBar;  // 타이틀 바 (카드 최상단)
    private static Text          _tooltipTitleText; // 타이틀 바 텍스트
    private static GameObject    _tooltipIconBg;    // 아이콘 배경 영역
    private static Image         _tooltipIconImg;   // 아이콘 이미지
    private static RectTransform _tooltipIconBgRt;  // 아이콘 영역 RectTransform (위치 동적 조정)
    private static RectTransform _tooltipTextRt;    // 텍스트 RectTransform (offset 동적 조정)

    // ====================================================================
    // 공유 UI 초기화 / 정리 (패널 빌드 시 호출)
    // ====================================================================
    public static void EnsureSharedUI(Canvas canvas, Font font)
    {
        _rootCanvas = canvas;

        if (_tooltip == null)
        {
            // ── 카드 외부틀 ─────────────────────────────────────────────────
            _tooltip = new GameObject("__SlotTooltip");
            _tooltip.transform.SetParent(canvas.transform, false);

            var bg = _tooltip.AddComponent<Image>();
            bg.color = new Color(0.07f, 0.05f, 0.10f, 0.97f);

            var outline = _tooltip.AddComponent<Outline>();
            outline.effectColor    = new Color(0.75f, 0.60f, 0.22f, 1f);
            outline.effectDistance = new Vector2(2.5f, -2.5f);

            var rt = _tooltip.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(360f, 120f);

            // ── 타이틀 바 ──────────────────────────────────────────────────
            var titleBar = new GameObject("TitleBar");
            titleBar.transform.SetParent(_tooltip.transform, false);
            var titleBarImg = titleBar.AddComponent<Image>();
            titleBarImg.color = new Color(0.16f, 0.11f, 0.22f, 1f);
            var titleBarRt = titleBar.GetComponent<RectTransform>();
            titleBarRt.anchorMin = new Vector2(0f, 1f);
            titleBarRt.anchorMax = new Vector2(1f, 1f);
            titleBarRt.pivot     = new Vector2(0.5f, 1f);
            titleBarRt.anchoredPosition = Vector2.zero;
            titleBarRt.sizeDelta = new Vector2(0f, 44f);
            // 타이틀 Outline
            var titleBarOutline = titleBar.AddComponent<Outline>();
            titleBarOutline.effectColor = new Color(0.6f, 0.45f, 0.12f, 0.8f);
            titleBarOutline.effectDistance = new Vector2(0f, -1.5f);

            var titleTextGo = new GameObject("TitleText");
            titleTextGo.transform.SetParent(titleBar.transform, false);
            _tooltipTitleText = titleTextGo.AddComponent<Text>();
            _tooltipTitleText.font       = font ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _tooltipTitleText.fontSize   = 19;
            _tooltipTitleText.fontStyle  = FontStyle.Bold;
            _tooltipTitleText.color      = new Color(1f, 0.88f, 0.50f);
            _tooltipTitleText.alignment  = TextAnchor.MiddleCenter;
            _tooltipTitleText.supportRichText    = false;
            _tooltipTitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _tooltipTitleText.verticalOverflow   = VerticalWrapMode.Overflow;
            var titleTextRt = titleTextGo.GetComponent<RectTransform>();
            titleTextRt.anchorMin = Vector2.zero; titleTextRt.anchorMax = Vector2.one;
            titleTextRt.offsetMin = new Vector2(8f, 2f); titleTextRt.offsetMax = new Vector2(-8f, -2f);
            _tooltipTitleBar = titleBar;
            titleBar.SetActive(false);

            // ── 아이콘 배경 영역 ────────────────────────────────────────────
            var iconBg = new GameObject("IconBg");
            iconBg.transform.SetParent(_tooltip.transform, false);
            var iconBgImg = iconBg.AddComponent<Image>();
            iconBgImg.color = new Color(0.11f, 0.08f, 0.16f, 0.97f);
            var iconBgRt = iconBg.GetComponent<RectTransform>();
            iconBgRt.anchorMin = new Vector2(0f, 1f);
            iconBgRt.anchorMax = new Vector2(1f, 1f);
            iconBgRt.pivot     = new Vector2(0.5f, 1f);
            iconBgRt.anchoredPosition = Vector2.zero;
            iconBgRt.sizeDelta = new Vector2(0f, 110f);
            _tooltipIconBgRt = iconBgRt;

            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(iconBg.transform, false);
            _tooltipIconImg = iconGo.AddComponent<Image>();
            _tooltipIconImg.preserveAspect = true;
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.pivot     = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = Vector2.zero;
            iconRt.sizeDelta = new Vector2(82f, 82f);
            _tooltipIconBg = iconBg;
            iconBg.SetActive(false);

            // ── 본문 텍스트 영역 ────────────────────────────────────────────
            var child = new GameObject("Text");
            child.transform.SetParent(_tooltip.transform, false);
            _tooltipText = child.AddComponent<Text>();
            _tooltipText.font            = font ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _tooltipText.fontSize        = 16;
            _tooltipText.color           = new Color(0.95f, 0.90f, 0.78f);
            _tooltipText.alignment       = TextAnchor.UpperLeft;
            _tooltipText.supportRichText = true;
            _tooltipText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _tooltipText.verticalOverflow   = VerticalWrapMode.Overflow;
            var trt = child.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(14f, 10f); trt.offsetMax = new Vector2(-14f, -10f);
            _tooltipTextRt = trt;

            _tooltip.SetActive(false);
            _tooltip.transform.SetAsLastSibling();
        }
        else
        {
            // 이미 생성된 경우 폰트만 갱신
            if (font != null)
            {
                if (_tooltipText     != null) _tooltipText.font     = font;
                if (_tooltipTitleText != null) _tooltipTitleText.font = font;
            }
        }

        _rootCanvas = canvas;
    }

    // ── 툴팁 위치 계산: 슬롯 바로 아래, 화면 경계 클램핑 ───────────────────────
    private static void PositionTooltipBelowSlot(RectTransform slotRt)
    {
        if (_tooltip == null || _rootCanvas == null || slotRt == null) return;

        var trt      = _tooltip.GetComponent<RectTransform>();
        var canvasRt = _rootCanvas.GetComponent<RectTransform>();
        float halfW  = canvasRt.rect.width  * 0.5f;
        float halfH  = canvasRt.rect.height * 0.5f;
        float tipW   = trt.sizeDelta.x;
        float tipH   = trt.sizeDelta.y;
        const float gap    = 8f;
        const float margin = 6f;

        // 슬롯의 4개 월드 코너 (Screen-Space Overlay에서 world = screen pixels)
        Vector3[] corners = new Vector3[4];
        slotRt.GetWorldCorners(corners);
        // corners[0]=BL, [1]=TL, [2]=TR, [3]=BR

        // 슬롯 하단 중심 / 상단 중심 → 캔버스 로컬 좌표로 변환
        Vector2 botScreen = ((Vector2)corners[0] + (Vector2)corners[3]) * 0.5f;
        Vector2 topScreen = ((Vector2)corners[1] + (Vector2)corners[2]) * 0.5f;
        Vector2 botLocal, topLocal;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRt, botScreen, _rootCanvas.worldCamera, out botLocal);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRt, topScreen, _rootCanvas.worldCamera, out topLocal);

        // 기본: 슬롯 아래에 표시 (pivot = top-center)
        // anchoredPosition = canvasLocal + (halfW, halfH)  [anchor=BL 이므로]
        float lx = botLocal.x;
        float ly = botLocal.y - gap;
        float anchoredY = ly + halfH;

        // 하단 클리핑 체크: 툴팁 하단이 캔버스 밖으로 나가면 슬롯 위에 표시
        if (anchoredY - tipH < margin)
        {
            ly = topLocal.y + gap + tipH;   // 슬롯 위 → pivot(top-center) 위치
            anchoredY = ly + halfH;
        }

        // 상단 클리핑 체크
        if (anchoredY > canvasRt.rect.height - margin)
            anchoredY = canvasRt.rect.height - margin;

        // 수평 중앙 정렬 + 좌우 경계 클램핑
        float anchoredX = lx + halfW;
        anchoredX = Mathf.Clamp(anchoredX, tipW * 0.5f + margin,
                                            canvasRt.rect.width - tipW * 0.5f - margin);

        trt.anchoredPosition = new Vector2(anchoredX, anchoredY);
    }

    /// <summary>풀 카드형 툴팁 — 타이틀 바 + 아이콘 영역 + 본문.</summary>
    public static void ShowTooltip(Sprite icon, string title, string bodyText, RectTransform slotRt = null)
    {
        if (_tooltip == null || _tooltipText == null) return;

        // ── 타이틀 바 ──────────────────────────────────────────────────────
        bool hasTitle = !string.IsNullOrEmpty(title);
        float titleH  = 0f;
        if (_tooltipTitleBar != null)
        {
            _tooltipTitleBar.SetActive(hasTitle);
            if (hasTitle && _tooltipTitleText != null)
                _tooltipTitleText.text = title;
            titleH = hasTitle ? 44f : 0f;
        }

        // ── 아이콘 영역 ────────────────────────────────────────────────────
        bool hasIcon = icon != null && _tooltipIconBg != null;
        if (_tooltipIconBg != null) _tooltipIconBg.SetActive(hasIcon);
        if (hasIcon && _tooltipIconImg != null) _tooltipIconImg.sprite = icon;
        float iconH = hasIcon ? 110f : 0f;
        // 아이콘 위치 = 타이틀 바 바로 아래
        if (_tooltipIconBgRt != null)
            _tooltipIconBgRt.anchoredPosition = new Vector2(0f, -titleH);

        // ── 본문 텍스트 ────────────────────────────────────────────────────
        string text = bodyText ?? "";
        _tooltipText.text = text;
        int lines = 0;
        foreach (char c in text) if (c == '\n') lines++;
        lines++;
        float textH = 12f + lines * 24f + 12f;
        // 텍스트 영역 offsetMax: 타이틀 + 아이콘 높이 아래에서 시작
        if (_tooltipTextRt != null)
            _tooltipTextRt.offsetMax = new Vector2(-14f, -(titleH + iconH + 10f));

        var trt = _tooltip.GetComponent<RectTransform>();
        trt.sizeDelta = new Vector2(360f, titleH + iconH + textH);
        if (slotRt != null) PositionTooltipBelowSlot(slotRt);
        _tooltip.transform.SetAsLastSibling();
        _tooltip.SetActive(true);
    }

    /// <summary>아이콘 + 본문(타이틀 없음) 오버로드.</summary>
    public static void ShowTooltip(Sprite icon, string text, RectTransform slotRt = null)
        => ShowTooltip(icon, null, text, slotRt);

    /// <summary>텍스트 전용 툴팁 표시 (하위 호환).</summary>
    public static void ShowTooltip(string text, RectTransform slotRt = null)
        => ShowTooltip(null, null, text, slotRt);

    /// <summary>외부에서 툴팁을 숨긴다.</summary>
    public static void HideTooltip()
    {
        if (_tooltip != null) _tooltip.SetActive(false);
    }

    /// <summary>패널이 파괴될 때 공유 UI를 정리한다.</summary>
    public static void DestroySharedUI()
    {
        if (_tooltip != null)
        {
            Destroy(_tooltip);
            _tooltip = null; _tooltipText = null;
            _tooltipTitleBar = null; _tooltipTitleText = null;
            _tooltipIconBg = null; _tooltipIconImg = null; _tooltipIconBgRt = null;
            _tooltipTextRt = null;
        }
        if (_dragProxy != null) { Destroy(_dragProxy); _dragProxy = null; }
        _draggingSlot = null;
    }

    // ====================================================================
    // IPointerEnterHandler / IPointerExitHandler (툴팁)
    // ====================================================================
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_tooltip == null) return;
        string title = GetTooltipTitle?.Invoke() ?? "";
        string tip   = GetTooltip?.Invoke() ?? "";
        Sprite icon  = GetTooltipIcon?.Invoke();
        if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(tip) && icon == null) return;
        ShowTooltip(icon, title, tip, GetComponent<RectTransform>());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltip();
    }

    // ====================================================================
    // IPointerClickHandler (좌클릭 = 사용, 우클릭 = 버리기)
    // ====================================================================
    public void OnPointerClick(PointerEventData eventData)
    {
        if (_draggingSlot != null) return; // 드래그 중에는 무시

        if (eventData.button == PointerEventData.InputButton.Left)
            OnUse?.Invoke(col, row);
        else if (eventData.button == PointerEventData.InputButton.Right)
            OnDiscard?.Invoke(col, row);
    }

    // ====================================================================
    // Drag & Drop
    // ====================================================================
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_rootCanvas == null) return;

        if (_tooltip != null) _tooltip.SetActive(false);

        _draggingSlot = this;

        // 드래그 프록시 생성 (마우스 따라다니는 아이콘 이미지)
        _dragProxy = new GameObject("__DragProxy");
        _dragProxy.transform.SetParent(_rootCanvas.transform, false);

        var proxyImg = _dragProxy.AddComponent<Image>();
        proxyImg.raycastTarget = false;

        // 원본 슬롯의 자식 이미지에서 아이콘 스프라이트 복사
        var children = GetComponentsInChildren<Image>(true);
        foreach (var img in children)
        {
            if (img.gameObject != gameObject && img.sprite != null)
            {
                proxyImg.sprite = img.sprite;
                proxyImg.preserveAspect = true;
                proxyImg.color = new Color(1f, 1f, 1f, 0.75f);
                break;
            }
        }
        if (proxyImg.sprite == null)
            proxyImg.color = new Color(1f, 1f, 0.3f, 0.6f);

        var rt = _dragProxy.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.zero;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(60f, 60f);
        _dragProxy.transform.SetAsLastSibling();

        // 원본 슬롯 반투명 처리
        var selfImg = GetComponent<Image>();
        if (selfImg) selfImg.color = new Color(selfImg.color.r, selfImg.color.g, selfImg.color.b, 0.4f);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_dragProxy == null || _rootCanvas == null) return;

        Vector2 localPt;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rootCanvas.GetComponent<RectTransform>(),
            eventData.position,
            _rootCanvas.worldCamera,
            out localPt);
        _dragProxy.GetComponent<RectTransform>().anchoredPosition = localPt;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_dragProxy != null) { Destroy(_dragProxy); _dragProxy = null; }

        // 원본 슬롯 불투명 복구
        var selfImg = GetComponent<Image>();
        if (selfImg)
        {
            var c = selfImg.color;
            selfImg.color = new Color(c.r, c.g, c.b, 1f);
        }

        _draggingSlot = null;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (_draggingSlot == null || _draggingSlot == this) return;

        // 이동 콜백 실행
        OnDropped?.Invoke(_draggingSlot.col, _draggingSlot.row, col, row);

        // 드래그 정리
        if (_dragProxy != null) { Destroy(_dragProxy); _dragProxy = null; }
        _draggingSlot = null;
    }

    private void OnDestroy()
    {
        // 이 컴포넌트가 파괴될 때 드래그 중이었다면 정리
        if (_draggingSlot == this)
        {
            if (_dragProxy != null) { Destroy(_dragProxy); _dragProxy = null; }
            _draggingSlot = null;
        }
        if (_tooltip != null) _tooltip.SetActive(false);
    }
}
