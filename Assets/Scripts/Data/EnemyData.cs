// ============================================================
// EnemyData.cs
// 적 몬스터 정의 ScriptableObject.
// Unity 에디터: Assets > Create > DungeonGame > Enemy Data
// ============================================================
using UnityEngine;

// ── 몬스터 등급 ───────────────────────────────────────────────────────────────
public enum EnemyRank
{
    Normal,   // 일반 몬스터
    Elite,    // 정예 몬스터
    Boss,     // 보스 몬스터
}

// ── 아이템 드롭 테이블 항목 ───────────────────────────────────────────────────
[System.Serializable]
public class ItemDropEntry
{
    [Tooltip("드롭할 아이템 (null이면 골드만 드롭)")]
    public ItemData item;

    [Tooltip("드롭 확률 (0.0 ~ 1.0)")]
    [Range(0f, 1f)]
    public float    dropChance = 0.1f;

    [Tooltip("골드 드롭 최솟값")]
    public int      goldMin    = 0;

    [Tooltip("골드 드롭 최댓값")]
    public int      goldMax    = 0;
}

// ── EnemyData ScriptableObject ───────────────────────────────────────────────
[CreateAssetMenu(fileName = "New Enemy", menuName = "DungeonGame/Enemy Data")]
public class EnemyData : ScriptableObject
{
    // ── 기본 정보 ─────────────────────────────────────────────────────────────
    [Header("기본 정보")]
    [Tooltip("몬스터 이름")]
    public string    enemyName   = "몬스터";

    [TextArea(2, 4)]
    [Tooltip("몬스터 설명")]
    public string    description = "";

    [Tooltip("스프라이트")]
    public Sprite    sprite;

    [Tooltip("몬스터 등급")]
    public EnemyRank rank        = EnemyRank.Normal;

    // ── 기본 스탯 (층수 배율 적용 전) ────────────────────────────────────────
    [Header("기본 스탯 (층수 배율 적용 전 수치)")]
    [Tooltip("기본 체력")]
    public int   baseHp         = 10;

    [Tooltip("기본 공격력")]
    public int   baseAttack     = 3;

    [Tooltip("기본 방어력")]
    public int   baseDefense    = 0;

    [Tooltip("처치 시 기본 경험치")]
    public int   baseExp        = 5;

    [Header("레벨당 스탯 증가량")]
    [Tooltip("레벨당 체력 증가")]
    public float hpPerLevel     = 5f;

    [Tooltip("레벨당 공격력 증가")]
    public float attackPerLevel = 1.5f;

    [Tooltip("레벨당 경험치 증가")]
    public float expPerLevel    = 3f;

    // ── 골드 드롭 ─────────────────────────────────────────────────────────────
    [Header("기본 골드 드롭")]
    [Tooltip("최솟값")]
    public int   goldDropMin    = 1;

    [Tooltip("최댓값")]
    public int   goldDropMax    = 5;

    // ── 어그로 ────────────────────────────────────────────────────────────────
    [Header("어그로")]
    [Tooltip("어그로 감지 범위 (타일 수, 매료 시 2배 적용됨)")]
    public int   aggroRange     = 3;

    // ── 등장 층수 ─────────────────────────────────────────────────────────────
    [Header("등장 층수 범위")]
    [Tooltip("등장 최소 층수")]
    public int   minFloor       = 1;

    [Tooltip("등장 최대 층수 (0이면 제한 없음)")]
    public int   maxFloor       = 5;

    // ── 드롭 테이블 ───────────────────────────────────────────────────────────
    [Header("아이템 드롭 테이블")]
    public ItemDropEntry[] dropTable;

    // ── 피격 시 상태이상 부여 ─────────────────────────────────────────────────
    [Header("피격 시 상태이상 부여 (플레이어에게)")]
    [Tooltip("피격 시 상태이상 부여 확률 (0 = 없음)")]
    [Range(0f, 1f)]
    public float           statusOnHitChance = 0f;

    [Tooltip("부여할 상태이상 종류")]
    public StatusEffectType onHitStatusEffect = StatusEffectType.None;

    // ── 편의 메서드 ───────────────────────────────────────────────────────────
    /// <summary>지정 레벨의 체력을 계산.</summary>
    public int GetHpAtLevel(int level)
        => Mathf.RoundToInt(baseHp + hpPerLevel * (level - 1));

