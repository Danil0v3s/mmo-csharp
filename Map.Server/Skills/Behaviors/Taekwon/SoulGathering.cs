using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SOA_SOUL_GATHERING — Animation only; soulball generation TODO.</summary>
public sealed class SoulGathering : SkillImpl
{
    public SoulGathering() : base(SkillIds.SOA_SOUL_GATHERING) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
