// ============================================================
// DialogueManager.cs
// 대화 시스템 선택 로직 (정적 유틸리티)
//
//   GetDialogueSetToPlay()
//     1. 달성목표 달성 && 아직 미표시인 조건부 대화 → 우선 선택 + 1회 표시 기록
//     2. 없으면 일반(General) 대화 중 랜덤 선택
//
//   표시 이력은 PlayerPrefs 에 영구 저장됩니다.
// ============================================================
using UnityEngine;
using System.Collections.Generic;

public static class DialogueManager
{
    private const string K_PLAYED_PREFIX = "dlg_played_";

    private static DialogueDatabase _database;

    // ── 데이터베이스 로드 ─────────────────────────────────────────────────────
    public static DialogueDatabase GetDatabase()
    {
        if (_database == null)
            _database = Resources.Load<DialogueDatabase>("DialogueDatabase");
        return _database;
    }

    // ── 표시할 대화 세트 선택 ─────────────────────────────────────────────────
    /// <summary>
    /// 대기화면 진입 시 호출합니다.<br/>
    /// 달성목표 연동 대화(미표시)가 있으면 우선 반환하고 1회 완료로 기록합니다.<br/>
    /// 없으면 일반 대화 중 무작위로 반환합니다. 데이터가 없으면 null.
    /// </summary>
    public static DialogueSet GetDialogueSetToPlay()
    {
        var db = GetDatabase();
        if (db == null || db.dialogueSets == null || db.dialogueSets.Count == 0)
        {
            Debug.LogWarning("[DialogueManager] DialogueDatabase 를 찾을 수 없습니다. " +
                             "Assets/Resources/DialogueDatabase.asset 경로를 확인하세요.");
            return null;
        }

        var history = GameManager.Instance?.History;

        // ── 조건 달성 + 미표시 목록 수집 ────────────────────────────────────
        var pending = new List<DialogueSet>();
        foreach (var set in db.dialogueSets)
        {
            if (set == null || set.lines == null || set.lines.Count == 0) continue;
            if (set.condition == DialogueConditionType.General) continue;
            if (IsPlayed(set)) continue;

            string achId = DialogueConditionHelper.ToAchievementID(set.condition);
            if (achId != null && history != null && history.IsAchieved(achId))
                pending.Add(set);
        }

        if (pending.Count > 0)
        {
            var chosen = pending[0];
            MarkPlayed(chosen);
            return chosen;
        }

        // ── 일반 대화 랜덤 ───────────────────────────────────────────────────
        var generals = new List<DialogueSet>();
        foreach (var set in db.dialogueSets)
            if (set != null && set.lines != null && set.lines.Count > 0
                && set.condition == DialogueConditionType.General)
                generals.Add(set);

        if (generals.Count == 0) return null;
        return generals[Random.Range(0, generals.Count)];
    }

    // ── 1회 표시 여부 ─────────────────────────────────────────────────────────
    public static bool IsPlayed(DialogueSet set)
        => PlayerPrefs.GetInt(K_PLAYED_PREFIX + set.name, 0) == 1;

    public static void MarkPlayed(DialogueSet set)
    {
        PlayerPrefs.SetInt(K_PLAYED_PREFIX + set.name, 1);
        PlayerPrefs.Save();
    }

    // ── 내부 캐시 무효화 (씬 재로드 등 대비) ──────────────────────────────────
    public static void InvalidateCache() => _database = null;

    // ── 디버그: 조건 대화 표시 이력 전체 초기화 ──────────────────────────────
    public static void ResetAllPlayed()
    {
        var db = GetDatabase();
        if (db == null) return;
        foreach (var set in db.dialogueSets)
        {
            if (set == null) continue;
            PlayerPrefs.DeleteKey(K_PLAYED_PREFIX + set.name);
        }
        PlayerPrefs.Save();
        Debug.Log("[DialogueManager] 조건 대화 표시 이력을 초기화했습니다.");
    }
}
