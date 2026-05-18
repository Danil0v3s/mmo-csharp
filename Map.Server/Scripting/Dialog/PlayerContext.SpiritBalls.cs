using Core.Server.Packets;
using Core.Server.Packets.Out.ZC;
using Microsoft.ClearScript;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Scripting.Dialog;

public sealed partial class PlayerContext
{
    public Task addSpiritBall(int count, int durationMs)
        => ScriptStub.CallAsync(Cat, "addSpiritBall", count, durationMs);

    public Task delSpiritBall(int count)
        => ScriptStub.CallAsync(Cat, "delSpiritBall", count);

    public int countSpiritBall()
        => ScriptStub.Call(Cat, "countSpiritBall", 0);
}