    /// <summary>지정 레벨의 공격력을 계산.</summary>
    public int GetAttackAtLevel(int level)
        => Mathf.RoundToInt(baseAttack + attackPerLevel * (level - 1));

    /// <summary>지정 레벨의 경험치를 계산.</summary>
    public int GetExpAtLevel(int level)
        => Mathf.RoundToInt(baseExp + expPerLevel * (level - 1));

    /// <summary>골드 드롭 랜덤 계산.</summary>
    public int RollGoldDrop()
        => Random.Range(goldDropMin, goldDropMax + 1);
}

/*  ────────────────────────────────────────────────────────────────────────────
 *  예시 몬스터 목록 (Create > DungeonGame > Enemy Data 로 생성)
 *
 *  ■ 1~5층
 *  [고블린]     Normal  HP:8   ATK:2  DEF:0  EXP:4   Gold:1~3  Aggro:3  Floor:1~30
 *  [해골 병사]  Normal  HP:10  ATK:3  DEF:1  EXP:5   Gold:1~4  Aggro:3  Floor:1~30
 *  [독 슬라임]  Normal  HP:6   ATK:2  DEF:0  EXP:4   Gold:0~2  Aggro:2  Floor:1~15
 *               onHitStatusEffect=Poison, chance=0.3
 *
 *  ■ 6~10층
 *  [오크]       Normal  HP:18  ATK:5  DEF:2  EXP:9   Gold:3~6  Aggro:4  Floor:6~30
 *  [불 슬라임]  Normal  HP:12  ATK:4  DEF:0  EXP:7   Gold:2~5  Floor:6~20
 *               onHitStatusEffect=Burn, chance=0.3
 *  [좀비]       Normal  HP:15  ATK:4  DEF:1  EXP:8   Gold:2~5  Floor:6~20
 *               onHitStatusEffect=Poison, chance=0.2
 *
 *  ■ 11~15층
 *  [다크 나이트] Elite  HP:25  ATK:7  DEF:3  EXP:14  Gold:5~10 Floor:11~30
 *  [화염 마법사] Elite  HP:20  ATK:8  DEF:1  EXP:13  Floor:11~25
 *               onHitStatusEffect=Burn, chance=0.5
 *
 *  ■ 16~20층
 *  [트롤]       Elite   HP:35  ATK:10 DEF:5  EXP:20  Gold:8~15 Floor:16~30
 *  [메두사]     Elite   HP:28  ATK:9  DEF:2  EXP:18  Floor:16~30
 *               onHitStatusEffect=Fatigue, chance=0.4
 *
 *  ■ 21~25층
 *  [골렘]       Elite   HP:50  ATK:12 DEF:8  EXP:28  Floor:21~30
 *  [뱀파이어]   Elite   HP:40  ATK:13 DEF:3  EXP:26  Floor:21~30
 *               onHitStatusEffect=Charm, chance=0.3
 *
 *  ■ 26~30층
 *  [악마]       Elite   HP:60  ATK:15 DEF:6  EXP:35  Gold:15~30 Floor:26~30
 *  [리치]       Elite   HP:55  ATK:14 DEF:5  EXP:33  Floor:26~30
 *               onHitStatusEffect=Confusion, chance=0.4
 *
 *  ■ 보스 (5층마다 등장)
 *  [고블린 왕]  Boss    HP:40  ATK:8  DEF:2  EXP:30  Gold:20~40  Floor:5
 *  [오크 군주]  Boss    HP:70  ATK:12 DEF:5  EXP:55  Floor:10
 *  [화염 군주]  Boss    HP:100 ATK:16 DEF:7  EXP:80  Floor:15    onHit=Burn,0.6
 *  [어둠의 기사]Boss    HP:140 ATK:20 DEF:10 EXP:110 Floor:20
 *  [원소 군주]  Boss    HP:180 ATK:24 DEF:12 EXP:150 Floor:25    onHit=Fatigue,0.4
 *  [마왕]       Boss    HP:250 ATK:30 DEF:15 EXP:300 Floor:30    onHit=Confusion,0.5
 *  ──────────────────────────────────────────────────────────────────────────── */
