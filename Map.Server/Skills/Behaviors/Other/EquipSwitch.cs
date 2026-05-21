using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// ALL_EQSWITCH — Equip Switch. Manual port of
/// <c>rathena-fork/src/map/skills/other/equipswitch.cpp</c>.
/// Iterates the caster's equip_switch slots and runs pc_equipswitch on
/// each. Equip-switch pipeline is TODO; we animate.
/// </summary>
public sealed class EquipSwitch : SkillImpl
{
    public EquipSwitch() : base(SkillIds.ALL_EQSWITCH) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        // TODO: iterate sd.EquipSwitchIndex and call EquipService.SwitchSlot.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
