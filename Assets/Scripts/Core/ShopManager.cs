// ============================================================
// ShopManager.cs
// 보스 룸 입구 옆 상점 관리.
//
// 기능:
//   1. 골드 입금 (플레이어 보유 골드 → 금고, 사망해도 보존)
//   2. 골드 출금 (금고 → 플레이어 보유 골드)
//   3. 소비 아이템 랜덤 진열 (ItemData.isConsumable=true, shopPrice>0 인 아이템들에서 N개 선택)
//   4. 아이템 구매 (골드 소모 후 인벤토리에 추가)
//
// 사용 방법:
//   1. 보스룸 옆 Shop NPC/오브젝트에 이 컴포넌트를 부착.
//   2. Inspector에서 allConsumables 에 판매 가능한 ItemData 목록을 등록.
//   3. 상점 열기: Open() 호출 → ShopItems 에 랜덤 선택된 목록이 채워짐.
//   4. 구매: TryBuyItem(item) 호출.
//   5. 입금: TryDeposit(amount) / 출금: TryWithdraw(amount) 호출.
// ============================================================
using UnityEngine;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    // ── Inspector 설정 ────────────────────────────────────────────────────────
    [Header("판매 후보 아이템 목록")]
    [Tooltip("isConsumable=true & shopPrice>0 인 ItemData 를 등록하세요")]
    public ItemData[] allConsumables;

    [Header("상점 진열 슬롯 수 (기본 4)")]
    [Tooltip("상점을 열 때 무작위로 선택할 아이템 수")]
    public int shopSlotCount = 4;

    // ── 상태 ─────────────────────────────────────────────────────────────────
    /// <summary>현재 진열된 아이템 목록 (Open() 시 갱신).</summary>
    public IReadOnlyList<ItemData> ShopItems => _shopItems;
    private readonly List<ItemData> _shopItems = new List<ItemData>();

    // ── 이벤트 ───────────────────────────────────────────────────────────────
    /// <summary>상점 진열이 갱신되거나 아이템 구매 후 발생.</summary>
    public event System.Action OnShopUpdated;

    // ── 상점 열기 / 닫기 ─────────────────────────────────────────────────────

    /// <summary>
    /// 상점을 열고 소비 아이템을 랜덤으로 진열한다.
    /// 가격(shopPrice)이 높을수록 확률적 가중치가 낮아지도록 배분하지 않고,
    /// 단순 균등 랜덤으로 선택한다. (향후 가중치 로직 확장 가능)
    /// </summary>
    public void Open()
    {
        _shopItems.Clear();

        // 조건: isConsumable=true, shopPrice>0
        var candidates = new List<ItemData>();
        if (allConsumables != null)
        {
            foreach (var item in allConsumables)
                if (item != null && item.isConsumable && item.shopPrice > 0)
                    candidates.Add(item);
        }

        // Fisher–Yates 셔플 후 앞에서 slotCount개 선택
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }

        int take = Mathf.Min(shopSlotCount, candidates.Count);
        for (int i = 0; i < take; i++)
            _shopItems.Add(candidates[i]);

        OnShopUpdated?.Invoke();
    }

    // ── 구매 ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 아이템을 구매한다.
    /// 골드가 부족하거나 인벤토리 공간이 없으면 false 반환.
    /// 구매 성공 시 진열 목록에서 해당 아이템을 제거한다.
    /// </summary>
    public bool TryBuyItem(ItemData item)
    {
        if (item == null) return false;
        if (!_shopItems.Contains(item)) return false;

        var player = GameManager.Instance?.Player;
        if (player == null) return false;
        if (player.Gold < item.shopPrice) return false;

        var inventory = GameManager.Instance?.Inventory;
        if (inventory == null) return false;

        // 인벤토리에 추가 가능한지 먼저 시도
        if (!inventory.TryAddItem(item))
        {
            GameManager.Instance?.UI?.AddLog("인벤토리 공간이 부족합니다.");
            return false;
        }

        player.SpendGold(item.shopPrice);
        _shopItems.Remove(item);
        OnShopUpdated?.Invoke();

        GameManager.Instance?.UI?.AddLog($"[상점] '{item.itemName}' 구매 ({item.shopPrice}G)");
        return true;
    }

    // ── 골드 입금 / 출금 ─────────────────────────────────────────────────────

    /// <summary>
    /// 플레이어 보유 골드를 금고에 입금.
    /// 사망해도 금고 골드는 보존된다.
    /// </summary>
    public bool TryDeposit(int amount)
    {
        var history = GameManager.Instance?.History;
        if (history == null) return false;

        bool ok = history.DepositGold(amount);
        if (ok)
            GameManager.Instance?.UI?.AddLog($"[상점] {amount}G 입금 완료 (금고: {history.VaultGold}G)");
        else
            GameManager.Instance?.UI?.AddLog($"[상점] 입금 실패 (보유 골드 부족)");

        return ok;
    }

    /// <summary>
    /// 금고에서 플레이어 보유 골드로 출금.
    /// </summary>
    public bool TryWithdraw(int amount)
    {
        var history = GameManager.Instance?.History;
        if (history == null) return false;

        bool ok = history.WithdrawGold(amount);
        if (ok)
            GameManager.Instance?.UI?.AddLog($"[상점] {amount}G 출금 완료 (금고: {history.VaultGold}G)");
        else
            GameManager.Instance?.UI?.AddLog($"[상점] 출금 실패 (금고 잔액 부족)");

        return ok;
    }

    /// <summary>현재 금고 잔액 반환.</summary>
    public int VaultGold => GameManager.Instance?.History?.VaultGold ?? 0;
}
