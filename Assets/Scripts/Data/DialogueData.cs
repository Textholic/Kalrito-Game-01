// ============================================================
// DialogueData.cs
// 대화 시스템 공통 데이터 정의 (열거형 · 구조체 · 헬퍼)
//   - DialogueConditionType  : 달성목표 연동 조건 열거형
//   - CharacterType          : 대화 캐릭터 종류
//   - DialogueLine           : 대사 1줄 (캐릭터 · 표정 · 대사)
//   - DialogueConditionHelper: 조건 ↔ AchievementID 변환
//
// ScriptableObject 클래스는 별도 파일에 있습니다.
//   DialogueSet.cs      — 대화 세트
//   DialogueDatabase.cs — 전체 데이터베이스
//
// ※ 달성목표 추가 방법
//     1. AchievementID          에 상수 추가
//     2. DialogueConditionType  에 Ach_XXX 값 추가
//     3. DialogueConditionHelper._map 에 매핑 한 줄 추가
//     4. Inspector 에서 해당 조건의 DialogueSet 생성
// ============================================================
using UnityEngine;
using System.Collections.Generic;

// ── 대화 조건 (달성목표와 1:1 대응) ──────────────────────────────────────────
// General = 일반 대화(랜덤), Ach_* = 해당 달성목표 최초 달성 시 1회 표시
public enum DialogueConditionType
{
    General            = 0,
    Ach_FirstFloor     = 1,   // AchievementID.FIRST_FLOOR
    Ach_Floor10        = 2,   // AchievementID.FLOOR_10
    Ach_Floor20        = 3,   // AchievementID.FLOOR_20
    Ach_Floor30        = 4,   // AchievementID.FLOOR_30
    Ach_Kill100        = 5,   // AchievementID.KILL_100
    Ach_Gold1000       = 6,   // AchievementID.GOLD_1000
    Ach_Gold10000      = 7,   // AchievementID.GOLD_10000
    Ach_Item50         = 8,   // AchievementID.ITEM_50
    Ach_FullEquip      = 9,   // AchievementID.FULL_EQUIP
    Ach_NoSurpriseRun  = 10,  // AchievementID.NO_SURPRISE_RUN
}

// ── 캐릭터 종류 ───────────────────────────────────────────────────────────────
public enum CharacterType
{
    NPC01_Innkeeper = 0,  // 여관주인  → Resources/NPC01_face
    Player          = 1,  // 플레이어  → Resources/Player_face
}
// ── 캐릭터 이름 · 기본 스프라이트 매핑 ─────────────────────────────────
// ★ 캐릭터 추가/수정 시 이 클래스만 편집하면 됩니다.
public static class CharacterInfo
{
    /// <summary>관리자 화면에 표시되는 이름입니다. 여기서 수정하세요.</summary>
    public static string DisplayName(CharacterType ch)
    {
        switch (ch)
        {
            case CharacterType.NPC01_Innkeeper: return "여관주인";
            case CharacterType.Player:          return "Lord Kalrito";
            default:                            return "";
        }
    }

    /// <summary>Resources 폴더 기준 기본 얼굴 스프라이트 이름. faceSprite 가 비어있는 경우 사용됩니다.</summary>
    public static string DefaultSpriteName(CharacterType ch)
    {
        switch (ch)
        {
            case CharacterType.NPC01_Innkeeper: return "NPC01_face";
            case CharacterType.Player:          return "Player_face";
            default:                            return "NPC01_face";
        }
    }
}
// ── 대사 1줄 ──────────────────────────────────────────────────────────────────
[System.Serializable]
public class DialogueLine
{
    [Tooltip("대사를 말하는 캐릭터")]
    public CharacterType character;

    [Tooltip("얼굴 스프라이트. 비워두면 CharacterType 기본 스프라이트 사용.")]
    public Sprite faceSprite;

    [TextArea(2, 6)]
    [Tooltip("대사 내용 (최대 300자)")]
    public string text;
}

// ── 조건 ↔ AchievementID 변환 헬퍼 ───────────────────────────────────────────
// ※ 달성목표 추가 시 아래 _map 에도 한 줄 추가하세요.
public static class DialogueConditionHelper
{
    private static readonly Dictionary<DialogueConditionType, string> _map
        = new Dictionary<DialogueConditionType, string>
    {
        { DialogueConditionType.Ach_FirstFloor,    AchievementID.FIRST_FLOOR    },
        { DialogueConditionType.Ach_Floor10,       AchievementID.FLOOR_10       },
        { DialogueConditionType.Ach_Floor20,       AchievementID.FLOOR_20       },
        { DialogueConditionType.Ach_Floor30,       AchievementID.FLOOR_30       },
        { DialogueConditionType.Ach_Kill100,       AchievementID.KILL_100       },
        { DialogueConditionType.Ach_Gold1000,      AchievementID.GOLD_1000      },
        { DialogueConditionType.Ach_Gold10000,     AchievementID.GOLD_10000     },
        { DialogueConditionType.Ach_Item50,        AchievementID.ITEM_50        },
        { DialogueConditionType.Ach_FullEquip,     AchievementID.FULL_EQUIP     },
        { DialogueConditionType.Ach_NoSurpriseRun, AchievementID.NO_SURPRISE_RUN},
    };

    /// <summary>조건 타입에 대응하는 AchievementID 문자열을 반환합니다. General 이면 null.</summary>
    public static string ToAchievementID(DialogueConditionType type)
        => _map.TryGetValue(type, out var id) ? id : null;

    /// <summary>AchievementID 문자열에 대응하는 DialogueConditionType을 반환합니다. 없으면 General.</summary>
    public static DialogueConditionType FromAchievementID(string achievementId)
    {
        foreach (var kv in _map)
            if (kv.Value == achievementId) return kv.Key;
        return DialogueConditionType.General;
    }

    /// <summary>모든 AchievementID 상수 목록을 반환합니다 (이력 화면 등에서 사용).</summary>
    public static IEnumerable<string> AllAchievementIDs => _map.Values;
}
