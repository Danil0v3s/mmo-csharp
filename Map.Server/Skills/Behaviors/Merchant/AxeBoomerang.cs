using Map.Server.Entities;
using Map.Server.Inventory;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_AXEBOOMERANG — Mechanic Axe Boomerang. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/axeboomerang.cpp</c>.
/// Ratio <c>+(150 + 50*lv)</c> + <c>weapon_weight / 10</c> when the
/// caster wields a right-hand weapon (rAthena divides by 10 because
/// item_db weights are stored at ×10 the user-facing value).
/// </summary>
public sealed class AxeBoomerang : WeaponSkillImpl
{
    public AxeBoomerang() : base(SkillIds.NC_AXEBOOMERANG) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx, int miscflag)
    {
        var ratio = baseRatio + 150 + 50 * skillLevel;
        if (src is PlayerEntity pc)
        {
            var session = ctx.Sessions?.TryGet(pc);
            var weapon = session != null ? ctx.Equip?.FindEquipped(session, EquipBits.HandR) : null;
            if (weapon != null)
            {
                var row = ctx.Catalog?.Get(weapon.NameId);
                if (row?.Weight is { } w && w > 0)
                    ratio += w / 10;
            }
        }
        return ratio;
    }
}
