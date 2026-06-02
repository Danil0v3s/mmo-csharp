using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Status;

public interface INaturalHealService
{
    /// <summary>Game-loop pump — applies HP/SP regen ticks to all eligible entities.</summary>
    void Tick(long nowTick);
}

/// <summary>
/// First-slice port of rAthena <c>status_natural_heal</c>
/// (status.cpp:15015). Drives baseline HP/SP regen for players and
/// mobs. Walking gates HP regen for PCs; sitting doubles the rate;
/// dead entities and full pools skip. Status-effect-driven HoT (e.g.
/// SC_HEALING) flows through <see cref="IStatusChangeService"/> instead.
///
/// Formulas follow rAthena renewal documentation:
///   HP/tick = 1 + maxhp/200 + vit/5
///   SP/tick = 1 + maxsp/100 + int/6
/// Default intervals are rAthena's <c>natural_healhp_interval</c>
/// (6 s) and <c>natural_healsp_interval</c> (8 s).
///
/// <para>T2.4b+ SC overlays:</para>
/// <list type="bullet">
///   <item><c>SC_BLEEDING</c> — blocks HP regen (rAthena
///     status_check_natural_heal).</item>
///   <item><c>SC_MAGNIFICAT</c> — doubles SP regen.</item>
/// </list>
/// </summary>
public sealed class NaturalHealService : INaturalHealService
{
    private const int HpIntervalMs = 6_000;
    private const int SpIntervalMs = 8_000;

    private readonly IEntityRegistry _entities;
    private readonly ILogger<NaturalHealService> _logger;
    // Optional SC overlay so existing constructors keep working; DI
    // resolves the real IStatusChangeService automatically.
    private readonly IStatusChangeService? _sc;

    // Per-entity tick anchors for HP and SP. Initialized on first observation.
    private readonly Dictionary<EntityId, RegenState> _state = new();

    public NaturalHealService(
        IEntityRegistry entities,
        ILogger<NaturalHealService> logger,
        IStatusChangeService? sc = null)
    {
        _entities = entities;
        _logger = logger;
        _sc = sc;
    }

    public void Tick(long nowTick)
    {
        foreach (var entity in _entities.All())
        {
            if (entity is not (PlayerEntity or MobEntity)) continue;
            if (!IsAlive(entity)) { _state.Remove(entity.Id); continue; }

            if (!_state.TryGetValue(entity.Id, out var rs))
            {
                rs = new RegenState { NextHpTick = nowTick + HpIntervalMs, NextSpTick = nowTick + SpIntervalMs };
                _state[entity.Id] = rs;
            }

            // Sitting bonus only applies to PCs; mobs use the base cadence.
            var sitting = entity is PlayerEntity pcSit && pcSit.IsSitting;
            var walking = entity.Walk != null;
            var s = entity.Stats;

            if (nowTick >= rs.NextHpTick && s.Hp < s.MaxHp)
            {
                if (!(entity is PlayerEntity && walking))
                {
                    // SC_BLEEDING blocks HP regen entirely (rAthena
                    // status_check_natural_heal). Walk-not-allowed +
                    // standing still still yields zero regen.
                    var bleeding = _sc?.Get(entity, StatusType.Bleeding) != null;
                    // P0.3 — SC_SATURDAYNIGHTFEVER also suppresses HP regen
                    // (rAthena WL_SATURDAY_NIGHT_FEVER: all heals blocked).
                    var snf = _sc?.Get(entity, StatusType.Saturdaynightfever) != null;
                    if (!bleeding && !snf)
                    {
                        var amount = 1 + s.MaxHp / 200 + s.Vit / 5;
                        if (sitting) amount *= 2;
                        // P0.3 — SC_INSPIRATION (Royal Guard) accelerates HP regen
                        // by val1 % (status.cpp:11366 area). Read Val1 directly.
                        var inspiration = _sc?.Get(entity, StatusType.Inspiration);
                        if (inspiration != null && inspiration.Val1 > 0)
                        {
                            amount = amount * (100 + inspiration.Val1) / 100;
                        }
                        // COMBAT-23 — bonus bHPrecovRate (SP_HP_RECOV_RATE): % HP-regen
                        // bonus from equipment (rAthena status_natural_heal hp_regen).
                        if (entity is PlayerEntity hpc && hpc.EquipBonuses.HpRecovRate != 0)
                            amount = amount * (100 + hpc.EquipBonuses.HpRecovRate) / 100;
                        if (amount < 1) amount = 1;
                        s.Hp = Math.Min(s.MaxHp, s.Hp + amount);
                    }
                }
                rs.NextHpTick = nowTick + HpIntervalMs;
            }

            if (nowTick >= rs.NextSpTick && s.Sp < s.MaxSp)
            {
                if (!walking || sitting)
                {
                    var amount = 1 + s.MaxSp / 100 + s.IntStat / 6;
                    if (sitting) amount *= 2;
                    // SC_MAGNIFICAT doubles SP regen (rAthena renewal:
                    // 50 % faster, simplified to 2× here while the
                    // Val1-level table ports).
                    if (_sc?.Get(entity, StatusType.Magnificat) != null) amount *= 2;
                    // COMBAT-23 — bonus bSPrecovRate (SP_SP_RECOV_RATE).
                    if (entity is PlayerEntity spc && spc.EquipBonuses.SpRecovRate != 0)
                        amount = amount * (100 + spc.EquipBonuses.SpRecovRate) / 100;
                    if (amount < 1) amount = 1;
                    s.Sp = Math.Min(s.MaxSp, s.Sp + amount);
                }
                rs.NextSpTick = nowTick + SpIntervalMs;
            }
        }

        // Periodic prune of stale anchors (entity died / left map).
        if (_state.Count > 0 && (nowTick & 0x3FFF) == 0)
        {
            var stale = _state.Keys.Where(id => _entities.Get(id) == null).ToList();
            foreach (var id in stale) _state.Remove(id);
        }
    }

    private static bool IsAlive(Entity e) => e switch
    {
        PlayerEntity p => p.Hp > 0,
        MobEntity m => m.Hp > 0,
        _ => false,
    };

    private sealed class RegenState
    {
        public long NextHpTick;
        public long NextSpTick;
    }
}
