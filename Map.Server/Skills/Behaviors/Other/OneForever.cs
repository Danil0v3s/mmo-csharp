using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// WE_ONEFOREVER — Wedding "One Forever" resurrect. Manual port of
/// <c>rathena-fork/src/map/skills/other/oneforever.cpp</c>.
/// Revives a dead family-member at 30% HP. Marriage / death pipeline
/// is TODO; we land the animation.
/// </summary>
public sealed class OneForever : SkillImpl
{
    public OneForever() : base(SkillIds.WE_ONEFOREVER) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        // TODO: marriage / family lookup, status_isdead gate, revive with 30% HP.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
