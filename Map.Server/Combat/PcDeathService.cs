using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Visibility;
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

    private readonly IAttackService _attack;
    private readonly IVisibilityService _visibility;
    private readonly ILogger<PcDeathService> _logger;
    private readonly HashSet<int> _dead = new();

    public PcDeathService(IAttackService attack, IVisibilityService visibility, ILogger<PcDeathService> logger)
    {
        _attack = attack;
        _visibility = visibility;
        _logger = logger;
    }

    public void OnPcDead(PlayerEntity pc, Entity? source)
    {
        if (!_dead.Add(pc.CharacterId)) return;

        // Stop any in-flight activity.
        _attack.StopAttack(pc);
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

        // rAthena pc_respawn(sd, CLR_OUTSIGHT) → pc_setrestartvalue(sd, 3) +
        // pc_setpos(savepoint). pc_setpos is the next slice; here we just
        // restore HP to MaxHp and re-broadcast the entity into AOI.
        pc.Hp = pc.MaxHp;
        pc.Sp = pc.MaxSp;
        _visibility.NotifySpawnedToArea(pc);
        _logger.LogInformation(
            "PC {Name} (char {CharId}) respawned at ({X},{Y})",
            pc.Name, pc.CharacterId, pc.X, pc.Y);
    }

    public bool IsDead(PlayerEntity pc) => _dead.Contains(pc.CharacterId);
}
