using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EL_WATER_DROP — Elemental Water Drop. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/waterdrop.cpp</c>.
/// Toggles SC_WATER_DROP on target + SC_WATER_DROP_OPTION on the
/// elemental.
/// </summary>
public sealed class WaterDrop : SkillImpl
{
    public WaterDrop() : base(SkillIds.EL_WATER_DROP) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc?.Get(target, StatusType.WaterDrop) != null
            || ctx.Sc?.Get(src, StatusType.WaterDropOption) != null)
        {
            ctx.Sc.End(target, StatusType.WaterDrop);
            ctx.Sc.End(src, StatusType.WaterDropOption);
            return;
        }
        ctx.Sc?.Start(src, StatusType.WaterDropOption, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Sc?.Start(target, StatusType.WaterDrop, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
