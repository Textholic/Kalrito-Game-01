// ============================================================
// ItemData.cs
// 아이템 정의 ScriptableObject.
// Unity 에디터: Assets > Create > DungeonGame > Item Data
// ============================================================
using UnityEngine;

// ── 아이템 분류 ──────────────────────────────────────────────────────────────
public enum ItemCategory
{
    Gem,            // 보석류            → 메인 아이템박스
    StatusPotion,   // 상태이상 포션     → 메인 아이템박스
    Equipment,      // 장비              → 장비 아이템박스 (1×8)
    Misc,           // 기타              → 메인 아이템박스
    Cursed,         // 저주 아이템       → 버릴 수 없음
}

// ── 아이템 효과 종류 ─────────────────────────────────────────────────────────
public enum ItemEffectType
{
    None,
    HpRestore,          // 즉시 체력 회복 (value = 회복량)
    HpMaxBonus,         // 장착 시 최대 체력 +value
    AttackBonus,        // 장착 시 공격력 +value
    DefenseBonus,       // 장착 시 방어력 +value
    HealingBoostPct,    // 회복약 회복량 +value %         (장비)
    ReviveOnDeath,      // 1회 사망 시 부활 (아이템 소비) (장비)
    WeightLimitBonus,   // 무게 제한 +value kg
    CureStatusEffect,   // 상태이상 해제 (statusEffectType 지정)
    ApplyStatusEffect,  // 상태이상 부여 (적 또는 자신)
    CursedWeight,       // value kg 강제 점유, 버릴 수 없음
}

// ── 단일 아이템 효과 데이터 ──────────────────────────────────────────────────
[System.Serializable]
public class ItemEffect
{
    [Tooltip("효과 종류")]
    public ItemEffectType   effectType      = ItemEffectType.None;

    [Tooltip("효과 수치 (회복량, %, kg 등 효과 종류에 따라 달라짐)")]
    public float            value           = 0f;

    [Tooltip("상태이상 관련 효과 지정 시 사용")]
    public StatusEffectType statusEffectType = StatusEffectType.None;
}

// ── ItemData ScriptableObject ─────────────────────────────────────────────────
[CreateAssetMenu(fileName = "New Item", menuName = "DungeonGame/Item Data")]
public class ItemData : ScriptableObject
{
    // ── 기본 정보 ─────────────────────────────────────────────────────────────
    [Header("기본 정보")]
    [Tooltip("아이템 이름")]
    public string       itemName    = "아이템";

    [TextArea(2, 5)]
    [Tooltip("아이템 설명")]
    public string       description = "";

    [Tooltip("아이템 분류")]
    public ItemCategory category    = ItemCategory.Gem;

    [Tooltip("인벤토리 표시 아이콘")]
    public Sprite       icon;

    // ── 무게 ─────────────────────────────────────────────────────────────────
    [Header("무게")]
    [Tooltip("무게 (그람 단위, 1g ~ 40,000g = 40kg)")]
    [Range(1f, 40000f)]
    public float        weightGrams = 500f;

    /// <summary>무게를 kg 단위로 반환.</summary>
    public float WeightKg => weightGrams / 1000f;

    // ── 효과 목록 ─────────────────────────────────────────────────────────────
    [Header("효과 목록")]
    [Tooltip("이 아이템이 적용하는 효과들")]
    public ItemEffect[] effects;

    // ── 아이템 박스 옵션 ──────────────────────────────────────────────────────
    [Header("아이템 박스 옵션")]
    [Tooltip("true면 장비 아이템박스(E)에 들어가며 장착 효과가 발동됨")]
    public bool         isEquipment = false;

    [Tooltip("true면 버릴 수 없음 (저주 아이템)")]
    public bool         isCursed    = false;

    // ── 판매 / 획득 가치 ──────────────────────────────────────────────────────
    [Header("골드 가치")]
    [Tooltip("드롭 시 골드 환산 가치 (0 = 골드로 환산 불가)")]
    public int          goldValue   = 0;

    // ── 편의 메서드 ───────────────────────────────────────────────────────────
    public bool HasEffect(ItemEffectType type)
    {
        if (effects == null) return false;
        foreach (var e in effects)
            if (e.effectType == type) return true;
        return false;
    }
}

/*  ────────────────────────────────────────────────────────────────────────────
 *  예시 아이템 목록 (Project 창에서 Create > DungeonGame > Item Data 로 생성)
 *
 *  [보석류]
 *  - 루비 조각      : 100g,  GoldValue 50
 *  - 에메랄드       : 200g,  GoldValue 120
 *  - 흑요석         : 500g,  GoldValue 80
 *  - 황금 묵직한 덩어리 : 5000g, GoldValue 500
 *
 *  [상태이상 포션]
 *  - 화상 치료제    : 50g,  CureStatusEffect(Burn)
 *  - 해독제         : 50g,  CureStatusEffect(Poison)
 *  - 피로 회복제    : 80g,  CureStatusEffect(Fatigue)
 *  - 혼돈 포션      : 50g,  ApplyStatusEffect(Confusion) ← 적에게 사용
 *
 *  [장비]
 *  - 생명의 반지    : 30g,   HpMaxBonus +20,    isEquipment=true
 *  - 힘의 장갑      : 500g,  AttackBonus +5,    isEquipment=true
 *  - 회복 목걸이    : 20g,   HealingBoostPct 30%, isEquipment=true
 *  - 불사의 부적    : 100g,  ReviveOnDeath,     isEquipment=true
 *  - 무게 경감 벨트 : 800g,  WeightLimitBonus +50kg, isEquipment=true
 *
 *  [저주 아이템]
 *  - 카레 40kg      : 40000g, isCursed=true (각인 '카레의 저주' 에서 강제 삽입)
 *  ──────────────────────────────────────────────────────────────────────────── */
