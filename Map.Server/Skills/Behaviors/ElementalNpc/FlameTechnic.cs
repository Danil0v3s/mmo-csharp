using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EM_EL_FLAMETECHNIC — Elemental Flame Technic. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/flametechnic.cpp</c>.
/// Toggles SC_FLAMETECHNIC on target + SC_FLAMETECHNIC_OPTION on the
/// elemental.
/// </summary>
public sealed class FlameTechnic : SkillImpl
{
    public FlameTechnic() : base(SkillIds.EM_EL_FLAMETECHNIC) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc?.Get(target, StatusType.Flametechnic) != null
            || ctx.Sc?.Get(src, StatusType.FlametechnicOption) != null)
        {
            ctx.Sc.End(target, StatusType.Flametechnic);
            ctx.Sc.End(src, StatusType.FlametechnicOption);
            return;
        }
        ctx.Sc?.Start(src, StatusType.FlametechnicOption, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Sc?.Start(target, StatusType.Flametechnic, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
