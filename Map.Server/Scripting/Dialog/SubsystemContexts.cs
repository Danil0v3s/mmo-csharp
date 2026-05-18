namespace Map.Server.Scripting.Dialog;

// Each subsystem rAthena exposes lives here as its own ctx.* surface.
// Grouped together because every method is a stub today and the file
// makes the full subsystem surface easy to skim.

public sealed class PartyContext
{
    private const string Cat = "party";

    public string getName(int partyId)            => ScriptStub.Call(Cat, "getName", string.Empty, partyId);
    public object[] getMembers(int partyId, int type = 0)
                                                  => ScriptStub.Call(Cat, "getMembers", Array.Empty<object>(), partyId, type);
    public int getLeader(int partyId, int type = 0)
                                                  => ScriptStub.Call(Cat, "getLeader", 0, partyId, type);
    public bool isLeader(int? partyId = null)     => ScriptStub.Call(Cat, "isLeader", false, partyId);

    public Task<int> create(string name, int? leaderCharId = null, bool itemShare = false, int itemShareType = 0)
        => ScriptStub.CallAsync(Cat, "create", 0, name, leaderCharId, itemShare, itemShareType);
    public Task destroy(int partyId)              => ScriptStub.CallAsync(Cat, "destroy", partyId);
    public Task addMember(int partyId, int charId)=> ScriptStub.CallAsync(Cat, "addMember", partyId, charId);
    public Task delMember(int charId, int? partyId = null)
                                                  => ScriptStub.CallAsync(Cat, "delMember", charId, partyId);
    public Task changeLeader(int partyId, int charId)
                                                  => ScriptStub.CallAsync(Cat, "changeLeader", partyId, charId);
    public Task changeOption(int partyId, int option, bool flag)
                                                  => ScriptStub.CallAsync(Cat, "changeOption", partyId, option, flag);
}

public sealed class GuildContext
{
    private const string Cat = "guild";

    public string getName(int guildId)               => ScriptStub.Call(Cat, "getName", string.Empty, guildId);
    public string getMaster(int guildId)             => ScriptStub.Call(Cat, "getMaster", string.Empty, guildId);
    public int getMasterId(int guildId)              => ScriptStub.Call(Cat, "getMasterId", 0, guildId);
    public int info(int guildId, int type)           => ScriptStub.Call(Cat, "info", 0, guildId, type);
    public object[] getMembers(int guildId, int type = 0)
                                                     => ScriptStub.Call(Cat, "getMembers", Array.Empty<object>(), guildId, type);
    public int getSkillLv(int guildId, int skillId)  => ScriptStub.Call(Cat, "getSkillLv", 0, guildId, skillId);
    public int getAlliance(int g1, int g2)           => ScriptStub.Call(Cat, "getAlliance", 0, g1, g2);
    public int getMapUsers(string mapName, int guildId)
                                                     => ScriptStub.Call(Cat, "getMapUsers", 0, mapName, guildId);
    public Task changeMaster(int guildId, string newMasterName)
                                                     => ScriptStub.CallAsync(Cat, "changeMaster", guildId, newMasterName);
    public Task requestInfo(int guildId, string? eventLabel = null)
                                                     => ScriptStub.CallAsync(Cat, "requestInfo", guildId, eventLabel);
}

public sealed class InstanceContext
{
    private const string Cat = "instance";

    public Task<int> create(string name, int mode = 0, int? ownerId = null)
        => ScriptStub.CallAsync(Cat, "create", 0, name, mode, ownerId);
    public Task destroy(int? instanceId = null)
        => ScriptStub.CallAsync(Cat, "destroy", instanceId);
    public Task<int> enter(string name, int? x = null, int? y = null, int? charId = null, int? instanceId = null)
        => ScriptStub.CallAsync(Cat, "enter", 0, name, x, y, charId, instanceId);
    public string npcName(string npcName, int? instanceId = null)
        => ScriptStub.Call(Cat, "npcName", npcName, npcName, instanceId);
    public string mapName(string mapName, int? instanceId = null)
        => ScriptStub.Call(Cat, "mapName", mapName, mapName, instanceId);
    public int id(int mode = 0) => ScriptStub.Call(Cat, "id", 0, mode);
    public Task warpAll(string mapName, int x, int y, int? instanceId = null, int flag = 0)
        => ScriptStub.CallAsync(Cat, "warpAll", mapName, x, y, instanceId, flag);
    public Task announce(int instanceId, string text, int flag = 0, object? opts = null)
        => ScriptStub.CallAsync(Cat, "announce", instanceId, text, flag, opts);
    public bool checkParty(int partyId, int? amount = null, int? minLv = null, int? maxLv = null)
        => ScriptStub.Call(Cat, "checkParty", false, partyId, amount, minLv, maxLv);
    public bool checkGuild(int guildId, int? amount = null, int? minLv = null, int? maxLv = null)
        => ScriptStub.Call(Cat, "checkGuild", false, guildId, amount, minLv, maxLv);
    public bool checkClan(int clanId, int? amount = null, int? minLv = null, int? maxLv = null)
        => ScriptStub.Call(Cat, "checkClan", false, clanId, amount, minLv, maxLv);
    public object? info(string name, int infoType, int? mapIndex = null)
        => ScriptStub.Call<object?>(Cat, "info", null, name, infoType, mapIndex);
    public object? liveInfo(int infoType, int? instanceId = null)
        => ScriptStub.Call<object?>(Cat, "liveInfo", null, infoType, instanceId);
    public object[] list(string mapName, int mode = 0)
        => ScriptStub.Call(Cat, "list", Array.Empty<object>(), mapName, mode);
    public object? getVar(string name, int instanceId)
        => ScriptStub.Call<object?>(Cat, "getVar", null, name, instanceId);
    public Task setVar(string name, object value, int instanceId)
        => ScriptStub.CallAsync(Cat, "setVar", name, value, instanceId);
}

