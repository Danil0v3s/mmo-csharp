using Core.Server.Packets;
using Core.Server.Packets.Out.ZC;
using Microsoft.ClearScript;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Scripting.Dialog;

public sealed partial class PlayerContext
{
    public int getReputation(int type)
        => ScriptStub.Call(Cat, "getReputation", 0, type);
    public Task setReputation(int type, int points)
        => ScriptStub.CallAsync(Cat, "setReputation", type, points);
    public Task addReputation(int type, int points)
        => ScriptStub.CallAsync(Cat, "addReputation", type, points);

    public int getFame() => ScriptStub.Call(Cat, "getFame", 0);
    public Task addFame(int amount) => ScriptStub.CallAsync(Cat, "addFame", amount);
    public int getFameRank() => ScriptStub.Call(Cat, "getFameRank", 0);
}
