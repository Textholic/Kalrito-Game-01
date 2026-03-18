// ============================================================
// EngravingData.cs
// 슬레이어 각인 정의 ScriptableObject.
// Unity 에디터: Assets > Create > DungeonGame > Engraving Data
// ============================================================
using UnityEngine;

// ── 각인 효과 종류 ────────────────────────────────────────────────────────────
public enum EngravingEffectType
{
    MaxHpBonus,             // 최대 체력 +/- value
    HpRegenRateBonus,       // 체력 자연 회복률 +/- value (% per step)
    WeightLimitBonus,       // 무게 제한 +/- value (kg)
    AttackBonus,            // 공격력 +/- value
    DefenseBonus,           // 방어력 +/- value
    AggroRangeMultiplier,   // 어그로 감지 범위 배율 (value 배, 예: 2.0)
    ForceApplyBurn,         // 게임 시작 시 화상 강제 적용
    ForceApplyPoison,       // 게임 시작 시 중독 강제 적용
    ForceApplyCharm,        // 게임 시작 시 매료 강제 적용
    ForceApplyFatigue,      // 게임 시작 시 피로 강제 적용
    ForceApplyConfusion,    // 게임 시작 시 혼란 강제 적용
    CursedItemForced,       // 버릴 수 없는 특정 아이템을 인벤토리에 강제 삽입
    PotionHealBonus,        // 회복약 회복량 +/- value
    ReviveChanceBonus,      // 부활 아이템 발동 추가 확률 +value (0~1)
}

// ── 단일 각인 효과 데이터 ─────────────────────────────────────────────────────
[System.Serializable]
public class EngravingEffect
{
    [Tooltip("효과 종류")]
    public EngravingEffectType effectType      = EngravingEffectType.MaxHpBonus;

    [Tooltip("효과 수치 (+/-, 버프는 양수, 디버프는 음수)")]
    public float               value           = 0f;

    [Tooltip("CursedItemForced 용: 강제로 넣을 아이템")]
    public ItemData            cursedItem;
}

// ── EngravingData ScriptableObject ───────────────────────────────────────────
[CreateAssetMenu(fileName = "New Engraving", menuName = "DungeonGame/Engraving Data")]
public class EngravingData : ScriptableObject
{
    // ── 기본 정보 ─────────────────────────────────────────────────────────────
    [Header("기본 정보")]
    [Tooltip("각인 이름")]
    public string            engravingName = "각인";

    [TextArea(3, 7)]
    [Tooltip("각인 설명 (효과 및 플레이어에 미치는 영향 서술)")]
    public string            description   = "";

    [Tooltip("각인 아이콘")]
    public Sprite            icon;

    [Tooltip("디버프 각인이면 true (UI 색상 구분용)")]
    public bool              isDebuff      = false;

    // ── 효과 목록 ─────────────────────────────────────────────────────────────
    [Header("효과 목록 (복수 효과 허용)")]
    public EngravingEffect[] effects;
}

/*  ────────────────────────────────────────────────────────────────────────────
 *  예시 각인 목록 (Create > DungeonGame > Engraving Data 로 생성)
 *
 *  ■ 버프 각인
 *  [강철 의지]      isDebuff=false
 *    MaxHpBonus +50
 *
 *  [날카로운 감각]  isDebuff=false
 *    AttackBonus +8
 *
 *  [가벼운 발걸음]  isDebuff=false
 *    WeightLimitBonus +100kg
 *
 *  [회복의 기도]    isDebuff=false
 *    PotionHealBonus +15
 *
 *  [불사의 맹세]    isDebuff=false
 *    MaxHpBonus +30, ReviveChanceBonus +0.1
 *
 *  ■ 디버프 각인
 *  [카레의 저주]    isDebuff=true
 *    CursedItemForced (카레 40kg 아이템)
 *    AggroRangeMultiplier 2.0
 *    → 사망하기 전까지 카레 40kg이 버릴 수 없는 상태로 아이템박스를 점유하며,
 *      적 어그로 범위가 2배가 됨
 *
 *  [독의 각인]      isDebuff=true
 *    ForceApplyPoison (게임 시작 시 중독 적용)
 *    AttackBonus -3
 *
 *  [피로의 족쇄]    isDebuff=true
 *    ForceApplyFatigue (게임 시작 시 피로 적용)
 *    WeightLimitBonus -50kg
 *
 *  [화염의 낙인]    isDebuff=true
 *    ForceApplyBurn (게임 시작 시 화상 적용)
 *    MaxHpBonus -20
 *
 *  ■ 혼합 각인 (버프+디버프)
 *  [악마와의 계약]  isDebuff=true
 *    AttackBonus +15, DefenseBonus -5, ForceApplyCharm
 *    → 공격력이 크게 오르지만 방어력이 떨어지고 적 어그로 범위가 넓어짐
 *
 *  [광전사의 혼]    isDebuff=true
 *    AttackBonus +10, MaxHpBonus -30, ForceApplyConfusion
 *    → 공격력이 오르지만 체력이 줄고 5% 자해 확률이 생김
 *  ──────────────────────────────────────────────────────────────────────────── */
