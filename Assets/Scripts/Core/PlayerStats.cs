// ============================================================
// PlayerStats.cs
// 플레이어 런타임 스탯 및 상태이상 관리.
// GameManager가 소유하며 씬 전환에도 유지됨.
// ============================================================
using UnityEngine;
using System.Collections.Generic;

public class PlayerStats : MonoBehaviour
{
    // ── 기본 스탯 ─────────────────────────────────────────────────────────────
    public string PlayerName     { get; set; } = "슬레이어";
    public int    Level          { get; private set; } = 1;
    public int    Exp            { get; private set; } = 0;
    public int    MaxHp          { get; private set; } = 100;
    public int    CurrentHp      { get; private set; } = 100;
    public int    BaseAttack     { get; private set; } = 10;
    public int    BaseDefense    { get; private set; } = 0;
    public int    Gold           { get; private set; } = 0;
    public float  WeightLimitKg  { get; private set; } = 300f;

    // ── 아이템 박스 추가 행 (각인 효과) ──────────────────────────────────────
    public int ExtraItemBoxRows  { get; private set; } = 0;

    // ── 회복약 회복량 보정 ────────────────────────────────────────────────────
    private float _healingBoostFlat = 0f;   // 장비/각인으로 누적된 회복량 보정
    public float HealingBoostFlat => _healingBoostFlat;

    // ── 부활 플래그 ───────────────────────────────────────────────────────────
    public bool HasReviveItem    { get; set; } = false;

    // ── 상태이상 ─────────────────────────────────────────────────────────────
    private readonly HashSet<StatusEffectType> _activeEffects = new HashSet<StatusEffectType>();

    // 피로: 공격력 감소 비율 (0.0 ~ 0.4), 상태이상 추가 시 랜덤 책정
    private float _fatigueReduceFraction = 0f;

    // 이동 횟수 카운터 (화상/중독 주기 발동용)
    private int _moveCounter = 0;

    // ── 레벨업 경험치 테이블 ──────────────────────────────────────────────────
    public int ExpToNextLevel => Level * 50;

    // ── 이벤트 ───────────────────────────────────────────────────────────────
    public event System.Action<int> OnLevelUp;                              // (새 레벨)
    public event System.Action<int, int> OnHpChanged;                       // (현재HP, 최대HP)
    public event System.Action<int> OnGoldChanged;                          // (현재골드)
    public event System.Action OnDeath;
    public event System.Action<StatusEffectType, bool> OnStatusEffectChanged; // (type, 추가=true)

    // ── 초기화 ───────────────────────────────────────────────────────────────
    public void InitializeNewGame()
    {
        Level            = 1;
        Exp              = 0;
        MaxHp            = 100;
        CurrentHp        = 100;
        BaseAttack       = 10;
        BaseDefense      = 0;
        Gold             = 0;
        WeightLimitKg    = 300f;
        ExtraItemBoxRows = 0;
        _healingBoostFlat    = 0f;
        HasReviveItem    = false;
        _moveCounter     = 0;
        _fatigueReduceFraction = 0f;
        _activeEffects.Clear();
    }

    // ── 이동 처리 ─────────────────────────────────────────────────────────────
    /// <summary>플레이어가 타일 1칸 이동할 때마다 호출하세요.</summary>
    public void OnMoved()
    {
        _moveCounter++;

        // 화상/중독: 3회 이동마다 주기 데미지
        if (_moveCounter % 3 == 0)
            ProcessPeriodicStatusEffects();
    }

    private void ProcessPeriodicStatusEffects()
    {
        // 화상: 1~5 데미지
        if (HasEffect(StatusEffectType.Burn))
        {
            int dmg = Random.Range(1, 6);
            TakeDamage(dmg, "화상");
            GameManager.Instance?.UI?.AddLog($"화상으로 {dmg} 데미지를 입었습니다.");
        }

        // 중독: 3~8 데미지
        if (HasEffect(StatusEffectType.Poison))
        {
            int dmg = Random.Range(3, 9);
            TakeDamage(dmg, "중독");
            GameManager.Instance?.UI?.AddLog($"중독으로 {dmg} 데미지를 입었습니다.");
        }
    }

    // ── 전투 계산 ─────────────────────────────────────────────────────────────
    /// <summary>피로 효과 적용 후 실제 공격력 반환.</summary>
    public int GetEffectiveAttack()
    {
        float reduceFrac = HasEffect(StatusEffectType.Fatigue) ? _fatigueReduceFraction : 0f;
        return Mathf.Max(0, Mathf.RoundToInt(BaseAttack * (1f - reduceFrac)));
    }

    /// <summary>혼란 상태일 때 5% 확률로 true 반환 (자해 판정).</summary>
    public bool RollSelfAttack()
        => HasEffect(StatusEffectType.Confusion) && Random.value < 0.05f;

