using System.Collections.Concurrent;
using System.Collections.Generic;
using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Guild;

/// <summary>
/// Default <see cref="IGuildExpService"/>. Per-PC accumulator keyed
/// by char_id; flushed to the cached <see cref="GuildEntity.Members"/>[i].Exp
/// every minute (caller drives via <see cref="FlushAll"/> on the
/// game-loop's once-per-minute tick).
///
/// The cap is rAthena's <c>MAX_GUILD_EXP = INT32_MAX</c>
/// (config/const.hpp:71); we hold the same constant here so the
/// accumulator can't grow past the on-wire cap.
/// </summary>
public sealed class GuildExpService : IGuildExpService
{
    /// <summary>rAthena <c>MAX_GUILD_EXP</c> (config/const.hpp:71).</summary>
    public const long MaxGuildExp = int.MaxValue;

    private readonly ILogger<GuildExpService> _logger;
    private readonly IGuildService _guilds;

    private readonly ConcurrentDictionary<int, ExpCacheEntry> _cache = new();

    public GuildExpService(ILogger<GuildExpService> logger, IGuildService guilds)
    {
        _logger = logger;
        _guilds = guilds;
    }

    public long PayExp(PlayerEntity pc, long exp)
    {
        if (pc == null || exp <= 0) return 0;
        if (pc.GuildId <= 0) return 0;
        var g = _guilds.Find(pc.GuildId);
        if (g == null) return 0;
        var pos = g.GetPosition(pc.AccountId, pc.CharacterId);
        if (pos < 0 || pos >= g.Positions.Count) return 0;
        var taxRate = g.Positions[pos].ExpMode;
        if (taxRate < 1) return 0; // no tax for this rank

        long taxed = taxRate < 100 ? (exp * taxRate / 100) : exp;
        if (taxed <= 0) return 0;
        Accumulate(pc, taxed);
        return taxed;
    }

    public long GetExp(PlayerEntity pc, long exp)
    {
        if (pc == null || exp <= 0) return 0;
        if (pc.GuildId <= 0) return 0;
        var g = _guilds.Find(pc.GuildId);
        if (g == null) return 0;
        Accumulate(pc, exp);
        return exp;
    }

    public long FlushOne(int charId)
    {
        if (!_cache.TryRemove(charId, out var entry)) return 0;
        if (entry.Exp <= 0) return 0;
        var g = _guilds.Find(entry.GuildId);
        if (g == null) return 0;
        var idx = g.GetIndex(entry.AccountId, charId);
        if (idx < 0) return 0;
        var current = g.Members[idx].Exp;
        var added = entry.Exp;
        var sum = current + added;
        if (sum > MaxGuildExp || sum < current) sum = MaxGuildExp; // overflow-safe
        g.Members[idx].Exp = sum;
        // IPC dispatch (intif_guild_change_memberinfo GMI_EXP) is
        // owned by the typed wrapper — wires when the packet handler
        // for guild_skillup / shared-EXP rollover ports. Until then,
        // the cached entity is the source of truth that the next
        // RecvInfo replicates back.
        return added;
    }

    public int FlushAll()
    {
        int flushed = 0;
        // Snapshot the keys to avoid concurrent-modify; FlushOne
        // removes each entry as it processes.
        var keys = new List<int>(_cache.Keys);
        foreach (var charId in keys)
            if (FlushOne(charId) > 0) flushed++;
        if (flushed > 0)
            _logger.LogDebug("GuildExp: flushed {Count} accumulator entries", flushed);
        return flushed;
    }

    public long Peek(int charId)
        => _cache.TryGetValue(charId, out var e) ? e.Exp : 0;

    public System.Collections.Generic.IReadOnlyDictionary<int, (int GuildId, int AccountId, long Exp)> Snapshot()
    {
        var dict = new Dictionary<int, (int, int, long)>();
        foreach (var kv in _cache)
            dict[kv.Key] = (kv.Value.GuildId, kv.Value.AccountId, kv.Value.Exp);
        return dict;
    }

    private void Accumulate(PlayerEntity pc, long amount)
    {
        _cache.AddOrUpdate(
            pc.CharacterId,
            _ => new ExpCacheEntry(pc.GuildId, pc.AccountId, ClampAdd(0, amount)),
            (_, existing) =>
            {
                // If the PC switched guilds since the last accumulate,
                // discard the stale tally — flushing it to the new
                // guild would be a parity bug.
                if (existing.GuildId != pc.GuildId)
                    return new ExpCacheEntry(pc.GuildId, pc.AccountId, ClampAdd(0, amount));
                return existing with { Exp = ClampAdd(existing.Exp, amount) };
            });
    }

    private static long ClampAdd(long a, long b)
    {
        var sum = a + b;
        if (sum > MaxGuildExp || sum < a) return MaxGuildExp;
        return sum;
    }

    private sealed record ExpCacheEntry(int GuildId, int AccountId, long Exp);
}
