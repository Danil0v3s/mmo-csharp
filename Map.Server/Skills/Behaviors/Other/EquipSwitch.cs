using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// ALL_EQSWITCH — Equip Switch (skill.cpp:ALL_EQSWITCH arm). Calls
/// <see cref="IEquipService.SwitchAll"/>: every inventory row whose
/// <see cref="InventoryItem.EquipSwitch"/> mask is set swaps with
/// whatever is currently worn in the matching slot. The mask survives
/// the swap (lands on the unequipped row), so re-casting reverses it
/// — rAthena's bidirectional <c>pc_equipswitch_all</c>.
/// </summary>
public sealed class EquipSwitch : SkillImpl
{
    public EquipSwitch() : base(SkillIds.ALL_EQSWITCH) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        var session = ctx.Sessions?.TryGet(pc);
        if (session == null || ctx.Equip == null) return;
        ctx.Equip.SwitchAll(session);
    }
}
