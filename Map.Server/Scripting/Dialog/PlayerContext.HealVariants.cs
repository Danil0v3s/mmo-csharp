using Core.Server.Packets;
using Core.Server.Packets.Out.ZC;
using Microsoft.ClearScript;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Scripting.Dialog;

public sealed partial class PlayerContext
{
    public Task healAp(int ap)
        => ScriptStub.CallAsync(Cat, "healAp", ap);

    public Task itemHeal(int hp, int sp)
        => ScriptStub.CallAsync(Cat, "itemHeal", hp, sp);

    public Task percentHeal(int hpPercent, int spPercent)
        => ScriptStub.CallAsync(Cat, "percentHeal", hpPercent, spPercent);

    public Task recovery(int type, object? opts = null)
        => ScriptStub.CallAsync(Cat, "recovery", type, opts);
}