public sealed class BattlegroundContext
{
    private const string Cat = "bg";

    public Task<int> create(string mapName, int x, int y, string? onQuitEvent = null, string? onDeathEvent = null)
        => ScriptStub.CallAsync(Cat, "create", 0, mapName, x, y, onQuitEvent, onDeathEvent);
    public Task<int> join(int battleGroup, string? mapName = null, int x = 0, int y = 0, int? charId = null)
        => ScriptStub.CallAsync(Cat, "join", 0, battleGroup, mapName, x, y, charId);
    public Task setTeamXY(int battleGroup, int x, int y)
        => ScriptStub.CallAsync(Cat, "setTeamXY", battleGroup, x, y);
    public Task<int> reserve(string mapName, bool ended = false)
        => ScriptStub.CallAsync(Cat, "reserve", 0, mapName, ended);
    public Task unbook(string mapName) => ScriptStub.CallAsync(Cat, "unbook", mapName);
    public Task desert(int? charId = null) => ScriptStub.CallAsync(Cat, "desert", charId);
    public Task warp(int battleGroup, string mapName, int x, int y)
        => ScriptStub.CallAsync(Cat, "warp", battleGroup, mapName, x, y);
    public Task<long> spawnMonster(int battleGroup, string mapName, int x, int y, string displayName, int mobId, string eventLabel)
        => ScriptStub.CallAsync(Cat, "spawnMonster", 0L, battleGroup, mapName, x, y, displayName, mobId, eventLabel);
    public Task setMonsterTeam(long gid, int battleGroup)
        => ScriptStub.CallAsync(Cat, "setMonsterTeam", gid, battleGroup);
    public Task leave(int? charId = null) => ScriptStub.CallAsync(Cat, "leave", charId);
    public Task destroy(int battleGroup) => ScriptStub.CallAsync(Cat, "destroy", battleGroup);
    public Task waitingRoomToBgSingle(int battleGroup, string mapName, int x, int y, string? npcName = null)
        => ScriptStub.CallAsync(Cat, "waitingRoomToBgSingle", battleGroup, mapName, x, y, npcName);
    public Task waitingRoomToBg(string mapName, int x, int y, string? onQuitEvent = null, string? onDeathEvent = null, string? npcName = null)
        => ScriptStub.CallAsync(Cat, "waitingRoomToBg", mapName, x, y, onQuitEvent, onDeathEvent, npcName);
    public int getData(int battleGroup, int type) => ScriptStub.Call(Cat, "getData", 0, battleGroup, type);
    public int areaUsers(int battleGroup, string mapName, int x0, int y0, int x1, int y1)
        => ScriptStub.Call(Cat, "areaUsers", 0, battleGroup, mapName, x0, y0, x1, y1);
    public Task updateScore(string mapName, int guillaumeScore, int croixScore)
        => ScriptStub.CallAsync(Cat, "updateScore", mapName, guillaumeScore, croixScore);
    public object? info(string bgName, int type)
        => ScriptStub.Call<object?>(Cat, "info", null, bgName, type);
}

public sealed class ChannelContext
{
    private const string Cat = "channel";

    public Task create(string name, string alias, string? password = null,
        int option = 0, int delay = 0, int color = 0, int? charId = null)
        => ScriptStub.CallAsync(Cat, "create", name, alias, password, option, delay, color, charId);
    public Task join(string name, int? charId = null)        => ScriptStub.CallAsync(Cat, "join", name, charId);
    public Task setOption(string name, int option, int value)=> ScriptStub.CallAsync(Cat, "setOption", name, option, value);
    public int getOption(string name, int option)            => ScriptStub.Call(Cat, "getOption", 0, name, option);
    public Task setColor(string name, int color)             => ScriptStub.CallAsync(Cat, "setColor", name, color);
    public Task setPassword(string name, string password)    => ScriptStub.CallAsync(Cat, "setPassword", name, password);
    public Task setGroups(string name, object groupIds)      => ScriptStub.CallAsync(Cat, "setGroups", name, groupIds);
    public Task chat(string name, string message, int color = 0)
                                                             => ScriptStub.CallAsync(Cat, "chat", name, message, color);
    public Task ban(string name, int charId)                 => ScriptStub.CallAsync(Cat, "ban", name, charId);
    public Task unban(string name, int charId)               => ScriptStub.CallAsync(Cat, "unban", name, charId);
    public Task kick(string name, int charId)                => ScriptStub.CallAsync(Cat, "kick", name, charId);
    public Task delete(string name)                          => ScriptStub.CallAsync(Cat, "delete", name);
}
