using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SOA_TALISMAN_OF_MAGICIAN — Status-only buff (animation only).</summary>
public sealed class TalismanOfMagician : SkillImpl
{
    public TalismanOfMagician() : base(SkillIds.SOA_TALISMAN_OF_MAGICIAN) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
