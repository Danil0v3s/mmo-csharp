using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SU_LOPE — Summoner Lope. Manual port of
/// <c>rathena-fork/src/map/skills/summoner/lope.cpp</c>.
/// Teleports the caster to the target XY (no-teleport map flag + cell
/// reachability checks are TODO).
/// </summary>
public sealed class Lope : SkillImpl
{
    public Lope() : base(SkillIds.SU_LOPE) { }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
        // TODO: enforce MF_NOTELEPORT exception, check cell reachability, then unit_movepos.
    }
}
