// ============================================================
// GameHistoryManager.cs
// 게임 이력 영구 저장 (PlayerPrefs).
//   - 최대 도달 층수
//   - 아이템 획득 누적 총수
//   - 골드 획득 누적 총수
//   - 몬스터별 처치 누적 수
//   - 직입당한(기습 공격) 누적 수
//   - 각종 달성 목표 기록
//   - 이력 초기화
// ============================================================
using UnityEngine;
using System.Collections.Generic;

// ── 달성 목표 ID 상수 ─────────────────────────────────────────────────────────
public static class AchievementID
{
    public const string FIRST_FLOOR        = "first_floor";         // 첫 번째 층 입장
    public const string FLOOR_10           = "floor_10";            // 10층 도달
    public const string FLOOR_20           = "floor_20";            // 20층 도달
    public const string FLOOR_30           = "floor_30";            // 30층 클리어
    public const string KILL_100           = "kill_100";            // 총 처치 100마리
    public const string GOLD_1000          = "gold_1000";           // 누적 골드 1,000
    public const string GOLD_10000         = "gold_10000";          // 누적 골드 10,000
    public const string ITEM_50            = "item_50";             // 아이템 50개 획득
    public const string FULL_EQUIP         = "full_equip";          // 장비 슬롯 만석
    public const string NO_SURPRISE_RUN    = "no_surprise_run";     // 직입 없이 10층 돌파
}

public class GameHistoryManager : MonoBehaviour
{
    // ── 통계 수치 ─────────────────────────────────────────────────────────────
    public int MaxFloorReached     { get; private set; } = 0;
    public int TotalItemsObtained  { get; private set; } = 0;
    public int TotalGoldObtained   { get; private set; } = 0;
    public int TotalSurpriseAttacks{ get; private set; } = 0;  // 직입당한 횟수

    // ── 영구 저장 데이터 (이력 초기화 시 함께 리셋) ──────────────────────────
    /// <summary>상점에 입금한 보존 골드 (사망해도 유지, 이력 초기화 시 소멸).</summary>
    public int VaultGold            { get; private set; } = 0;

    /// <summary>각인을 제거한 누적 횟수. 비용 계산에 사용 (1000→2000→4000→...).</summary>
    public int EngravingRemoveCount { get; private set; } = 0;

    private Dictionary<string, int>  _monsterKills   = new Dictionary<string, int>();
    private Dictionary<string, bool> _achievements   = new Dictionary<string, bool>();

    // ── 현재 런 (이번 플레이)에서 직입 없이 진행된 층수 (달성 목표용) ──────────
    private int _currentRunSurpriseFreeFloors = 0;

    // ── PlayerPrefs 키 ────────────────────────────────────────────────────────
    private const string K_MAX_FLOOR   = "hist_maxFloor";
    private const string K_ITEMS       = "hist_items";
    private const string K_GOLD        = "hist_gold";
    private const string K_SURPRISE    = "hist_surprise";
    private const string K_KILLS_JSON  = "hist_kills";
    private const string K_ACH_JSON    = "hist_achievements";
    private const string K_VAULT_GOLD  = "hist_vaultGold";
    private const string K_ENG_REMOVE  = "hist_engRemove";

    public event System.Action<string> OnAchievementUnlocked; // (id)

    void Awake() => LoadFromPrefs();

    // ── 기록 갱신 ────────────────────────────────────────────────────────────

    public void RecordFloor(int floor)
    {
        if (floor > MaxFloorReached)
        {
            MaxFloorReached = floor;
            SaveToPrefs();
        }

        // 달성 목표 체크
        if (floor == 1)  UnlockAchievement(AchievementID.FIRST_FLOOR);
        if (floor >= 10) UnlockAchievement(AchievementID.FLOOR_10);
        if (floor >= 20) UnlockAchievement(AchievementID.FLOOR_20);
        if (floor >= 30) UnlockAchievement(AchievementID.FLOOR_30);

        // 직입 없이 10층 체크
        if (!IsAchieved(AchievementID.NO_SURPRISE_RUN))
        {
            _currentRunSurpriseFreeFloors = floor;
            if (_currentRunSurpriseFreeFloors >= 10)
                UnlockAchievement(AchievementID.NO_SURPRISE_RUN);
        }
    }

    public void RecordItemObtained(int count = 1)
    {
        TotalItemsObtained += count;
        if (TotalItemsObtained >= 50) UnlockAchievement(AchievementID.ITEM_50);
        SaveToPrefs();
    }

    public void RecordGoldObtained(int amount)
    {
        TotalGoldObtained += amount;
        if (TotalGoldObtained >= 1000)  UnlockAchievement(AchievementID.GOLD_1000);
        if (TotalGoldObtained >= 10000) UnlockAchievement(AchievementID.GOLD_10000);
        SaveToPrefs();
    }

    public void RecordMonsterKill(string enemyName)
    {
        if (!_monsterKills.ContainsKey(enemyName))
            _monsterKills[enemyName] = 0;
        _monsterKills[enemyName]++;

        int total = 0;
        foreach (var kv in _monsterKills) total += kv.Value;
        if (total >= 100) UnlockAchievement(AchievementID.KILL_100);

        SaveToPrefs();
    }

    public void RecordSurpriseAttack()
    {
        TotalSurpriseAttacks++;
        _currentRunSurpriseFreeFloors = -1; // 이번 런에서 직입 방지 달성 불가
        SaveToPrefs();
    }

    public void RecordDeath()
    {
        RecordFloor(GameManager.Instance.CurrentFloor);
        _currentRunSurpriseFreeFloors = 0; // 런 초기화
    }

