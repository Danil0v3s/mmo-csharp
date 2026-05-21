using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SG_FEEL — Feeling the Sun/Moon/Stars. Map memorisation per skill_lv slot. Memory persistence TODO.</summary>
public sealed class FeelingtheSunMoonandStars : SkillImpl
{
    public FeelingtheSunMoonandStars() : base(SkillIds.SG_FEEL) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
