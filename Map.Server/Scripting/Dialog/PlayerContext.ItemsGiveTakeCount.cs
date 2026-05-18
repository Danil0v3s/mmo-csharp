using Core.Server.Packets;
using Core.Server.Packets.Out.ZC;
using Microsoft.ClearScript;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Scripting.Dialog;

public sealed partial class PlayerContext
{
    public Task giveItem(int itemId, int amount = 1, object? opts = null)
    {
        // opts is reserved for slice 2+ (identify/refine/cards/bound/options).
        // Slice 1 just lands clean stackable adds via IInventoryService.
        _inventory.GiveItem(_session, (uint)itemId, amount);
        return Task.CompletedTask;
    }

    public int countItem(int itemId, object? opts = null)
    {
        if (_session.Inventory is not { } items) return 0;
        return items
            .Where(it => it.NameId == (uint)itemId)
            .Sum(it => (int)it.Amount);
    }

    public bool hasItem(int itemId, int amount = 1, object? opts = null)
        => countItem(itemId, opts) >= amount;

    public Task giveRentItem(int itemId, int seconds, object? opts = null)
        => ScriptStub.CallAsync(Cat, "giveRentItem", itemId, seconds, opts);

    public Task giveNamedItem(int itemId, string inscribeName)
        => ScriptStub.CallAsync(Cat, "giveNamedItem", itemId, inscribeName);

    public Task giveRandomGroupItem(int groupId, int qty = 1, object? opts = null)
        => ScriptStub.CallAsync(Cat, "giveRandomGroupItem", groupId, qty, opts);

    public Task giveGroupItem(int groupId, object? opts = null)
        => ScriptStub.CallAsync(Cat, "giveGroupItem", groupId, opts);

    public Task delItem(int itemId, int amount = 1, object? opts = null)
        => ScriptStub.CallAsync(Cat, "delItem", itemId, amount, opts);

    public Task delItemAtIndex(int index, int amount = 1)
        => ScriptStub.CallAsync(Cat, "delItemAtIndex", index, amount);

    public int countBound(int boundType = 0)
        => ScriptStub.Call(Cat, "countBound", 0, boundType);

    public Task clearItems()
        => ScriptStub.CallAsync(Cat, "clearItems");

    public Task consumeItem(int itemId)
        => ScriptStub.CallAsync(Cat, "consumeItem", itemId);

    public int[] searchItem(string namePart)
        => ScriptStub.Call(Cat, "searchItem", Array.Empty<int>(), namePart);

    public object[] getInventory()
        => ScriptStub.Call<object[]>(Cat, "getInventory", Array.Empty<object>());

    public Task mergeItems(int? itemId = null)
        => ScriptStub.CallAsync(Cat, "mergeItems", itemId);

    public Task identifyAll(int type = 0)
        => ScriptStub.CallAsync(Cat, "identifyAll", type);

    public bool checkWeight(int itemId, int amount, object? more = null)
        => ScriptStub.Call(Cat, "checkWeight", true, itemId, amount, more);
}
