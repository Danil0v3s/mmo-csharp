namespace Map.Server.Handlers.Actions;

/// <summary>
/// action-code → <see cref="IActionHandler"/> dispatch table. Same
/// strategy shape as SkillResolverRegistry / ItemEffectRegistry.
/// New action codes ship as a new IActionHandler class + DI line.
/// </summary>
public sealed class ActionRegistry
{
    private readonly Dictionary<byte, IActionHandler> _byCode = new();

    public ActionRegistry(IEnumerable<IActionHandler> handlers)
    {
        foreach (var h in handlers) _byCode[h.ActionCode] = h;
    }

    public IActionHandler? Get(byte actionCode) => _byCode.GetValueOrDefault(actionCode);
}
