using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SO_EL_CONTROL — Sorcerer Spirit Control (skill.cpp:SO_EL_CONTROL
/// arm). Controls the caster's bound elemental:
/// <list type="bullet">
///   <item>lv 1 → EL_MODE_PASSIVE</item>
///   <item>lv 2 → EL_MODE_ASSIST</item>
///   <item>lv 3 → EL_MODE_AGGRESSIVE</item>
///   <item>lv 4 → dismiss (elemental_delete)</item>
/// </list>
/// </summary>
public sealed class SpiritControl : SkillImpl
{
    // rAthena EL_MODE_* values (elemental.hpp).
    private const int ElModePassive = 1;
    private const int ElModeAssist = 2;
    private const int ElModeAggressive = 4;

    public SpiritControl() : base(SkillIds.SO_EL_CONTROL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (pc.ActiveElementalClassId == 0) return;
        if (ctx.Elemental == null) return;
        switch (skillLevel)
        {
            case 1: ctx.Elemental.ChangeMode(pc, ElModePassive); break;
            case 2: ctx.Elemental.ChangeMode(pc, ElModeAssist); break;
            case 3: ctx.Elemental.ChangeMode(pc, ElModeAggressive); break;
            case 4: ctx.Elemental.Delete(pc); break;
        }
    }
}
