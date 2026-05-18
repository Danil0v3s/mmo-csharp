using Microsoft.ClearScript;
using Microsoft.ClearScript.JavaScript;
using Microsoft.ClearScript.V8;
using Map.Server.Scripting.Registrars;

namespace Map.Server.Scripting;

/// <summary>
/// Owns the V8 JavaScript engine for the lifetime of the map server. At
/// boot it evaluates <c>{ScriptsRoot}/{EntryFile}</c> (default
/// <c>../scripts/dist/main.js</c>), which side-effect-imports the rest of
/// the TS bundle and triggers every <c>register*()</c> call.
///
/// V8 has native <c>async</c>/<c>await</c>, so hook closures are real async
/// functions; the dispatcher awaits the returned Promise. Suspending host
/// functions return <see cref="System.Threading.Tasks.Task"/> — ClearScript
/// auto-marshals these to JS Promises, and resolving the Task wakes the
/// awaiting JS function.
/// </summary>
public sealed class ScriptHost : IDisposable
{
    private readonly INpcRegistry _registry;
    private readonly ScriptHostOptions _options;
    private readonly ILogger<ScriptHost> _logger;
    private V8ScriptEngine? _engine;

    public ScriptHost(INpcRegistry registry, ScriptHostOptions options, ILogger<ScriptHost> logger)
    {
        _registry = registry;
        _options = options;
        _logger = logger;
    }

    public V8ScriptEngine Engine => _engine
        ?? throw new InvalidOperationException("ScriptHost has not loaded a bundle yet.");

    public void LoadEntryPoint()
    {
        var entryPath = ResolveEntryPath();
        if (!File.Exists(entryPath))
        {
            _logger.LogWarning(
                "Scripting bundle not found at {Path}. " +
                "Run `npm run build` in the scripts/ directory. " +
                "The map server will start with no NPCs / shops / warps / spawns from the script side.",
                entryPath);
            _engine = BuildEngine();
            return;
        }

        var source = File.ReadAllText(entryPath);
        _engine = BuildEngine();
        try
        {
            _engine.Execute(new DocumentInfo(new Uri(entryPath)), source);
        }
        catch (ScriptRegistrationException)
        {
            throw;
        }
        catch (ScriptEngineException ex)
        {
            throw new ScriptRegistrationException(
                $"Failed to evaluate scripting bundle '{entryPath}': {ex.Message}\n{ex.ErrorDetails}", ex);
        }
        catch (Exception ex)
        {
            throw new ScriptRegistrationException(
                $"Failed to evaluate scripting bundle '{entryPath}': {ex.Message}", ex);
        }

        _logger.LogInformation(
            "Scripts loaded from {Path}: {Npcs} NPCs / {Floating} floating / {Shops} shops / {Warps} warps / {Spawns} spawns / {MapFlags} mapflags",
            entryPath,
            _registry.NpcCount, _registry.FloatingCount,
            _registry.ShopCount, _registry.WarpCount, _registry.SpawnCount,
            _registry.MapFlagCount);
    }

    private string ResolveEntryPath()
    {
        var root = Path.IsPathRooted(_options.ScriptsRoot)
            ? _options.ScriptsRoot
            : Path.GetFullPath(_options.ScriptsRoot, AppContext.BaseDirectory);
        return Path.Combine(root, _options.EntryFile);
    }

    private V8ScriptEngine BuildEngine()
    {
        // Strict-mode JS, no host CLR exposure beyond what we explicitly add.
        // DisableGlobalMembers prevents scripts from accidentally reaching
        // the global host. EnableTaskPromiseConversion is the magic that
        // turns C# `Task<T>` into JS `Promise<T>`, which is what makes
        // `await ctx.mes(...)` work natively.
        var flags = V8ScriptEngineFlags.DisableGlobalMembers
                  | V8ScriptEngineFlags.EnableTaskPromiseConversion
                  | V8ScriptEngineFlags.EnableValueTaskPromiseConversion;
        var engine = new V8ScriptEngine("mmo-scripts", flags);
        RegistrarBindings.Bind(engine, _registry);
        return engine;
    }

    public void Dispose()
    {
        _engine?.Dispose();
        _engine = null;
    }
}
