using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SP_SOULUNITY — Soul Unity. Party soulball share / unity link. Animation only.</summary>
public sealed class SoulUnity : SkillImpl
{
    public SoulUnity() : base(SkillIds.SP_SOULUNITY) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
