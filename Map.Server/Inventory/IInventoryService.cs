using Map.Server.Session;

namespace Map.Server.Inventory;

/// <summary>
/// Owns the per-session inventory: loads it from the DB on connect,
/// emits the <c>clif_inventorylist</c> packet cascade so the client's
/// bag UI populates, and (later slices) will mediate add / delete /
/// equip operations + persistence.
///
/// rAthena reference: <c>clif_inventorylist</c> (clif.cpp:3056) — the
/// SOURCE-of-truth call that gets fired from <c>pc_authok</c> and from
/// <c>clif_parse_LoadEndAck</c>. We mirror the LoadEndAck call site only
/// (slice 1); the second pc_authok emit is redundant for clients that
/// process the LoadEndAck one.
/// </summary>
public interface IInventoryService
{
    /// <summary>
    /// Pull every inventory row for <paramref name="session"/>'s character
    /// from the DB and stash the list on <see cref="MapSessionData.Inventory"/>.
    /// Idempotent — re-running rebuilds from scratch.
    /// </summary>
    Task LoadAsync(MapSessionData session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send the four-packet <c>clif_inventorylist</c> cascade to the
    /// client: <c>ZC_INVENTORY_START</c> → optional NORMAL chunk →
    /// optional EQUIP chunk → <c>ZC_INVENTORY_END</c>.
    /// Requires <see cref="LoadAsync"/> to have run first; no-op if the
    /// session's inventory hasn't been loaded.
    /// </summary>
    void SendInventoryList(MapSessionData session);

    /// <summary>
    /// Add <paramref name="amount"/> of <paramref name="nameId"/> to
    /// <paramref name="session"/>'s inventory and notify the client via
    /// <see cref="Core.Server.Packets.Out.ZC.ZC_ITEM_PICKUP_ACK"/>.
    /// Stackable items merge into an existing slot with the same nameid
    /// + cards + identify + refine + options; non-stackable items
    /// append a new slot. Mirrors rAthena <c>pc_additem</c>.
    /// Returns <c>true</c> on success.
    /// </summary>
    bool GiveItem(MapSessionData session, uint nameId, int amount);
}
