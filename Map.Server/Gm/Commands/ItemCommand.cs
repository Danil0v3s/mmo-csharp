using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Items;
using Map.Server.Status;
using Map.Server.Visibility;

namespace Map.Server.Gm.Commands;

/// <summary>
/// <c>@item &lt;name|id&gt; [amount]</c> — grant an item to the caller.
/// rAthena <c>atcommand_item</c> (atcommand.cpp). Amount defaults to 1;
/// caps at item_db stack rules through <see cref="IInventoryService.GiveItem"/>.
/// </summary>
public sealed class ItemCommand(
    IVisibilityService visibility,
    IItemCatalog catalog,
    IInventoryService inventory,
    Map.Server.Status.ISessionManagerAccessor sessions
) : IGmCommand
{
    public string Name => "item";
    public int MinGroupId => 60;
    public string Description => "@item <name|id> [amount] — give item to caller.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0)
        {
            visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT { Message = "@item: usage — @item <name|id> [amount]" });
            return Task.CompletedTask;
        }

        // Numeric id first, then aegis-name lookup. Mirrors rAthena
        // atcommand_item's two-pass resolve.
        Core.Database.Entities.ItemEntity? row = null;
        if (uint.TryParse(args[0], out var id))
        {
            row = catalog.Get(id);
        }
        row ??= catalog.GetByAegisName(args[0]);
        if (row == null)
        {
            visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT { Message = $"@item: '{args[0]}' not in item_db." });
            return Task.CompletedTask;
        }

        var amount = 1;
        if (args.Count >= 2 && int.TryParse(args[1], out var a) && a > 0) amount = a;

        var session = sessions.GetByEntityId(caller.Id);
        if (session == null) return Task.CompletedTask;

        if (!inventory.GiveItem(session, row.Id, amount))
        {
            visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT { Message = "@item: inventory full." });
        }
        return Task.CompletedTask;
    }
}
