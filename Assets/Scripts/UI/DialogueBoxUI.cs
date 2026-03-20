// ============================================================
// DialogueBoxUI.cs
// 대기화면 하단 대화창 UI
//
// [레이아웃]
//   ┌─────────────────────────────────────────────────────────┐
//   │ ┌──────┐  여관주인                                       │
//   │ │ 얼굴 │  대사가 여기에 타이핑 애니메이션으로 한 자씩...  │
//   │ │ 이미지│  두 번째 줄도 여기에 표시됩니다.               │
//   │ └──────┘  (스크롤 가능 · 최대 2줄 동시 표시)             │
//   └─────────────────────────────────────────────────────────┘
//
// [동작]
//   - 좌측 정사각형: 현재 대사의 캐릭터 표정 스프라이트
//   - 우측: 캐릭터 이름 + 스크롤 가능한 대사 텍스트
//   - 타이핑 애니메이션 (한 자씩), 클릭/스페이스로 스킵 가능
//   - 한 줄 완료 후 다음 화자로 자동 전환
//   - 마우스 휠로 스크롤 가능. 타이핑 중에는 자동으로 하단 고정
//
// [사용법]
//   DialogueBoxUI.Create(canvasTransform, font, theme);
//   → DialogueManager.GetDialogueSetToPlay() 를 자동 호출
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DialogueBoxUI : MonoBehaviour
{
    // ── 레이아웃 상수 ─────────────────────────────────────────────────────────
    private const float BOX_HEIGHT    = 230f;   // 대화창 전체 높이
    private const float FACE_SIZE     = 150f;   // 얼굴 이미지 한 변 길이
    private const float FACE_PAD_L    = 18f;    // 얼굴 왼쪽 여백
    private const float H_PAD         = 16f;    // 가로 여백
    private const float V_PAD         = 14f;    // 세로 여백
    private const int   FONT_SIZE     = 30;     // 대사 폰트 크기
    private const float LINE_H        = 42f;    // 1행 높이 (폰트 크기 기준)
    private const int   VISIBLE_LINES = 3;      // 동시 표시 최대 줄 수
    private const float NAME_H        = 34f;    // 이름 라벨 높이
    private const float CHAR_INTERVAL = 0.04f;  // 타이핑 속도 (초/문자)

    // ── 런타임 UI 참조 ────────────────────────────────────────────────────────
    private Image      _faceImage;
    private Text       _nameText;
    private Text       _dialogueText;
    private ScrollRect _scrollRect;
    private RectTransform _contentRect;
    private Text       _nextLabel;
    private Font       _font;

    // ── 상태 ──────────────────────────────────────────────────────────────────
    private bool _isTyping       = false;
    private bool _skipRequested  = false;
    private bool _waitingForNext = false;
    private bool _nextRequested  = false;

    // ── 공개 팩토리 메서드 ────────────────────────────────────────────────────
    /// <summary>
    /// 대화창 UI 를 생성하고 재생을 시작합니다.
    /// DialogueManager.GetDialogueSetToPlay() 로 재생할 세트를 자동 선택합니다.
    /// </summary>
    public static DialogueBoxUI Create(Transform canvasRoot, Font font, UIKitTheme theme)
    {
        var go  = new GameObject("DialogueBoxController");
        var ui  = go.AddComponent<DialogueBoxUI>();
        ui._font = font;
        ui.BuildUI(canvasRoot, theme);

        var set = DialogueManager.GetDialogueSetToPlay();
        if (set != null && set.lines != null && set.lines.Count > 0)
            ui.StartCoroutine(ui.PlaySet(set));

        return ui;
    }

    // ── UI 구성 ───────────────────────────────────────────────────────────────
    private void BuildUI(Transform canvasRoot, UIKitTheme theme)
    {
        // ── 메인 패널 (하단 전폭) ──────────────────────────────────────────
        var panel   = new GameObject("DialogueBox");
        panel.transform.SetParent(canvasRoot, false);
        panel.transform.SetAsLastSibling();  // 최상위 렌더링

        var panelImg = panel.AddComponent<Image>();
        if (theme?.panelDark != null)
        {
            panelImg.sprite = theme.panelDark;
            panelImg.type   = Image.Type.Sliced;
            panelImg.color  = new Color(1f, 1f, 1f, 0.88f);
        }
        else
        {
            panelImg.color  = new Color(0.04f, 0.03f, 0.07f, 0.82f);
        }

        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin        = new Vector2(0f, 0f);
        panelRt.anchorMax        = new Vector2(1f, 0f);
        panelRt.pivot            = new Vector2(0.5f, 0f);
        panelRt.anchoredPosition = Vector2.zero;
        panelRt.sizeDelta        = new Vector2(0f, BOX_HEIGHT);

        // ── 얼굴 테두리 (황금빛) ───────────────────────────────────────────
        var borderGo  = new GameObject("FaceBorder");
        borderGo.transform.SetParent(panel.transform, false);
        var borderImg = borderGo.AddComponent<Image>();
        borderImg.color = new Color(0.62f, 0.50f, 0.28f, 1f);
        var borderRt  = borderGo.GetComponent<RectTransform>();
        borderRt.anchorMin        = new Vector2(0f, 0.5f);
        borderRt.anchorMax        = new Vector2(0f, 0.5f);
        borderRt.pivot            = new Vector2(0f, 0.5f);
        borderRt.anchoredPosition = new Vector2(FACE_PAD_L - 3f, 0f);
        borderRt.sizeDelta        = new Vector2(FACE_SIZE + 6f, FACE_SIZE + 6f);

        // ── 얼굴 이미지 ────────────────────────────────────────────────────
        var faceGo  = new GameObject("FaceImage");
        faceGo.transform.SetParent(panel.transform, false);
        _faceImage              = faceGo.AddComponent<Image>();
        _faceImage.color        = Color.white;
        _faceImage.preserveAspect = false;
        var faceRt = faceGo.GetComponent<RectTransform>();
        faceRt.anchorMin        = new Vector2(0f, 0.5f);
        faceRt.anchorMax        = new Vector2(0f, 0.5f);
        faceRt.pivot            = new Vector2(0f, 0.5f);
        faceRt.anchoredPosition = new Vector2(FACE_PAD_L, 0f);
        faceRt.sizeDelta        = new Vector2(FACE_SIZE, FACE_SIZE);

        // ── 텍스트 영역 (얼굴 오른쪽) ────────────────────────────────────
        float textLeft = FACE_PAD_L + FACE_SIZE + H_PAD;

        var textArea   = new GameObject("TextArea");
        textArea.transform.SetParent(panel.transform, false);
        var textAreaRt = textArea.AddComponent<RectTransform>();
        textAreaRt.anchorMin = Vector2.zero;
        textAreaRt.anchorMax = Vector2.one;
        textAreaRt.offsetMin = new Vector2(textLeft, V_PAD);
        textAreaRt.offsetMax = new Vector2(-H_PAD, -V_PAD);

        // ── 캐릭터 이름 라벨 ─────────────────────────────────────────────
        var nameGo  = new GameObject("NameLabel");
        nameGo.transform.SetParent(textArea.transform, false);
        _nameText            = nameGo.AddComponent<Text>();
        _nameText.font       = _font;
        _nameText.fontSize   = 26;
        _nameText.fontStyle  = FontStyle.Bold;
        _nameText.color      = new Color(1f, 0.88f, 0.50f);
        _nameText.alignment  = TextAnchor.MiddleLeft;
        _nameText.horizontalOverflow = HorizontalWrapMode.Overflow;
        _nameText.verticalOverflow   = VerticalWrapMode.Overflow;
        var nameRt           = nameGo.GetComponent<RectTransform>();
        nameRt.anchorMin     = new Vector2(0f, 1f);
        nameRt.anchorMax     = new Vector2(1f, 1f);
        nameRt.pivot         = new Vector2(0f, 1f);
        nameRt.anchoredPosition = Vector2.zero;
        nameRt.sizeDelta     = new Vector2(0f, NAME_H);

        // ── 구분선 ────────────────────────────────────────────────────────
        var divGo  = new GameObject("Divider");
        divGo.transform.SetParent(textArea.transform, false);
        var divImg = divGo.AddComponent<Image>();
        divImg.color = new Color(0.55f, 0.45f, 0.25f, 0.6f);
        var divRt  = divGo.GetComponent<RectTransform>();
        divRt.anchorMin        = new Vector2(0f, 1f);
        divRt.anchorMax        = new Vector2(1f, 1f);
        divRt.pivot            = new Vector2(0f, 1f);
        divRt.anchoredPosition = new Vector2(0f, -(NAME_H + 2f));
        divRt.sizeDelta        = new Vector2(0f, 1f);

        // ── ScrollRect ────────────────────────────────────────────────────
        float scrollH = LINE_H * VISIBLE_LINES;        // 3줄 높이

        var scrollGo = new GameObject("DialogueScroll");
        scrollGo.transform.SetParent(textArea.transform, false);
        var scrollRt         = scrollGo.AddComponent<RectTransform>();
        scrollRt.anchorMin   = new Vector2(0f, 1f);
        scrollRt.anchorMax   = new Vector2(1f, 1f);
        scrollRt.pivot       = new Vector2(0f, 1f);
        scrollRt.anchoredPosition = new Vector2(0f, -(NAME_H + 6f));
        scrollRt.sizeDelta   = new Vector2(0f, scrollH);

        _scrollRect                   = scrollGo.AddComponent<ScrollRect>();
        _scrollRect.horizontal        = false;
        _scrollRect.vertical          = true;
        _scrollRect.scrollSensitivity = 40f;
        _scrollRect.movementType      = ScrollRect.MovementType.Clamped;
        _scrollRect.inertia           = false;

        // Viewport (RectMask2D でクリッピング)
        var vpGo = new GameObject("Viewport");
        vpGo.transform.SetParent(scrollGo.transform, false);
        vpGo.AddComponent<RectMask2D>();
        var vpRt         = vpGo.GetComponent<RectTransform>();
        vpRt.anchorMin   = Vector2.zero;
        vpRt.anchorMax   = Vector2.one;
        vpRt.offsetMin   = Vector2.zero;
        vpRt.offsetMax   = Vector2.zero;

        // Content (세로로 자동 확장)
        var contentGo = new GameObject("Content");
        contentGo.transform.SetParent(vpGo.transform, false);
        _dialogueText                  = contentGo.AddComponent<Text>();
        _dialogueText.font             = _font;
        _dialogueText.fontSize         = FONT_SIZE;
        _dialogueText.color            = new Color(0.95f, 0.92f, 0.85f);
        _dialogueText.alignment        = TextAnchor.UpperLeft;
        _dialogueText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _dialogueText.verticalOverflow   = VerticalWrapMode.Overflow;
        _dialogueText.lineSpacing      = 1.1f;

        _contentRect             = contentGo.GetComponent<RectTransform>();
        _contentRect.anchorMin   = new Vector2(0f, 1f);
        _contentRect.anchorMax   = new Vector2(1f, 1f);
        _contentRect.pivot       = new Vector2(0.5f, 1f);
        _contentRect.anchoredPosition = Vector2.zero;
        _contentRect.sizeDelta   = new Vector2(0f, 0f);

        var csf             = contentGo.AddComponent<ContentSizeFitter>();
        csf.verticalFit     = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit   = ContentSizeFitter.FitMode.Unconstrained;

        _scrollRect.viewport = vpRt;
        _scrollRect.content  = _contentRect;

        // ── NEXT 레이블 (패널 우측 하단) ──────────────────────────────────
        var nextGo  = new GameObject("NextLabel");
        nextGo.transform.SetParent(panel.transform, false);
        _nextLabel               = nextGo.AddComponent<Text>();
        _nextLabel.font          = _font;
        _nextLabel.fontSize      = 22;
        _nextLabel.fontStyle     = FontStyle.Bold;
        _nextLabel.color         = new Color(1f, 0.88f, 0.50f, 0.95f);
        _nextLabel.alignment     = TextAnchor.MiddleRight;
        _nextLabel.text          = "NEXT \u25b6";
        _nextLabel.enabled       = false;
        var nextRt = nextGo.GetComponent<RectTransform>();
        nextRt.anchorMin        = new Vector2(1f, 0f);
        nextRt.anchorMax        = new Vector2(1f, 0f);
        nextRt.pivot            = new Vector2(1f, 0f);
        nextRt.anchoredPosition = new Vector2(-H_PAD, V_PAD);
        nextRt.sizeDelta        = new Vector2(110f, 26f);
    }

    // ── 대화 세트 재생 ────────────────────────────────────────────────────────
    private IEnumerator PlaySet(DialogueSet set)
    {
        for (int i = 0; i < set.lines.Count; i++)
        {
            var line    = set.lines[i];
            bool hasMore = i < set.lines.Count - 1;

            // 캐릭터 정보 갱신 (변경 여부와 무관하게 항상 최신화)
            UpdateFace(line);
            _nameText.text = CharacterInfo.DisplayName(line.character);

            // 대사창 리프레시: 매 줄마다 텍스트를 지우고 시작
            _dialogueText.text = "";
            SetNextVisible(false);

            string lineText = line.text ?? "";
            if (lineText.Length > 300) lineText = lineText.Substring(0, 300);

            // 타이핑 애니메이션
            _isTyping      = true;
            _skipRequested = false;

            for (int c = 1; c <= lineText.Length; c++)
            {
                if (_skipRequested)
                {
                    _dialogueText.text = lineText;
                    break;
                }
                _dialogueText.text = lineText.Substring(0, c);
                ScrollToBottom();
                yield return new WaitForSeconds(CHAR_INTERVAL);
            }

            _dialogueText.text = lineText;
            _isTyping = false;
            ScrollToBottom();

            // 다음 대사가 있으면 NEXT 표시 후 클릭 대기
            if (hasMore)
            {
                SetNextVisible(true);
                _waitingForNext = true;
                _nextRequested  = false;
                yield return null;          // 1프레임 대기 (현재 클릭 무시)
                while (!_nextRequested) yield return null;
                _waitingForNext = false;
                SetNextVisible(false);
            }
        }
    }

    // ── NEXT 레이블 표시 제어 ─────────────────────────────────────────────────
    private void SetNextVisible(bool visible)
    {
        if (_nextLabel != null)
            _nextLabel.enabled = visible;
    }

    // ── 스크롤 하단으로 이동 ──────────────────────────────────────────────────
    private void ScrollToBottom()
    {
        Canvas.ForceUpdateCanvases();
        if (_scrollRect != null)
            _scrollRect.verticalNormalizedPosition = 0f;
    }

    // ── 얼굴 이미지 교체 ──────────────────────────────────────────────────────
    private void UpdateFace(DialogueLine line)
    {
        // Inspector에서 직접 지정한 스프라이트를 우선 사용
        var sprite = line.faceSprite;

        // 지정되지 않았으면 CharacterType 기본값을 Resources.Load 로 불러옵니다
        if (sprite == null)
        {
            string resName = CharacterInfo.DefaultSpriteName(line.character);
            sprite = Resources.Load<Sprite>(resName);
            if (sprite == null)
            {
                var tex = Resources.Load<Texture2D>(resName);
                if (tex != null)
                    sprite = Sprite.Create(tex,
                        new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f));
            }
        }

        if (_faceImage != null && sprite != null)
            _faceImage.sprite = sprite;
    }

    // ── 입력 처리 ─────────────────────────────────────────────────────────────
    void Update()
    {
        bool clicked = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space);
        if (_isTyping && clicked)
            _skipRequested = true;
        else if (_waitingForNext && clicked)
            _nextRequested = true;
    }

}
