// ============================================================
// EngravingPanelUI.cs
// 대기화면(여관화면) 좌상단 각인 패널 전체 관리.
//   - 5열 × 2행 슬롯 배열 (최대 10개)
//   - EngravingManager 변경 이벤트를 구독해 자동 갱신
//
// 사용 방법:
//   1. Canvas 하위에 EngravingPanel 오브젝트를 만들고
//      GridLayoutGroup (5열, 자동 행)을 설정한다.
//   2. 이 컴포넌트를 부착하고 slotPrefab을 등록한다.
//      (EngravingSlotUI 컴포넌트가 붙은 prefab)
//   3. 씬 로드 시 Refresh() 를 한 번 호출하거나,
//      Start() 에서 EngravingManager.OnEngravingsChanged 구독.
// ============================================================
using UnityEngine;

public class EngravingPanelUI : MonoBehaviour
{
    // ── Inspector 연결 ────────────────────────────────────────────────────────
    [Header("슬롯 프리팹 (EngravingSlotUI 컴포넌트 포함)")]
    public EngravingSlotUI slotPrefab;

    [Header("슬롯 부모 Transform (GridLayoutGroup 권장)")]
    public Transform slotsParent;

    // ── 내부 ─────────────────────────────────────────────────────────────────
    private EngravingSlotUI[] _slots;
    private const int TOTAL_SLOTS = EngravingManager.MAX_ENGRAVINGS; // 10

    void Start()
    {
        BuildSlots();
        Refresh();

        // EngravingManager 변경 시 자동 갱신
        if (GameManager.Instance?.Engraving != null)
            GameManager.Instance.Engraving.OnEngravingsChanged += Refresh;
    }

    void OnDestroy()
    {
        if (GameManager.Instance?.Engraving != null)
            GameManager.Instance.Engraving.OnEngravingsChanged -= Refresh;
    }

    // ── 슬롯 생성 ────────────────────────────────────────────────────────────
    private void BuildSlots()
    {
        if (slotPrefab == null || slotsParent == null) return;

        _slots = new EngravingSlotUI[TOTAL_SLOTS];
        for (int i = 0; i < TOTAL_SLOTS; i++)
        {
            _slots[i] = Instantiate(slotPrefab, slotsParent);
            _slots[i].Clear();
        }
    }

    // ── 슬롯 갱신 ────────────────────────────────────────────────────────────
    public void Refresh()
    {
        if (_slots == null) return;

        var engraving = GameManager.Instance?.Engraving;
        var list      = engraving?.Unlocked;

        for (int i = 0; i < _slots.Length; i++)
        {
            if (list != null && i < list.Count)
                _slots[i].Bind(list[i]);
            else
                _slots[i].Clear();
        }
    }
}
