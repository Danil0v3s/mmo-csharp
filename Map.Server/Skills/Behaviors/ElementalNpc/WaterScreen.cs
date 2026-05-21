using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EL_WATER_SCREEN — Elemental Water Screen. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/waterscreen.cpp</c>.
/// Toggles SC_WATER_SCREEN on target + SC_WATER_SCREEN_OPTION on the
/// elemental. End semantics differ slightly from the other toggles —
/// rAthena ends target/type and src/type2 (instead of src/type).
/// </summary>
public sealed class WaterScreen : SkillImpl
{
    public WaterScreen() : base(SkillIds.EL_WATER_SCREEN) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
        if (ctx.Sc?.Get(target, StatusType.WaterScreen) != null
            || ctx.Sc?.Get(src, StatusType.WaterScreenOption) != null)
        {
            ctx.Sc.End(target, StatusType.WaterScreen);
            ctx.Sc.End(src, StatusType.WaterScreenOption);
            return;
        }
        ctx.Sc?.Start(src, StatusType.WaterScreenOption, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Sc?.Start(target, StatusType.WaterScreen, val1: skillLevel, val2: (int)src.Id.Value, 0, 0, durationMs: 60_000, src);
    }
}
