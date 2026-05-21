using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WM_DANCE_WITH_WUG — Minstrel/Wanderer Dance With a Warg. Manual
/// port of <c>rathena-fork/src/map/skills/archer/dancewithawarg.cpp</c>.
/// Party-wide ASPD buff. Splash via party_foreachsamemap TODO; lands
/// on caster.
/// </summary>
public sealed class DanceWithAWarg : SkillImpl
{
    public DanceWithAWarg() : base(SkillIds.WM_DANCE_WITH_WUG) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(src, StatusType.Dancewithwug, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
