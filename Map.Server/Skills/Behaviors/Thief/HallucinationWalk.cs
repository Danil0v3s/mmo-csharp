using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// GC_HALLUCINATIONWALK — Hallucination Walk. Manual port of
/// <c>rathena-fork/src/map/skills/thief/hallucinationwalk.cpp</c>.
/// Costs 10% of target.MaxHp; fails if insufficient HP. Then grants
/// the SC_HALLUCINATIONWALK status.
/// </summary>
public sealed class HallucinationWalk : StatusSkillImpl
{
    public HallucinationWalk() : base(SkillIds.GC_HALLUCINATIONWALK) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (target is PlayerEntity p)
        {
            var heal = p.MaxHp / 10;
            if (p.Hp <= heal)
            {
                ctx.Client?.BroadcastSkillFail(p, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
                return;
            }
            p.Hp -= heal;
        }
        base.CastendNoDamageId(src, target, skillLevel, ctx);
    }
}
