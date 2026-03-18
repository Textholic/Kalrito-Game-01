// ============================================================
// InventoryManager.cs
// 3종류 아이템 박스 관리.
//   1. 메인 아이템박스  : 시작 5×3, 레벨 5단위마다 열·행 +1
//   2. 회복약 슬롯      : UI 하단 고정, 최대 99개, 1번 키 사용
//   3. 장비 아이템박스  : 1×8 고정, 장착 효과 자동 적용
// 300kg 초과 시 이동 불가 안내.
// ============================================================
using UnityEngine;
using System.Collections.Generic;

// ── 인벤토리에 배치된 아이템 인스턴스 ────────────────────────────────────────
[System.Serializable]
public class InventorySlot
{
    public ItemData data;
    public int      col;         // 메인박스: 열, 장비박스: 슬롯 인덱스
    public int      row;         // 메인박스: 행, 장비박스: 0 고정
    public bool     isCursed;    // 버릴 수 없음 (저주 각인·아이템)
}

public class InventoryManager : MonoBehaviour
{
    // ── 메인 아이템박스 ──────────────────────────────────────────────────────
    private int         _cols = 5;
    private int         _rows = 3;
    private InventorySlot[,] _mainGrid;     // [col, row]

    public int MainBoxCols => _cols;
    public int MainBoxRows => _rows;

    // ── 회복약 ───────────────────────────────────────────────────────────────
    public const int MAX_POTIONS     = 99;
    public const int POTION_HEAL_BASE = 30;   // 기본 회복량 (장비 보정 전)

    public int PotionCount { get; private set; } = 0;

    // ── 장비 아이템박스 ──────────────────────────────────────────────────────
    public const int EQUIP_SLOTS = 8;
    private InventorySlot[] _equipSlots = new InventorySlot[EQUIP_SLOTS];

    // ── 무게 ─────────────────────────────────────────────────────────────────
    public float CurrentWeightKg { get; private set; } = 0f;

    // ── 이벤트 ───────────────────────────────────────────────────────────────
    public event System.Action       OnInventoryChanged;
    public event System.Action       OnPotionCountChanged;
    /// <summary>true = 초과 상태 → 이동 불가 메시지 표시</summary>
    public event System.Action<bool> OnOverweightChanged;

    private bool _wasOverweight = false;

    // ── 초기화 ───────────────────────────────────────────────────────────────
    public void Reset()
    {
        _cols = 5;
        _rows = 3;
        _mainGrid   = new InventorySlot[_cols, _rows];
        _equipSlots = new InventorySlot[EQUIP_SLOTS];
        PotionCount = 0;
        CurrentWeightKg = 0f;
        _wasOverweight  = false;
    }

    // ── 메인 박스 확장 (레벨 5단위마다) ─────────────────────────────────────
    public void ExpandMainBox()
    {
        var oldGrid = _mainGrid;
        int oldCols = _cols;
        int oldRows = _rows;
        _cols++;
        _rows++;
        _mainGrid = new InventorySlot[_cols, _rows];

        for (int c = 0; c < oldCols; c++)
            for (int r = 0; r < oldRows; r++)
                _mainGrid[c, r] = oldGrid[c, r];

        OnInventoryChanged?.Invoke();
    }

    // ── 아이템 추가 ──────────────────────────────────────────────────────────
    /// <summary>
    /// 아이템을 알맞은 박스에 자동으로 배치시도.
    /// 공간이 없으면 false 반환.
    /// </summary>
    public bool TryAddItem(ItemData item, bool cursed = false)
    {
        if (item == null) return false;

        // 장비 아이템 → 장비 박스
        if (item.isEquipment)
            return TryAddEquipment(item, cursed);

        // 회복약 → 회복약 슬롯 (ItemCategory로 구분도 가능하지만, isEquipment=false인
        //          포션 타입 아이템은 별도 Route로 AddPotion 을 직접 호출하세요)

        // 그 외 → 메인박스
        return TryAddToMainGrid(item, cursed);
    }

