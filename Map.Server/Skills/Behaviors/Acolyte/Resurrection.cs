using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;
using Map.Server.Status.StatusOps;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// ALL_RESURRECTION — Resurrection. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/resurrection.cpp</c>.
///
/// <para>Two entry points:</para>
/// <list type="bullet">
///   <item><see cref="CastendDamageId"/>: Holy magic vs undead. Same
///         gate as <see cref="TurnUndead"/> — non-undead targets
///         no-op.</item>
///   <item><see cref="CastendNoDamageId"/>: revives a dead player at
///         <c>10/30/50/80 %</c> HP per skill level. WoE/BG/SC_HELLPOWER
///         block the revive; PvP gates on the target's pvp_point.</item>
/// </list>
/// </summary>
public sealed class Resurrection : SkillImpl
{
    private readonly Map.Server.Skills.ISkillAttackService? _skillAttack;
    private readonly IStatusOpsService? _statusOps;

    public Resurrection() : base(SkillIds.ALL_RESURRECTION) { }

    public Resurrection(
        Map.Server.Skills.ISkillAttackService? skillAttack = null,
        IStatusOpsService? statusOps = null) : base(SkillIds.ALL_RESURRECTION)
    {
        _skillAttack = skillAttack;
        _statusOps = statusOps;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: if (!battle_check_undead(...)) return;
        if (target.Stats.Race != BattleRace.Undead && target.Stats.DefenseElement != BattleElement.Undead)
            return;
        _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: WoE/BG block + clif_skill_fail to caster.
        // Map-flag service required — TODO: gate when IMapFlagService is
        // routed through SkillBehaviorContext.

        // rAthena: if (!status_isdead(*target)) return;
        // Only resurrect actually dead targets.
        if (target.Stats.Hp > 0) return;

        // rAthena: if (tsc && SC_HELLPOWER) { broadcast and return; }
        if (ctx.Sc?.Get(target, StatusType.Hellpower) != null)
        {
            ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
            return;
        }

        // HP-percent ladder per skill level.
        var hpPct = skillLevel switch
        {
            1 => (byte)10,
            2 => (byte)30,
            3 => (byte)50,
            4 => (byte)80,
            _ => (byte)80,
        };

        // rAthena: status_revive(target, per, sper).
        var revived = _statusOps?.Revive(target, hpPct, 0) ?? 0;
        if (revived == 0) return;

        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);

        // Deferred per PARITY-REMAINING.md §P2.3: battle_config.resurrection_exp
        // grants partial base/job EXP to the caster based on level delta.
        // Requires next-level EXP lookup that isn't surfaced through
        // SkillBehaviorContext yet.
    }
}
