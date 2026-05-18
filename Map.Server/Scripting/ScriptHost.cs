using Jint;
using Map.Server.Scripting.Registrars;

namespace Map.Server.Scripting;

/// <summary>
/// Owns the Jint engine for the lifetime of the map server. At boot it
/// evaluates <c>{ScriptsRoot}/{EntryFile}</c> (default <c>../scripts/dist/main.js</c>),
/// which side-effect-imports the rest of the TS bundle and triggers every
/// <c>register*()</c> call. Phase 1 stops there: the registry is populated,
/// hook closures are captured as opaque <c>JsValue</c>s, and the engine sits
/// idle until Phase 2 wires the dispatcher that actually invokes them.
///
/// The engine is created with CLR interop disabled (scripts cannot reach
/// arbitrary .NET types), recursion limited, and a generous statement budget
/// for the one-time registration pass.
/// </summary>
public sealed class ScriptHost
{
    private readonly INpcRegistry _registry;
    private readonly ScriptHostOptions _options;
    private readonly ILogger<ScriptHost> _logger;
    private Engine? _engine;

    public ScriptHost(INpcRegistry registry, ScriptHostOptions options, ILogger<ScriptHost> logger)
    {
        _registry = registry;
        _options = options;
        _logger = logger;
    }

    public Engine Engine => _engine
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
            // Build an empty engine so callers that depend on ScriptHost can still resolve.
            _engine = BuildEngine();
            return;
        }

        var source = File.ReadAllText(entryPath);
        _engine = BuildEngine();
        try
        {
            _engine.Execute(source, entryPath);
        }
        catch (ScriptRegistrationException)
        {
            // Already carries a useful message; re-throw to fail boot.
            throw;
        }
        catch (Exception ex)
        {
            throw new ScriptRegistrationException(
                $"Failed to evaluate scripting bundle '{entryPath}': {ex.Message}", ex);
        }

        _logger.LogInformation(
            "Scripts loaded from {Path}: {Npcs} NPCs / {Floating} floating / {Shops} shops / {Warps} warps / {Spawns} spawns",
            entryPath,
            _registry.NpcCount, _registry.FloatingCount,
            _registry.ShopCount, _registry.WarpCount, _registry.SpawnCount);
    }

    private string ResolveEntryPath()
    {
        var root = Path.IsPathRooted(_options.ScriptsRoot)
            ? _options.ScriptsRoot
            : Path.GetFullPath(_options.ScriptsRoot, AppContext.BaseDirectory);
        return Path.Combine(root, _options.EntryFile);
    }

    private Engine BuildEngine()
    {
        var engine = new Engine(opts =>
        {
            // AllowClr is opt-in — by NOT calling it, scripts have no access to
            // System.IO.File / arbitrary .NET types. Keep it that way; if Phase 2+
            // needs a controlled host surface, expose specific objects via SetValue,
            // never the CLR.
            opts.LimitRecursion(100);
            opts.MaxStatements(10_000_000);       // generous; registration is one-time
            opts.Strict();
            // Phase 2 dialog dispatch is driven by generator functions
            // (function* / yield). Suspension model is: the host pulls one
            // DialogStep at a time via iter.next(), sends the corresponding
            // packet, and resumes when the client responds.
            //
            // KNOWN JINT QUIRK (4.0.3): the yielded value of a `yield`
            // expression is dropped when the yield is the RHS of an
            // assignment — both `const a = yield x` and `a = yield x` yield
            // {value: undefined} instead of {value: x}. Workaround pattern:
            // never put `yield` inside an assignment; if you need to read
            // a client response, the host stashes it on `ctx.lastSelection`
            // (or `ctx.lastInput`) before resuming the generator, and the
            // author reads it via a plain `const x = ctx.lastSelection`
            // AFTER the yield.
            opts.ExperimentalFeatures = ExperimentalFeature.Generators;
        });
        RegistrarBindings.Bind(engine, _registry);
        return engine;
    }
}
