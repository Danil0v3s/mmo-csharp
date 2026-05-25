using Map.Server.Entities;
using Map.Server.Inventory;
using Microsoft.Extensions.Logging;

namespace Map.Server.Status;

/// <summary>
/// Default <see cref="IPlayerScDataReceivedService"/>. Port of
/// <c>pc_scdata_received</c> (pc.cpp:14747).
/// </summary>
public sealed class PlayerScDataReceivedService : IPlayerScDataReceivedService
{
    private readonly IStatusChangeService _sc;
    private readonly IPlayerInventoryHelpers? _inventoryHelpers;
    private readonly ILogger<PlayerScDataReceivedService> _logger;

    public PlayerScDataReceivedService(
        IStatusChangeService sc,
        ILogger<PlayerScDataReceivedService> logger,
        IPlayerInventoryHelpers? inventoryHelpers = null)
    {
        _sc = sc;
        _logger = logger;
        _inventoryHelpers = inventoryHelpers;
    }

    public void OnReceived(PlayerEntity pc, byte[] scDataPayload)
    {
        // Step 1: decode the wire payload from the char-server.
        var rows = ScDataCodec.Decode(scDataPayload);

        // Step 2: re-apply each persisted SC. rAthena's chrif_load_scdata
        // path (chrif.cpp) iterates the stored rows and calls sc_start4
        // per entry using the saved Val1..Val4 + remaining tick. Rows
        // whose tick is already negative are dropped (expired).
        int reapplied = 0;
        int skipped = 0;
        foreach (var row in rows)
        {
            if (row.TickMs <= 0)
            {
                skipped++;
                continue;
            }
            if (!System.Enum.IsDefined(typeof(StatusType), (short)row.Type))
            {
                skipped++;
                continue;
            }
            var type = (StatusType)row.Type;
            _sc.Start(pc, type,
                row.Val1, row.Val2, row.Val3, row.Val4,
                durationMs: (int)Math.Min(int.MaxValue, row.TickMs),
                source: pc);
            reapplied++;
        }

        // Step 3: rental expiration sweep — rAthena calls
        // pc_inventory_rentals after chrif_load_scdata so any rental whose
        // associated SC just got restored gets a fresh expiry tick.
        _inventoryHelpers?.InventoryRentalsTick(pc);

        // Step 4: latch the pc_loaded flag — gameplay code reads this to
        // gate "is the player fully ready" decisions (atcommand `@reload`,
        // some script vars, etc.). PlayerEntity.PcLoaded mirrors rAthena's
        // sd->state.pc_loaded latch.
        pc.PcLoaded = true;

        _logger.LogDebug(
            "pc_scdata_received: char {Char} restored {Reapplied} SCs ({Skipped} expired/skipped)",
            pc.CharacterId, reapplied, skipped);
    }
}
