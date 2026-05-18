using Core.Server.Packets.Out.ZC;
using Microsoft.ClearScript;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Scripting.Dialog;

public sealed partial class NpcInfo
{
    public Task shopSet(object items)             => ScriptStub.CallAsync(Cat, "shopSet", items);
    public Task shopAdd(object items)             => ScriptStub.CallAsync(Cat, "shopAdd", items);
    public Task shopDel(object itemIds)           => ScriptStub.CallAsync(Cat, "shopDel", itemIds);
    public Task shopAttach(bool flag = true)      => ScriptStub.CallAsync(Cat, "shopAttach", flag);
    public Task shopUpdate(int itemId, int price, int? stock = null)
        => ScriptStub.CallAsync(Cat, "shopUpdate", itemId, price, stock);
}
