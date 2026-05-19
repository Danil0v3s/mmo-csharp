using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Movement;
using Map.Server.Visibility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Map.Server.Combat;

/// <summary>
/// Concrete <see cref="IPcDeathService"/>. Tracks dead PCs in a private
/// set — entity remains in the registry so the client retains its
/// session/EntityId binding, but combat / movement / skill / item
/// services should treat <see cref="IsDead"/> = true as "no further
/// activity allowed until <see cref="Respawn"/>."
///
/// Death penalty mirrors rAthena <c>battle_config.death_penalty_*</c>
/// renewal defaults: 1% of base EXP and 1% of job EXP lost on death,
/// modulo per-map and per-job flags that aren't ported yet.
/// </summary>
public sealed class PcDeathService : IPcDeathService
{
    /// <summary>
    /// Renewal default <c>death_penalty_base</c> / <c>death_penalty_job</c>
    /// from <c>conf/battle/exp.conf</c> — 1% of current exp.
    /// </summary>
    private const int DeathPenaltyPermil = 10; // 1.0% in tenths-of-percent.

    // Resolved through IServiceProvider so the cycle
    //   DamageService → PcDeathService → IAttackStopper → AttackService
    //   → DamageService
    // doesn't form at construction time. StopAttack on death only runs
    // at runtime, well after every singleton has been built.
    private readonly IServiceProvider _services;
    private IAttackStopper? _cachedAttack;
    private IAttackStopper Attack => _cachedAttack ??= _services.GetRequiredService<IAttackStopper>();
    private readonly IVisibilityService _visibility;
    private readonly IPcSetposService? _setpos;
    private readonly ILogger<PcDeathService> _logger;
    private readonly HashSet<int> _dead = new();
    // Per-character savepoint snapshot — captured at session enter from
    // the CharacterData payload, since the PlayerEntity itself doesn't
    // (yet) carry the saved map/x/y fields.
    private readonly Dictionary<int, Savepoint> _savepoints = new();

    public PcDeathService(
        IServiceProvider services,
        IVisibilityService visibility,
        ILogger<PcDeathService> logger,
        IPcSetposService? setpos = null)
    {
        _services = services;
        _visibility = visibility;
        _setpos = setpos;
        _logger = logger;
    }

    /// <summary>Record this PC's savepoint (called by NotifyActorInitHandler).</summary>
    public void SetSavepoint(int characterId, string mapName, short x, short y)
        => _savepoints[characterId] = new Savepoint(mapName, x, y);

    private readonly record struct Savepoint(string MapName, short X, short Y);

    public void OnPcDead(PlayerEntity pc, Entity? source)
    {
        if (!_dead.Add(pc.CharacterId)) return;

        // Stop any in-flight activity.
        Attack.StopAttack(pc);
        pc.Walk = null;

        // Death penalty — pc.cpp pc_dead doesn't subtract here directly
        // (that's pc_loseexp via the script-side battle config), but for
        // the first slice we apply it inline.
        var baseLost = (long)((double)pc.BaseExp * DeathPenaltyPermil / 1000);
        var jobLost = (long)((double)pc.JobExp * DeathPenaltyPermil / 1000);
        pc.BaseExp = Math.Max(0, pc.BaseExp - baseLost);
        pc.JobExp = Math.Max(0, pc.JobExp - jobLost);

        // Broadcast vanish (Dead reason) to the AOI. rAthena: clif_clearunit_area(CLR_DEAD).
        _visibility.NotifyVanishedToArea(pc, VanishReason.Died);

        // ZC_RESTART_ACK type=0 isn't sent yet — rAthena sends ZC_RESURRECTION
        // / clears the entity then the client itself opens the respawn UI.
        // The respawn UI then sends CZ_RESTART type=0 → Respawn() below.
        _logger.LogInformation(
            "PC {Name} (char {CharId}) died (source: {Source}); -{BaseLost} base EXP, -{JobLost} job EXP",
            pc.Name, pc.CharacterId, source?.Id.Value, baseLost, jobLost);
    }

    public void Respawn(PlayerEntity pc)
    {
        if (!_dead.Remove(pc.CharacterId)) return;

        // pc_respawn(sd, CLR_OUTSIGHT) → pc_setrestartvalue(sd, 3) +
        // pc_setpos(savepoint). Restore HP/SP first, then teleport.
        pc.Hp = pc.MaxHp;
        pc.Sp = pc.MaxSp;

        if (_setpos != null && _savepoints.TryGetValue(pc.CharacterId, out var sp))
        {
            _setpos.Setpos(pc, sp.MapName, sp.X, sp.Y);
        }
        else
        {
            // Fallback: stay in place, just re-broadcast. Useful for tests
            // that don't wire IPcSetposService or for sessions that never
            // registered a savepoint.
            _visibility.NotifySpawnedToArea(pc);
        }
        _logger.LogInformation(
            "PC {Name} (char {CharId}) respawned at ({X},{Y}) on map 0x{Map:X8}",
            pc.Name, pc.CharacterId, pc.X, pc.Y, pc.MapId);
    }

    public bool IsDead(PlayerEntity pc) => _dead.Contains(pc.CharacterId);
}
