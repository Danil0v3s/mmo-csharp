namespace Map.Server.Scripting.Dialog;

public sealed partial class WorldContext
{
    public int getMapUsers(string mapName) => ScriptStub.Call(Cat, "getMapUsers", 0, mapName);
    public int getAreaUsers(string mapName, int x1, int y1, int x2, int y2)
        => ScriptStub.Call(Cat, "getAreaUsers", 0, mapName, x1, y1, x2, y2);
    public int getServerUsers(int type = 0) => ScriptStub.Call(Cat, "getServerUsers", 0, type);
    public bool isLoggedIn(int accountId, int? charId = null)
        => ScriptStub.Call(Cat, "isLoggedIn", false, accountId, charId);
    public string ridToName(long rid) => ScriptStub.Call(Cat, "ridToName", string.Empty, rid);

    public object[] getAreaDropItem(string mapName, int x1, int y1, int x2, int y2, int? itemId = null)
        => ScriptStub.Call(Cat, "getAreaDropItem", Array.Empty<object>(), mapName, x1, y1, x2, y2, itemId);
}
