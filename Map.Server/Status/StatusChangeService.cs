using Map.Server.Combat;
using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Status;

/// <summary>
/// Concrete <see cref="IStatusChangeService"/>. Stores active SCs in a
/// per-entity dictionary; ticks them all from one game-loop pump.
///
/// Refresh-on-restart matches rAthena's default <c>SCSTART_NOAVOID</c>
/// behavior: re-applying the same SC removes the old one (running the
/// stat-mod revert) and applies a fresh instance. Custom stack rules
/// would slot in via <see cref="StatusEffectHandler"/> if/when needed.
/// </summary>
public sealed class StatusChangeService : IStatusChangeService
{
    private readonly IDamageService _damage;
    private readonly IEntityRegistry _entities;
    private readonly StatusEffectRegistry _effects;
    private readonly IStatusDbFlagCache? _flagCache;
    private readonly Map.Server.Handlers.ClifWire.IClifWireService? _clif;
    private readonly Map.Server.World.IMapFlagService? _mapFlags;
    private readonly Map.Server.World.IMapWorldRegistry? _world;
    private readonly Random _rng;
    private readonly ILogger<StatusChangeService> _logger;

    /// <summary>
    /// All active SCs by attached entity. <see cref="StatusType"/> is the
    /// inner dict key so per-type lookup / refresh is O(1).
    /// </summary>
    private readonly Dictionary<EntityId, Dictionary<StatusType, StatusChange>> _active = new();

    public StatusChangeService(
        IDamageService damage,
        IEntityRegistry entities,
        StatusEffectRegistry effects,
        ILogger<StatusChangeService> logger,
        IStatusDbFlagCache? flagCache = null,
        Map.Server.Handlers.ClifWire.IClifWireService? clif = null,
        Map.Server.World.IMapFlagService? mapFlags = null,
        Map.Server.World.IMapWorldRegistry? world = null,
        Random? rng = null)
    {
        _damage = damage;
        _entities = entities;
        _effects = effects;
        _flagCache = flagCache;
        _clif = clif;
        _mapFlags = mapFlags;
        _world = world;
        _rng = rng ?? Random.Shared;
        _logger = logger;
    }

    // SKILL-01 — the legacy no-rate entry point is now a thin wrapper that
    // applies guaranteed (no stat resist, no duration reduction, no roll), so
    // every existing self-buff / scripted apply is unaffected. New proc call
    // sites pass the raw skill chance through the rate-aware overload below.
    public StatusChange? Start(
        Entity target,
        StatusType type,
        int val1, int val2, int val3, int val4,
        int durationMs,
        Entity? source = null,
        long nowTick = long.MinValue)
        => Start(target, type, rate: 10000, val1, val2, val3, val4, durationMs, source,
                 flag: ScStartFlag.NoRateDef | ScStartFlag.NoTickDef | ScStartFlag.NoAvoid, nowTick);

