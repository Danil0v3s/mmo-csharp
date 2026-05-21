using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>SA_GRAVITY — Sage Gravity. Only triggered by Hocus Pocus; just emits the cast frame.</summary>
public sealed class Gravity : SkillImpl
{
    public Gravity() : base(SkillIds.SA_GRAVITY) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
