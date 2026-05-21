using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// OB_OBOROGENSOU — Moonlight Fantasy. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/moonlightfantasy.cpp</c>.
/// Status-only buff; does not work on mobs or status-immune targets.
/// </summary>
public sealed class MoonlightFantasy : StatusSkillImpl
{
    public MoonlightFantasy() : base(SkillIds.OB_OBOROGENSOU) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (target is MobEntity || (target.Stats.Mode & MobMode.StatusImmune) != 0)
        {
            if (src is PlayerEntity sd)
                ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
            return;
        }
        base.CastendNoDamageId(src, target, skillLevel, ctx);
    }
}
