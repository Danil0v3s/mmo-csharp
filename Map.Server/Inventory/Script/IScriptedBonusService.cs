using Map.Server.Entities;
using Map.Server.Items;

namespace Map.Server.Inventory.Script;

/// <summary>
/// rAthena item-script DSL → V8 execution pipeline. Handles the
/// dynamic scripts (~3,275 in stock item_combos.yml: conditionals,
/// autobonus, autospell) that <see cref="BonusScriptExtractor"/>'s
/// regex pass can't decode. The static ~30k bonus/bonus2 statements
/// stay on the regex fast path.
///
/// <para>
/// Pipeline per Apply call:
/// </para>
/// <list type="number">
///   <item><see cref="RathenaScriptParser"/> tokenizes + builds an AST.</item>
///   <item><see cref="RathenaToJsTranslator"/> emits a JS function body.</item>
///   <item>A dedicated <c>ClearScript V8</c> engine evaluates that body
///         with a fresh <see cref="ScriptedBonusHost"/> instance bound
///         as <c>h</c>. The host mutates the supplied
///         <see cref="EquipBonusBundle"/> in-place; on the autobonus
///         path it registers entries with <see cref="IPlayerBonusService"/>.</item>
/// </list>
///
/// <para>
/// Translation results are cached by script-body hash — the same combo
/// script text translates to the same JS regardless of which PC equips
/// it, so the per-execution work is just the host swap + V8 Execute.
/// </para>
///
/// <para>
/// Errors at any stage (parse / translate / execute) are caught and
/// logged at debug level; a broken script doesn't take down the
/// equip recalc.
/// </para>
/// </summary>
public interface IScriptedBonusService
{
    /// <summary>
    /// Apply <paramref name="script"/> to <paramref name="bundle"/> on
    /// behalf of <paramref name="pc"/>. Returns true if the script
    /// executed cleanly; false on parse/translate/execute error.
    /// </summary>
    bool Apply(string script, PlayerEntity pc, EquipBonusBundle bundle,
        IReadOnlyList<InventoryItem>? equipped = null);

    /// <summary>
    /// Quick gate used by callers (EquipBonusAggregator) to know if a
    /// script needs the V8 path or can go through the regex extractor
    /// alone. Returns true when the script contains dynamic features
    /// (if/.@var/autobonus/etc.).
    /// </summary>
    static bool NeedsDynamicEval(string? script)
    {
        if (string.IsNullOrEmpty(script)) return false;
        // Cheap substring check — the regex extractor handles plain
        // bonus/bonus2 statements alone; anything with these markers
        // needs the parser. False positives are fine (we'd just run
        // the V8 path on a static script and get the same outcome).
        return script.Contains(".@", StringComparison.Ordinal)
            || script.Contains("if ", StringComparison.Ordinal)
            || script.Contains("if(", StringComparison.Ordinal)
            || script.Contains("autobonus", StringComparison.Ordinal)
            || script.Contains("bAutoSpell", StringComparison.Ordinal);
    }

    /// <summary>Diagnostics — translation-cache hit / miss counts.</summary>
    (int Hits, int Misses, int CachedScripts) CacheStats { get; }
}
