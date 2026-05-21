using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EL_PYROTECHNIC — Elemental Pyrotechnic. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/pyrotechnic.cpp</c>.
/// Toggles SC_PYROTECHNIC on target + SC_PYROTECHNIC_OPTION on the
/// elemental.
/// </summary>
public sealed class Pyrotechnic : SkillImpl
{
    public Pyrotechnic() : base(SkillIds.EL_PYROTECHNIC) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc?.Get(target, StatusType.Pyrotechnic) != null
            || ctx.Sc?.Get(src, StatusType.PyrotechnicOption) != null)
        {
            ctx.Sc.End(target, StatusType.Pyrotechnic);
            ctx.Sc.End(src, StatusType.PyrotechnicOption);
            return;
        }
        ctx.Sc?.Start(src, StatusType.PyrotechnicOption, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Sc?.Start(target, StatusType.Pyrotechnic, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
