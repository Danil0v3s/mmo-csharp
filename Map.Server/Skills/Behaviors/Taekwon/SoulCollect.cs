using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SP_SOULCOLLECT — Soul Collect. Grants soulballs; ball allocation TODO.</summary>
public sealed class SoulCollect : SkillImpl
{
    public SoulCollect() : base(SkillIds.SP_SOULCOLLECT) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
