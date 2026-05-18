namespace Map.Server.Scripting.Dialog;

public sealed partial class WorldContext
{
    public string mapIdToName(int mapId) => ScriptStub.Call(Cat, "mapIdToName", string.Empty, mapId);
    public object? getMapXY(long gid, int type = 0)
        => ScriptStub.Call<object?>(Cat, "getMapXY", null, gid, type);
    public int distance(int x0, int y0, int x1, int y1)
    {
        var dx = x1 - x0; var dy = y1 - y0;
        return Math.Max(Math.Abs(dx), Math.Abs(dy));
    }

    public Task setCell(string mapName, int x1, int y1, int x2, int y2, int type, bool flag)
        => ScriptStub.CallAsync(Cat, "setCell", mapName, x1, y1, x2, y2, type, flag);
    public int checkCell(string mapName, int x, int y, int type)
        => ScriptStub.Call(Cat, "checkCell", 0, mapName, x, y, type);
    public object? getFreeCell(string mapName, int? x = null, int? y = null, int rangeX = 0, int rangeY = 0, int flag = 0)
        => ScriptStub.Call<object?>(Cat, "getFreeCell", null, mapName, x, y, rangeX, rangeY, flag);
    public Task setWall(string mapName, int x, int y, int size, int dir, bool shootable, string name)
        => ScriptStub.CallAsync(Cat, "setWall", mapName, x, y, size, dir, shootable, name);
    public Task delWall(string name) => ScriptStub.CallAsync(Cat, "delWall", name);
    public bool checkWall(string name) => ScriptStub.Call(Cat, "checkWall", false, name);

    public Task makeItem(int itemId, int amount, string mapName, int x, int y, bool effect = false, object? opts = null)
        => ScriptStub.CallAsync(Cat, "makeItem", itemId, amount, mapName, x, y, effect, opts);
    public Task cleanArea(string mapName, int x1, int y1, int x2, int y2)
        => ScriptStub.CallAsync(Cat, "cleanArea", mapName, x1, y1, x2, y2);
    public Task cleanMap(string mapName) => ScriptStub.CallAsync(Cat, "cleanMap", mapName);

    public Task warpPortal(int srcX, int srcY, string toMap, int toX, int toY)
        => ScriptStub.CallAsync(Cat, "warpPortal", srcX, srcY, toMap, toX, toY);
    public Task mapWarp(string fromMap, string toMap, int x, int y, int? type = null, long? id = null)
        => ScriptStub.CallAsync(Cat, "mapWarp", fromMap, toMap, x, y, type, id);
    public Task areaWarp(string fromMap, int x1, int y1, int x2, int y2,
        string toMap, int toX, int toY, int? toX2 = null, int? toY2 = null)
        => ScriptStub.CallAsync(Cat, "areaWarp", fromMap, x1, y1, x2, y2, toMap, toX, toY, toX2, toY2);
    public Task warpParty(string toMap, int x, int y, int partyId, object? fromOpts = null)
        => ScriptStub.CallAsync(Cat, "warpParty", toMap, x, y, partyId, fromOpts);
    public Task warpGuild(string toMap, int x, int y, int guildId)
        => ScriptStub.CallAsync(Cat, "warpGuild", toMap, x, y, guildId);

    public Task areaPercentHeal(string mapName, int x1, int y1, int x2, int y2, int hp, int sp)
        => ScriptStub.CallAsync(Cat, "areaPercentHeal", mapName, x1, y1, x2, y2, hp, sp);

    public Task attachRid(int accountId, bool force = false)
        => ScriptStub.CallAsync(Cat, "attachRid", accountId, force);
    public Task addRid(int type, int flag = 0, object? parameters = null)
        => ScriptStub.CallAsync(Cat, "addRid", type, flag, parameters);
    public int playerAttached() => ScriptStub.Call(Cat, "playerAttached", 0);
    public int getAttachedRid() => ScriptStub.Call(Cat, "getAttachedRid", 0);
}
