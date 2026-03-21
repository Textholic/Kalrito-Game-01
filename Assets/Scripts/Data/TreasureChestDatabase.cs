// ============================================================
// TreasureChestDatabase.cs
// 아이템 상자에서 등장할 소비형·장비 아이템 정의 데이터베이스.
// 각 항목은 스프라이트를 Inspector에서 교체 가능.
// Unity 에디터: Assets > Create > DungeonGame > Treasure Chest Database
// ============================================================
using UnityEngine;
using System.Collections.Generic;

// ── 소비형 아이템 정의 ────────────────────────────────────────────────────────
[System.Serializable]
public class ConsumableItemDef
{
    [Tooltip("아이템 고유 ID (코드에서 참조)")]
    public string id = "";

    [Tooltip("표시 이름")]
    public string displayName = "";

    [TextArea(1, 3)]
    [Tooltip("아이템 설명 (상자에서 선택 시 전체 표시)")]
    public string description = "";

    [Tooltip("아이템 아이콘 스프라이트 (Inspector에서 교체 가능)")]
    public Sprite icon;

    // ── 효과 ─────────────────────────────────────────────────────────────────
    [Tooltip("골드 획득 (0이면 없음)")]
    public int goldGain = 0;

    [Tooltip("레벨 +1 여부")]
    public bool levelUp = false;

    [Tooltip("HP 회복량 (0이면 없음)")]
    public int hpHeal = 0;

    [Tooltip("최대 HP 증가량 (0이면 없음)")]
    public int maxHpBonus = 0;

    [Tooltip("공격력 증가량 (0이면 없음)")]
    public int attackBonus = 0;

    [Tooltip("방어력 증가량 (0이면 없음)")]
    public int defenseBonus = 0;

    [Tooltip("방어 확률 증가 (0.0~1.0, 0이면 없음). 예: 0.1 = +10%")]
    [Range(0f, 1f)]
    public float defChanceBonus = 0f;

    [Tooltip("해제할 상태이상 (None이면 무효)")]
    public StatusEffectType cureEffect = StatusEffectType.None;

    [TextArea(1, 2)]
    [Tooltip("서명글 — 짧은 이탤릭 분위기 문구 (툴팁 하단 회색 표시)")]
    public string flavorText = "";

    [Tooltip("최소 출현 층")]
    public int minFloor = 1;
}

// ── 장비 아이템 정의 ──────────────────────────────────────────────────────────
[System.Serializable]
public class EquipmentItemDef
{
    [Tooltip("아이템 고유 ID")]
    public string id = "";

    [Tooltip("표시 이름")]
    public string displayName = "";

    [TextArea(1, 3)]
    [Tooltip("아이템 설명 (장비 인벤토리 입장 전에는 ??? 표시)")]
    public string description = "";

    [Tooltip("아이템 아이콘 스프라이트 (Inspector에서 교체 가능)")]
    public Sprite icon;

    // ── 스탯 보정 (버프/디버프) ──────────────────────────────────────────────
    [Tooltip("공격력 보정 (+/-서 모두 가능)")]
    public int attackMod = 0;

    [Tooltip("방어력 보정 (+/-)")]
    public int defenseMod = 0;

    [Tooltip("방어 확률 보정 (+/-). 예: 0.1 = +10%, -0.1 = -10%")]
    [Range(-1f, 1f)]
    public float defChanceMod = 0f;

    [Tooltip("최대 HP 보정 (+/-)")]
    public int maxHpMod = 0;

    [Tooltip("회복량 보정 (+/-)")]
    public int healMod = 0;

    [TextArea(1, 2)]
    [Tooltip("서명글 — 짧은 이탤릭 분위기 문구 (툴팁 하단 회색 표시)")]
    public string flavorText = "";

    [Tooltip("최소 출현 층")]
    public int minFloor = 1;
}

// ── 데이터베이스 ScriptableObject ────────────────────────────────────────────
[CreateAssetMenu(fileName = "TreasureChestDatabase", menuName = "DungeonGame/Treasure Chest Database")]
public class TreasureChestDatabase : ScriptableObject
{
    [Header("소비형 아이템 목록 (30개 권장)")]
    public List<ConsumableItemDef> consumables = new List<ConsumableItemDef>();

    [Header("장비 아이템 목록 (30개 권장)")]
    public List<EquipmentItemDef> equipments = new List<EquipmentItemDef>();

