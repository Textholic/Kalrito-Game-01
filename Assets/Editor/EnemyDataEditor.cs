#if UNITY_EDITOR
// ============================================================
// EnemyDataEditor.cs
// EnemyData ScriptableObject 전용 커스텀 인스펙터.
//   • 오른쪽/왼쪽 스프라이트 2장 나란히 미리보기
//   • 보스: 고정 스탯만 표시 (레벨 스케일링 숨김)
//   • 일반/정예: 1레벨 기준 스탯 + 레벨당 증가량
//   • 층수 시뮬레이터: 특정 층에서의 스탯 미리보기
// ============================================================
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EnemyData))]
public class EnemyDataEditor : Editor
{
    // ── 섹션 펼침 상태 ────────────────────────────────────────────────────
    private bool _foldStats   = true;
    private bool _foldDrop    = false;
    private bool _foldSimul   = false;

    // ── 시뮬레이터 ────────────────────────────────────────────────────────
    private int  _simulFloor  = 1;

    // ── 스타일 캐시 ───────────────────────────────────────────────────────
    private GUIStyle _headerStyle;
    private GUIStyle _subStyle;
    private GUIStyle _infoBoxStyle;

    private void InitStyles()
    {
        if (_headerStyle != null) return;

        _headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 12,
            normal   = { textColor = new Color(1f, 0.85f, 0.45f) },
        };
        _subStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 11,
            normal   = { textColor = new Color(0.75f, 0.92f, 1f) },
        };
        _infoBoxStyle = new GUIStyle(EditorStyles.helpBox);
    }

    // ====================================================================
    public override void OnInspectorGUI()
    {
        InitStyles();

        var data = (EnemyData)target;
        serializedObject.Update();

        // ── 기본 정보 ───────────────────────────────────────────────────
        DrawSectionHeader("기본 정보");
        EditorGUILayout.PropertyField(serializedObject.FindProperty("enemyName"),    new GUIContent("몬스터 이름"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("rank"),         new GUIContent("등급 (보스 / 일반 / 정예)"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("description"),  new GUIContent("설명"));

        // ── 스프라이트 2장 ──────────────────────────────────────────────
        EditorGUILayout.Space(6);
        DrawSectionHeader("스프라이트 (2장)");

        EditorGUILayout.BeginHorizontal();

        // 오른쪽 스프라이트
        EditorGUILayout.BeginVertical(GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.48f));
        EditorGUILayout.LabelField("▶ 오른쪽 바라보기", EditorStyles.miniLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("spriteRight"), GUIContent.none);
        DrawSpritePreview(data.spriteRight, 90);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(4);

        // 왼쪽 스프라이트
        EditorGUILayout.BeginVertical(GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.48f));
        EditorGUILayout.LabelField("◀ 왼쪽 바라보기", EditorStyles.miniLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("spriteLeft"), GUIContent.none);
        DrawSpritePreview(data.spriteLeft, 90);
        if (data.spriteLeft == null && data.spriteRight != null)
            EditorGUILayout.HelpBox("미설정 시 오른쪽 스프라이트를\n수평 반전하여 사용합니다.", MessageType.None);
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        // ── 스탯 ────────────────────────────────────────────────────────
        EditorGUILayout.Space(6);
        bool isBoss = data.rank == EnemyRank.Boss;
        _foldStats = DrawFoldout(_foldStats, isBoss ? "스탯  (고정 — 보스)" : "스탯  (1레벨 기준 — 일반/정예)");
        if (_foldStats)
        {
            EditorGUI.indentLevel++;

            if (isBoss)
            {
                EditorGUILayout.HelpBox(
                    "보스는 층수/레벨에 관계없이 아래 수치가 그대로 적용됩니다.",
                    MessageType.Info);

                DrawStatField("baseHp",      "체력 (고정)");
                DrawStatField("baseAttack",  "공격력 (고정)");
                DrawStatField("baseDefense", "방어력 (고정)");
                DrawStatField("baseExp",     "경험치 (고정)");
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "1레벨 기준 수치입니다.\n층수가 올라갈수록 레벨이 증가하며 아래 증가량이 누적됩니다.",
                    MessageType.Info);

                EditorGUILayout.LabelField("▸ 1레벨 기준 스탯", _subStyle);
                DrawStatField("baseHp",      "체력 (1레벨)");
                DrawStatField("baseAttack",  "공격력 (1레벨)");
                DrawStatField("baseDefense", "방어력 (1레벨)");
                DrawStatField("baseExp",     "경험치 (1레벨)");

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("▸ 레벨당 증가량", _subStyle);
                DrawStatFieldFloat("hpPerLevel",     "체력 증가 / 레벨");
                DrawStatFieldFloat("attackPerLevel", "공격력 증가 / 레벨");
                DrawStatFieldFloat("expPerLevel",    "경험치 증가 / 레벨");
            }

            EditorGUI.indentLevel--;
        }

        // ── 골드 드롭 ────────────────────────────────────────────────────
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("골드 드롭", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("goldDropMin"), new GUIContent("최소"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("goldDropMax"), new GUIContent("최대"));
        EditorGUILayout.EndHorizontal();

        // ── 출현 및 어그로 ───────────────────────────────────────────────
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("출현 설정", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("aggroRange"), new GUIContent("어그로 범위 (타일)"));
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("minFloor"), new GUIContent("등장 최소 층"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("maxFloor"), new GUIContent("최대 층 (0=무제한)"));
        EditorGUILayout.EndHorizontal();

        // ── 드롭 테이블 / 상태이상 ──────────────────────────────────────
        EditorGUILayout.Space(4);
        _foldDrop = DrawFoldout(_foldDrop, "드롭 테이블 / 피격 상태이상");
        if (_foldDrop)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("dropTable"),
                new GUIContent("아이템 드롭 테이블"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("statusOnHitChance"),
                new GUIContent("피격 상태이상 확률"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onHitStatusEffect"),
                new GUIContent("부여 상태이상 종류"));
            EditorGUI.indentLevel--;
        }

        // ── 층수 시뮬레이터 (일반/정예 전용) ────────────────────────────
        if (!isBoss)
        {
            EditorGUILayout.Space(6);
            _foldSimul = DrawFoldout(_foldSimul, "층수 시뮬레이터 (미리보기)");
            if (_foldSimul)
            {
                EditorGUI.indentLevel++;
                _simulFloor = EditorGUILayout.IntSlider("시뮬 층수", _simulFloor, 1, 30);
                int simLevel = Mathf.Max(1, _simulFloor);  // 층수 = 레벨로 간략 가정

                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.IntField("체력",   data.GetHpAtLevel(simLevel));
                EditorGUILayout.IntField("공격력", data.GetAttackAtLevel(simLevel));
                EditorGUILayout.IntField("경험치", data.GetExpAtLevel(simLevel));
                EditorGUI.EndDisabledGroup();
                EditorGUI.indentLevel--;
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    // ====================================================================
    // 헬퍼 메서드
    // ====================================================================

    private void DrawSectionHeader(string title)
    {
        var rect = EditorGUILayout.GetControlRect(false, 22f);
        EditorGUI.DrawRect(rect, new Color(0.18f, 0.18f, 0.18f, 0.6f));
        rect.x += 6;
        GUI.Label(rect, title, _headerStyle);
    }

    private static bool DrawFoldout(bool state, string label)
        => EditorGUILayout.Foldout(state, label, true, EditorStyles.foldoutHeader);

    private void DrawStatField(string prop, string label)
        => EditorGUILayout.PropertyField(serializedObject.FindProperty(prop), new GUIContent(label));

    private void DrawStatFieldFloat(string prop, string label)
        => EditorGUILayout.PropertyField(serializedObject.FindProperty(prop), new GUIContent(label));

    /// <summary>
    /// Sprite를 아틀라스 UV를 고려해 지정 높이로 그립니다.
    /// </summary>
    private static void DrawSpritePreview(Sprite sprite, float size)
    {
        if (sprite == null) return;

        float aspect = sprite.rect.width / sprite.rect.height;
        float w = size * aspect;
        var rect = GUILayoutUtility.GetRect(w, size, GUILayout.Width(w), GUILayout.Height(size));

        // 아틀라스 UV 계산
        var tex = sprite.texture;
        var tc  = new Rect(
            sprite.rect.x      / tex.width,
            sprite.rect.y      / tex.height,
            sprite.rect.width  / tex.width,
            sprite.rect.height / tex.height);

        // 텍스처 읽기 권한 없을 때 폴백 (아틀라스 패킹 포함)
        if (!tex.isReadable || tex == null)
        {
            EditorGUI.DrawPreviewTexture(rect, tex, null, ScaleMode.ScaleToFit);
            return;
        }
        GUI.DrawTextureWithTexCoords(rect, tex, tc);
    }

    // 아틀라스 텍스처 미리보기 (읽기 권한 없이도 동작)
    public override bool HasPreviewGUI() => ((EnemyData)target).spriteRight != null
                                         || ((EnemyData)target).spriteLeft  != null;

    public override void OnPreviewGUI(Rect r, GUIStyle background)
    {
        var data = (EnemyData)target;
        GUI.Box(r, GUIContent.none, background);

        int count  = (data.spriteRight != null ? 1 : 0) + (data.spriteLeft != null ? 1 : 0);
        if (count == 0) return;

        float half  = r.width / Mathf.Max(count, 2);
        float pad   = 6f;
        float x     = r.x + pad;

        DrawPreviewSprite(data.spriteRight, new Rect(x, r.y + pad, half - pad * 2, r.height - pad * 2), "右");
        if (data.spriteLeft != null)
            DrawPreviewSprite(data.spriteLeft, new Rect(x + half, r.y + pad, half - pad * 2, r.height - pad * 2), "左");
        else if (data.spriteRight != null)
        {
            // 반전 미리보기
            var matrix = GUI.matrix;
            var flipRect = new Rect(x + half, r.y + pad, half - pad * 2, r.height - pad * 2);
            GUIUtility.ScaleAroundPivot(new Vector2(-1, 1),
                new Vector2(flipRect.x + flipRect.width / 2f, flipRect.y + flipRect.height / 2f));
            DrawPreviewSprite(data.spriteRight, flipRect, "(반전)");
            GUI.matrix = matrix;
        }
    }

    private static void DrawPreviewSprite(Sprite sprite, Rect rect, string label)
    {
        if (sprite == null) return;
        EditorGUI.DrawPreviewTexture(rect, sprite.texture, null, ScaleMode.ScaleToFit);
        EditorGUI.DropShadowLabel(new Rect(rect.x, rect.yMax - 16, rect.width, 16), label);
    }

    public override GUIContent GetPreviewTitle()
        => new GUIContent("스프라이트 미리보기");
}
#endif