    // ── 달성 목표 ─────────────────────────────────────────────────────────────
    public void UnlockAchievement(string id)
    {
        if (IsAchieved(id)) return;
        _achievements[id] = true;
        OnAchievementUnlocked?.Invoke(id);
        SaveToPrefs();
    }

    public bool IsAchieved(string id)
        => _achievements.TryGetValue(id, out bool v) && v;

    // ── 조회 ─────────────────────────────────────────────────────────────────
    public int GetMonsterKills(string enemyName)
        => _monsterKills.TryGetValue(enemyName, out int v) ? v : 0;

    public Dictionary<string, int> GetAllMonsterKills()
        => new Dictionary<string, int>(_monsterKills);

    public Dictionary<string, bool> GetAllAchievements()
        => new Dictionary<string, bool>(_achievements);

    // ── 상점 금고 (보존 골드) ─────────────────────────────────────────────────
    /// <summary>플레이어 골드를 금고에 입금. 사망해도 보존됨.</summary>
    public bool DepositGold(int amount)
    {
        if (amount <= 0) return false;
        var player = GameManager.Instance?.Player;
        if (player == null || !player.SpendGold(amount)) return false;
        VaultGold += amount;
        SaveToPrefs();
        return true;
    }

    /// <summary>금고에서 플레이어에게 골드 출금.</summary>
    public bool WithdrawGold(int amount)
    {
        if (amount <= 0 || amount > VaultGold) return false;
        VaultGold -= amount;
        GameManager.Instance?.Player?.AddGold(amount);
        SaveToPrefs();
        return true;
    }

    // ── 각인 제거 이력 ────────────────────────────────────────────────────────
    /// <summary>각인 제거 횟수를 1 증가시킴. EngravingManager.TryRemoveEngraving()에서 호출.</summary>
    public void RecordEngravingRemove()
    {
        EngravingRemoveCount++;
        SaveToPrefs();
    }

    // ── 이력 초기화 ──────────────────────────────────────────────────────────
    public void ResetHistory()
    {
        MaxFloorReached      = 0;
        TotalItemsObtained   = 0;
        TotalGoldObtained    = 0;
        TotalSurpriseAttacks = 0;
        VaultGold            = 0;
        EngravingRemoveCount = 0;
        _monsterKills.Clear();
        _achievements.Clear();
        _currentRunSurpriseFreeFloors = 0;
        SaveToPrefs();
        Debug.Log("[GameHistory] 이력이 초기화되었습니다.");
    }

    // ── PlayerPrefs 저장/불러오기 ────────────────────────────────────────────
    private void SaveToPrefs()
    {
        PlayerPrefs.SetInt(K_MAX_FLOOR,  MaxFloorReached);
        PlayerPrefs.SetInt(K_ITEMS,      TotalItemsObtained);
        PlayerPrefs.SetInt(K_GOLD,       TotalGoldObtained);
        PlayerPrefs.SetInt(K_SURPRISE,   TotalSurpriseAttacks);
        PlayerPrefs.SetInt(K_VAULT_GOLD, VaultGold);
        PlayerPrefs.SetInt(K_ENG_REMOVE, EngravingRemoveCount);

        // 몬스터 킬 직렬화
        var killList = new List<KillRecord>();
        foreach (var kv in _monsterKills)
            killList.Add(new KillRecord { n = kv.Key, c = kv.Value });
        PlayerPrefs.SetString(K_KILLS_JSON, JsonUtility.ToJson(new KillWrapper { items = killList }));

        // 달성 목표 직렬화
        var achList = new List<string>();
        foreach (var kv in _achievements)
            if (kv.Value) achList.Add(kv.Key);
        PlayerPrefs.SetString(K_ACH_JSON, JsonUtility.ToJson(new AchWrapper { ids = achList }));

        PlayerPrefs.Save();
    }

    private void LoadFromPrefs()
    {
        MaxFloorReached      = PlayerPrefs.GetInt(K_MAX_FLOOR,  0);
        TotalItemsObtained   = PlayerPrefs.GetInt(K_ITEMS,      0);
        TotalGoldObtained    = PlayerPrefs.GetInt(K_GOLD,       0);
        TotalSurpriseAttacks = PlayerPrefs.GetInt(K_SURPRISE,   0);
        VaultGold            = PlayerPrefs.GetInt(K_VAULT_GOLD, 0);
        EngravingRemoveCount = PlayerPrefs.GetInt(K_ENG_REMOVE, 0);

        // 몬스터 킬
        string killJson = PlayerPrefs.GetString(K_KILLS_JSON, "");
        if (!string.IsNullOrEmpty(killJson))
        {
            var w = JsonUtility.FromJson<KillWrapper>(killJson);
            if (w?.items != null)
                foreach (var r in w.items)
                    _monsterKills[r.n] = r.c;
        }

        // 달성 목표
        string achJson = PlayerPrefs.GetString(K_ACH_JSON, "");
        if (!string.IsNullOrEmpty(achJson))
        {
            var w = JsonUtility.FromJson<AchWrapper>(achJson);
            if (w?.ids != null)
                foreach (var id in w.ids)
                    _achievements[id] = true;
        }
    }

    // ── JSON 직렬화용 내부 클래스 ────────────────────────────────────────────
    [System.Serializable] private class KillRecord  { public string n; public int c; }
    [System.Serializable] private class KillWrapper { public List<KillRecord> items; }
    [System.Serializable] private class AchWrapper  { public List<string> ids; }
}
