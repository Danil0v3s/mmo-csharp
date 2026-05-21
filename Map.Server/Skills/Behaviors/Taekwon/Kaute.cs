using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SP_KAUTE — Kaute (SP transfer). Soul-link sharing — partner-link gating + SP transfer formula TODO.</summary>
public sealed class Kaute : SkillImpl
{
    public Kaute() : base(SkillIds.SP_KAUTE) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
