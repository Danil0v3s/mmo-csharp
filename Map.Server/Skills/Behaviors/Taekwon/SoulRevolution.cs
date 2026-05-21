using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SP_SOULREVOLVE — Soul Revolve. SP transfer to target — animation only.</summary>
public sealed class SoulRevolution : SkillImpl
{
    public SoulRevolution() : base(SkillIds.SP_SOULREVOLVE) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
