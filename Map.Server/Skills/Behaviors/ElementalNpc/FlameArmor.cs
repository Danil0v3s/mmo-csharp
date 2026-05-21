using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EM_EL_FLAMEARMOR — Elemental Flame Armor. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/flamearmor.cpp</c>.
/// Toggles SC_FLAMEARMOR on target + SC_FLAMEARMOR_OPTION on the
/// elemental.
/// </summary>
public sealed class FlameArmor : SkillImpl
{
    public FlameArmor() : base(SkillIds.EM_EL_FLAMEARMOR) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc?.Get(target, StatusType.Flamearmor) != null
            || ctx.Sc?.Get(src, StatusType.FlamearmorOption) != null)
        {
            ctx.Sc.End(target, StatusType.Flamearmor);
            ctx.Sc.End(src, StatusType.FlamearmorOption);
            return;
        }
        ctx.Sc?.Start(src, StatusType.FlamearmorOption, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Sc?.Start(target, StatusType.Flamearmor, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
