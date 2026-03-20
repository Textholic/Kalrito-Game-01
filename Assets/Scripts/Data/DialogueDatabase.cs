// ============================================================
// DialogueDatabase.cs
// 전체 대화 데이터베이스 ScriptableObject
// Assets/Resources/DialogueDatabase.asset 으로 저장하면 DialogueManager 가 자동 로드합니다.
// ============================================================
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "DialogueDatabase", menuName = "Game/Dialogue/Dialogue Database")]
public class DialogueDatabase : ScriptableObject
{
    [Tooltip("모든 대화 세트를 등록하세요.")]
    public List<DialogueSet> dialogueSets = new List<DialogueSet>();
}
