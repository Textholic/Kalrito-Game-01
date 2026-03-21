using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 장비 인벤토리 슬롯에 마우스오버 툴팁을 표시하는 컴포넌트.
/// ItemSlotUI의 공유 툴팁 레이어를 재사용합니다.
/// </summary>
public class EquipSlotTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private string _title;
    private string _body;
    private Sprite _icon;

    public void SetEquip(EquipmentItemDef eq, Font font)
    {
        _icon  = eq?.icon;
        _title = eq?.displayName ?? "";
        var sb = new System.Text.StringBuilder();
        if (!string.IsNullOrEmpty(eq.description))
            sb.AppendLine($"<color=#CCBBDD>{eq.description}</color>");

        var parts = new List<string>();
        if (eq.attackMod    != 0)  parts.Add($"<color=#FFAA44>ATK {(eq.attackMod    > 0 ? "+" : "")}{eq.attackMod}</color>");
        if (eq.defenseMod   != 0)  parts.Add($"<color=#88AAFF>DEF {(eq.defenseMod   > 0 ? "+" : "")}{eq.defenseMod}</color>");
        if (eq.defChanceMod != 0f) parts.Add($"<color=#AADDFF>방어확률 {(eq.defChanceMod > 0 ? "+" : "")}{Mathf.RoundToInt(eq.defChanceMod * 100)}%</color>");
        if (eq.maxHpMod     != 0)  parts.Add($"<color=#FF8888>MaxHP {(eq.maxHpMod     > 0 ? "+" : "")}{eq.maxHpMod}</color>");
        if (eq.healMod      != 0)  parts.Add($"<color=#66FF88>회복 {(eq.healMod      > 0 ? "+" : "")}{eq.healMod}</color>");

        if (parts.Count > 0)
            sb.AppendLine(string.Join("  │  ", parts));

        if (!string.IsNullOrEmpty(eq.flavorText))
        {
            sb.AppendLine("<color=#443344>─────────────────────────</color>");
            sb.Append($"<color=#887799><i>{eq.flavorText}</i></color>");
        }

        _body = sb.ToString().TrimEnd();
    }

    public void OnPointerEnter(PointerEventData _)
    {
        if (string.IsNullOrEmpty(_title) && string.IsNullOrEmpty(_body)) return;
        ItemSlotUI.ShowTooltip(_icon, _title, _body, GetComponent<RectTransform>());
    }

    public void OnPointerExit(PointerEventData _)
    {
        ItemSlotUI.HideTooltip();
    }

    private void OnDestroy()
    {
        ItemSlotUI.HideTooltip();
    }
}
