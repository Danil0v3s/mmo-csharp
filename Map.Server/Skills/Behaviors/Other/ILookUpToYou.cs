using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// WE_FEMALE — Wedding Female "I look up to you" (SP transfer). Manual
/// port of <c>rathena-fork/src/map/skills/other/ilookuptoyou.cpp</c>.
/// Costs a slice of caster SP and grants that same fraction of target
/// MaxSP. SP cost lookup pipeline is TODO; we land the animation.
/// </summary>
public sealed class ILookUpToYou : SkillImpl
{
    public ILookUpToYou() : base(SkillIds.WE_FEMALE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