    public StatusChange? Start(
        Entity target,
        StatusType type,
        int rate,
        int val1, int val2, int val3, int val4,
        int durationMs,
        Entity? source = null,
        ScStartFlag flag = ScStartFlag.None,
        long nowTick = long.MinValue)
    {
        var handler = _effects.Get(type);
        if (handler == null)
        {
            _logger.LogDebug("StatusChange.Start: no handler registered for {Type}", type);
            return null;
        }

        // SKILL-01 — rAthena status_change_start resist pipeline. Run the raw
        // chance through status_get_sc_def (stat resist + level-diff + boss/MVP
        // immunity), then roll. rate >= 10000 with the guaranteed flags (the
        // wrapper above) skips the roll entirely. Done before the refresh
        // End() so a resisted proc never disturbs an existing instance.
        if (!(flag.HasFlag(ScStartFlag.Loaded)))
        {
            var (resistedRate, reducedDuration) = GetScDef(source, target, type, rate, durationMs, flag);
            if (resistedRate <= 0)
            {
                _logger.LogDebug("StatusChange.Start: {Type} on {Target} fully resisted", type, target.Id);
                return null;
            }
            if (resistedRate < 10000 && _rng.Next(10000) >= resistedRate)
            {
                _logger.LogDebug("StatusChange.Start: {Type} on {Target} rolled out (rate {Rate})", type, target.Id, resistedRate);
                return null;
            }
            durationMs = reducedDuration;
        }

        // P0.5 — rAthena status_change_isDisabledOnMap gate. The map's
        // `nostatus` flag refuses every non-permanent SC. Run before
        // any Val storage / lifecycle hook so the SC simply doesn't
        // attach. Permanent SCs (god-items / BasilicaCell) bypass.
        if (IsDisabledOnMap(target.MapId, type))
        {
            _logger.LogDebug("StatusChange.Start: {Type} refused on map {MapId} (nostatus flag)", type, target.MapId);
            return null;
        }

        // DBR-1e: rAthena Fail/EndReturn/EndOnStart matrix from status.yml
        // (status_db_flag). Fail wins first — if any of the SCs listed in
        // <type>'s Fail set are currently active on the target, the
        // start is rejected (parity with status_change_start's fail check).
        if (_flagCache != null && _active.TryGetValue(target.Id, out var existingActive))
        {
            foreach (var failType in _flagCache.GetFailScs(type))
            {
                if (existingActive.ContainsKey(failType))
                {
                    _logger.LogDebug("StatusChange.Start: {Type} on {Target} blocked by active {Fail}",
                        type, target.Id, failType);
                    return null;
                }
            }
            // EndReturn: end the listed SCs and abort the start when ANY matched.
            // rAthena yml semantic: "end these SCs when this one activates
            // and won't give its effect." Used by Stone/StoneWait self-suppression.
            var aborted = false;
            foreach (var endType in _flagCache.GetEndReturn(type))
            {
                if (existingActive.ContainsKey(endType))
                {
                    End(target, endType);
                    aborted = true;
                }
            }
            if (aborted)
            {
                _logger.LogDebug("StatusChange.Start: {Type} on {Target} aborted by EndReturn",
                    type, target.Id);
                return null;
            }
            // EndOnStart: end the listed SCs but proceed with the start.
            foreach (var endType in _flagCache.GetEndOnStart(type))
            {
                if (existingActive.ContainsKey(endType)) End(target, endType);
            }
        }

        // Refresh-on-restart: end the previous instance so its OnEnd
        // reverts any stat mods before we re-apply.
        End(target, type);

        // Sentinel: int.MinValue (passed by callers that want a default).
        // Negative `nowTick` from a deterministic-time caller is allowed
        // and meaningful; only the explicit sentinel falls back to wall
        // clock so the test/game timebases don't bleed into each other.
        if (nowTick == long.MinValue) nowTick = Environment.TickCount64;
        var sc = new StatusChange
        {
            Type = type,
            Val1 = val1, Val2 = val2, Val3 = val3, Val4 = val4,
            ExpiresAt = durationMs > 0 ? nowTick + durationMs : -1,
            PeriodMs = handler.PeriodMs,
            NextTick = handler.PeriodMs > 0 ? nowTick + handler.PeriodMs : 0,
        };

        if (!_active.TryGetValue(target.Id, out var perEntity))
        {
            perEntity = new Dictionary<StatusType, StatusChange>();
            _active[target.Id] = perEntity;
        }
        perEntity[type] = sc;

        handler.OnStart(target, sc, source);

        // T5.3b — clif_status_change to AOI. The client uses this to
        // turn on the buff/debuff icon over the target. Per rAthena
        // (status.cpp:status_change_start) the broadcast fires AFTER
        // the handler has populated its visible state.
        _clif?.StatusChange(target, type, active: true, durationMs,
            val1, val2, val3);

        return sc;
    }

