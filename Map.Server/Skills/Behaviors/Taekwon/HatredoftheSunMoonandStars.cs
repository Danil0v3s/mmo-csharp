using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SG_HATE — Hatred of the Sun, Moon, and Stars. Marks a mob race the SG kills for bonus damage. Hate persistence TODO.</summary>
public sealed class HatredoftheSunMoonandStars : SkillImpl
{
    public HatredoftheSunMoonandStars() : base(SkillIds.SG_HATE) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
