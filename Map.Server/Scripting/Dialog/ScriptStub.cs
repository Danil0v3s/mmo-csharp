namespace Map.Server.Scripting.Dialog;

/// <summary>
/// Helper for the first-pass scripting API surface. Every binding is a
/// "stub" — the method exists, calling it logs that it isn't wired yet,
/// and a placeholder return value comes back. Lets script authors iterate
/// against the full surface today; the internals land later.
///
/// Static state is intentional: stubs are pure logging + placeholders, and
/// threading a logger through every sub-context would be all noise. The
/// host calls <see cref="Configure"/> at boot.
/// </summary>
internal static class ScriptStub
{
    private static ILogger? _logger;

    public static void Configure(ILoggerFactory factory)
    {
        _logger = factory.CreateLogger("Map.Server.Scripting.Stubs");
    }

    public static void Call(string category, string method, params object?[] args)
    {
        _logger?.LogInformation(
            "Script stub: {Category}.{Method}({Args}) — not yet implemented",
            category, method, FormatArgs(args));
    }

    public static T Call<T>(string category, string method, T placeholder, params object?[] args)
    {
        Call(category, method, args);
        return placeholder;
    }

    public static Task CallAsync(string category, string method, params object?[] args)
    {
        Call(category, method, args);
        return Task.CompletedTask;
    }

    public static Task<T> CallAsync<T>(string category, string method, T placeholder, params object?[] args)
    {
        Call(category, method, args);
        return Task.FromResult(placeholder);
    }

    private static string FormatArgs(object?[] args)
    {
        if (args.Length == 0) return string.Empty;
        return string.Join(", ", args.Select(a => a switch
        {
            null => "null",
            string s => $"\"{s}\"",
            _ => a.ToString() ?? "null"
        }));
    }
}
