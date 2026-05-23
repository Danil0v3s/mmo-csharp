using System.Collections.Concurrent;
using System.Reflection;
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
        // Pre-install a JS Set of the host's real method names so the
        // per-call Proxy in WrapWithHostProxy can decide "pass through
        // to the C# host" (known method) vs "no-op return 0" (unknown
        // rAthena builtin we haven't ported). Without this distinction
        // the proxy would either:
        //   (a) propagate every TypeError from missing builtins and
        //       collapse the whole script — fail-closed, what we had
        //       before the proxy.
        //   (b) silently swallow every call (including real ones) and
        //       all bonuses become no-ops — fail-open-too-far, what
        //       caught us earlier.
        // With the known-methods Set the proxy fires the wrapper only
        // for unknown names; known names route via t[p] which ClearScript
        // binds correctly.
        var names = typeof(ScriptedBonusHost)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => !m.IsSpecialName && m.DeclaringType == typeof(ScriptedBonusHost))
            .Select(m => m.Name)
            .Distinct(StringComparer.Ordinal);
        var literal = "[" + string.Join(",", names.Select(n => $"\"{n}\"")) + "]";
        _engine.Execute($"globalThis.__hostMethods = new Set({literal});");
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
        // host per call is safe. We expose the C# object as
        // `__rawHost` and wrap it in a JS Proxy named `h` (the name
        // the translator emits) so that unknown rAthena builtins like
        // <c>getenchantgrade()</c> or <c>setarray()</c> resolve to a
        // no-op returning 0 instead of throwing TypeError. Missing
        // builtins should fail open (skip the line, keep the bonuses
        // already applied), not fail closed (kill the whole combo).
        var host = new ScriptedBonusHost(pc, bundle, equipped, _catalog, _bonusSvc);
        try
        {
            _engine.AddHostObject("__rawHost", host);
            _engine.Execute(WrapWithHostProxy(js));
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

    /// <summary>
    /// Wrap the translated body in an IIFE that exposes <c>h</c> as a
    /// Proxy over the real host. Every property access on <c>h</c>
    /// returns a wrapper function that attempts the underlying host
    /// call and falls back to <c>0</c> on any error — TypeError from
    /// a missing rAthena builtin (e.g. <c>getenchantgrade</c>,
    /// <c>setarray</c>), ArgumentException from a mismatched arity,
    /// etc. Missing builtins should fail open (skip the line, keep
    /// the bonuses already applied), not fail closed.
    ///
    /// <para>
    /// We can't sniff <c>typeof __rawHost[p] === 'function'</c> to
    /// decide whether to bind — ClearScript wraps C# methods as host
    /// invocables that <c>typeof</c> reports as <c>"object"</c>, not
    /// <c>"function"</c>. So we go the other way: always return a
    /// closure that does <c>__rawHost[p](...args)</c> inside a
    /// try/catch. The translator only ever emits <c>h.&lt;name&gt;(...)</c>
    /// — never bare property reads — so the "always a function" shape
    /// is semantically safe.
    /// </para>
    ///
    /// <para>
    /// IIFE is mandatory: the translated body declares <c>let</c> at
    /// top-level, and the per-call <c>const h</c> needs to be scoped
    /// alongside it. <c>var</c> in plain <c>Execute</c> would leak
    /// into V8 global scope and collide across Apply() calls.
    /// </para>
    /// </summary>
    private static string WrapWithHostProxy(string body) => $$"""
(function() {
  const h = new Proxy(__rawHost, {
    get: function(t, p) {
      // Known host method → pass the ClearScript-wrapped function
      // through so binding + argument marshalling stay correct.
      // Unknown name (rAthena builtin we don't model) → return a
      // no-op function returning 0 so the script keeps going.
      if (__hostMethods.has(p)) return t[p];
      return function() { return 0; };
    }
  });
{{body}}
})();
""";

    public void Dispose() => _engine.Dispose();
}