    private bool TryAddToMainGrid(ItemData item, bool cursed)
    {
        for (int r = 0; r < _rows; r++)
            for (int c = 0; c < _cols; c++)
                if (_mainGrid[c, r] == null)
                {
                    _mainGrid[c, r] = new InventorySlot { data = item, col = c, row = r, isCursed = cursed || item.isCursed };
                    RecalcWeight();
                    OnInventoryChanged?.Invoke();
                    GameManager.Instance?.History?.RecordItemObtained();
                    return true;
                }
        return false; // 공간 없음
    }

    private bool TryAddEquipment(ItemData item, bool cursed)
    {
        // 빈 슬롯이 있으면 그곳에 배치
        for (int i = 0; i < EQUIP_SLOTS; i++)
            if (_equipSlots[i] == null)
            {
                _equipSlots[i] = new InventorySlot { data = item, col = i, row = 0, isCursed = cursed || item.isCursed };
                ApplyEquipmentEffects(item, +1);
                RecalcWeight();
                OnInventoryChanged?.Invoke();
                GameManager.Instance?.History?.RecordItemObtained();
                return true;
            }

        // FIFO: 모든 슬롯이 가득 찼을 때 첫 번째 아이템을 제거하고 새 아이템을 마지막에 추가
        ApplyEquipmentEffects(_equipSlots[0].data, -1); // 첫 번째 아이템 효과 해제
        GameManager.Instance?.UI?.AddLog($"[{_equipSlots[0].data.itemName}] 이(가) 장비 목록에서 밀려났습니다.");
        for (int i = 0; i < EQUIP_SLOTS - 1; i++)
        {
            _equipSlots[i] = _equipSlots[i + 1];
            if (_equipSlots[i] != null) _equipSlots[i].col = i;
        }
        _equipSlots[EQUIP_SLOTS - 1] = new InventorySlot { data = item, col = EQUIP_SLOTS - 1, row = 0, isCursed = cursed || item.isCursed };
        ApplyEquipmentEffects(item, +1);
        RecalcWeight();
        OnInventoryChanged?.Invoke();
        GameManager.Instance?.History?.RecordItemObtained();
        return true;
    }