    // SKILL-01 — renewal status_get_sc_def (status.cpp:9350). Returns the
    // resisted landing rate (1/100-%) + reduced duration; does NOT roll.
    public (int resistedRate, int reducedDurationMs) GetScDef(
        Entity? src, Entity target, StatusType type, int rate, int durationMs, ScStartFlag flag)
    {
        int tick = durationMs;
        // rAthena: src == null short-circuits (no resist).
        if (src == null) return (rate, tick);

        var entry = ScDefTable.For(type);
        if (entry == null)
            return (rate, tick); // buff / non-stat-resistible: caller still rolls `rate`.

        var e = entry.Value;
        var status = target.Stats;

        // Curse special: 100% immune when target LUK == 0.
        if (e.CurseLukImmune && status.Luk == 0) return (0, tick);

        // Boss / MVP resist (status_change_start:9897). MD_STATUSIMMUNE / MD_MVP
        // mobs are immune to the flagged CC SCs unless NoAvoid is set.
        if ((flag & ScStartFlag.NoAvoid) == 0 && target is MobEntity mob)
        {
            var mode = mob.Stats.Mode;
            if (e.BossResist && (mode & MobMode.StatusImmune) != 0) return (0, tick);
            if (e.MvpResist && (mode & MobMode.Mvp) != 0) return (0, tick);
        }

        // levelAdv = (max(0, lvSrc - lvTgt))^2 / 5 * 100  (renewal).
        int diff = src.Level - target.Level;
        int levelAdv = diff > 0 ? (diff * diff / 5) * 100 : 0;

        int scDef = ScDefTable.StatValue(status, e.Stat) * e.StatMul
                  + target.Level * e.LevelMul
                  + status.Luk * e.LukMul
                  - (e.UseLevelAdv ? levelAdv : 0);

        // battle_config pc/mob_sc_def_rate default 100 (no-op); cap at
        // pc/mob_max_sc_def*100 = 10000 (renewal cap_value 0..max).
        // (SC_SCRESIST / Siegfried resist-buff adds are SKILL-14.)
        scDef = Math.Clamp(scDef, 0, 10000);
        int tickDef = scDef; // tick_def == -1 → use sc_def.
        int tickDef2 = e.TickDef2Ms;

        // Natural rate resistance.
        if ((flag & ScStartFlag.NoRateDef) == 0)
        {
            rate -= rate * scDef / 10000;
            // Aegis accuracy rounding (round up to the next multiple of 10).
            if (rate > 0 && rate % 10 != 0) rate += 10 - rate % 10;
        }
        if (rate < 0) rate = 0; // (per-SC status.yml min_rate floor → SKILL-14)

        // Duration resistance (separate from rate).
        if ((flag & ScStartFlag.NoTickDef) == 0)
        {
            tick -= tick * tickDef / 10000;
            tick -= tickDef2; // negative tickDef2 lengthens — faithful to rAthena.
        }
        if (tick < 1) tick = 1; // status.yml min_duration floor (default).
        return (rate, tick);
    }

    public bool End(Entity target, StatusType type)
    {
        if (!_active.TryGetValue(target.Id, out var perEntity)) return false;
        if (!perEntity.Remove(type, out var sc)) return false;

        var handler = _effects.Get(type);
        handler?.OnEnd(target, sc);

        if (perEntity.Count == 0) _active.Remove(target.Id);

        // T5.3b — icon-off broadcast.
        _clif?.StatusChange(target, type, active: false);

        // DBR-1e: EndOnEnd matrix — when this SC ends, end every SC
        // listed in its EndOnEnd set too. Recursive cleanup: e.g. ending
        // a buff that maintains downstream SCs (rare in stock yml: 23 rows).
        if (_flagCache != null)
        {
            foreach (var cascadeType in _flagCache.GetEndOnEnd(type))
                End(target, cascadeType);
        }

        return true;
    }

    public StatusChange? Get(Entity target, StatusType type)
    {
        if (!_active.TryGetValue(target.Id, out var perEntity)) return null;
        return perEntity.GetValueOrDefault(type);
    }

    /// <summary>
    /// COMBAT-33 — re-apply every active SC's derived-stat contribution after a
    /// recalc rebuilt the derived fields. Iterates the entity's active SCs and
    /// invokes each handler's <see cref="StatusEffectHandler.OnRecalc"/> (null
    /// for presence-only SCs and bespoke handlers still on the COMBAT-53 list).
    /// OnRecalc only mutates <c>BattleStats</c>, but we snapshot the SC list
    /// first to stay safe if a handler ever ends an SC.
    /// </summary>
    public void ReapplyDerivedStatMods(Entity target)
    {
        if (!_active.TryGetValue(target.Id, out var perEntity) || perEntity.Count == 0) return;
        var snapshot = new List<StatusChange>(perEntity.Values);
        foreach (var sc in snapshot)
            _effects.Get(sc.Type)?.OnRecalc?.Invoke(target, sc);
    }

    // COMBAT-73 — the MaxHp/MaxSp re-fold pass (runs after CalcPc's MaxHp/MaxSp block).
    public void ReapplyMaxHpSpMods(Entity target)
    {
        if (!_active.TryGetValue(target.Id, out var perEntity) || perEntity.Count == 0) return;
        var snapshot = new List<StatusChange>(perEntity.Values);
        foreach (var sc in snapshot)
            _effects.Get(sc.Type)?.OnRecalcPool?.Invoke(target, sc);
    }

