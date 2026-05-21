using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EL_HEATER — Elemental Heater. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/heater.cpp</c>.
/// Toggles SC_HEATER on target + SC_HEATER_OPTION on the elemental.
/// </summary>
public sealed class Heater : SkillImpl
{
    public Heater() : base(SkillIds.EL_HEATER) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc?.Get(target, StatusType.Heater) != null
            || ctx.Sc?.Get(src, StatusType.HeaterOption) != null)
        {
            ctx.Sc.End(target, StatusType.Heater);
            ctx.Sc.End(src, StatusType.HeaterOption);
            return;
        }
        ctx.Sc?.Start(src, StatusType.HeaterOption, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Sc?.Start(target, StatusType.Heater, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
