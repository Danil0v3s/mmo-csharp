using Core.Server.Packets;
using Core.Server.Packets.Out.ZC;
using Microsoft.ClearScript;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Scripting.Dialog;

public sealed partial class PlayerContext
{
    public Task giveExp(long baseExp, long jobExp, object? opts = null)
        => ScriptStub.CallAsync(Cat, "giveExp", baseExp, jobExp, opts);

    public long baseExpRatio(int percent, int level = 0)
        => ScriptStub.Call<long>(Cat, "baseExpRatio", 0, percent, level);

    public long jobExpRatio(int percent, int level = 0)
        => ScriptStub.Call<long>(Cat, "jobExpRatio", 0, percent, level);
}