    // ── 층 범위에 맞는 소비형 아이템 랜덤 3개 뽑기 ──────────────────────────
    public List<ConsumableItemDef> GetRandomConsumables(int floor, int count = 3)
    {
        var pool = new List<ConsumableItemDef>();
        int maxFloor = floor + 3;
        foreach (var c in consumables)
            if (c.minFloor <= maxFloor) pool.Add(c);
        if (pool.Count == 0) pool.AddRange(consumables);

        var result = new List<ConsumableItemDef>();
        var temp   = new List<ConsumableItemDef>(pool);
        for (int i = 0; i < count && temp.Count > 0; i++)
        {
            int idx = Random.Range(0, temp.Count);
            result.Add(temp[idx]);
            temp.RemoveAt(idx);
        }
        return result;
    }

    // ── 층 범위에 맞는 장비 아이템 랜덤 3개 뽑기 ────────────────────────────
    public List<EquipmentItemDef> GetRandomEquipments(int floor, int count = 3)
    {
        var pool = new List<EquipmentItemDef>();
        int maxFloor = floor + 3;
        foreach (var e in equipments)
            if (e.minFloor <= maxFloor) pool.Add(e);
        if (pool.Count == 0) pool.AddRange(equipments);

        var result = new List<EquipmentItemDef>();
        var temp   = new List<EquipmentItemDef>(pool);
        for (int i = 0; i < count && temp.Count > 0; i++)
        {
            int idx = Random.Range(0, temp.Count);
            result.Add(temp[idx]);
            temp.RemoveAt(idx);
        }
        return result;
    }

    // ── 소비형 + 장비 혼합 3개 뽑기 (층 범위 고려) ──────────────────────────
    /// <summary>소비형과 장비를 합쳐 랜덤 3개 선별. 각 아이템은 오브젝트로 래핑.</summary>
    public List<ChestItemWrapper> GetRandomMixed(int floor, int count = 3)
    {
        var pool = new List<ChestItemWrapper>();
        int maxFloor = floor + 3;

        foreach (var c in consumables)
            if (c.minFloor <= maxFloor)
                pool.Add(new ChestItemWrapper { consumable = c, isEquipment = false });

        foreach (var e in equipments)
            if (e.minFloor <= maxFloor)
                pool.Add(new ChestItemWrapper { equipment = e, isEquipment = true });

        if (pool.Count == 0)
        {
            foreach (var c in consumables)
                pool.Add(new ChestItemWrapper { consumable = c, isEquipment = false });
            foreach (var e in equipments)
                pool.Add(new ChestItemWrapper { equipment = e, isEquipment = true });
        }

        var result = new List<ChestItemWrapper>();
        var temp   = new List<ChestItemWrapper>(pool);
        for (int i = 0; i < count && temp.Count > 0; i++)
        {
            int idx = Random.Range(0, temp.Count);
            result.Add(temp[idx]);
            temp.RemoveAt(idx);
        }
        return result;
    }
}

// ── 래퍼 (소비형/장비 구분) ────────────────────────────────────────────────────
public class ChestItemWrapper
{
    public bool               isEquipment;
    public ConsumableItemDef  consumable;
    public EquipmentItemDef   equipment;

    public string DisplayName => isEquipment ? equipment.displayName : consumable.displayName;
    public Sprite Icon        => isEquipment ? equipment.icon        : consumable.icon;

    /// <summary>소비형은 설명 전체, 장비는 장비 인벤토리 입장 전 ???</summary>
    public string GetDescription(bool revealed = false)
    {
        if (!isEquipment) return consumable.description;
        if (!revealed)    return "???";
        return equipment.description;
    }

    /// <summary>장비 능력 요약 (인벤토리 진입 후 표시용)</summary>
    public string GetEquipmentStats()
    {
        if (!isEquipment) return "";
        var sb = new System.Text.StringBuilder();
        if (equipment.attackMod  != 0) sb.AppendLine($"공격  {(equipment.attackMod  > 0 ? "+" : "")}{equipment.attackMod}");
        if (equipment.defenseMod != 0) sb.AppendLine($"방어  {(equipment.defenseMod > 0 ? "+" : "")}{equipment.defenseMod}");
        if (equipment.maxHpMod   != 0) sb.AppendLine($"최대HP {(equipment.maxHpMod  > 0 ? "+" : "")}{equipment.maxHpMod}");
        if (equipment.healMod    != 0) sb.AppendLine($"회복  {(equipment.healMod    > 0 ? "+" : "")}{equipment.healMod}");
        return sb.Length > 0 ? sb.ToString().TrimEnd() : "특수 효과 없음";
    }
}
