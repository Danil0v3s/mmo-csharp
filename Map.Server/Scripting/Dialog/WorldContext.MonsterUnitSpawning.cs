namespace Map.Server.Scripting.Dialog;

public sealed partial class WorldContext
{
    public Task<int> spawnMob(string mapName, int x, int y, string displayName,
        int mobId, int amount = 1, string? onDeathEvent = null)
        => ScriptStub.CallAsync(Cat, "spawnMob", 0, mapName, x, y, displayName, mobId, amount, onDeathEvent);
    public Task<int> spawnAreaMob(string mapName, int x1, int y1, int x2, int y2,
        string displayName, int mobId, int amount = 1, string? onDeathEvent = null)
        => ScriptStub.CallAsync(Cat, "spawnAreaMob", 0, mapName, x1, y1, x2, y2, displayName, mobId, amount, onDeathEvent);
    public Task<int> spawnGuardian(string mapName, int x, int y, string displayName,
        int mobId, string? onDeathEvent = null, int? guardianIndex = null)
        => ScriptStub.CallAsync(Cat, "spawnGuardian", 0, mapName, x, y, displayName, mobId, onDeathEvent, guardianIndex);
    public object? guardianInfo(string mapName, int guardianIndex, int type)
        => ScriptStub.Call<object?>(Cat, "guardianInfo", null, mapName, guardianIndex, type);
    public Task<int> killMonster(string mapName, string eventLabel)
        => ScriptStub.CallAsync(Cat, "killMonster", 0, mapName, eventLabel);
    public Task<int> killMonsterAll(string mapName)
        => ScriptStub.CallAsync(Cat, "killMonsterAll", 0, mapName);
    public int mobCount(string mapName, string eventLabel)
        => ScriptStub.Call(Cat, "mobCount", 0, mapName, eventLabel);
    public Task respawnGuildOwned(string mapName, int guildId, int flag = 0)
        => ScriptStub.CallAsync(Cat, "respawnGuildOwned", mapName, guildId, flag);

    public int getRandomMobId(int type, int flag = 0, int level = 0)
        => ScriptStub.Call(Cat, "getRandomMobId", 1002, type, flag, level);
    public object? getMonsterInfo(int mobId, int type)
        => ScriptStub.Call<object?>(Cat, "getMonsterInfo", null, mobId, type);
    public object[] getMobDrops(int mobId)
        => ScriptStub.Call(Cat, "getMobDrops", Array.Empty<object>(), mobId);
    public string mobInfo(int type, int mobId)
        => ScriptStub.Call(Cat, "mobInfo", string.Empty, type, mobId);
}
