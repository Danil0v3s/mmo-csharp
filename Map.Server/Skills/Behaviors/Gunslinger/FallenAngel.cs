using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// RL_FALLEN_ANGEL — Rebellion Fallen Angel. Manual port of
/// <c>rathena-fork/src/map/skills/gunslinger/fallenangel.cpp</c>.
/// Snap-warps the caster to (x, y) and applies SC_FALLEN_ANGEL. Unit
/// move pipeline is TODO.
/// </summary>
public sealed class FallenAngel : SkillImpl
{
    public FallenAngel() : base(SkillIds.RL_FALLEN_ANGEL) { }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // TODO: unit_movepos to (x, y), then clif_snap.
        ctx.Sc?.Start(src, StatusType.FallenAngel, val1: skillLevel, 0, 0, 0, durationMs: 10_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
