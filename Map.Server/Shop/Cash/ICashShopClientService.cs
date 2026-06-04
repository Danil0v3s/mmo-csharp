using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;

namespace Map.Server.Shop.Cash;

/// <summary>
/// Emits the cash-shop ZC packets to a player's session (the client-emit hub for
/// <see cref="CashShopService"/> + the cash-shop handlers). rAthena <c>clif_cashshop_*</c> / <c>clif_sale_*</c>.
/// </summary>
public interface ICashShopClientService
{
    /// <summary>rAthena <c>clif_cashshop_open</c> (0x0b6e) — the shop opened; carry the player's
    /// current cash/kafra point balances + the focused tab.</summary>
    void SendOpen(PlayerEntity pc, int tab);

    /// <summary>rAthena <c>clif_cashshop_list</c> (0x08ca) — one packet per non-empty catalog tab.</summary>
    void SendCatalog(PlayerEntity pc, IReadOnlyList<(int tab, IReadOnlyList<(uint itemId, int price)> items)> tabs);

    /// <summary>rAthena <c>clif_cashshop_result</c> (0x0849) — the buy outcome + the resulting balances.</summary>
    void SendBuyResult(PlayerEntity pc, uint itemId, CashShopBuyResult result);

    /// <summary>rAthena <c>sale_notify_login</c> (0x09b2 + 0x09c4) — per active sale, start + amount.</summary>
    void SendActiveSales(PlayerEntity pc, IReadOnlyList<(int itemId, int amount, int remainingSeconds)> sales);
}