    public void Tick(long nowTick)
    {
        if (_active.Count == 0) return;

        // Snapshot keys: handler callbacks may end the SC (or another
        // SC on the same entity) while we iterate.
        foreach (var (entityId, perEntity) in _active.ToArray())
        {
            var entity = _entities.Get(entityId);
            if (entity == null)
            {
                _active.Remove(entityId);
                continue;
            }

            // Wave 83 — Shadow Scar timer prune. Runs alongside the SC
            // tick so the SC_SHADOW_SCAR Val1 count stays in sync with
            // the timer list (and the SC ends when the list empties).
            if (entity.ShadowScarTimers.Count > 0)
            {
                Map.Server.Movement.UnitOps.UnitOpsService
                    .PruneExpiredShadowScars(entity, nowTick, this);
            }

            foreach (var sc in perEntity.Values.ToArray())
            {
                // Periodic first — rAthena fires the tick effect before
                // checking expiry so a final DoT lands on the boundary.
                if (sc.PeriodMs > 0 && sc.NextTick != 0 && nowTick >= sc.NextTick)
                {
                    sc.NextTick = nowTick + sc.PeriodMs;
                    var handler = _effects.Get(sc.Type);
                    handler?.OnPeriodic?.Invoke(entity, sc, dmg => _damage.ApplyDamage(entity, dmg));
                }

                // Expiry. ExpiresAt = -1 means "until manually removed."
                if (sc.ExpiresAt > 0 && nowTick >= sc.ExpiresAt)
                {
                    End(entity, sc.Type);
                }
            }
        }
    }

    // ============= ST.1 — close-out methods =============

    /// <inheritdoc />
    public int ClearAll(Entity target, byte type = 0)
    {
        // rAthena status.cpp:11497. type=0 leaves Permanent SCs alone
        // (Wedding, Casket, equipment-set buffs); type=1 includes them
        // (PC disconnect / death cleanup).
        if (!_active.TryGetValue(target.Id, out var perEntity) || perEntity.Count == 0)
            return 0;
        var cleared = 0;
        foreach (var t in perEntity.Keys.ToArray())
        {
            var flags = _effects.GetEffectiveFlags(t);
            if (type == 0 && (flags & ScfFlag.Permanent) != 0)
                continue;
            if (End(target, t)) cleared++;
        }
        return cleared;
    }

    /// <inheritdoc />
    public int ClearBuffs(Entity target, SccbFlag flag)
    {
        // rAthena status.cpp:11616. The flag mask selects which SCs to
        // wipe: SCCB_BUFFS removes ScfFlag.Buff entries, SCCB_DEBUFFS
        // removes ScfFlag.Debuff, SCCB_REFRESH removes
        // ScfFlag.RemoveOnRefresh. Permanent flag wins (parity).
        if (flag == SccbFlag.None) return 0;
        if (!_active.TryGetValue(target.Id, out var perEntity) || perEntity.Count == 0)
            return 0;

        var cleared = 0;
        foreach (var t in perEntity.Keys.ToArray())
        {
            var flags = _effects.GetEffectiveFlags(t);
            if ((flags & ScfFlag.Permanent) != 0) continue;

            var match = false;
            if ((flag & SccbFlag.Buffs) != 0 && (flags & ScfFlag.Buff) != 0) match = true;
            if ((flag & SccbFlag.Debuffs) != 0 && (flags & ScfFlag.Debuff) != 0) match = true;
            if ((flag & SccbFlag.Refresh) != 0 && (flags & ScfFlag.RemoveOnRefresh) != 0) match = true;
            // ChemProtect / Luxanima / Hermode wire in as their consumer
            // skills land; for now they no-op so callers don't get
            // a surprise wipe.

            if (match && End(target, t)) cleared++;
        }
        return cleared;
    }

    /// <inheritdoc />
    public int ClearOnChangeMap(Entity target)
    {
        // rAthena status.cpp:11848. Drop SCs flagged RemoveOnChangeMap;
        // keep everything else (equipment buffs, WoE persistent SCs).
        if (!_active.TryGetValue(target.Id, out var perEntity) || perEntity.Count == 0)
            return 0;
        var cleared = 0;
        foreach (var t in perEntity.Keys.ToArray())
        {
            var flags = _effects.GetEffectiveFlags(t);
            if ((flags & ScfFlag.RemoveOnChangeMap) != 0)
            {
                if (End(target, t)) cleared++;
            }
        }
        return cleared;
    }

