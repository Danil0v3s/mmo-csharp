using Map.Server.Entities;
using Map.Server.Items;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// WS_WEAPONREFINE — Whitesmith Upgrade Weapon (skill.cpp:WS_WEAPONREFINE
/// arm). Emits <c>clif_item_refine_list</c> with every refinable
/// weapon in the caster's bag (Weapon type, Refine &lt; cap, not
/// broken). The pick routes to <see cref="ISkillProductionService.WeaponRefine"/>.
/// </summary>
public sealed class UpgradeWeapon : SkillImpl
{
    private const byte AttrBroken = 0x01;
    private const byte MaxRefine = 20;

    private readonly IItemCatalog? _catalog;

    public UpgradeWeapon() : base(SkillIds.WS_WEAPONREFINE) { }

    public UpgradeWeapon(IItemCatalog? catalog = null) : base(SkillIds.WS_WEAPONREFINE)
    {
        _catalog = catalog;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        var session = ctx.Sessions?.TryGet(pc);
        var inv = session?.Inventory;
        if (inv == null) return;
        var refinable = new List<ushort>();
        foreach (var row in inv)
        {
            if (row.NameId == 0) continue;
            if (row.Refine >= MaxRefine) continue;
            if ((row.Attribute & AttrBroken) != 0) continue;
            var catRow = _catalog?.Get(row.NameId);
            if (catRow != null && catRow.Type != "Weapon") continue;
            if (row.NameId is > 0 and <= ushort.MaxValue) refinable.Add((ushort)row.NameId);
        }
        ctx.Client?.BroadcastItemRefineList(pc, refinable);
    }
}