    /// <summary>매료 상태 여부에 따른 어그로 범위 배율 반환.</summary>
    public float GetAggroMultiplier()
        => HasEffect(StatusEffectType.Charm) ? 2f : 1f;

    // ── HP 관리 ──────────────────────────────────────────────────────────────
    /// <summary>데미지 적용 (방어력 차감 후). source는 로그용.</summary>
    public void TakeDamage(int rawAmount, string source = "")
    {
        int reduced = Mathf.Max(1, rawAmount - BaseDefense);
        CurrentHp   = Mathf.Max(0, CurrentHp - reduced);
        OnHpChanged?.Invoke(CurrentHp, MaxHp);

        if (CurrentHp <= 0)
        {
            // 부활 아이템 판정
            if (HasReviveItem)
            {
                HasReviveItem = false;
                CurrentHp = Mathf.Max(1, MaxHp / 4);
                OnHpChanged?.Invoke(CurrentHp, MaxHp);
                GameManager.Instance?.UI?.AddLog("부활 아이템이 발동했습니다!");
            }
            else
            {
                OnDeath?.Invoke();
            }
        }
    }

    /// <summary>체력 회복 (회복약 회복량 보정 포함).</summary>
    public void Heal(int baseAmount)
    {
        int total = Mathf.RoundToInt(baseAmount + _healingBoostFlat);
        CurrentHp = Mathf.Min(MaxHp, CurrentHp + Mathf.Max(1, total));
        OnHpChanged?.Invoke(CurrentHp, MaxHp);
    }

    // ── 골드 관리 ─────────────────────────────────────────────────────────────
    public void AddGold(int amount)
    {
        Gold += amount;
        GameManager.Instance?.History?.RecordGoldObtained(amount);
        OnGoldChanged?.Invoke(Gold);
    }

    // ── 경험치 / 레벨업 ───────────────────────────────────────────────────────
    public void AddExp(int amount)
    {
        Exp += amount;
        while (Exp >= ExpToNextLevel)
        {
            Exp -= ExpToNextLevel;
            LevelUp();
        }
    }

    private void LevelUp()
    {
        Level++;
        int hpGain = 10;
        MaxHp     += hpGain;
        CurrentHp  = MaxHp;
        BaseAttack  += 2;
        BaseDefense += 1;

        OnLevelUp?.Invoke(Level);
        GameManager.Instance?.UI?.AddLog($"레벨 업! Lv.{Level}");

        // 5레벨마다 아이템 박스 확장
        if (Level % 5 == 0)
            GameManager.Instance?.Inventory?.ExpandMainBox();
    }

    // ── 상태이상 관리 ─────────────────────────────────────────────────────────
    public bool HasEffect(StatusEffectType type) => _activeEffects.Contains(type);

    public void AddStatusEffect(StatusEffectType type)
    {
        if (type == StatusEffectType.None) return;
        bool already = _activeEffects.Contains(type);
        _activeEffects.Add(type);

        if (type == StatusEffectType.Fatigue && !already)
            _fatigueReduceFraction = Random.Range(0f, 0.4f);

        if (!already)
            OnStatusEffectChanged?.Invoke(type, true);
    }

    public void RemoveStatusEffect(StatusEffectType type)
    {
        if (_activeEffects.Remove(type))
        {
            if (type == StatusEffectType.Fatigue)
                _fatigueReduceFraction = 0f;
            OnStatusEffectChanged?.Invoke(type, false);
        }
    }

    public void ClearAllStatusEffects()
    {
        var copy = new List<StatusEffectType>(_activeEffects);
        foreach (var e in copy) RemoveStatusEffect(e);
    }

    public IReadOnlyCollection<StatusEffectType> GetActiveEffects() => _activeEffects;

    // ── 스탯 보정 (장비 / 각인 적용 인터페이스) ────────────────────────────────
    public void ApplyMaxHpBonus(int bonus)
    {
        MaxHp     = Mathf.Max(1, MaxHp + bonus);
        CurrentHp = Mathf.Clamp(CurrentHp, 0, MaxHp);
        OnHpChanged?.Invoke(CurrentHp, MaxHp);
    }

    public void ApplyAttackBonus(int bonus)  => BaseAttack  = Mathf.Max(0, BaseAttack  + bonus);
    public void ApplyDefenseBonus(int bonus) => BaseDefense = Mathf.Max(0, BaseDefense + bonus);

    public void ApplyWeightLimitBonus(float bonusKg)
        => WeightLimitKg = Mathf.Max(0f, WeightLimitKg + bonusKg);

    public void ApplyHealingBonus(float bonusFlat)
        => _healingBoostFlat = Mathf.Max(0f, _healingBoostFlat + bonusFlat);
}
