# 던전 슬레이어 — 시스템 설계서

> **버전**: 1.0  
> **엔진**: Unity (URP) / C#  
> **프로젝트 경로**: `f:\workspace\My project`

---

## 목차

1. [아키텍처 개요](#1-아키텍처-개요)
2. [씬 흐름도](#2-씬-흐름도)
3. [플레이어 스탯 시스템](#3-플레이어-스탯-시스템)
4. [상태이상 시스템](#4-상태이상-시스템)
5. [아이템 시스템](#5-아이템-시스템)
6. [각인 시스템 (슬레이어 각인)](#6-각인-시스템-슬레이어-각인)
7. [던전 구성](#7-던전-구성)
8. [몬스터 데이터 명세](#8-몬스터-데이터-명세)
9. [게임 이력 & 달성 목표](#9-게임-이력--달성-목표)
10. [데이터 레이어 명세 (ScriptableObject)](#10-데이터-레이어-명세-scriptableobject)
11. [런타임 코어 명세](#11-런타임-코어-명세)
12. [씬 매니저 명세](#12-씬-매니저-명세)
13. [지속성 (PlayerPrefs 키 목록)](#13-지속성-playerprefs-키-목록)

---

## 1. 아키텍처 개요

```
┌──────────────────────────────────────────────────────────────────┐
│ DontDestroyOnLoad 싱글턴 레이어                                   │
│                                                                  │
│  GameManager                                                     │
│    ├── PlayerStats        (플레이어 스탯 / 상태이상)              │
│    ├── InventoryManager   (3종 아이템박스 / 무게)                 │
│    ├── EngravingManager   (각인 10개 / 해금 / 적용)              │
│    ├── GameHistoryManager (이력 / 달성목표 / PlayerPrefs)        │
│    └── AudioManager       (BGM / SFX / PlayerPrefs)             │
│                                                                  │
│  IGameUI ← 현재 활성 씬의 UI 구현체가 등록                        │
└──────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────┐
│ 데이터 레이어 (ScriptableObject)                                  │
│  ItemData / StatusEffectData / EnemyData                         │
│  EngravingData / DungeonFloorConfig                              │
└──────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────┐
│ 씬 레이어                                                         │
│  TitleScene → GameOptionsScene ↔ SettingsScene / GameHistoryScene│
│                GameOptionsScene → GameScene                      │
└──────────────────────────────────────────────────────────────────┘
```

### 핵심 설계 원칙

| 원칙 | 내용 |
|------|------|
| 데이터 분리 | 모든 게임 데이터는 ScriptableObject로 정의, 코드와 분리 |
| 단일 진입점 | `GameManager` 싱글턴이 모든 서브시스템을 소유·초기화 |
| 씬 독립성 | 씬 전환 후에도 런타임 상태 유지 (DontDestroyOnLoad) |
| 이벤트 기반 UI | 스탯 변화 → C# 이벤트 → UI 리프레시 (폴링 없음) |

---

## 2. 씬 흐름도

```
[TitleScene]
    │
    └─► [GameOptionsScene]  ── 로비
            │
            ├─► [SettingsScene]      ── 환경 설정 (BGM/SFX 볼륨)
            │         └─► 돌아가기 → GameOptionsScene
            │
            ├─► [GameHistoryScene]   ── 게임 이력 & 달성목표
            │         └─► 돌아가기 → GameOptionsScene
            │
            └─► [GameScene]          ── 던전 플레이
                      └─► 사망/클리어 → GameOptionsScene
```

### 씬별 상수 (GameManager)

| 상수명 | 값 |
|--------|----|
| `SCENE_TITLE` | `"TitleScene"` |
| `SCENE_LOBBY` | `"GameOptionsScene"` |
| `SCENE_SETTING` | `"SettingsScene"` |
| `SCENE_HISTORY` | `"GameHistoryScene"` |
| `SCENE_GAME` | `"GameScene"` |

### GameBootstrap (TitleScene)

- `GameBootstrap.cs`가 TitleScene에 배치됨
- `Awake()`에서 Resources 폴더의 `GameManager` 프리팹을 자동 스폰
- 프리팹 없을 경우 런타임에 `new GameObject("GameManager")`로 생성

---

## 3. 플레이어 스탯 시스템

### 초기 스탯

| 스탯 | 초기값 | 비고 |
|------|--------|------|
| 레벨 | 1 | - |
| 최대 HP | 100 | 레벨업마다 +10 |
| 현재 HP | 100 | - |
| 기본 공격력 | 10 | 레벨업마다 +2 |
| 기본 방어력 | 0 | 레벨업마다 +1 |
| 골드 | 0 | - |
| 무게 제한 | 300 kg | - |

### 레벨업 규칙

- 경험치 필요량: `레벨 × 50`
- 레벨업 시 보상:
  - 최대 HP +10 (현재 HP도 최대치로 회복)
  - 공격력 +2
  - 방어력 +1
- 5레벨 단위마다 메인 아이템박스 확장 (+1열, +1행)

### 데미지 계산

```
실제 데미지 = max(1, 원래 데미지 - 방어력)
```

### 회복약 회복량

```
실제 회복량 = max(1, POTION_HEAL_BASE(30) + HealingBoostFlat)
```

`HealingBoostFlat`은 장비 장착 효과·각인으로 누적됩니다.

### 부활 시스템

- `HasReviveItem = true` 상태에서 HP가 0이 되면 자동 발동
- 발동 후: `CurrentHp = max(1, MaxHp / 4)`, `HasReviveItem = false`

### 이동 카운터

- `PlayerStats.OnMoved()` 를 이동 1칸마다 호출
- 내부 `_moveCounter` 카운트 증가
- 3회 이동마다 `ProcessPeriodicStatusEffects()` 호출 (화상·중독 주기 데미지)

---

## 4. 상태이상 시스템

### 5종 상태이상

| 이름 | 열거형 | 효과 | 주기 |
|------|--------|------|------|
| 화상 (Burn) | `StatusEffectType.Burn` | 이동 3회마다 1~5 데미지 | 3회 이동 |
| 중독 (Poison) | `StatusEffectType.Poison` | 이동 3회마다 3~8 데미지 | 3회 이동 |
| 피로 (Fatigue) | `StatusEffectType.Fatigue` | 공격력 × (1 − 0~0.4) 감소 | 즉시·지속 |
| 매료 (Charm) | `StatusEffectType.Charm` | 적 어그로 범위 ×2 | 즉시·지속 |
| 혼란 (Confusion) | `StatusEffectType.Confusion` | 이동/행동마다 5% 자해 가능성 | 즉시·지속 |

### 피로 (Fatigue) 상세

- 상태이상 부여 시 `_fatigueReduceFraction = Random.Range(0f, 0.4f)` (0~40% 랜덤)
- 실제 공격력: `max(0, BaseAttack × (1 − _fatigueReduceFraction))`

### 혼란 (Confusion) 상세

- `RollSelfAttack()` 반환값이 `true`인 경우 자해 처리
- 확률: `Random.value < 0.05f` (5%)

### 매료 (Charm) 상세

- `GetAggroMultiplier()` 반환 2.0f → 적 감지 범위 2배 적용

### 상태이상 추가/조회 API

```csharp
player.AddStatusEffect(StatusEffectType.Burn);
player.HasEffect(StatusEffectType.Poison);   // bool
player.RemoveStatusEffect(StatusEffectType.Fatigue);
```

---

## 5. 아이템 시스템

### 5.1 아이템 카테고리

| `ItemCategory` | 설명 |
|----------------|------|
| `Gem` | 보석류 (골드 가치, 무게 있음) |
| `StatusPotion` | 상태이상 포션 (효과 적용 아이템) |
| `Equipment` | 장비 아이템 (장비박스 배치, 효과 즉시 적용) |
| `Misc` | 기타 잡화 |
| `Cursed` | 저주 아이템 (버릴 수 없음) |

### 5.2 아이템 효과 종류 (`ItemEffectType`)

| 열거형 | 설명 | value 단위 |
|--------|------|-----------|
| `None` | 효과 없음 | - |
| `HpRestore` | 즉시 HP 회복 | 정수 (HP량) |
| `HpMaxBonus` | 최대 HP 증가 | 정수 |
| `AttackBonus` | 공격력 증가 | 정수 |
| `DefenseBonus` | 방어력 증가 | 정수 |
| `HealingBoostPct` | 회복약 회복량 추가 | float |
| `ReviveOnDeath` | 사망 시 부활 플래그 설정 | - |
| `WeightLimitBonus` | 무게 제한 증가 (kg) | float |
| `CureStatusEffect` | 특정 상태이상 해제 | `statusEffectType` |
| `ApplyStatusEffect` | 특정 상태이상 부여 | `statusEffectType` |
| `CursedWeight` | 무거운 저주 아이템 (버릴 수 없음) | float (kg) |

### 5.3 아이템 박스 3종

```
┌─────────────────────────────────────────────────────┐
│ 메인 아이템박스 [I 키]                               │
│  초기: 5열 × 3행 = 15칸                             │
│  5레벨마다 열+1, 행+1 (레벨 5 → 6×4, 레벨 10 → 7×5)│
│  저주 아이템은 버리기 불가                           │
│  드래그&드롭으로 슬롯 간 이동 가능                   │
├─────────────────────────────────────────────────────┤
│ 회복약 슬롯 [1 키]                                  │
│  UI 하단 고정 표시                                   │
│  최대 99개                                          │
│  기본 회복량 30 HP + HealingBoostFlat               │
├─────────────────────────────────────────────────────┤
│ 장비 아이템박스 [E 키]                               │
│  1행 × 8칸 고정                                     │
│  장착 시 즉시 효과 적용, 해제 시 효과 제거          │
│  저주 장비는 해제 불가                               │
└─────────────────────────────────────────────────────┘
```

### 5.4 무게 시스템

- 무게 단위: **그램 (g)** — `ItemData.weightGrams` (1 ~ 40,000)
- 인벤토리 표시 단위: **킬로그램 (kg)**
- 기본 무게 제한: **300 kg**
- 300 kg 초과 시: `OnOverweightChanged(true)` 이벤트 발생, 이동 불가 처리
- 무게 제한은 장비 효과·각인으로 증감 가능

### 5.5 아이템 속성 필드

| 필드명 | 타입 | 설명 |
|--------|------|------|
| `itemName` | string | 아이템 이름 |
| `description` | string | 설명 텍스트 |
| `category` | `ItemCategory` | 카테고리 |
| `icon` | Sprite | UI 아이콘 |
| `weightGrams` | int | 무게 (g) |
| `effects[]` | `ItemEffect[]` | 효과 목록 (중복 가능) |
| `isEquipment` | bool | 장비 박스 배치 여부 |
| `isCursed` | bool | 버리기/해제 불가 여부 |
| `goldValue` | int | 판매 골드 가치 |

---

## 6. 각인 시스템 (슬레이어 각인)

### 개요

- 플레이어가 **사망할 때마다** 2% 확률로 새로운 각인 해금
- 최대 **10개** 각인 보유 가능
- 해금된 각인은 다음 게임 시작 시 자동으로 효과 적용
- PlayerPrefs에 영구 저장

### 각인 효과 종류 (`EngravingEffectType`)

| 열거형 | 설명 |
|--------|------|
| `MaxHpBonus` | 최대 HP ± value |
| `HpRegenRateBonus` | 스텝마다 HP 자연 회복률 ± value (%) |
| `WeightLimitBonus` | 무게 제한 ± value (kg) |
| `AttackBonus` | 공격력 ± value |
| `DefenseBonus` | 방어력 ± value |
| `AggroRangeMultiplier` | 적 어그로 감지 범위 배율 (value 배) |
| `ForceApplyBurn` | 게임 시작 시 화상 강제 적용 |
| `ForceApplyPoison` | 게임 시작 시 중독 강제 적용 |
| `ForceApplyCharm` | 게임 시작 시 매료 강제 적용 |
| `ForceApplyFatigue` | 게임 시작 시 피로 강제 적용 |
| `ForceApplyConfusion` | 게임 시작 시 혼란 강제 적용 |
| `CursedItemForced` | 특정 저주 아이템을 인벤토리에 강제 삽입 |
| `PotionHealBonus` | 회복약 회복량 ± value |
| `ReviveChanceBonus` | 부활 아이템 발동 추가 확률 +value (0~1) |

### 예시 각인 목록

#### 버프 각인

| 이름 | 효과 |
|------|------|
| 강철 의지 | 최대 HP +50 |
| 날카로운 감각 | 공격력 +8 |
| 가벼운 발걸음 | 무게 제한 +100 kg |
| 회복의 기도 | 회복약 회복량 +15 |
| 불사의 맹세 | 최대 HP +30, 부활 확률 +10% |

#### 디버프 각인

| 이름 | 효과 |
|------|------|
| 카레의 저주 | 40 kg 카레 아이템 강제 삽입, 어그로 범위 ×2 |
| 독의 각인 | 시작 시 중독, 공격력 -3 |
| 피로의 족쇄 | 시작 시 피로, 무게 제한 -50 kg |
| 화염의 낙인 | 시작 시 화상, 최대 HP -20 |

#### 복합 각인 (버프+디버프)

| 이름 | 효과 |
|------|------|
| 악마와의 계약 | 공격력 +15, 방어력 -5, 시작 시 매료 |
| 광전사의 혼 | 공격력 +10, 최대 HP -30, 시작 시 혼란 |

### 각인 해금 흐름

```
플레이어 사망
    │
    └─► EngravingManager.TryUnlockOnDeath()
            │
            ├─ Random.value > 0.02f ? → 해금 없음 (98% 확률)
            │
            └─ Random.value <= 0.02f ? → 미해금 각인 풀에서 랜덤 선택
                        │
                        ├─ 이미 10개 보유 중 → 해금 없음
                        └─ 해금 성공 → PlayerPrefs 저장
```

---

## 7. 던전 구성

### 기본 설정

| 항목 | 값 |
|------|-----|
| 총 층수 | 30 |
| 보스 등장 간격 | 5층마다 (5, 10, 15, 20, 25, 30층) |
| 맵 크기 | 40 × 25 타일 |
| 티어 구분 | 5층 단위 6구간 |

### 층수 구간별 설정 (DungeonFloorConfig)

| 구간 | 층수 범위 | 일반 최대 레벨 | 보스 오프셋 | HP 배율 | 공격 배율 |
|------|-----------|---------------|-------------|---------|----------|
| 티어 1 | 1 ~ 5층 | 6 | +2 | ×1.0 | ×1.0 |
| 티어 2 | 6 ~ 10층 | 11 | +2 | ×1.4 | ×1.3 |
| 티어 3 | 11 ~ 15층 | 16 | +2 | ×1.9 | ×1.7 |
| 티어 4 | 16 ~ 20층 | 21 | +2 | ×2.5 | ×2.2 |
| 티어 5 | 21 ~ 25층 | 26 | +2 | ×3.2 | ×2.8 |
| 티어 6 | 26 ~ 30층 | 31 | +2 | ×4.0 | ×3.5 |

### 몬스터 레벨 계산

```
일반 몬스터 레벨 = min(현재 층수 + 1, normalMaxLevelCap)
보스 몬스터 레벨 = 현재 층수 + bossLevelOffset
```

### 권장 몬스터 배치표

| 구간 | 일반 몬스터 | 보스 |
|------|------------|------|
| 1 ~ 5층 | 고블린, 해골 병사, 독 슬라임 | 고블린 족장 |
| 6 ~ 10층 | 오크, 불 슬라임, 좀비 | 오크 전사장 |
| 11 ~ 15층 | 다크 나이트, 화염 마법사 | 화염 마법사장 |
| 16 ~ 20층 | 트롤, 메두사 | 석화 메두사 |
| 21 ~ 25층 | 골렘, 뱀파이어 | 뱀파이어 군주 |
| 26 ~ 30층 | 악마, 리치 | 마계의 군주 |

---

## 8. 몬스터 데이터 명세

### EnemyData 필드

| 필드명 | 타입 | 설명 |
|--------|------|------|
| `enemyName` | string | 몬스터 이름 |
| `rank` | `EnemyRank` | 등급 (Normal / Elite / Boss) |
| `baseHp` | int | 기본 HP (레벨 1 기준) |
| `baseAttack` | int | 기본 공격력 |
| `baseDefense` | int | 기본 방어력 |
| `baseExp` | int | 기본 경험치 |
| `hpPerLevel` | float | 레벨당 HP 증가량 |
| `attackPerLevel` | float | 레벨당 공격력 증가량 |
| `expPerLevel` | float | 레벨당 경험치 증가량 |
| `goldDropMin/Max` | int | 골드 드롭 범위 |
| `aggroRange` | float | 어그로 감지 거리 (타일) |
| `minFloor / maxFloor` | int | 등장 가능 층수 범위 |
| `dropTable[]` | `ItemDropEntry[]` | 아이템 드롭 테이블 |
| `statusOnHitChance` | float | 상태이상 부여 확률 (0~1) |
| `onHitStatusEffect` | `StatusEffectType` | 부여할 상태이상 종류 |

### 레벨 스케일 공식

```
GetHpAtLevel(level)     = baseHp     + hpPerLevel     × (level - 1)
GetAttackAtLevel(level) = baseAttack + attackPerLevel  × (level - 1)
GetExpAtLevel(level)    = baseExp    + expPerLevel     × (level - 1)
RollGoldDrop()          = Random.Range(goldDropMin, goldDropMax + 1)
```

### ItemDropEntry 구조

```csharp
public class ItemDropEntry
{
    public ItemData item;        // 드롭 아이템
    public float    dropChance;  // 드롭 확률 (0~1)
    public int      goldMin;     // 골드 드롭 최솟값
    public int      goldMax;     // 골드 드롭 최댓값
}
```

---

## 9. 게임 이력 & 달성 목표

### 게임 이력 수치

| 항목 | 저장 키 | 설명 |
|------|---------|------|
| 최대 도달 층수 | `hist_maxFloor` | 전체 게임 중 최고 층수 |
| 총 아이템 획득 수 | `hist_items` | 누적 아이템 획득 횟수 |
| 총 골드 획득량 | `hist_gold` | 누적 획득 골드 합계 |
| 총 기습 당한 횟수 | `hist_surprises` | 적에게 기습당한 횟수 |
| 몬스터 킬 수 | `hist_kill_{name}` | 종류별 처치 횟수 |

### 달성 목표 10종 (`AchievementID`)

| ID 상수 | 이름 | 달성 조건 |
|---------|------|----------|
| `FIRST_FLOOR` | 첫 발걸음 | 1층 도달 |
| `FLOOR_10` | 심층 탐험가 | 10층 도달 |
| `FLOOR_20` | 어둠의 심연 | 20층 도달 |
| `FLOOR_30` | 던전의 정복자 | 30층 전체 클리어 |
| `KILL_100` | 학살자 | 몬스터 100마리 처치 |
| `GOLD_1000` | 보물 수집가 | 골드 1,000 이상 획득 |
| `GOLD_10000` | 전설의 부富 | 골드 10,000 이상 획득 |
| `ITEM_50` | 아이템 수집광 | 아이템 50개 이상 획득 |
| `FULL_EQUIP` | 완전 무장 | 장비 8칸 모두 채우기 |
| `NO_SURPRISE_RUN` | 귀신 같은 발걸음 | 기습 없이 5층 이상 클리어 |

---

## 10. 데이터 레이어 명세 (ScriptableObject)

### 파일 구조

```
Assets/
  Scripts/
    Data/
      ItemData.cs            ← 아이템 정의
      StatusEffectData.cs    ← 상태이상 파라미터
      EnemyData.cs           ← 몬스터 정의
      EngravingData.cs       ← 각인 정의
      DungeonFloorConfig.cs  ← 던전 층수 구성
    Core/
      GameManager.cs
      PlayerStats.cs
      InventoryManager.cs
      EngravingManager.cs
      GameHistoryManager.cs
      AudioManager.cs
      IGameUI.cs
  Scenes/
    GameBootstrap.cs
    GameHistorySceneManager.cs
    GameOptionsSceneManager.cs
    SettingsSceneManager.cs
  Editor/
    ProjectInitializer.cs
```

### 에셋 생성 메뉴

| 메뉴 경로 | 생성 에셋 | C# 클래스 |
|-----------|----------|-----------|
| `Create > DungeonGame > Item Data` | ItemData | `ItemData` |
| `Create > DungeonGame > Status Effect Data` | StatusEffectData | `StatusEffectData` |
| `Create > DungeonGame > Enemy Data` | EnemyData | `EnemyData` |
| `Create > DungeonGame > Engraving Data` | EngravingData | `EngravingData` |
| `Create > DungeonGame > Dungeon Floor Config` | DungeonConfig | `DungeonFloorConfig` |

---

## 11. 런타임 코어 명세

### GameManager

| 멤버 | 종류 | 설명 |
|------|------|------|
| `Instance` | 정적 프로퍼티 | 싱글턴 인스턴스 |
| `Player` | 프로퍼티 | PlayerStats 서브시스템 |
| `Inventory` | 프로퍼티 | InventoryManager 서브시스템 |
| `Engraving` | 프로퍼티 | EngravingManager 서브시스템 |
| `History` | 프로퍼티 | GameHistoryManager 서브시스템 |
| `Audio` | 프로퍼티 | AudioManager 서브시스템 |
| `UI` | 프로퍼티 | IGameUI 현재 씬 구현체 |
| `CurrentFloor` | 프로퍼티 | 현재 던전 층수 |
| `IsGameActive` | 프로퍼티 | 게임 진행 중 여부 |
| `DungeonConfig` | 프로퍼티 | 던전 설정 ScriptableObject |
| `StartNewGame()` | 메서드 | 새 게임 초기화 후 GameScene 전환 |
| `AdvanceFloor()` | 메서드 | 다음 층으로 이동 |
| `OnPlayerDeath()` | 메서드 | 사망 처리 (각인 해금 시도 → 로비 이동) |
| `GoToLobby/Setting/History/Title()` | 메서드 | 각 씬으로 이동 |

### PlayerStats 이벤트

| 이벤트 | 시그니처 | 발생 시점 |
|--------|---------|----------|
| `OnLevelUp` | `Action<int>` | 레벨업 (새 레벨 전달) |
| `OnHpChanged` | `Action<int, int>` | HP 변경 (현재, 최대) |
| `OnGoldChanged` | `Action<int>` | 골드 변경 |
| `OnDeath` | `Action` | 사망 |
| `OnStatusEffectChanged` | `Action<StatusEffectType, bool>` | 상태이상 추가/제거 |

### InventoryManager 이벤트

| 이벤트 | 시그니처 | 발생 시점 |
|--------|---------|----------|
| `OnInventoryChanged` | `Action` | 아이템 추가/제거/이동 |
| `OnPotionCountChanged` | `Action` | 회복약 개수 변경 |
| `OnOverweightChanged` | `Action<bool>` | 무게 초과 상태 변경 (true=초과) |

---

## 12. 씬 매니저 명세

### GameOptionsSceneManager (로비)

버튼 3개:
- **던전 입장** → `GameManager.Instance.StartNewGame()`
- **환경 설정** → `GameManager.Instance.GoToSetting()`
- **게임 이력** → `GameManager.Instance.GoToHistory()`

### SettingsSceneManager (설정)

- BGM 음량 슬라이더 → `AudioManager.SetBgmVolume(float)`
- 효과음 음량 슬라이더 → `AudioManager.SetSfxVolume(float)`
- **돌아가기** 버튼 → `GameManager.Instance.GoToLobby()`

### GameHistorySceneManager (이력)

표시 항목:
- 최대 도달 층수, 총 아이템, 총 골드, 몬스터 킬 수, 기습 당한 횟수
- 달성 목표 10종 체크리스트

버튼:
- **이력 초기화** → `History.ResetHistory()` 후 씬 리로드
- **돌아가기** → `GameManager.Instance.GoToLobby()`

---

## 13. 지속성 (PlayerPrefs 키 목록)

| 키 | 의미 | 관리 클래스 |
|----|------|------------|
| `audio_bgm` | BGM 볼륨 (0~1) | AudioManager |
| `audio_sfx` | SFX 볼륨 (0~1) | AudioManager |
| `engravings_unlocked` | 해금된 각인 JSON | EngravingManager |
| `hist_maxFloor` | 최대 도달 층수 | GameHistoryManager |
| `hist_items` | 총 아이템 획득 수 | GameHistoryManager |
| `hist_gold` | 총 골드 획득량 | GameHistoryManager |
| `hist_surprises` | 총 기습 당한 횟수 | GameHistoryManager |
| `hist_kill_{name}` | 종류별 몬스터 킬 수 | GameHistoryManager |
| `achv_{id}` | 달성 목표 달성 여부 | GameHistoryManager |
