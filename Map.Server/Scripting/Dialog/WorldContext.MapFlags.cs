namespace Map.Server.Scripting.Dialog;

public sealed partial class WorldContext
{
    public Task setMapFlag(string mapName, int flag, string? zone = null, int? type = null)
        => ScriptStub.CallAsync(Cat, "setMapFlag", mapName, flag, zone, type);
    public Task removeMapFlag(string mapName, int flag, string? zone = null)
        => ScriptStub.CallAsync(Cat, "removeMapFlag", mapName, flag, zone);
    public int getMapFlag(string mapName, int flag, int? type = null)
        => ScriptStub.Call(Cat, "getMapFlag", 0, mapName, flag, type);
    public Task setMapFlagNoSave(string mapName, string altMapName, int x, int y)
        => ScriptStub.CallAsync(Cat, "setMapFlagNoSave", mapName, altMapName, x, y);
}
