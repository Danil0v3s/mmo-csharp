using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// PF_MINDBREAKER — Professor Mind Breaker. Manual port of
/// <c>rathena-fork/src/map/skills/mage/mindbreaker.cpp</c>.
///
/// <para>Applies SC_MINDBREAKER at <c>55 + 5*lv %</c> rate. Fails on
/// status-immune mobs (boss) and Undead-race/element targets. Cancels
/// the target's ongoing cast on success. Mob aggro retarget is TODO.</para>
/// </summary>
public sealed class MindBreaker : SkillImpl
{
    private readonly Random _rng;

    public MindBreaker() : base(SkillIds.PF_MINDBREAKER) => _rng = Random.Shared;

    public MindBreaker(Random? rng = null) : base(SkillIds.PF_MINDBREAKER) => _rng = rng ?? Random.Shared;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if ((target.Stats.Mode & MobMode.StatusImmune) != 0)
            return;
        if (target.Stats.Race == BattleRace.Undead || target.Stats.DefenseElement == BattleElement.Undead)
            return;
        if (_rng.Next(100) >= 55 + 5 * skillLevel)
        {
            if (src is PlayerEntity sd)
                ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
            return;
        }
        ctx.Sc?.Start(target, StatusType.Mindbreaker, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // rAthena PF_MINDBREAKER arm also cancels the target's current cast
        // (skill.cpp:11008 unit_skillcastcancel(bl, 0)) and pushes the
        // target's mob aggro at the caster. The aggro-retarget hook isn't
        // surfaced on a separate service today; the cast-cancel half
        // lands via ctx.Cast.CancelCast.
        ctx.Cast?.CancelCast(target.Id);
    }
}
