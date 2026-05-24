using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// BS_REPAIRWEAPON — Blacksmith Repair Weapon (skill.cpp:BS_REPAIRWEAPON
/// arm). Emits <c>clif_item_repair_list</c> with the target's broken
/// inventory rows (ATTR_BROKEN bit set). Caster picks one and the
/// repair routes through <see cref="ISkillProductionService.RepairWeapon"/>.
/// </summary>
public sealed class WeaponRepair : SkillImpl
{
    private const byte ATTR_BROKEN = 0x01;

    public WeaponRepair() : base(SkillIds.BS_REPAIRWEAPON) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        if (target is not PlayerEntity tgt) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        var session = ctx.Sessions?.TryGet(tgt);
        var inv = session?.Inventory;
        if (inv == null) return;
        var broken = new List<ushort>();
        foreach (var row in inv)
        {
            if ((row.Attribute & ATTR_BROKEN) != 0 && row.NameId is > 0 and <= ushort.MaxValue)
                broken.Add((ushort)row.NameId);
        }
        ctx.Client?.BroadcastItemRepairList(pc, broken);
    }
}
