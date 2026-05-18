using Core.Server.Packets;
using Core.Server.Packets.Out.ZC;
using Microsoft.ClearScript;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Scripting.Dialog;

public sealed partial class PlayerContext
{
    public string charInfo(int type)
        => ScriptStub.Call(Cat, "charInfo", string.Empty, type);

    public int readParam(int paramNumber)
        => ScriptStub.Call(Cat, "readParam", 0, paramNumber);

    public int charId4Type(int type)
        => ScriptStub.Call(Cat, "charId4Type", id, type);

    public string charIp()
        => ScriptStub.Call(Cat, "charIp", string.Empty);

    public Task kick()
        => ScriptStub.CallAsync(Cat, "kick");

    public Task ignoreTimeout(bool flag)
        => ScriptStub.CallAsync(Cat, "ignoreTimeout", flag);

    public int autoLoot(int? rate = null)
        => ScriptStub.Call(Cat, "autoLoot", rate ?? 0, rate);
    public bool hasAutoLoot() => ScriptStub.Call(Cat, "hasAutoLoot", false);

    public bool jobCanEnterMap(string map, int? jobId = null)
        => ScriptStub.Call(Cat, "jobCanEnterMap", true, map, jobId);

    public bool checkVending() => ScriptStub.Call(Cat, "checkVending", false);
    public bool checkChatting() => ScriptStub.Call(Cat, "checkChatting", false);
    public bool checkIdle() => ScriptStub.Call(Cat, "checkIdle", false);

    public Task navigateTo(string map, int x = 0, int y = 0, int flag = 0,
        bool hideWindow = false, int monsterId = 0)
        => ScriptStub.CallAsync(Cat, "navigateTo", map, x, y, flag, hideWindow, monsterId);

    public Task clanJoin(int clanId) => ScriptStub.CallAsync(Cat, "clanJoin", clanId);
    public Task clanLeave() => ScriptStub.CallAsync(Cat, "clanLeave");

    public object? cameraInfo(double range, double rotation, double latitude)
        => ScriptStub.Call<object?>(Cat, "cameraInfo", null, range, rotation, latitude);
}
