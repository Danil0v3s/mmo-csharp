using Map.Server.Items;
using Map.Server.Status;

namespace Map.Server.Inventory;

/// <summary>
/// Sums up the equipped items' bonus contributions to a player's
/// <see cref="BattleStats"/>. Mirrors the read path inside
/// rAthena's <c>status_calc_pc_</c> (status.cpp:5990) — for each equip
/// slot with an item, accumulate <c>Atk</c>, <c>Def</c>, <c>Range</c>,
/// and pick the right-hand weapon's element / attack range as the
/// canonical weapon element.
///
/// This is the bridge between <see cref="IInventoryService"/> (which
/// already loads the rows) and <see cref="IStatusCalcService"/> (which
/// takes equipment-derived numbers as <see cref="PcBaseInputs"/>).
/// Cards / refines / random options / job bonuses / set bonuses are
/// out of scope for this slice — those need <c>item_db</c> script
/// parsing which lands later.
/// </summary>
public static class EquipBonusAggregator
{
    /// <summary>Right-hand bit in <see cref="InventoryItem.Equip"/> (rAthena EQP_HAND_R = 0x002).</summary>
    public const uint EquipRightHand = 0x002;
    public const uint EquipLeftHand = 0x020;
    public const uint EquipArmor = 0x010;
    public const uint EquipShield = 0x020;
    public const uint EquipHelm = 0x100;
    public const uint EquipShoes = 0x040;
    public const uint EquipGarment = 0x004;
    public const uint EquipAccessoryR = 0x008;
    public const uint EquipAccessoryL = 0x080;

    public readonly record struct EquipSummary(
        int WeaponAtkMin,
        int WeaponAtkMax,
        int EquipDef,
        int EquipMdef,
        int AttackRange,
        BattleElement WeaponElement)
    {
        public static EquipSummary Empty => new(0, 0, 0, 0, 1, BattleElement.Neutral);
    }

    public static EquipSummary Aggregate(IEnumerable<InventoryItem>? inventory, IItemCatalog catalog)
    {
        if (inventory == null) return EquipSummary.Empty;

        int watk = 0;
        int def = 0;
        int mdef = 0;
        int range = 1;
        var element = BattleElement.Neutral;
        // No weapon equipped → null; renewal status_base_atk_min/max
        // returns the catalog values for the equipped weapon row.

        foreach (var item in inventory)
        {
            if (item.Equip == 0) continue;
            var row = catalog.Get(item.NameId);
            if (row == null) continue;

            // Weapon contributions: ATK + range only when right-hand.
            if ((item.Equip & EquipRightHand) != 0)
            {
                watk += row.Attack ?? 0;
                if (row.Range is > 0) range = row.Range.Value;
            }
            // Both hands can contribute defense, but most weapons report 0.
            def += row.Defense ?? 0;
            // No MDEF column on the item_db row in this slice — armors
            // would carry it via script bonuses. Leave 0 here.
            // Element of the right-hand weapon stays Neutral until the
            // item-script parser ports (`bonus bAtkEle, Ele_Fire` etc.).
            _ = element;
        }

        return new EquipSummary(
            WeaponAtkMin: watk,
            WeaponAtkMax: watk, // rAthena uses ATK ± weapon variance; first slice flattens
            EquipDef: def,
            EquipMdef: mdef,
            AttackRange: range,
            WeaponElement: element);
    }
}
