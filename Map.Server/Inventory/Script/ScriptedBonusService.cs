using System.Collections.Concurrent;
using Map.Server.Entities;
using Map.Server.Items;
using Map.Server.Status;
using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;
using Microsoft.Extensions.Logging;

namespace Map.Server.Inventory.Script;

/// <summary>
/// Default <see cref="IScriptedBonusService"/>. Owns a dedicated V8
/// engine (separate from the NPC scripting engine in
/// <c>Map.Server.Scripting.ScriptHost</c>) so item-script execution
/// can't leak host-API state into the NPC namespace, and vice versa.
///
/// <para>
/// Singleton — one engine for the lifetime of the map server. Equip
/// recalc runs on the game loop, so we don't need per-call engines;
/// the per-call <see cref="ScriptedBonusHost"/> instance carries all
/// the mutable state.
/// </para>
/// </summary>
public sealed class ScriptedBonusService : IScriptedBonusService, IDisposable
{
    private readonly V8ScriptEngine _engine;
    private readonly IPlayerBonusService? _bonusSvc;
    private readonly IItemCatalog? _catalog;
    private readonly ILogger<ScriptedBonusService> _logger;

    // Translation cache: rAthena script body → JS function body.
    // Bounded to a few thousand entries (stock item_combo_db is
    // 7,767 unique scripts; in practice many combos share scripts).
    private readonly ConcurrentDictionary<string, string> _jsCache = new();
    private int _hits;
    private int _misses;

    public ScriptedBonusService(
        ILogger<ScriptedBonusService> logger,
        IPlayerBonusService? bonusSvc = null,
        IItemCatalog? catalog = null)
    {
        _logger = logger;
        _bonusSvc = bonusSvc;
        _catalog = catalog;
        // Same flag combo as ScriptHost — strict-ish JS with no global
        // host members. We don't enable Task↔Promise conversion (bonus
        // scripts are fully synchronous).
        _engine = new V8ScriptEngine("mmo-bonus-scripts",
            V8ScriptEngineFlags.DisableGlobalMembers);
    }

    public (int Hits, int Misses, int CachedScripts) CacheStats
        => (_hits, _misses, _jsCache.Count);

    public bool Apply(string script, PlayerEntity pc, EquipBonusBundle bundle,
        IReadOnlyList<InventoryItem>? equipped = null)
    {
        if (string.IsNullOrWhiteSpace(script)) return true;

        // 1. Translate (cached).
        string js;
        try
        {
            if (_jsCache.TryGetValue(script, out var cached))
            {
                js = cached;
                Interlocked.Increment(ref _hits);
            }
            else
            {
                var ast = RathenaScriptParser.Parse(script);
                js = RathenaToJsTranslator.Translate(ast);
                _jsCache[script] = js;
                Interlocked.Increment(ref _misses);
            }
        }
        catch (ScriptParseException ex)
        {
            _logger.LogDebug(ex, "ScriptedBonus.Apply: parse failed (script: {Snippet})", Snippet(script));
            return false;
        }

        // 2. Bind a fresh host + execute. The engine is single-threaded
        // (equip recalc runs on the game loop) so swapping the global
        // 'h' per call is safe.
        var host = new ScriptedBonusHost(pc, bundle, equipped, _catalog, _bonusSvc);
        try
        {
            _engine.AddHostObject("h", host);
            _engine.Execute(js);
            return true;
        }
        catch (ScriptEngineException ex)
        {
            _logger.LogDebug(ex,
                "ScriptedBonus.Apply: V8 exec failed (script: {Snippet}; details: {Details})",
                Snippet(script), ex.ErrorDetails);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "ScriptedBonus.Apply: host call threw (script: {Snippet})", Snippet(script));
            return false;
        }
    }

    private static string Snippet(string s)
    {
        if (s.Length <= 80) return s.Replace('\n', ' ');
        return s[..80].Replace('\n', ' ') + "…";
    }

    public void Dispose() => _engine.Dispose();
}
