// ============================================================
// DialogueSet.cs
// 대화 세트 ScriptableObject
// ============================================================
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewDialogueSet", menuName = "Game/Dialogue/Dialogue Set")]
public class DialogueSet : ScriptableObject
{
    [Tooltip("General = 일반 대화(랜덤)\nAch_* = 해당 달성목표 달성 시 1회 표시")]
    public DialogueConditionType condition = DialogueConditionType.General;

    [Tooltip("대사 목록 — 위에서 아래 순서로 순차 표시됩니다.")]
    public List<DialogueLine> lines = new List<DialogueLine>();
}
