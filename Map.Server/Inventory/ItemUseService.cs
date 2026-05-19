using Core.Server.Network;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Items;
using Map.Server.Status;
using Microsoft.Extensions.Logging;

namespace Map.Server.Inventory;

/// <summary>
/// First-slice <see cref="IItemUseService"/>. Resolves a small set of
/// the most common consumable items by aegis name. Each entry is the
/// equivalent of one rAthena item Script — pure HP/SP heal,
/// SC application, or warp.
/// </summary>
public sealed class ItemUseService : IItemUseService
{
    private readonly IItemCatalog _catalog;
    private readonly IStatusChangeService _scService;
    private readonly Status.ISessionManagerAccessor _sessions;
    private readonly ILogger<ItemUseService> _logger;

    public ItemUseService(
        IItemCatalog catalog,
        IStatusChangeService scService,
        Status.ISessionManagerAccessor sessions,
        ILogger<ItemUseService> logger)
    {
        _catalog = catalog;
        _scService = scService;
        _sessions = sessions;
        _logger = logger;
    }

    public bool UseItem(PlayerEntity user, int slotIndex)
    {
        // Need a session to find the inventory (it's session-scoped right
        // now; PlayerEntity doesn't carry the list directly).
        var session = _sessions.GetByEntityId(user.Id);
        if (session?.Inventory is not { } inv) return false;
        if (slotIndex < 0 || slotIndex >= inv.Count) return false;
        var item = inv[slotIndex];
        if (item.Amount <= 0) return false;

        var row = _catalog.Get(item.NameId);
        if (row == null) return false;

        // Resolve by aegis name — placeholder until the item Script
        // parser lands. Covers the starter potions + a few buffs so the
        // damage / regen / SC subsystems get exercised end-to-end.
        if (!TryApplyEffect(user, row.NameAegis)) return false;

        // Decrement stack; remove slot if empty (matches rAthena
        // pc_delitem one-by-one consume).
        item.Amount -= 1;
        if (item.Amount <= 0)
        {
            inv.RemoveAt(slotIndex);
        }
        _logger.LogDebug(
            "Char {Char} used item {Aegis} (slot {Slot}); {Remaining} left",
            user.CharacterId, row.NameAegis, slotIndex, item.Amount);
        return true;
    }

    /// <summary>
    /// Hand-built effect table — the equivalent of the rAthena
    /// db/re/item_db/item_db_usable.yml Script column for the starter
    /// consumables. Returns false for unknown items so the consume path
    /// rejects them with an ack failure.
    /// </summary>
    private bool TryApplyEffect(PlayerEntity user, string aegisName)
    {
        switch (aegisName)
        {
            // Standard healing potions — itemheal formula in rAthena is
            // (base + STR/5 + (potion_pitcher_lv/100)). We approximate
            // with flat ranges that match the published min/max values.
            case "Red_Potion": HealHp(user, 50);  return true;
            case "Orange_Potion": HealHp(user, 105); return true;
            case "Yellow_Potion": HealHp(user, 175); return true;
            case "White_Potion": HealHp(user, 425); return true;
            case "Blue_Potion": HealSp(user, 50); return true;

            // Buff scrolls / food — minimal SC chain so SC engine gets exercised.
            case "Blessing_10_Scroll":
                _scService.Start(user, StatusType.Blessing, val1: 10, 0, 0, 0, durationMs: 240_000);
                return true;
            case "Increase_Agility_10_Scroll":
                _scService.Start(user, StatusType.IncreaseAgi, val1: 10, 0, 0, 0, durationMs: 140_000);
                return true;

            default:
                _logger.LogDebug("ItemUseService: no effect for aegis '{Aegis}'", aegisName);
                return false;
        }
    }

    private static void HealHp(PlayerEntity user, int amount)
    {
        user.Hp = Math.Min(user.MaxHp, user.Hp + amount);
    }

    private static void HealSp(PlayerEntity user, int amount)
    {
        user.Sp = Math.Min(user.MaxSp, user.Sp + amount);
    }
}
