namespace Map.Server.Scripting;

/// <summary>
/// Binds to the <c>Scripting</c> section of <c>appsettings.json</c>:
/// <code>
/// "Scripting": {
///   "ScriptsRoot": "../scripts/dist",
///   "EntryFile": "main.js",
///   "HotReload": true
/// }
/// </code>
/// </summary>
public sealed class ScriptHostOptions
{
    /// <summary>
    /// Filesystem path (absolute or relative to the map-server working directory)
    /// where the esbuild bundle lives. Defaults to <c>../scripts/dist</c> for the
    /// dev workflow (running from <c>Map.Server/</c>).
    /// </summary>
    public string ScriptsRoot { get; set; } = "../scripts/dist";

    /// <summary>The bundle file inside <see cref="ScriptsRoot"/>.</summary>
    public string EntryFile { get; set; } = "main.js";

    /// <summary>
    /// When true, the host re-evaluates the entry file when its mtime changes.
    /// Phase 1 ships restart-message-only; the actual live diff lands in Phase 2.
    /// </summary>
    public bool HotReload { get; set; } = false;
}
