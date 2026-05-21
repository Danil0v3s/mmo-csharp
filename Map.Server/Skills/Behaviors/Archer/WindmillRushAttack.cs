using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// MI_RUSH_WINDMILL — Minstrel Windmill Rush Attack. Manual port of
/// <c>rathena-fork/src/map/skills/archer/windmillrushattack.cpp</c>.
/// Party-wide buff. Splash via party_foreachsamemap TODO.
/// </summary>
public sealed class WindmillRushAttack : SkillImpl
{
    public WindmillRushAttack() : base(SkillIds.MI_RUSH_WINDMILL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Rushwindmill, val1: skillLevel, val2: 5, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
