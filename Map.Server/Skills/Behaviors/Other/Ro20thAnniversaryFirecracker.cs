using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// ALL_EVENT_20TH_ANNIVERSARY — RO 20th Anniversary firecracker. Manual
/// port of <c>rathena-fork/src/map/skills/other/ro20thanniversaryfirecracker.cpp</c>.
/// Animation only.
/// </summary>
public sealed class Ro20thAnniversaryFirecracker : SkillImpl
{
    public Ro20thAnniversaryFirecracker() : base(SkillIds.ALL_EVENT_20TH_ANNIVERSARY) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
}