    /// <inheritdoc />
    public int ClearOnLogout(Entity target)
    {
        // rAthena: most SCs drop on logout; WoE-pinning + Permanent
        // SCs persist (re-applied on next login from char-side scdata
        // table once the persistence pipe lands).
        if (!_active.TryGetValue(target.Id, out var perEntity) || perEntity.Count == 0)
            return 0;
        var cleared = 0;
        foreach (var t in perEntity.Keys.ToArray())
        {
            var flags = _effects.GetEffectiveFlags(t);
            // Drop unless Permanent. Buffs / Debuffs / explicit
            // RemoveOnLogout all qualify (rAthena default).
            if ((flags & ScfFlag.Permanent) != 0) continue;
            if ((flags & (ScfFlag.RemoveOnLogout | ScfFlag.Buff | ScfFlag.Debuff)) != 0)
            {
                if (End(target, t)) cleared++;
            }
        }
        return cleared;
    }

    /// <inheritdoc />
    public int Spread(Entity source, Entity target)
    {
        // rAthena status.cpp:12188. For each SCF_SPREADEFFECT SC on
        // source, restart the SC on target with the same val1..val4 +
        // remaining duration. Source / target must both be alive.
        if (!_active.TryGetValue(source.Id, out var perEntity) || perEntity.Count == 0)
            return 0;
        var nowTick = Environment.TickCount64;
        var propagated = 0;
        foreach (var sc in perEntity.Values.ToArray())
        {
            var flags = _effects.GetEffectiveFlags(sc.Type);
            if ((flags & ScfFlag.SpreadEffect) == 0) continue;
            var remaining = sc.ExpiresAt > 0 ? (int)Math.Max(1, sc.ExpiresAt - nowTick) : 30_000;
            if (Start(target, sc.Type, sc.Val1, sc.Val2, sc.Val3, sc.Val4, remaining, source, nowTick) != null)
                propagated++;
        }
        return propagated;
    }

    /// <inheritdoc />
    public int GetMaxStacks(StatusType type)
    {
        var handler = _effects.Get(type);
        return handler?.MaxStacks ?? 1;
    }

    /// <inheritdoc />
    public bool IsDisabledOnMap(uint mapId, StatusType type)
    {
        // P0.5 — rAthena status_change_isDisabledOnMap. The map carries
        // a `nostatus` flag set; if active, every non-permanent SC is
        // refused. SCs flagged ScfFlag.Permanent bypass the gate
        // (WoE-only / god-item buffs that survive everything).
        if (_mapFlags == null || _world == null) return false;
        var flags = _effects.Get(type)?.Flags ?? ScfFlag.None;
        if ((flags & ScfFlag.Permanent) != 0) return false;
        // Hashed-name → MapData same as MovementService.
        foreach (var map in _world.All)
        {
            if ((uint)map.Name.GetHashCode() != mapId) continue;
            return _mapFlags.IsSet(map.Name, Map.Server.World.MapFlag.NoStatus);
        }
        return false;
    }

    // Weapon-element SC set — used by ST.7 Refresh to know which SCs
    // to drop + reapply on weapon swap. Matches rAthena
    // pc_calcweapontype's SC reapply list (status.cpp:status_change_refresh).
    private static readonly StatusType[] _weaponElementScs = new[]
    {
        StatusType.Fireweapon,
        StatusType.Earthweapon,
        StatusType.Windweapon,
        StatusType.Waterweapon,
    };

    /// <inheritdoc />
    public int Refresh(Entity target)
    {
        // rAthena status_change_refresh re-applies a fixed set of SCs:
        // weapon-element family + ASPD potion + Berserk + Concentration
        // are the common ones. Map-side we cover the weapon-element set;
        // other SCs that need refresh on equip change land as their
        // consumer code paths port (e.g. SC_ENDURE on shield equip).
        if (!_active.TryGetValue(target.Id, out var perEntity) || perEntity.Count == 0)
            return 0;

        var refreshed = 0;
        var nowTick = Environment.TickCount64;
        foreach (var type in _weaponElementScs)
        {
            if (!perEntity.TryGetValue(type, out var sc)) continue;
            var remaining = sc.ExpiresAt > 0 ? (int)Math.Max(1, sc.ExpiresAt - nowTick) : -1;
            var val1 = sc.Val1; var val2 = sc.Val2;
            var val3 = sc.Val3; var val4 = sc.Val4;
            // End drops the OnEnd stat mod + cleans the dictionary.
            End(target, type);
            // Re-Start re-applies OnStart with current entity state
            // (e.g. picks up the new weapon's element). 0 duration means
            // "use the original SC duration" — we pass `remaining`
            // explicitly so the SC keeps its tail.
            Start(target, type, val1, val2, val3, val4,
                durationMs: remaining > 0 ? remaining : 60_000,
                source: null, nowTick: nowTick);
            refreshed++;
        }
        return refreshed;
    }
}
