using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WA_SWING_DANCE — Wanderer/Minstrel Swing Dance. Manual port of
/// <c>rathena-fork/src/map/skills/archer/swingdance.cpp</c>.
/// Party-wide ASPD buff. Splash via party_foreachsamemap TODO.
/// </summary>
public sealed class SwingDance : SkillImpl
{
    public SwingDance() : base(SkillIds.WA_SWING_DANCE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Swingdance, val1: skillLevel, val2: 5, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
