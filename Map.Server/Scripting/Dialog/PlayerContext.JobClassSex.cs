using Core.Server.Packets;
using Core.Server.Packets.Out.ZC;
using Microsoft.ClearScript;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Scripting.Dialog;

public sealed partial class PlayerContext
{
    public Task jobChange(int jobId, object? opts = null)
        => ScriptStub.CallAsync(Cat, "jobChange", jobId, opts);

    public Task changeBase(int classId)
        => ScriptStub.CallAsync(Cat, "changeBase", classId);

    public Task changeSex()
        => ScriptStub.CallAsync(Cat, "changeSex");

    public string jobName(int jobId)
        => ScriptStub.Call(Cat, "jobName", $"Job_{jobId}", jobId);
}
