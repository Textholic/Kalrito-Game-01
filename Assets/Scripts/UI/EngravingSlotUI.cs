// ============================================================
// EngravingSlotUI.cs
// 대기화면(여관화면) 좌상단 각인 슬롯 UI.
//
// 레이아웃: 5열 × 2행 = 최대 10개 슬롯
//
// 동작:
//   - 각인 아이콘을 슬롯에 표시
//   - 마우스 오버(PointerEnter) 시 툴팁 카드 표시
//   - 마우스 아웃(PointerExit) 시 툴팁 카드 숨김
//   - 클릭(PointerClick) 시 삭제 확인 팝업 표시
//
// 사용 방법:
//   1. 각인 슬롯 하나당 이 컴포넌트를 부착.
//   2. EngravingPanelUI.cs 가 슬롯 배열을 관리하며 Bind(engraving) 호출.
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(Button))]
public class EngravingSlotUI : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    // ── Inspector 연결 ────────────────────────────────────────────────────────
    [Header("슬롯 아이콘")]
    public Image iconImage;

    [Header("툴팁 카드 (Canvas 하위 별도 오브젝트)")]
    public GameObject      tooltipCard;
    public Image           tooltipIcon;
    public TMP_Text        tooltipNameText;
    public TMP_Text        tooltipEffectText;
    public TMP_Text        tooltipDescText;
    public TMP_Text        tooltipRemoveCostText;

    [Header("삭제 확인 팝업")]
    public GameObject      removeConfirmPopup;
    public TMP_Text        removeConfirmNameText;
    public TMP_Text        removeConfirmCostText;
    public Button          removeConfirmYesBtn;
    public Button          removeConfirmNoBtn;

    // ── 내부 상태 ─────────────────────────────────────────────────────────────
    private EngravingData _engraving;

    // ── 바인딩 ───────────────────────────────────────────────────────────────
    public void Bind(EngravingData data)
    {
        _engraving = data;

        bool hasData = data != null;
        if (iconImage != null)
        {
            iconImage.sprite  = hasData ? data.icon : null;
            iconImage.enabled = hasData && data.icon != null;
        }

        gameObject.SetActive(hasData);
    }

    public void Clear()
    {
        _engraving = null;
        gameObject.SetActive(false);
    }

    // ── 이벤트 구현 ──────────────────────────────────────────────────────────
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_engraving == null) return;
        ShowTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltip();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_engraving == null) return;
        HideTooltip();
        ShowRemoveConfirm();
    }

    // ── 툴팁 ─────────────────────────────────────────────────────────────────
    private void ShowTooltip()
    {
        if (tooltipCard == null || _engraving == null) return;

        tooltipCard.SetActive(true);

        if (tooltipIcon != null)
        {
            tooltipIcon.sprite  = _engraving.icon;
            tooltipIcon.enabled = _engraving.icon != null;
        }

        if (tooltipNameText   != null) tooltipNameText.text   = _engraving.engravingName;
        if (tooltipDescText   != null) tooltipDescText.text   = _engraving.description;
        if (tooltipEffectText != null) tooltipEffectText.text = BuildEffectSummary(_engraving);

        int cost = GameManager.Instance?.Engraving?.GetRemoveCost() ?? 1000;
        if (tooltipRemoveCostText != null)
            tooltipRemoveCostText.text = $"제거 비용: {cost:N0}G";
    }

    private void HideTooltip()
    {
        if (tooltipCard != null)
            tooltipCard.SetActive(false);
    }

    // ── 삭제 확인 팝업 ────────────────────────────────────────────────────────
    private void ShowRemoveConfirm()
    {
        if (removeConfirmPopup == null || _engraving == null) return;

        int cost = GameManager.Instance?.Engraving?.GetRemoveCost() ?? 1000;

        if (removeConfirmNameText != null)
            removeConfirmNameText.text = $"[{_engraving.engravingName}]";

        if (removeConfirmCostText != null)
            removeConfirmCostText.text = $"제거 비용: {cost:N0}G\n각인을 지우시겠습니까?";

        // 버튼 리스너 등록 (중복 방지 위해 먼저 제거)
        if (removeConfirmYesBtn != null)
        {
            removeConfirmYesBtn.onClick.RemoveAllListeners();
            removeConfirmYesBtn.onClick.AddListener(OnConfirmRemove);
        }

        if (removeConfirmNoBtn != null)
        {
            removeConfirmNoBtn.onClick.RemoveAllListeners();
            removeConfirmNoBtn.onClick.AddListener(OnCancelRemove);
        }

        removeConfirmPopup.SetActive(true);
    }

    private void OnConfirmRemove()
    {
        if (_engraving == null) return;

        bool success = GameManager.Instance?.Engraving?.TryRemoveEngraving(_engraving) ?? false;
        if (!success)
        {
            GameManager.Instance?.UI?.AddLog("각인 제거 실패 (골드 부족)");
        }

        if (removeConfirmPopup != null)
            removeConfirmPopup.SetActive(false);
    }

    private void OnCancelRemove()
    {
        if (removeConfirmPopup != null)
            removeConfirmPopup.SetActive(false);
    }

    // ── 유틸 ─────────────────────────────────────────────────────────────────
    private static string BuildEffectSummary(EngravingData data)
    {
        if (data?.effects == null || data.effects.Length == 0)
            return "(효과 없음)";

        var sb = new System.Text.StringBuilder();
        foreach (var e in data.effects)
        {
            string sign = e.value >= 0 ? "+" : "";
            switch (e.effectType)
            {
                case EngravingEffectType.MaxHpBonus:
                    sb.AppendLine($"최대 체력 {sign}{e.value:F0}"); break;
                case EngravingEffectType.AttackBonus:
                    sb.AppendLine($"공격력 {sign}{e.value:F0}"); break;
                case EngravingEffectType.DefenseBonus:
                    sb.AppendLine($"방어력 {sign}{e.value:F0}"); break;
                case EngravingEffectType.WeightLimitBonus:
                    sb.AppendLine($"무게 제한 {sign}{e.value:F0}kg"); break;
                case EngravingEffectType.PotionHealBonus:
                    sb.AppendLine($"회복약 회복량 {sign}{e.value:F0}"); break;
                case EngravingEffectType.HpRegenRateBonus:
                    sb.AppendLine($"체력 재생률 {sign}{e.value:F1}%"); break;
                case EngravingEffectType.AggroRangeMultiplier:
                    sb.AppendLine($"어그로 범위 ×{e.value:F1}"); break;
                case EngravingEffectType.ReviveChanceBonus:
                    sb.AppendLine($"부활 확률 {sign}{e.value * 100:F0}%"); break;
                case EngravingEffectType.ForceApplyBurn:
                    sb.AppendLine("게임 시작 시 화상 적용"); break;
                case EngravingEffectType.ForceApplyPoison:
                    sb.AppendLine("게임 시작 시 중독 적용"); break;
                case EngravingEffectType.ForceApplyCharm:
                    sb.AppendLine("게임 시작 시 매료 적용"); break;
                case EngravingEffectType.ForceApplyFatigue:
                    sb.AppendLine("게임 시작 시 피로 적용"); break;
                case EngravingEffectType.ForceApplyConfusion:
                    sb.AppendLine("게임 시작 시 혼란 적용"); break;
                case EngravingEffectType.CursedItemForced:
                    string name = e.cursedItem != null ? e.cursedItem.itemName : "???";
                    sb.AppendLine($"저주 아이템 강제 획득: {name}"); break;
            }
        }
        return sb.ToString().TrimEnd();
    }

    void Awake()
    {
        if (tooltipCard        != null) tooltipCard.SetActive(false);
        if (removeConfirmPopup != null) removeConfirmPopup.SetActive(false);
    }
}
