using Core.Server.Packets;
using Core.Server.Packets.Out.ZC;
using Microsoft.ClearScript;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Scripting.Dialog;

public sealed partial class PlayerContext
{
    public Task resetStatus() => ScriptStub.CallAsync(Cat, "resetStatus");
    public Task resetSkill()  => ScriptStub.CallAsync(Cat, "resetSkill");
    public Task resetFeel()   => ScriptStub.CallAsync(Cat, "resetFeel");
    public Task resetHate()   => ScriptStub.CallAsync(Cat, "resetHate");
}
