using Map.Server.Entities;

namespace Map.Server.Items;

/// <summary>
/// Manages floor items: drop, pickup, auto-despawn. Inventory persistence
/// is out of scope for the MS3 first slice — pickup removes the floor
/// entity and surfaces a callback the caller (mob death, /drop GM
/// command, future inventory service) handles.
/// </summary>
public interface IItemDropService
{
    /// <summary>Outcome of a pickup request, mirrors rAthena's <c>pc_takeitem</c> result codes.</summary>
    enum PickupResult
    {
        Ok,
        ItemNotFound,
        OutOfRange,
        WrongMap,
        /// <summary>Drop is under the loot-protection window for another player / party.</summary>
        OwnerProtected,
    }

    /// <summary>
    /// rAthena <c>battle_config.mob_drop_idmask</c>-related loot-protection
    /// timings (default values from <c>conf/battle/drops.conf</c>).
    /// </summary>
    public const int OwnerProtectionMs = 3_000;
    public const int PartyProtectionMs = 5_000;

    /// <summary>Maximum cell distance a player can be from a floor item to pick it up.</summary>
    public const int PickupRange = 2;

    /// <summary>Auto-despawn TTL in milliseconds (rAthena's <c>flooritem_lifetime</c> default).</summary>
    public const int DefaultLifetimeMs = 60_000;

    /// <summary>
    /// Create a floor item at (x, y) on the given map. Broadcasts
    /// <c>ZC_ITEM_FALL_ENTRY</c> to PC viewers in AOI. Returns the new
    /// entity id so callers (mob death, GM /drop) can reference it.
    ///
    /// <paramref name="ownerCharId"/> &gt; 0 grants exclusive pickup for
    /// the rAthena <c>battle_config.item_first_get_time</c> window
    /// (~3s default). After that window, members of
    /// <paramref name="ownerPartyId"/> get priority for another
    /// <c>item_second_get_time</c> (~5s). After both windows, the drop
    /// becomes public.
    /// </summary>
    EntityId DropOnFloor(uint mapId, short x, short y, int itemId, short amount,
        byte subX = 0, byte subY = 0, bool identified = true,
        int ownerCharId = 0, int ownerPartyId = 0);

    /// <summary>
    /// Player picks up the item identified by <paramref name="itemEntityId"/>.
    /// Validates the picker is on the same map and within
    /// <see cref="PickupRange"/> cells; on success removes the floor item
    /// and broadcasts <c>ZC_ITEM_DISAPPEAR</c>. Inventory persistence is
    /// the caller's responsibility (returns the dropped item info for
    /// downstream IPC).
    /// </summary>
    PickupResult TryPickup(PlayerEntity picker, EntityId itemEntityId,
        out FloorItemEntity? item);

    /// <summary>
    /// Per-tick sweep: items older than the configured TTL are removed and
    /// a vanish packet (reason = TimedOut, mapped to <c>ZC_ITEM_DISAPPEAR</c>)
    /// is broadcast. Idempotent / cheap when there are no items.
    /// </summary>
    void Tick();
}
