using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>SA_QUESTION — Sage Questioning. Hocus-Pocus trigger only; emits a ? emote + cast frame.</summary>
public sealed class Questioning : SkillImpl
{
    public Questioning() : base(SkillIds.SA_QUESTION) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
