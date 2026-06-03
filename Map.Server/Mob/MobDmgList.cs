using Map.Server.Entities;

namespace Map.Server.Mob;

/// <summary>
/// rAthena <c>md-&gt;dmglog</c> — ring buffer of recent attackers and
/// the cumulative damage each one dealt. Used by:
/// <list type="bullet">
///   <item>EXP distribution at mob death (mob_dead damage-share split).</item>
///   <item><c>unit_counttargeted</c> — distinct attacker count, feeds
///         MSC_ATTACKPCGT / MSC_ATTACKPCGE.</item>
///   <item>Last-hit attribution for KS protection + loot ownership.</item>
/// </list>
///
/// <para>Size in rAthena is <c>DAMAGELOG_SIZE = 30</c>. The buffer
/// overwrites the oldest slot when full. Newer hits from an existing
/// attacker accumulate onto their slot instead of allocating a new one.</para>
/// </summary>
public sealed class MobDmgList
{
    /// <summary>rAthena <c>DAMAGELOG_SIZE</c>.</summary>
    public const int Capacity = 30;

    private readonly List<DmgEntry> _entries = new(Capacity);

    /// <summary>Read-only snapshot of current entries (in order of first hit).</summary>
    public IReadOnlyList<DmgEntry> Entries => _entries;

    /// <summary>
    /// Record a hit. If <paramref name="attackerId"/> already has a
    /// slot, the damage accumulates; otherwise a new slot is taken
    /// (evicting the oldest if at capacity).
    /// </summary>
    public void Record(EntityId attackerId, long damage)
    {
        if (damage <= 0) return;
        for (var i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].AttackerId == attackerId)
            {
                _entries[i] = _entries[i] with { Damage = _entries[i].Damage + damage };
                return;
            }
        }
        if (_entries.Count >= Capacity)
        {
            _entries.RemoveAt(0);  // evict oldest (rAthena overwrites slot 0)
        }
        _entries.Add(new DmgEntry(attackerId, damage));
    }

    /// <summary>Distinct attacker count (rAthena <c>unit_counttargeted</c> proxy).</summary>
    public int DistinctAttackerCount => _entries.Count;

    /// <summary>
    /// Lookup an attacker's accumulated damage. Returns 0 if not present.
    /// </summary>
    public long DamageFrom(EntityId attackerId)
    {
        foreach (var e in _entries)
            if (e.AttackerId == attackerId) return e.Damage;
        return 0;
    }

    /// <summary>Drop all entries (mob respawn / unit_remove_map).</summary>
    public void Clear() => _entries.Clear();

    /// <summary>
    /// FEATURE-01 — a copy of the current entries, taken at mob death before
    /// <c>KillMob</c> clears the log, so the death-observer's quest/achievement/MVP
    /// fan-out can iterate contributors after the live list is gone.
    /// </summary>
    public DmgEntry[] Snapshot() => _entries.ToArray();

    /// <summary>
    /// FEATURE-01 — the attacker that dealt the most cumulative damage (rAthena
    /// <c>mvp_sd</c> — the MVP-reward recipient). Null when the log is empty.
    /// </summary>
    public EntityId? TopDamageAttacker()
    {
        if (_entries.Count == 0) return null;
        var best = _entries[0];
        for (var i = 1; i < _entries.Count; i++)
            if (_entries[i].Damage > best.Damage) best = _entries[i];
        return best.AttackerId;
    }

    public readonly record struct DmgEntry(EntityId AttackerId, long Damage);
}
