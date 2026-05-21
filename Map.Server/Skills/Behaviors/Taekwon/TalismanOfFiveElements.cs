using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SOA_TALISMAN_OF_FIVE_ELEMENTS — Status-only buff (animation only).</summary>
public sealed class TalismanOfFiveElements : SkillImpl
{
    public TalismanOfFiveElements() : base(SkillIds.SOA_TALISMAN_OF_FIVE_ELEMENTS) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
