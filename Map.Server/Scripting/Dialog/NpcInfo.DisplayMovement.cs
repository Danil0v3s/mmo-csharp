using Core.Server.Packets.Out.ZC;
using Microsoft.ClearScript;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Scripting.Dialog;

public sealed partial class NpcInfo
{
    public Task setDisplay(string displayName, int classId, int size = 0)
        => ScriptStub.CallAsync(Cat, "setDisplay", displayName, classId, size);

    public Task speed(int value) => ScriptStub.CallAsync(Cat, "speed", value);
    public Task walkTo(int x, int y) => ScriptStub.CallAsync(Cat, "walkTo", x, y);
    public Task stop(bool clearTarget = false) => ScriptStub.CallAsync(Cat, "stop", clearTarget);
    public Task moveTo(int x, int y, int? dir = null) => ScriptStub.CallAsync(Cat, "moveTo", x, y, dir);
    public Task hide() => ScriptStub.CallAsync(Cat, "hide");
    public Task show() => ScriptStub.CallAsync(Cat, "show");
    public Task disable() => ScriptStub.CallAsync(Cat, "disable");
    public Task enable() => ScriptStub.CallAsync(Cat, "enable");
    public Task duplicateDynamic(int? charId = null)
        => ScriptStub.CallAsync(Cat, "duplicateDynamic", charId);
}
