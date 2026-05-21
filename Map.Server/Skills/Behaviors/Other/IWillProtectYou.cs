using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// WE_MALE — Wedding Male "I will protect you" (HP transfer). Manual
/// port of <c>rathena-fork/src/map/skills/other/iwillprotectyou.cpp</c>.
/// Costs a slice of caster HP and grants that same fraction of target
/// MaxHP. HP cost lookup pipeline is TODO; we land the animation.
/// </summary>
public sealed class IWillProtectYou : SkillImpl
{
    public IWillProtectYou() : base(SkillIds.WE_MALE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
