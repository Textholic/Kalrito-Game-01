// ============================================================
// DungeonFloorConfig.cs
// 던전 층수 구간별 설정 ScriptableObject.
// Unity 에디터: Assets > Create > DungeonGame > Dungeon Floor Config
// ============================================================
using UnityEngine;

// ── 층수 구간(티어) 설정 ─────────────────────────────────────────────────────
[System.Serializable]
public class FloorTierConfig
{
    [Header("적용 층수 범위")]
    [Tooltip("구간 시작 층수 (포함)")]
    public int fromFloor = 1;

    [Tooltip("구간 끝 층수 (포함)")]
    public int toFloor   = 5;

    [Header("몬스터 레벨 규칙")]
    [Tooltip("일반 몬스터 최대 레벨 상한 (floor + 1 값이 이 값 이하로 제한됨)")]
    public int normalMaxLevelCap = 6;

    [Tooltip("보스 몬스터 레벨 오프셋 (floor + 이 값)")]
    public int bossLevelOffset   = 2;

    [Header("스탯 배율 (층수 구간당 전체 배율)")]
    [Tooltip("이 구간에 적용되는 체력 추가 배율 (1.0 = 기본)")]
    public float tierHpMultiplier     = 1.0f;

    [Tooltip("이 구간에 적용되는 공격력 추가 배율")]
    public float tierAttackMultiplier = 1.0f;

    [Header("등장 가능 일반 몬스터 목록")]
    public EnemyData[] normalEnemies;

    [Header("등장 가능 보스 몬스터 목록 (5층 단위 마지막 층)")]
    public EnemyData[] bossEnemies;
}

// ── DungeonFloorConfig ScriptableObject ──────────────────────────────────────
[CreateAssetMenu(fileName = "DungeonConfig", menuName = "DungeonGame/Dungeon Floor Config")]
public class DungeonFloorConfig : ScriptableObject
{
    [Header("던전 기본 설정")]
    [Tooltip("던전 총 층수 (기본 30층)")]
    public int totalFloors = 30;

    [Tooltip("보스가 등장하는 층수 간격 (기본 5층마다)")]
    public int bossFloorInterval = 5;

    [Tooltip("맵 가로 크기 (타일)")]
    public int mapWidth  = 40;

    [Tooltip("맵 세로 크기 (타일)")]
    public int mapHeight = 25;

    [Header("보스 룸 설정")]
    [Tooltip("보스 룸으로 연결되는 복도 길이 (타일 수, 기본 8)")]
    public int  bossCorridorLength  = 8;

    [Tooltip("보스 룸 복도에 문(Door)을 배치 (항상 true 권장)")]
    public bool bossCorridorHasDoor = true;

    [Tooltip("보스 룸 문 옆 상점 자동 배치 여부 (기본 true)")]
    public bool hasBossRoomShop     = true;

    [Tooltip("다음 층 포탈은 보스 룸 내에만 생성됨 (항상 true, 참고용)")]
    public bool portalOnlyInBossRoom = true;

    [Header("층수 구간별 상세 설정 (5층 단위 6구간)")]
    public FloorTierConfig[] tiers;

    // ── 편의 메서드 ───────────────────────────────────────────────────────────

    /// <summary>주어진 층수에 해당하는 구간 설정을 반환.</summary>
    public FloorTierConfig GetTierForFloor(int floor)
    {
        foreach (var tier in tiers)
            if (floor >= tier.fromFloor && floor <= tier.toFloor)
                return tier;
        return tiers != null && tiers.Length > 0 ? tiers[tiers.Length - 1] : null;
    }

    /// <summary>일반 몬스터 레벨 계산 (층수 + 1, 구간 상한 이하).</summary>
    public int GetNormalMonsterLevel(int floor)
    {
        var tier = GetTierForFloor(floor);
        if (tier == null) return floor + 1;
        return Mathf.Min(floor + 1, tier.normalMaxLevelCap);
    }

    /// <summary>보스 몬스터 레벨 계산 (층수 + 오프셋).</summary>
    public int GetBossLevel(int floor)
    {
        var tier = GetTierForFloor(floor);
        return floor + (tier?.bossLevelOffset ?? 2);
    }

    /// <summary>해당 층이 보스층인지 확인.</summary>
    public bool IsBossFloor(int floor)
        => bossFloorInterval > 0 && floor % bossFloorInterval == 0;

    /// <summary>해당 층에 등장할 수 있는 일반 몬스터 목록 반환.</summary>
    public EnemyData[] GetAvailableNormalEnemies(int floor)
        => GetTierForFloor(floor)?.normalEnemies ?? new EnemyData[0];

    /// <summary>해당 층에 등장할 수 있는 보스 몬스터 목록 반환.</summary>
    public EnemyData[] GetAvailableBossEnemies(int floor)
        => GetTierForFloor(floor)?.bossEnemies ?? new EnemyData[0];
}

/*  ────────────────────────────────────────────────────────────────────────────
 *  권장 구간 설정 (Tiers 배열 요소 6개)
 *
 *  [0] 1~ 5층  normalMaxLevelCap=6,  bossLevelOffset=2, hpMult=1.0, atkMult=1.0
 *  [1] 6~10층  normalMaxLevelCap=11, bossLevelOffset=2, hpMult=1.4, atkMult=1.3
 *  [2]11~15층  normalMaxLevelCap=16, bossLevelOffset=2, hpMult=1.9, atkMult=1.7
 *  [3]16~20층  normalMaxLevelCap=21, bossLevelOffset=2, hpMult=2.5, atkMult=2.2
 *  [4]21~25층  normalMaxLevelCap=26, bossLevelOffset=2, hpMult=3.2, atkMult=2.8
 *  [5]26~30층  normalMaxLevelCap=31, bossLevelOffset=2, hpMult=4.0, atkMult=3.5
 *  ──────────────────────────────────────────────────────────────────────────── */
