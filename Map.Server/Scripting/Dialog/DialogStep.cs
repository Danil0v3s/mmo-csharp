namespace Map.Server.Scripting.Dialog;

/// <summary>
/// One step the script generator yielded. Each <c>yield ctx.&lt;method&gt;(...)</c>
/// in a generator produces an instance of <see cref="DialogStep"/>; the
/// dispatcher reads the kind tag and sends the matching packet.
///
/// Discriminated by <see cref="Kind"/>:
/// <list type="bullet">
///   <item><see cref="DialogStepKind.Mes"/> — emit ZC_SAY_DIALOG, resume immediately.</item>
///   <item><see cref="DialogStepKind.Next"/> — emit ZC_WAIT_DIALOG, resume on CZ_REQ_NEXT_SCRIPT.</item>
///   <item><see cref="DialogStepKind.Menu"/> — emit ZC_MENU_LIST, stash selection on ctx.lastSelection, resume on CZ_CHOOSE_MENU.</item>
///   <item><see cref="DialogStepKind.Close"/> — emit ZC_CLOSE_DIALOG, end dialog, resume on CZ_CLOSE_DIALOG.</item>
/// </list>
/// </summary>
public sealed record DialogStep(DialogStepKind Kind, string? Text, IReadOnlyList<string>? Options);

public enum DialogStepKind
{
    Mes,
    Next,
    Menu,
    Close,
}
