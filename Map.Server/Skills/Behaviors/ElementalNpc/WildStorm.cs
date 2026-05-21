using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EL_WILD_STORM — Elemental Wild Storm. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/wildstorm.cpp</c>.
/// Toggles SC_WILD_STORM on target + SC_WILD_STORM_OPTION on the
/// elemental.
/// </summary>
public sealed class WildStorm : SkillImpl
{
    public WildStorm() : base(SkillIds.EL_WILD_STORM) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc?.Get(target, StatusType.WildStorm) != null
            || ctx.Sc?.Get(src, StatusType.WildStormOption) != null)
        {
            ctx.Sc.End(target, StatusType.WildStorm);
            ctx.Sc.End(src, StatusType.WildStormOption);
            return;
        }
        ctx.Sc?.Start(src, StatusType.WildStormOption, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Sc?.Start(target, StatusType.WildStorm, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
