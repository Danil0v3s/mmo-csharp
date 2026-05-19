using System.Collections.Concurrent;
using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Status;

/// <summary>
/// Real <see cref="IPlayerBonusService"/>. Tracks bonus scripts and
/// autobonuses in-memory with their expiry and trigger metadata. The
/// stat-modifier interpretation (e.g. "bonus bStr, 10;") needs the
/// script-engine port to fully resolve; this service:
///
/// <list type="bullet">
///   <item>Stores the bonus row with its rAthena fields intact.</item>
///   <item>Ticks expiry — <see cref="Tick"/> sweeps expired entries.</item>
///   <item>On <see cref="ExecuteAutobonus"/>, runs each matching slot's
///         rate roll and logs a fire event (the actual auto-cast
///         lands when the script-engine wires up — for now combat just
///         observes the call site).</item>
/// </list>
///
/// Scripts are kept as opaque strings; future interpreter pulls them
/// out via <see cref="GetActiveBonusScripts"/>.
/// </summary>
public sealed class PlayerBonusService : IPlayerBonusService
{
    private readonly ILogger<PlayerBonusService> _logger;

    private sealed class BonusScript
    {
        public required string Script;
        public required long ExpiresAt;
        public required ushort IconType;
        public required bool Persistent;
    }

    private sealed class Autobonus
    {
        public required AutobonusTrigger Trigger;
        public required string Script;
        public required int RatePermille; // out of 10 000
        public required long ExpiresAt;
        public required ushort Flag;
    }

    private readonly ConcurrentDictionary<int, List<BonusScript>> _bonusScripts = new();
    private readonly ConcurrentDictionary<int, List<Autobonus>> _autobonuses = new();

    public PlayerBonusService(ILogger<PlayerBonusService> logger) => _logger = logger;

    public bool AddBonusScript(PlayerEntity pc, string script, int durationMs, ushort iconType, bool persistent)
    {
        var list = _bonusScripts.GetOrAdd(pc.CharacterId, _ => new());
        lock (list)
        {
            list.Add(new BonusScript
            {
                Script = script,
                ExpiresAt = durationMs > 0 ? Environment.TickCount64 + durationMs : long.MaxValue,
                IconType = iconType,
                Persistent = persistent,
            });
        }
        _logger.LogDebug(
            "pc_bonus_script: char {Char} duration {Dur}ms icon {Icon} persistent={Persist}",
            pc.CharacterId, durationMs, iconType, persistent);
        return true;
    }

    public void ClearBonusScripts(PlayerEntity pc, int flag)
    {
        if (!_bonusScripts.TryGetValue(pc.CharacterId, out var list)) return;
        lock (list)
        {
            // rAthena BSF_REM_ALL = 1, BSF_REM_BUFF = 2, etc. We don't
            // track per-script flags yet — default behavior clears
            // everything regardless of <paramref name="flag"/>.
            list.Clear();
        }
    }

    public bool AddAutobonus(PlayerEntity pc, AutobonusTrigger trigger, string script, int rate, int durationMs, ushort flag)
    {
        var list = _autobonuses.GetOrAdd(pc.CharacterId, _ => new());
        lock (list)
        {
            list.Add(new Autobonus
            {
                Trigger = trigger,
                Script = script,
                RatePermille = Math.Clamp(rate, 0, 10_000),
                ExpiresAt = durationMs > 0 ? Environment.TickCount64 + durationMs : long.MaxValue,
                Flag = flag,
            });
        }
        return true;
    }

    public void DelAutobonus(PlayerEntity pc, AutobonusTrigger trigger, bool restore)
    {
        if (!_autobonuses.TryGetValue(pc.CharacterId, out var list)) return;
        lock (list)
        {
            list.RemoveAll(b => b.Trigger == trigger);
        }
    }

    public void ExecuteAutobonus(PlayerEntity pc, AutobonusTrigger trigger)
    {
        if (!_autobonuses.TryGetValue(pc.CharacterId, out var list)) return;
        var now = Environment.TickCount64;
        lock (list)
        {
            for (var i = list.Count - 1; i >= 0; i--)
            {
                var b = list[i];
                if (b.ExpiresAt < now)
                {
                    list.RemoveAt(i);
                    continue;
                }
                if (b.Trigger != trigger) continue;
                if (Random.Shared.Next(10_000) >= b.RatePermille) continue;
                _logger.LogDebug(
                    "pc_exeautobonus: char {Char} trigger {Trigger} fired (script: {Script})",
                    pc.CharacterId, trigger, b.Script);
                // The actual script execution would call into the
                // script engine here; until that ports, the fire
                // event is observable via the log + telemetry.
            }
        }
    }

    /// <summary>
    /// Sweep expired bonus_script + autobonus entries across every
    /// tracked PC. Call from the autosave loop.
    /// </summary>
    public void Tick()
    {
        var now = Environment.TickCount64;
        foreach (var (_, list) in _bonusScripts)
        {
            lock (list) list.RemoveAll(b => b.ExpiresAt < now);
        }
        foreach (var (_, list) in _autobonuses)
        {
            lock (list) list.RemoveAll(b => b.ExpiresAt < now);
        }
    }

    /// <summary>
    /// Snapshot the active bonus script bodies for a PC. Used by the
    /// (forthcoming) bonus-script interpreter to apply stat mods.
    /// </summary>
    public IReadOnlyList<string> GetActiveBonusScripts(PlayerEntity pc)
    {
        if (!_bonusScripts.TryGetValue(pc.CharacterId, out var list)) return Array.Empty<string>();
        lock (list) return list.Select(b => b.Script).ToList();
    }
}
