namespace Map.Server.Scripting.Dialog;

public sealed partial class WorldContext
{
    public string itemName(int itemId) => ScriptStub.Call(Cat, "itemName", $"Item_{itemId}", itemId);
    public int itemSlots(int itemId) => ScriptStub.Call(Cat, "itemSlots", 0, itemId);
    public object? itemInfo(int itemId, int type) => ScriptStub.Call<object?>(Cat, "itemInfo", null, itemId, type);
    public Task setItemInfo(int itemId, int type, int value) => ScriptStub.CallAsync(Cat, "setItemInfo", itemId, type, value);
    public Task setItemScript(int itemId, string script, int type = 0) => ScriptStub.CallAsync(Cat, "setItemScript", itemId, script, type);

    public int gmLevel(int? charId = null) => ScriptStub.Call(Cat, "gmLevel", 0, charId);
    public int groupId(int? charId = null) => ScriptStub.Call(Cat, "groupId", 0, charId);

    public string itemLink(int itemId, object? opts = null)
        => ScriptStub.Call(Cat, "itemLink", $"<{itemId}>", itemId, opts);
}
