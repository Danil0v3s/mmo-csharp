using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SOA_TALISMAN_OF_WARRIOR — Status-only buff (animation only).</summary>
public sealed class TalismanOfWarrior : SkillImpl
{
    public TalismanOfWarrior() : base(SkillIds.SOA_TALISMAN_OF_WARRIOR) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
