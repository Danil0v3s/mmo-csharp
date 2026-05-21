using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EM_EL_COLD_FORCE — Elemental Cold Force. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/coldforce.cpp</c>.
/// Toggles SC_COLD_FORCE on target + SC_COLD_FORCE_OPTION on the
/// elemental.
/// </summary>
public sealed class ColdForce : SkillImpl
{
    public ColdForce() : base(SkillIds.EM_EL_COLD_FORCE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc?.Get(target, StatusType.ColdForce) != null
            || ctx.Sc?.Get(src, StatusType.ColdForceOption) != null)
        {
            ctx.Sc.End(target, StatusType.ColdForce);
            ctx.Sc.End(src, StatusType.ColdForceOption);
            return;
        }
        ctx.Sc?.Start(src, StatusType.ColdForceOption, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Sc?.Start(target, StatusType.ColdForce, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
