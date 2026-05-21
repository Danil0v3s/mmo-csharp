using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// ALL_WEWISH — Christmas Carol (We Wish You a Merry Christmas).
/// Manual port of <c>rathena-fork/src/map/skills/other/christmascarol.cpp</c>.
/// Animation only.
/// </summary>
public sealed class ChristmasCarol : SkillImpl
{
    public ChristmasCarol() : base(SkillIds.ALL_WEWISH) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