    // ── 아이템 제거 (버리기) ─────────────────────────────────────────────────
    /// <summary>메인박스 아이템 제거. 저주 아이템은 실패.</summary>
    public bool TryDiscardMainItem(int col, int row)
    {
        var slot = GetMainSlot(col, row);
        if (slot == null) return false;
        if (slot.isCursed)
        {
            GameManager.Instance?.UI?.AddLog($"[{slot.data.itemName}] 은 버릴 수 없는 아이템입니다.");
            return false;
        }
        _mainGrid[col, row] = null;
        RecalcWeight();
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>장비 슬롯 해제. 저주 아이템은 실패.</summary>
    public bool TryUnequip(int slot)
    {
        var s = _equipSlots[slot];
        if (s == null) return false;
        if (s.isCursed)
        {
            GameManager.Instance?.UI?.AddLog($"[{s.data.itemName}] 은 해제할 수 없는 아이템입니다.");
            return false;
        }
        ApplyEquipmentEffects(s.data, -1);
        _equipSlots[slot] = null;
        RecalcWeight();
        OnInventoryChanged?.Invoke();
        return true;
    }

    // ── 드래그&드롭 이동 ─────────────────────────────────────────────────────
    /// <summary>메인박스 내 아이템 이동. 목적지가 비어있어야 함.</summary>
    public bool TryMoveMainItem(int fromCol, int fromRow, int toCol, int toRow)
    {
        if (toCol < 0 || toCol >= _cols || toRow < 0 || toRow >= _rows) return false;
        if (_mainGrid[toCol, toRow] != null) return false;
        if (_mainGrid[fromCol, fromRow] == null) return false;

        _mainGrid[toCol, toRow] = _mainGrid[fromCol, fromRow];
        _mainGrid[toCol, toRow].col = toCol;
        _mainGrid[toCol, toRow].row = toRow;
        _mainGrid[fromCol, fromRow] = null;
        OnInventoryChanged?.Invoke();
        return true;
    }

    // ── 회복약 ───────────────────────────────────────────────────────────────
    public bool AddPotion(int count = 1)
    {
        if (PotionCount >= MAX_POTIONS) return false;
        PotionCount = Mathf.Min(MAX_POTIONS, PotionCount + count);
        GameManager.Instance?.History?.RecordItemObtained(count);
        OnPotionCountChanged?.Invoke();
        return true;
    }

    /// <summary>1번 키 → 회복약 사용.</summary>
    public bool UsePotion()
    {
        if (PotionCount <= 0) return false;
        PotionCount--;
        int heal = Mathf.RoundToInt(POTION_HEAL_BASE + (GameManager.Instance?.Player?.HealingBoostFlat ?? 0f));
        GameManager.Instance?.Player?.Heal(heal);
        GameManager.Instance?.UI?.AddLog($"회복약 사용: {heal} HP 회복");
        OnPotionCountChanged?.Invoke();
        return true;
    }

    // ── 무게 과부하 판정 ─────────────────────────────────────────────────────
    public bool IsOverweight()
    {
        float limit = GameManager.Instance?.Player?.WeightLimitKg ?? 300f;
        return CurrentWeightKg > limit;
    }

    private void RecalcWeight()
    {
        float total = 0f;
        for (int c = 0; c < _cols; c++)
            for (int r = 0; r < _rows; r++)
                if (_mainGrid[c, r] != null)
                    total += _mainGrid[c, r].data.WeightKg;

        for (int i = 0; i < EQUIP_SLOTS; i++)
            if (_equipSlots[i] != null)
                total += _equipSlots[i].data.WeightKg;

        CurrentWeightKg = total;

        bool over = IsOverweight();
        if (over != _wasOverweight)
        {
            _wasOverweight = over;
            OnOverweightChanged?.Invoke(over);
        }
    }

    // ── 장비 효과 적용/해제 ──────────────────────────────────────────────────
    private void ApplyEquipmentEffects(ItemData item, int sign) // +1=장착, -1=해제
    {
        if (item?.effects == null) return;
        var p = GameManager.Instance?.Player;
        if (p == null) return;

        foreach (var e in item.effects)
        {
            switch (e.effectType)
            {
                case ItemEffectType.HpMaxBonus:
                    p.ApplyMaxHpBonus(Mathf.RoundToInt(e.value) * sign); break;
                case ItemEffectType.AttackBonus:
                    p.ApplyAttackBonus(Mathf.RoundToInt(e.value) * sign); break;
                case ItemEffectType.DefenseBonus:
                    p.ApplyDefenseBonus(Mathf.RoundToInt(e.value) * sign); break;
                case ItemEffectType.WeightLimitBonus:
                    p.ApplyWeightLimitBonus(e.value * sign); break;
                case ItemEffectType.HealingBoostPct:
                    // flat 보정 = 기본 회복량 × %
                    p.ApplyHealingBonus(POTION_HEAL_BASE * (e.value / 100f) * sign); break;
                case ItemEffectType.ReviveOnDeath:
                    p.HasReviveItem = sign > 0; break;
            }
        }
    }

    // ── 조회 ─────────────────────────────────────────────────────────────────
    public InventorySlot  GetMainSlot(int col, int row)
        => (col >= 0 && col < _cols && row >= 0 && row < _rows) ? _mainGrid[col, row] : null;

    public InventorySlot  GetEquipSlot(int slot)
        => (slot >= 0 && slot < EQUIP_SLOTS) ? _equipSlots[slot] : null;

    public InventorySlot[] GetAllEquipSlots() => _equipSlots;

    /// <summary>전체 아이템 목록 (메인박스) 반환.</summary>
    public List<InventorySlot> GetAllMainItems()
    {
        var list = new List<InventorySlot>();
        for (int c = 0; c < _cols; c++)
            for (int r = 0; r < _rows; r++)
                if (_mainGrid[c, r] != null)
                    list.Add(_mainGrid[c, r]);
        return list;
    }
}
