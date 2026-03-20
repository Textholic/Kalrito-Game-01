// ============================================================
// EngravingManager.cs
// 슬레이어 스테이터스 각인 관리.
//   - 최대 10개 보관
//   - 플레이어 사망 시 2% 확률로 engravingPool에서 미해금 각인 추가
//   - 게임 시작 시 모든 각인 효과를 플레이어에게 적용
//   - PlayerPrefs로 영구 저장
// ============================================================
using UnityEngine;
using System.Collections.Generic;

public class EngravingManager : MonoBehaviour
{
    public const int MAX_ENGRAVINGS = 10;

    [Header("각인 풀 (Inspector에서 EngravingData 에셋 등록)")]
    [Tooltip("해금 가능한 전체 각인 목록")]
    public EngravingData[] engravingPool;

    private List<EngravingData> _unlocked = new List<EngravingData>();

    public IReadOnlyList<EngravingData> Unlocked => _unlocked;
    public int Count => _unlocked.Count;

    private const string PREFS_KEY = "engravings_unlocked";

    public event System.Action OnEngravingsChanged;

    void Awake() => LoadFromPrefs();

    // ── 해금 시도 (사망 시 카르마 수치 기반 확률) ───────────────────────────
    /// <summary>
    /// 사망 시 호출. karma(0~100)를 확률(0~100%)로 환산해 각인 해금을 시도.
    /// 예: karma=70 → 70% 확률로 미해금·중복되지 않는 각인 중 하나를 랜덤 추가.
    /// </summary>
    /// <returns>새로 해금된 EngravingData, 해금 없으면 null.</returns>
    public EngravingData TryUnlockOnDeath(float karma)
    {
        if (_unlocked.Count >= MAX_ENGRAVINGS) return null;
        if (engravingPool == null || engravingPool.Length == 0) return null;

        float chance = Mathf.Clamp01(karma / 100f);
        if (Random.value > chance) return null;

        // 이미 해금된 각인을 제외한 목록 (중복 방지)
        var available = new List<EngravingData>();
        foreach (var e in engravingPool)
            if (e != null && !_unlocked.Contains(e))
                available.Add(e);

        if (available.Count == 0) return null;

        var chosen = available[Random.Range(0, available.Count)];
        _unlocked.Add(chosen);
        SaveToPrefs();
        OnEngravingsChanged?.Invoke();
        Debug.Log($"[각인] 새 각인 해금: {chosen.engravingName} (카르마 {karma:F1}%, 확률 {chance * 100:F1}%)");
        return chosen;
    }

    // ── 게임 시작 시 모든 각인 적용 ─────────────────────────────────────────
    public void ApplyAllEngravings(PlayerStats player)
    {
        foreach (var eng in _unlocked)
            ApplySingle(eng, player);
    }

    private void ApplySingle(EngravingData eng, PlayerStats player)
    {
        if (eng?.effects == null) return;
        foreach (var e in eng.effects)
        {
            switch (e.effectType)
            {
                case EngravingEffectType.MaxHpBonus:
                    player.ApplyMaxHpBonus(Mathf.RoundToInt(e.value)); break;
                case EngravingEffectType.AttackBonus:
                    player.ApplyAttackBonus(Mathf.RoundToInt(e.value)); break;
                case EngravingEffectType.DefenseBonus:
                    player.ApplyDefenseBonus(Mathf.RoundToInt(e.value)); break;
                case EngravingEffectType.WeightLimitBonus:
                    player.ApplyWeightLimitBonus(e.value); break;
                case EngravingEffectType.HpRegenRateBonus:
                    player.ApplyHealingBonus(e.value); break;
                case EngravingEffectType.PotionHealBonus:
                    player.ApplyHealingBonus(e.value); break;
                case EngravingEffectType.AggroRangeMultiplier:
                    // 매료 상태로 표현
                    if (e.value > 1f) player.AddStatusEffect(StatusEffectType.Charm); break;
                case EngravingEffectType.ForceApplyBurn:
                    player.AddStatusEffect(StatusEffectType.Burn); break;
                case EngravingEffectType.ForceApplyPoison:
                    player.AddStatusEffect(StatusEffectType.Poison); break;
                case EngravingEffectType.ForceApplyCharm:
                    player.AddStatusEffect(StatusEffectType.Charm); break;
                case EngravingEffectType.ForceApplyFatigue:
                    player.AddStatusEffect(StatusEffectType.Fatigue); break;
                case EngravingEffectType.ForceApplyConfusion:
                    player.AddStatusEffect(StatusEffectType.Confusion); break;
                case EngravingEffectType.CursedItemForced:
                    if (e.cursedItem != null)
                        GameManager.Instance?.Inventory?.TryAddItem(e.cursedItem, cursed: true);
                    break;
            }
        }
    }

    // ── PlayerPrefs 저장/불러오기 ────────────────────────────────────────────
    public void SaveToPrefs()
    {
        // 각인 SO 에셋 이름(name)을 쉼표 구분 문자열로 저장
        var names = new List<string>();
        foreach (var e in _unlocked)
            if (e != null) names.Add(e.name);
        PlayerPrefs.SetString(PREFS_KEY, string.Join(",", names));
        PlayerPrefs.Save();
    }

    private void LoadFromPrefs()
    {
        _unlocked.Clear();
        string saved = PlayerPrefs.GetString(PREFS_KEY, "");
        if (string.IsNullOrEmpty(saved)) return;
        if (engravingPool == null) return;

        var names = new HashSet<string>(saved.Split(','));
        foreach (var e in engravingPool)
            if (e != null && names.Contains(e.name))
                _unlocked.Add(e);
    }

    // ── 특정 각인 수동 추가/제거 (디버그 / 에디터용) ──────────────────────────
    public bool AddEngraving(EngravingData data)
    {
        if (_unlocked.Count >= MAX_ENGRAVINGS) return false;
        if (_unlocked.Contains(data)) return false;
        _unlocked.Add(data);
        SaveToPrefs();
        OnEngravingsChanged?.Invoke();
        return true;
    }

    public bool RemoveEngraving(EngravingData data)
    {
        bool removed = _unlocked.Remove(data);
        if (removed) { SaveToPrefs(); OnEngravingsChanged?.Invoke(); }
        return removed;
    }

    // ── 각인 제거 비용 및 골드 소모 제거 ────────────────────────────────────
    /// <summary>
    /// 각인 제거 비용 반환. 이력에 저장된 제거 횟수에 따라 1000→2000→4000→...
    /// </summary>
    public int GetRemoveCost()
    {
        int count = GameManager.Instance?.History?.EngravingRemoveCount ?? 0;
        // 최대 2^20 = 1,048,576,000G 을 상한으로 오버플로우 방지
        int shift = Mathf.Min(count, 20);
        return 1000 * (1 << shift);
    }

    /// <summary>
    /// 골드를 소모해 각인 제거. 성공 시 이력에 제거 횟수 기록.
    /// </summary>
    public bool TryRemoveEngraving(EngravingData data)
    {
        if (!_unlocked.Contains(data)) return false;
        int cost = GetRemoveCost();
        var player = GameManager.Instance?.Player;
        if (player == null || !player.SpendGold(cost)) return false;

        _unlocked.Remove(data);
        GameManager.Instance.History.RecordEngravingRemove();
        SaveToPrefs();
        OnEngravingsChanged?.Invoke();
        Debug.Log($"[각인] '{data.engravingName}' 제거 완료 (비용 {cost}G)");
        return true;
    }
}
