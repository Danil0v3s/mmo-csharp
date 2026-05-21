using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EL_WIND_CURTAIN — Elemental Wind Curtain. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/windcurtain.cpp</c>.
/// Toggles SC_WIND_CURTAIN on target + SC_WIND_CURTAIN_OPTION on the
/// elemental.
/// </summary>
public sealed class WindCurtain : SkillImpl
{
    public WindCurtain() : base(SkillIds.EL_WIND_CURTAIN) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc?.Get(target, StatusType.WindCurtain) != null
            || ctx.Sc?.Get(src, StatusType.WindCurtainOption) != null)
        {
            ctx.Sc.End(target, StatusType.WindCurtain);
            ctx.Sc.End(src, StatusType.WindCurtainOption);
            return;
        }
        ctx.Sc?.Start(src, StatusType.WindCurtainOption, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Sc?.Start(target, StatusType.WindCurtain, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
