using Microsoft.ClearScript;

namespace Map.Server.Persistence;

/// <summary>
/// One variable scope (perm / account / accountGlobal) backed by a
/// <see cref="PropertyBag"/> for JS write/read access. The bag holds
/// values as their natural JS types (number or string); at save time
/// the persistence service dispatches each entry to the right DB table
/// based on value type — int-like → *_reg_num, string → *_reg_str.
///
/// We keep a copy of the *originally loaded* snapshot so the saver can
/// diff and only upsert rows that actually changed. New keys, changed
/// values, and (for completeness) removed keys are all visible.
/// </summary>
public sealed class PlayerVarScope
{
    /// <summary>JS-facing bag. Authors write <c>ctx.player.perm.foo = 1</c>.</summary>
    public PropertyBag Bag { get; }

    /// <summary>Frozen snapshot of what was loaded from the DB. Used by the diff at save time.</summary>
    internal IReadOnlyDictionary<string, object> Loaded { get; }

    public PlayerVarScope(IReadOnlyDictionary<string, object> loaded)
    {
        Loaded = loaded;
        Bag = new PropertyBag();
        foreach (var (k, v) in loaded) Bag[k] = v;
    }
}
