using Core.Server.Packets.Out.ZC;
using Microsoft.ClearScript;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Scripting.Dialog;

public sealed partial class DialogContext
{
    /// <summary>
    /// Ask the player for a numeric or string value. rAthena's <c>input</c>.
    /// Resolves with the value the client returned. Stub returns
    /// <paramref name="defaultValue"/> immediately.
    /// </summary>
    public Task<int> input(int min = int.MinValue, int max = int.MaxValue, int defaultValue = 0)
        => ScriptStub.CallAsync(Cat, "input", defaultValue, min, max, defaultValue);

    public Task<string> inputString(string defaultValue = "")
        => ScriptStub.CallAsync(Cat, "inputString", defaultValue);

    /// <summary>
    /// Suspend the dialog for the given duration. rAthena's <c>sleep2</c>.
    /// </summary>
    public Task sleep(int milliseconds)
    {
        ScriptStub.Call(Cat, "sleep", milliseconds);
        return Task.Delay(milliseconds);
    }

    /// <summary>
    /// Fire an event label on another NPC. rAthena's <c>doevent</c>.
    /// Target format: <c>"NpcName::OnLabel"</c>.
    /// </summary>
    public Task doevent(string eventTarget)
        => ScriptStub.CallAsync(Cat, "doevent", eventTarget);

    /// <summary>
    /// Schedule a one-shot timer that fires the given event label after
    /// <paramref name="milliseconds"/>. rAthena's <c>addtimer</c>.
    /// </summary>
    public Task addTimer(int milliseconds, string eventTarget)
        => ScriptStub.CallAsync(Cat, "addTimer", milliseconds, eventTarget);

    public Task delTimer(string eventTarget)
        => ScriptStub.CallAsync(Cat, "delTimer", eventTarget);

    public Task addPlayerTimer(int charId, int milliseconds, string eventTarget)
        => ScriptStub.CallAsync(Cat, "addPlayerTimer", charId, milliseconds, eventTarget);

    /// <summary>
    /// Call another NPC's function. rAthena's <c>callfunc</c>. Stubbed —
    /// in TS authors use JS imports for shared functions, so this is mostly
    /// a parity hook for code translated from rAthena scripts.
    /// </summary>
    public Task<object?> callfunc(string functionName, params object?[] args)
        => ScriptStub.CallAsync<object?>(Cat, "callfunc", null, functionName, args);

    /// <summary>End the script early — alias of <c>close</c> but without a Close button.</summary>
    public Task end()
    {
        FlushPlayerDirty();
        ScriptStub.Call(Cat, "end");
        return Task.CompletedTask;
    }

    public Task clear()
    {
        FlushPlayerDirty();
        ScriptStub.Call(Cat, "clear");
        return Task.CompletedTask;
    }
}
