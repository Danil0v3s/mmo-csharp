using Core.Server.Packets;
using Core.Server.Packets.Out.ZC;
using Microsoft.ClearScript;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Scripting.Dialog;

public sealed partial class PlayerContext
{
    public Task setOption(int option, bool flag = true)
        => ScriptStub.CallAsync(Cat, "setOption", option, flag);

    public bool checkOption(int option)
        => ScriptStub.Call(Cat, "checkOption", false, option);

    public bool checkOption1(int option)
        => ScriptStub.Call(Cat, "checkOption1", false, option);

    public bool checkOption2(int option)
        => ScriptStub.Call(Cat, "checkOption2", false, option);

    public Task scStart(int type, int durationMs, object? opts = null)
        => ScriptStub.CallAsync(Cat, "scStart", type, durationMs, opts);

    public Task scEnd(int? type = null)
        => ScriptStub.CallAsync(Cat, "scEnd", type);

    public int getStatus(int effectType, int infoType = 0)
        => ScriptStub.Call(Cat, "getStatus", 0, effectType, infoType);

    public bool isDead()
        => ScriptStub.Call(Cat, "isDead", _entity.Hp <= 0);

    public Task recalculateStat()
        => ScriptStub.CallAsync(Cat, "recalculateStat");

    public int needStatusPoint(int statType, int value)
        => ScriptStub.Call(Cat, "needStatusPoint", 0, statType, value);
}
