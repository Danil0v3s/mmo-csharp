using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>SA_MAGICROD — Sage Magic Rod. Self-buff that absorbs incoming magic.</summary>
public sealed class MagicRod : SkillImpl
{
    public MagicRod() : base(SkillIds.SA_MAGICROD) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
        ctx.Sc?.Start(target, StatusType.Magicrod, val1: skillLevel, 0, 0, 0, durationMs: 2000, src);
    }
}
