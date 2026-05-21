using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WA_SYMPHONY_OF_LOVER — Wanderer Symphony of Lovers. Manual port of
/// <c>rathena-fork/src/map/skills/archer/symphonyoflovers.cpp</c>.
/// Party-wide buff. Splash via party_foreachsamemap TODO.
/// </summary>
public sealed class SymphonyOfLovers : SkillImpl
{
    public SymphonyOfLovers() : base(SkillIds.WA_SYMPHONY_OF_LOVER) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Symphonyoflover, val1: skillLevel, val2: 5, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
