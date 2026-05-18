using Map.Server.Entities;

namespace Map.Server.Scripting.Dialog;

/// <summary>
/// Per-player dialog state, held on <see cref="Session.MapSessionData.Dialog"/>
/// while the player is in an NPC dialog.
///
/// Each suspending host call (<c>ctx.next()</c> / <c>ctx.select()</c> /
/// <c>ctx.close()</c>) creates a <see cref="TaskCompletionSource"/>; the
/// returned <see cref="Task"/> is what V8 awaits. When the client sends
/// the matching CZ packet, the resume handler calls <c>SetResult</c> and
/// the script resumes.
/// </summary>
public sealed class DialogSession
{
    public NpcEntity Npc { get; }

    /// <summary>
    /// Set exactly once by the dispatcher after the session and context
    /// have both been constructed (they reference each other; this lets us
    /// avoid `null!` in the constructor).
    /// </summary>
    public DialogContext Context { get; internal set; } = null!;

    /// <summary>
    /// The completion source the script is currently awaiting. Set by
    /// <c>ctx.next() / select() / close()</c>; cleared and signalled by
    /// the matching resume handler. Null when the script isn't blocked
    /// on a client packet (e.g. immediately after <c>ctx.mes(...)</c>).
    /// </summary>
    internal PendingWait? Pending { get; set; }

    public DialogSession(NpcEntity npc)
    {
        Npc = npc;
    }
}

/// <summary>Discriminated union of the suspending step a script is waiting on.</summary>
internal abstract record PendingWait
{
    /// <summary>ZC_WAIT_DIALOG out, CZ_REQ_NEXT_SCRIPT in.</summary>
    public sealed record Next(TaskCompletionSource Tcs) : PendingWait;

    /// <summary>ZC_MENU_LIST out, CZ_CHOOSE_MENU in. Resolves with the 1-based selection (0 = Escape).</summary>
    public sealed record Menu(TaskCompletionSource<int> Tcs) : PendingWait;

    /// <summary>ZC_CLOSE_DIALOG out, CZ_CLOSE_DIALOG in.</summary>
    public sealed record Close(TaskCompletionSource Tcs) : PendingWait;
}
