# 던전 슬레이어 — 개발자 메뉴얼

> **버전**: 1.0  
> **대상 독자**: 이 프로젝트를 유지·확장하는 개발자  
> **설계서 참조**: [DESIGN_SPEC.md](DESIGN_SPEC.md)

---

## 목차

1. [프로젝트 최초 설정](#1-프로젝트-최초-설정)
2. [씬 설정](#2-씬-설정)
3. [ScriptableObject 에셋 생성 방법](#3-scriptableobject-에셋-생성-방법)
4. [아이템 추가하기](#4-아이템-추가하기)
5. [몬스터 추가하기](#5-몬스터-추가하기)
6. [각인 추가하기](#6-각인-추가하기)
7. [던전 설정 수정하기](#7-던전-설정-수정하기)
8. [상태이상 파라미터 수정하기](#8-상태이상-파라미터-수정하기)
9. [달성 목표 추가하기](#9-달성-목표-추가하기)
10. [씬 전환 방법](#10-씬-전환-방법)
11. [오디오 시스템 사용법](#11-오디오-시스템-사용법)
12. [UI 연동 방법 (IGameUI)](#12-ui-연동-방법-igameui)
13. [플레이어 스탯 조작 API](#13-플레이어-스탯-조작-api)
14. [인벤토리 조작 API](#14-인벤토리-조작-api)
15. [각인 시스템 조작 API](#15-각인-시스템-조작-api)
16. [게임 이력 기록 API](#16-게임-이력-기록-api)
17. [키 입력 레퍼런스](#17-키-입력-레퍼런스)
18. [보스 룸 UI 사용법](#18-보스-룸-ui-사용법)
19. [상점 시스템 사용법](#19-상점-시스템-사용법)
20. [각인 패널 UI 설정](#20-각인-패널-ui-설정)
21. [자주 발생하는 문제 & 해결책](#21-자주-발생하는-문제--해결책)

---

## 1. 프로젝트 최초 설정

Unity Editor에서 `Tools` 메뉴를 순서대로 실행합니다.

### 실행 순서

| 순서 | 메뉴 | 설명 |
|------|------|------|
| 1 | `Tools > 1. 씬 생성` | TitleScene, GameOptionsScene, SettingsScene, GameHistoryScene, GameScene 생성 |
| 2 | `Tools > 2. 스크립트 할당` | 각 씬에 해당 SceneManager 스크립트 자동 할당 |
| 3 | `Tools > 3. 빌드 설정` | Build Settings에 5개 씬 자동 등록 |
| 4 | `Tools > 4. GameManager 프리팹 생성` | `Assets/Resources/GameManager.prefab` 생성 (DontDestroyOnLoad용) |

> **주의**: 반드시 1→2→3→4 순서로 실행하세요.  
> 이미 씬이 존재하면 1번 단계를 건너뛰어도 됩니다.

### GameManager 프리팹 구성

`Tools > 4` 실행 후 생성되는 프리팹 구성:

```
GameManager (GameObject)
  ├── GameManager.cs      (싱글턴 컴포넌트)
  ├── PlayerStats.cs      (자동 생성)
  ├── InventoryManager.cs (자동 생성)
  ├── EngravingManager.cs (자동 생성)
  ├── GameHistoryManager.cs (자동 생성)
  └── AudioManager.cs     (자동 생성)
```

`GameManager.cs`의 `EngravingPool` 필드에 각인 에셋들을 등록해야 합니다 (아래 §6 참조).

---

## 2. 씬 설정

### TitleScene

- `GameBootstrap.cs` 컴포넌트를 씬의 빈 GameObject에 배치
- `GameBootstrap`이 `Awake()`에서 `Resources/GameManager` 프리팹 자동 스폰

### GameOptionsScene (로비)

- `GameOptionsSceneManager.cs` 컴포넌트를 배치
- Inspector에서 버튼 3개 연결:
  - `startButton` → **던전 입장** 버튼
  - `settingsButton` → **환경 설정** 버튼
  - `historyButton` → **게임 이력** 버튼

### SettingsScene

- `SettingsSceneManager.cs` 배치
- BGM/SFX 슬라이더는 코드 내에서 동적으로 생성됩니다.
  (Inspector 연결 불필요)

### GameHistoryScene

- `GameHistorySceneManager.cs` 배치
- Inspector에서 연결:
  - `contentRoot`: 이력 텍스트가 들어갈 부모 Transform
  - `resetButton`: 이력 초기화 버튼
  - `backButton`: 돌아가기 버튼

### GameScene

- 게임 플레이 씬. `GameManager.IsGameActive = true` 상태에서 실행
- GameScene의 UI 스크립트는 `IGameUI` 인터페이스를 구현해야 합니다 (§12 참조)

---

## 3. ScriptableObject 에셋 생성 방법

Project 탭에서 원하는 폴더를 선택 후 우클릭 → `Create > DungeonGame > [타입]`

| 타입 | 메뉴 경로 | 저장 권장 위치 |
|------|-----------|---------------|
| 아이템 | `Create > DungeonGame > Item Data` | `Assets/Resources/Items/` |
| 상태이상 | `Create > DungeonGame > Status Effect Data` | `Assets/Resources/StatusEffects/` |
| 몬스터 | `Create > DungeonGame > Enemy Data` | `Assets/Resources/Enemies/` |
| 각인 | `Create > DungeonGame > Engraving Data` | `Assets/Resources/Engravings/` |
| 던전 설정 | `Create > DungeonGame > Dungeon Floor Config` | `Assets/Resources/` |

---

## 4. 아이템 추가하기

### 단계별 절차

1. `Assets/Resources/Items/` 폴더에 새 **Item Data** 에셋 생성
2. Inspector에서 아래 필드를 채웁니다:

```
Item Name        : 아이템 이름 (예: "화염의 검")
Description      : 설명 텍스트
Category         : Equipment (장비) / Gem / StatusPotion / Misc / Cursed
Icon             : 아이콘 Sprite
Weight Grams     : 무게 (그램 단위, 1000 = 1kg)
Is Equipment     : 장비박스에 배치할 경우 체크
Is Cursed        : 버리기/해제 불가 아이템이면 체크
Gold Value       : 판매 가격
Is Consumable    : 상점에서 판매 가능한 소비 아이템이면 체크
Shop Price       : 상점 구매 가격 (0이면 상점에서 판매 안 됨)
Effects          : 효과 목록 (아래 참조)
```

### 효과(Effects) 설정 예시

**회복 포션 (HP +50)**
```
Effects[0]:
  Effect Type : HpRestore
  Value       : 50
```

**공격력 장비 (+8 공격)**
```
Effects[0]:
  Effect Type : AttackBonus
  Value       : 8
```

**저주 아이템 (버릴 수 없는 40kg 카레)**
```
Is Cursed    : true
Weight Grams : 40000
Effects[0]:
  Effect Type  : CursedWeight
  Value        : 40
```

**상태이상 포션 (중독 해제)**
```
Effects[0]:
  Effect Type       : CureStatusEffect
  Status Effect Type: Poison
```

### 코드에서 아이템 참조 시

```csharp
// ItemData를 직접 참조하거나 Resources.Load로 불러옵니다
ItemData item = Resources.Load<ItemData>("Items/화염의 검");
GameManager.Instance.Inventory.TryAddItem(item);
```

---

## 5. 몬스터 추가하기

### 단계별 절차

1. `Assets/Resources/Enemies/` 에 새 **Enemy Data** 에셋 생성
2. Inspector에서 필드 설정:

```
Enemy Name          : 이름 (예: "불 골렘")
Rank                : Normal / Elite / Boss
Base Hp             : 기본 HP (레벨 1)
Base Attack         : 기본 공격력
Base Defense        : 기본 방어력
Base Exp            : 기본 경험치
Hp Per Level        : 레벨당 HP 증가량
Attack Per Level    : 레벨당 공격력 증가량
Exp Per Level       : 레벨당 경험치 증가량
Gold Drop Min/Max   : 골드 드롭 범위
Aggro Range         : 어그로 감지 거리 (타일 단위)
Min Floor / Max Floor : 등장 가능 층수 범위
Status On Hit Chance: 공격 시 상태이상 부여 확률 (0~1)
On Hit Status Effect: 부여할 상태이상 (없으면 None)
Boss Battle BGM     : (Rank = Boss 전용) 전투 시 재생할 BGM AudioClip (null이면 기본 BGM)
Drop Table          : 아이템 드롭 테이블 (아래 참조)
```

### Drop Table 설정 예시

```
Drop Table[0]:
  Item        : (ItemData 에셋 연결)
  Drop Chance : 0.3   ← 30% 확률
  Gold Min    : 0
  Gold Max    : 0
Drop Table[1]:
  Item        : null   ← 아이템 없이 골드만 드롭
  Drop Chance : 1.0
  Gold Min    : 10
  Gold Max    : 30
```

### DungeonFloorConfig에 등록

몬스터를 만든 후, 해당 층수 구간의 `FloorTierConfig`에 등록합니다:

1. `Assets/Resources/DungeonConfig.asset` 선택
2. `Tiers[구간]` → `Normal Enemies` 또는 `Boss Enemies` 배열에 추가

---

## 6. 각인 추가하기

### 단계별 절차

1. `Assets/Resources/Engravings/` 에 새 **Engraving Data** 에셋 생성
2. Inspector에서 필드 설정:

```
Engraving Name : 각인 이름 (예: "강철 의지")
Description    : 설명 (효과 서술 포함)
Icon           : 아이콘 Sprite
Is Debuff      : 디버프 각인이면 체크 (UI 빨간색 표시)
Effects        :
  Effects[0]:
    Effect Type : MaxHpBonus
    Value       : 50
```

### GameManager 프리팹에 등록

1. `Assets/Resources/GameManager.prefab` 선택
2. `EngravingManager` 컴포넌트 → `Engraving Pool` 배열에 새 각인 에셋 추가

> 풀에 등록되지 않은 각인은 해금 추첨에 포함되지 않습니다.

### 저주 아이템 강제 삽입 각인 설정

```
Effects[0]:
  Effect Type  : CursedItemForced
  Cursed Item  : (저주 ItemData 에셋 연결)
Effects[1]:
  Effect Type  : AggroRangeMultiplier
  Value        : 2.0
```

---

## 7. 던전 설정 수정하기

### DungeonFloorConfig 에셋 경로

```
Assets/Resources/DungeonConfig.asset
```

### 총 층수 변경

`Total Floors` 필드를 수정합니다.  
보스 층수 간격은 `Boss Floor Interval`로 조정합니다.

### 구간(티어) 설정 변경

`Tiers` 배열 내 각 `FloorTierConfig` 요소에서:

| 필드 | 수정 내용 |
|------|----------|
| `From Floor / To Floor` | 적용 층수 범위 |
| `Normal Max Level Cap` | 일반 몬스터 최대 레벨 상한 |
| `Boss Level Offset` | 보스 몬스터 레벨 = 층수 + 이 값 |
| `Tier Hp Multiplier` | 전체 HP 배율 |
| `Tier Attack Multiplier` | 전체 공격력 배율 |
| `Normal Enemies[]` | 등장 가능 일반 몬스터 목록 |
| `Boss Enemies[]` | 등장 가능 보스 목록 |

### GameManager에 DungeonConfig 연결

```csharp
// GameManager Inspector에서 DungeonConfig 필드에 에셋을 연결하거나
// 코드에서 Resources.Load로 로드합니다
DungeonConfig = Resources.Load<DungeonFloorConfig>("DungeonConfig");
```

---

## 8. 상태이상 파라미터 수정하기

### StatusEffectData 에셋 경로

```
Assets/Resources/StatusEffects/
```

### 각 상태이상별 수정 가능 필드

| 필드 | 기본값 | 설명 |
|------|--------|------|
| `minDamage` | 1 | 화상/중독 최소 데미지 |
| `maxDamageExclusive` | 6 (화상) / 9 (중독) | 최대 데미지 (미포함) |
| `minAttackReduceFraction` | 0.0 | 피로: 공격력 감소 최솟값 |
| `maxAttackReduceFraction` | 0.4 | 피로: 공격력 감소 최댓값 (40%) |
| `aggroRangeMultiplier` | 2.0 | 매료: 어그로 범위 배율 |
| `selfAttackChance` | 0.05 | 혼란: 자해 확률 (5%) |

> **주의**: `StatusEffectData`는 참조용 데이터입니다.  
> 실제 데미지 계산은 `PlayerStats.ProcessPeriodicStatusEffects()`에 하드코딩되어 있으므로, 파라미터를 바꾼 후 해당 메서드도 함께 수정해야 합니다.

---

## 9. 달성 목표 추가하기

### 1단계: 상수 추가

`Assets/Scripts/Core/GameHistoryManager.cs`의 `AchievementID` 클래스에 새 상수를 추가합니다:

```csharp
public static class AchievementID
{
    // 기존 항목들...
    public const string MY_NEW_ACHIEVEMENT = "my_new_achievement"; // 추가
}
```

### 2단계: 달성 조건 코드 작성

달성 조건이 충족되는 시점에 아래를 호출합니다:

```csharp
GameManager.Instance.History.UnlockAchievement(AchievementID.MY_NEW_ACHIEVEMENT);
```

### 3단계: UI에 표시

`GameHistorySceneManager.cs`의 달성목표 표시 루프에 새 항목 추가:

```csharp
// 기존 달성목표 표시 코드 아래에 추가
AddAchievementRow(AchievementID.MY_NEW_ACHIEVEMENT, "달성 목표 이름", "달성 조건 설명");
```

---

## 10. 씬 전환 방법

코드에서 씬을 전환할 때는 **반드시 `GameManager`의 메서드를 사용**합니다.  
`SceneManager.LoadScene()`을 직접 호출하지 마세요.

```csharp
// 로비로 이동
GameManager.Instance.GoToLobby();

// 설정 화면으로 이동
GameManager.Instance.GoToSetting();

// 게임 이력 화면으로 이동
GameManager.Instance.GoToHistory();

// 타이틀로 이동
GameManager.Instance.GoToTitle();

// 새 게임 시작 (플레이어 초기화 + GameScene 이동)
GameManager.Instance.StartNewGame();

// 다음 층으로 이동
GameManager.Instance.AdvanceFloor();

// 사망 처리 (각인 해금 시도 포함)
GameManager.Instance.OnPlayerDeath();
```

---

## 11. 오디오 시스템 사용법

```csharp
var audio = GameManager.Instance.Audio;

// BGM 재생
audio.PlayBgm(bgmAudioClip);

// BGM 정지
audio.StopBgm();

// BGM 볼륨 설정 (0~1, PlayerPrefs 자동 저장)
audio.SetBgmVolume(0.8f);

// SFX 재생
audio.PlaySfx(sfxAudioClip);

// SFX 볼륨 설정 (0~1, PlayerPrefs 자동 저장)
audio.SetSfxVolume(0.5f);
```

### AudioManager Inspector 설정

`GameManager.prefab`의 `AudioManager` 컴포넌트:
- `BgmSource`: BGM용 AudioSource
- `SfxSource`: SFX용 AudioSource

---

## 12. UI 연동 방법 (IGameUI)

GameScene의 UI 스크립트는 `IGameUI` 인터페이스를 구현해야 합니다.

```csharp
public class MyGameUI : MonoBehaviour, IGameUI
{
    public void AddLog(string message)
    {
        // 로그 메시지를 화면에 표시
        logText.text += message + "\n";
    }

    public void RefreshHUD()
    {
        // HP, 골드, 층수 등 HUD 갱신
        var player = GameManager.Instance.Player;
        hpText.text = $"{player.CurrentHp} / {player.MaxHp}";
        goldText.text = $"{player.Gold}";
    }
}
```

### GameScene Awake에서 등록

```csharp
void Awake()
{
    GameManager.Instance.UI = GetComponent<MyGameUI>();
}

void OnDestroy()
{
    if (GameManager.Instance != null)
        GameManager.Instance.UI = null;
}
```

---

## 13. 플레이어 스탯 조작 API

```csharp
var player = GameManager.Instance.Player;

// 데미지 적용 (방어력 자동 차감)
player.TakeDamage(50, "함정");

// HP 회복
player.Heal(30);

// 골드 추가
player.AddGold(100);

// 경험치 추가 (자동 레벨업 처리)
player.AddExp(200);

// 상태이상 추가
player.AddStatusEffect(StatusEffectType.Burn);

// 상태이상 제거
player.RemoveStatusEffect(StatusEffectType.Burn);

// 상태이상 확인
bool isBurning = player.HasEffect(StatusEffectType.Burn);

// 실제 공격력 (피로 감소 적용 후)
int atk = player.GetEffectiveAttack();

// 어그로 배율 (매료 적용 후)
float aggroMult = player.GetAggroMultiplier();

// 혼란 자해 판정
bool selfAtk = player.RollSelfAttack();

// 1타일 이동 시 반드시 호출 (화상/중독 주기 데미지 트리거)
player.OnMoved();

// 새 게임 초기화
player.InitializeNewGame();

// 골드 소모 (상점 구매, 각인 제거 등)
bool ok = player.SpendGold(500);  // 잔액 부족 시 false 반환

// 카르마 조회 (0~100)
float karma = player.Karma;

// 카르마 변경 이벤트 구독
player.OnKarmaChanged += (newKarma) => { /* UI 갱신 등 */ };

// 일반 몬스터 처치 시 카르마 증가 (0~2.5% 랜덤)
player.AddKarmaOnNormalKill();

// 보스 처치 시 카르마 증가 (5~10% 랜덤)
player.AddKarmaOnBossKill();

// 소비 아이템(ReduceKarma) 사용 시 카르마 감소
player.ReduceKarma(10f);
```

---

## 14. 인벤토리 조작 API

```csharp
var inv = GameManager.Instance.Inventory;

// 아이템 추가 (자동으로 적절한 박스에 배치)
bool ok = inv.TryAddItem(itemData);

// 저주 아이템으로 추가
bool ok = inv.TryAddItem(itemData, cursed: true);

// 메인박스 아이템 제거 (저주 아이템은 실패)
bool ok = inv.TryDiscardMainItem(col, row);

// 메인박스 아이템 이동
bool ok = inv.TryMoveMainItem(fromCol, fromRow, toCol, toRow);

// 장비 슬롯 해제 (저주 장비는 실패)
bool ok = inv.TryUnequip(slotIndex);

// 회복약 추가
inv.AddPotion(5);

// 회복약 사용 (1번 키)
bool ok = inv.UsePotion();

// 무게 초과 여부
bool over = inv.CurrentWeightKg > GameManager.Instance.Player.WeightLimitKg;

// 인벤토리 초기화 (새 게임 시)
inv.Reset();
```

### 메인박스 슬롯 직접 접근

```csharp
// 특정 슬롯의 아이템 읽기
InventorySlot slot = inv.GetMainSlot(col, row);
if (slot != null) Debug.Log(slot.data.itemName);

// 장비 슬롯 읽기
InventorySlot equip = inv.GetEquipSlot(index);
```

---

## 15. 각인 시스템 조작 API

```csharp
var eng = GameManager.Instance.Engraving;

// 사망 시 각인 해금 시도 (GameManager.OnPlayerDeath()에서 자동 호출됨)
// karma(0~100) 값이 해금 확률(%)로 사용됩니다
eng.TryUnlockOnDeath(player.Karma);

// 해금된 각인 목록 가져오기
List<EngravingData> unlocked = eng.GetUnlockedEngravings();

// 새 게임 시작 시 모든 각인 효과 적용 (GameManager.StartNewGame()에서 자동 호출됨)
eng.ApplyAllEngravings(player);

// 해금 여부 확인
bool has = eng.IsUnlocked(engravingData);

// 현재 각인 제거 비용 조회 (1,000 × 2^제거누적횟수)
int cost = eng.GetRemoveCost();

// 각인 제거: 비용 차감 후 각인 삭제 (실패 시 false)
bool ok = eng.TryRemoveEngraving(engravingData);
```

### 각인 풀 수동 등록 (런타임)

```csharp
// EngravingManager.engravingPool 배열에 에셋을 추가합니다.
// 주로 Inspector에서 설정하고, 런타임에는 수정하지 않는 것을 권장합니다.
```

---

## 16. 게임 이력 기록 API

```csharp
var hist = GameManager.Instance.History;

// 층수 도달 기록
hist.RecordFloor(currentFloor);

// 아이템 획득 기록
hist.RecordItemObtained();          // 1개
hist.RecordItemObtained(count);     // 복수

// 골드 획득 기록
hist.RecordGoldObtained(amount);

// 몬스터 처치 기록
hist.RecordMonsterKill(enemyName);

// 기습 당한 횟수 기록
hist.RecordSurpriseAttack();

// 사망 기록
hist.RecordDeath();

// 달성목표 해금
hist.UnlockAchievement(AchievementID.FLOOR_10);

// 달성목표 확인
bool done = hist.IsAchievementUnlocked(AchievementID.FLOOR_10);

// 이력 전체 초기화
hist.ResetHistory();

// 골드를 금고에 입금 (플레이어 골드 차감, 잔액 부족 시 false)
bool ok = hist.DepositGold(500);

// 금고에서 플레이어 골드로 출금 (금고 부족 시 false)
bool ok2 = hist.WithdrawGold(500);

// 현재 금고 보관 골드 조회
int vault = hist.VaultGold;

// 각인 제거 횟수 기록 (TryRemoveEngraving 내부에서 자동 호출됨)
hist.RecordEngravingRemove();
```

---

## 17. 키 입력 레퍼런스

> 아래 키 입력은 GameScene에서 유효합니다.  
> 실제 Input 처리는 GameScene의 UI 스크립트나 Player 컨트롤러에서 구현합니다.

| 키 | 기능 |
|----|------|
| `W / A / S / D` | 플레이어 이동 (이동 시 `player.OnMoved()` 호출 필요) |
| `1` | 회복약 사용 (`inventory.UsePotion()`) |
| `I` | 메인 아이템박스 열기/닫기 |
| `E` | 장비 아이템박스 열기/닫기 |
| `P` | 회복약 슬롯 표시 (UI 하단 고정, 별도 토글 불필요) |
| `Escape` | 인벤토리/설정 닫기 |

---

## 18. 보스 룸 UI 사용법

`BossRoomUI.cs`를 보스 룸 씬의 Canvas 하위에 배치하고, 보스 전투 시스템에서 아래 메서드를 호출합니다.

### Inspector 연결

| 필드 | 타입 | 설명 |
|------|------|------|
| `hudRoot` | GameObject | HUD 전체 루트 (Show/Hide 대상) |
| `bossNameText` | TextMeshProUGUI | 보스 이름 텍스트 |
| `bossDescText` | TextMeshProUGUI | 보스 설명 텍스트 |
| `hpSlider` | Slider | 보스 HP 슬라이더 |
| `hpText` | TextMeshProUGUI | "현재/최대 HP" 텍스트 |

### 코드 사용 예시

```csharp
// 보스 룸 진입 시
bossRoomUI.Show(bossEnemyData, currentHp, maxHp);

// HP 변경 시마다
bossRoomUI.UpdateHp(currentHp, maxHp);

// 보스 처치 / 룸 이탈 시
bossRoomUI.Hide();
```

> `Show()` 호출 시 `EnemyData.bossBattleBgm`이 null이 아니면 자동으로 BGM이 교체됩니다.

---

## 19. 상점 시스템 사용법

`ShopManager.cs`를 보스 룸 씬의 적절한 GameObject에 배치합니다.

### Inspector 연결

| 필드 | 타입 | 설명 |
|------|------|------|
| `allConsumables` | `ItemData[]` | 상점 진열 가능한 소비 아이템 풀 (isConsumable=true, shopPrice>0인 것들) |
| `shopSlotCount` | int | 한 번에 진열할 아이템 수 (기본값: 4) |

### 코드 사용 예시

```csharp
var shop = GetComponent<ShopManager>();

// 상점 열기 (아이템 랜덤 진열)
shop.Open();

// 아이템 구매 시도 (골드 부족 또는 인벤토리 가득 찼으면 false)
bool ok = shop.TryBuyItem(itemData);

// 금고 입금 (플레이어 소지 골드 → VaultGold)
bool ok = shop.TryDeposit(200);

// 금고 출금 (VaultGold → 플레이어 소지 골드)
bool ok = shop.TryWithdraw(200);

// 현재 금고 잔액 확인
int vault = shop.VaultGold;
```

> 금고(VaultGold)는 **사망해도 초기화되지 않으며**, `hist_vaultGold` 키로 PlayerPrefs에 저장됩니다.

---

## 20. 각인 패널 UI 설정

`EngravingPanelUI.cs`를 로비(GameOptionsScene) Canvas에 배치합니다.

### Inspector 연결

| 필드 | 타입 | 설명 |
|------|------|------|
| `slotPrefab` | `EngravingSlotUI` | 슬롯 단위 프리팩 |
| `gridParent` | Transform | GridLayoutGroup이 붙은 부모 Transform |

### 동작 방식

1. `Awake()`에서 `BuildSlots()`로 슬롯 10개 동적 생성
2. `Start()`에서 `Refresh()`로 현재 해금 각인 목록 반영
3. `EngravingManager.OnEngravingsChanged` 이벤트를 구독하여 각인 변화 시 자동 갱신

### EngravingSlotUI 동작

| 입력 | 동작 |
|------|------|
| 마우스 오버 | 이름·효과·설명·제거 비용 툴팁 카드 표시 |
| 마우스 아웃 | 툴팁 닫힄 |
| 클릭 | "이 각인을 제거하시겠습니까? (비용: XXX G)" 팝업 |
| 팝업 확인 | `EngravingManager.TryRemoveEngraving()` 호출 |

---

## 21. 자주 발생하는 문제 & 해결책

### Unity Safe Mode 진입 (컴파일 에러)

**증상**: Unity를 열면 "Safe Mode" 팝업이 뜨고 스크립트가 로드되지 않음

**원인 및 해결**:

1. **`#if / #endif` 불일치**: Editor 전용 스크립트에서 `#if UNITY_EDITOR`에 대응하는 `#endif`가 누락된 경우.
   - `Assets/Editor/ProjectInitializer.cs` 등 Editor 스크립트 파일 끝에 `#endif`가 있는지 확인

2. **누락된 참조**: 삭제된 에셋을 참조하는 스크립트가 있는 경우.
   - Console 탭 에러 메시지를 확인하고 해당 스크립트 수정

3. **네임스페이스 충돌**: 중복된 클래스명.
   - 새 스크립트 작성 시 기존 클래스명과 겹치지 않도록 주의

---

### 각인 효과가 게임에 반영되지 않는다

- `GameManager.prefab`의 `EngravingManager.engravingPool` 배열에 각인 에셋이 등록되어 있는지 확인
- `StartNewGame()` 호출 시 `eng.ApplyAllEngravings(player)`가 실행되는지 확인

---

### 몬스터가 특정 층에서 나타나지 않는다

- `DungeonConfig.asset`의 해당 구간 `FloorTierConfig.normalEnemies[]`에 몬스터가 등록되어 있는지 확인
- `EnemyData.minFloor / maxFloor` 범위가 해당 층수를 포함하는지 확인

---

### 무게 초과 상태가 해제되지 않는다

- 저주 아이템(`isCursed = true`)은 버릴 수 없어 무게에서 제외되지 않습니다.
- 의도된 동작입니다. 저주 각인 또는 부활 후에만 저주가 해제됩니다.

---

### PlayerPrefs 데이터 초기화가 필요할 때

```csharp
// 특정 키만 초기화
PlayerPrefs.DeleteKey("engravings_unlocked");

// 전체 초기화 (개발/테스트용)
PlayerPrefs.DeleteAll();
PlayerPrefs.Save();

// 게임 이력만 초기화 (GameHistoryScene 버튼 또는 코드)
GameManager.Instance.History.ResetHistory();
```

---

### 새 씬 추가 시 Build Settings 등록

1. `ProjectInitializer.cs`의 `SetupBuildSettings()` 메서드에 새 씬 경로 추가
2. 또는 Unity 메뉴 `File > Build Settings`에서 직접 Drag & Drop으로 추가

---

### DontDestroyOnLoad 중복 생성 문제

- GameManager는 `Awake()`에서 중복 인스턴스를 `Destroy(gameObject)`로 파괴합니다.
- TitleScene 외 다른 씬에서 직접 `GameManager.prefab`을 씬에 배치하면 중복 생성됩니다.
- **GameManager 프리팹은 TitleScene의 `GameBootstrap`을 통해서만 생성**되어야 합니다.
