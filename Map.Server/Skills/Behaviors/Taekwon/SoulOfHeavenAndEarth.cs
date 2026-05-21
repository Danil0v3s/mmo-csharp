using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SOA_SOUL_OF_HEAVEN_AND_EARTH — Animation only buff.</summary>
public sealed class SoulOfHeavenAndEarth : SkillImpl
{
    public SoulOfHeavenAndEarth() : base(SkillIds.SOA_SOUL_OF_HEAVEN_AND_EARTH) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
