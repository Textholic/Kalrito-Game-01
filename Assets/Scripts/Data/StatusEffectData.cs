// ============================================================
// StatusEffectData.cs
// 상태이상 정의 ScriptableObject.
// Unity 에디터: Assets > Create > DungeonGame > Status Effect Data
// ============================================================
using UnityEngine;

// ── 상태이상 종류 열거형 (코드 전역에서 공유) ──────────────────────────────────
public enum StatusEffectType
{
    None      = 0,
    Burn      = 1,   // 화상  : 이동 3회마다 1~5 데미지
    Poison    = 2,   // 중독  : 이동 3회마다 3~8 데미지
    Fatigue   = 3,   // 피로  : 공격력 0~40% 감소 (랜덤 책정)
    Charm     = 4,   // 매료  : 적 어그로 범위 2배
    Confusion = 5,   // 혼란  : 공격 시 5% 확률로 자해
}

// ── StatusEffectData ScriptableObject ──────────────────────────────────────────
[CreateAssetMenu(fileName = "New StatusEffect", menuName = "DungeonGame/Status Effect Data")]
public class StatusEffectData : ScriptableObject
{
    // ── 기본 정보 ─────────────────────────────────────────────────────────────
    [Header("기본 정보")]
    [Tooltip("적용될 상태이상 종류")]
    public StatusEffectType effectType  = StatusEffectType.None;

    [Tooltip("표시 이름 (UI용)")]
    public string           effectName  = "";

    [TextArea(2, 4)]
    [Tooltip("상태이상 설명")]
    public string           description = "";

    [Tooltip("상태이상 아이콘")]
    public Sprite           icon;

    // ── 화상 / 중독 설정 (이동 3회마다 발동) ────────────────────────────────
    [Header("화상 / 중독 — 주기적 데미지 설정")]
    [Tooltip("최소 데미지 (화상=1, 중독=3)")]
    public int   minDamage = 1;

    [Tooltip("최대 데미지 + 1 (화상=6, 중독=9 → 각각 1~5, 3~8)")]
    public int   maxDamageExclusive = 6;

    // ── 피로 설정 ─────────────────────────────────────────────────────────────
    [Header("피로 — 공격력 감소 설정")]
    [Tooltip("공격력 감소 최솟값 (0 = 0%)")]
    [Range(0f, 1f)]
    public float minAttackReduceFraction = 0f;

    [Tooltip("공격력 감소 최댓값 (0.4 = 40%)")]
    [Range(0f, 1f)]
    public float maxAttackReduceFraction = 0.4f;

    // ── 매료 설정 ─────────────────────────────────────────────────────────────
    [Header("매료 — 어그로 범위 배율")]
    [Tooltip("적 어그로 범위 배수 (기본 2.0)")]
    public float aggroRangeMultiplier = 2f;

    // ── 혼란 설정 ─────────────────────────────────────────────────────────────
    [Header("혼란 — 자해 확률")]
    [Tooltip("공격 시 자해 확률 (0.05 = 5%)")]
    [Range(0f, 1f)]
    public float selfAttackChance = 0.05f;
}

/*  ────────────────────────────────────────────────────────────────────────────
 *  예시 StatusEffectData 생성 가이드
 *
 *  [화상 (Burn)]
 *    effectType = Burn
 *    minDamage = 1, maxDamageExclusive = 6  → Random.Range(1,6) = 1~5
 *
 *  [중독 (Poison)]
 *    effectType = Poison
 *    minDamage = 3, maxDamageExclusive = 9  → Random.Range(3,9) = 3~8
 *
 *  [피로 (Fatigue)]
 *    effectType = Fatigue
 *    minAttackReduceFraction = 0.0, maxAttackReduceFraction = 0.4  → 0~40%
 *
 *  [매료 (Charm)]
 *    effectType = Charm
 *    aggroRangeMultiplier = 2.0
 *
 *  [혼란 (Confusion)]
 *    effectType = Confusion
 *    selfAttackChance = 0.05
 *  ──────────────────────────────────────────────────────────────────────────── */
