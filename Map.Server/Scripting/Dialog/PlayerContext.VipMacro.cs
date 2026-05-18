using Core.Server.Packets;
using Core.Server.Packets.Out.ZC;
using Microsoft.ClearScript;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Scripting.Dialog;

public sealed partial class PlayerContext
{
    public int vipStatus(int type)
        => ScriptStub.Call(Cat, "vipStatus", 0, type);
    public Task vipTime(int seconds)
        => ScriptStub.CallAsync(Cat, "vipTime", seconds);
    public Task macroDetector()
        => ScriptStub.CallAsync(Cat, "macroDetector");
}
