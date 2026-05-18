namespace Map.Server.Scripting.Records;

/// <summary>
/// The bundle of event-hook closures captured from a single
/// <c>registerNpc</c> / <c>registerFloatingNpc</c> call. Every field is
/// optional — authors only set the hooks they care about.
///
/// Phase 1 stores the handles; Phase 2 wires the dispatcher that actually
/// invokes them inside Jint. The fields here are the contract Phase 2
/// will read.
/// </summary>
public sealed record NpcHooks(
    ScriptHandle? OnClick,
    ScriptHandle? OnTouch,
    ScriptHandle? OnInit,
    IReadOnlyDictionary<int, ScriptHandle>? OnTimer,
    IReadOnlyDictionary<string, ScriptHandle>? OnClock,
    ScriptHandle? OnPCLogin,
    ScriptHandle? OnPCDeath,
    ScriptHandle? OnPCKill,
    ScriptHandle? OnNPCKill)
{
    public static readonly NpcHooks Empty = new(null, null, null, null, null, null, null, null, null);

    /// <summary>True when at least one hook is set.</summary>
    public bool Any =>
        OnClick != null || OnTouch != null || OnInit != null ||
        (OnTimer != null && OnTimer.Count > 0) ||
        (OnClock != null && OnClock.Count > 0) ||
        OnPCLogin != null || OnPCDeath != null ||
        OnPCKill != null || OnNPCKill != null;
}
