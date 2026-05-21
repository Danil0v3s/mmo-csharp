using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EL_COOLER — Elemental Cooler. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/cooler.cpp</c>.
/// Toggles SC_COOLER on target + SC_COOLER_OPTION on the elemental.
/// </summary>
public sealed class Cooler : SkillImpl
{
    public Cooler() : base(SkillIds.EL_COOLER) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc?.Get(target, StatusType.Cooler) != null
            || ctx.Sc?.Get(src, StatusType.CoolerOption) != null)
        {
            ctx.Sc.End(target, StatusType.Cooler);
            ctx.Sc.End(src, StatusType.CoolerOption);
            return;
        }
        ctx.Sc?.Start(src, StatusType.CoolerOption, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Sc?.Start(target, StatusType.Cooler, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
