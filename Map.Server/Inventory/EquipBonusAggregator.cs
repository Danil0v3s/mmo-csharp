using Map.Server.Entities;
using Map.Server.Inventory.Script;
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
        BattleElement WeaponElement,
        int WeaponLevel = 0)
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
        int weaponLevel = 0;
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
                // rAthena PC atkmin = dex*(80+weaponLv*20)/100 — capture the
                // right-hand weapon's level (1-5); bare-handed stays 0.
                if (row.WeaponLevel is { } wl) weaponLevel = wl;
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
            WeaponElement: element,
            WeaponLevel: weaponLevel);
    }

    /// <summary>
    /// Populate <paramref name="bundle"/> from each equipped item's
    /// per-item <c>onEquip</c> hook (CONV-4). Resets the bundle first.
    /// Race / element / size / class bonus arrays accumulate across
    /// pieces; flat numeric fields (FlatAtk, FlatHit, ...) also
    /// accumulate.
    ///
    /// <para>
    /// rAthena reference: <c>pc_bonus_script</c> + the
    /// <c>script_run</c> over <c>sd-&gt;inventory_data[i]-&gt;script</c>
    /// at <c>status_calc_pc</c> time. Every script column that came out
    /// of the converter (CONV-2) lives as a TypeScript <c>onEquip</c>
    /// hook now; if a TS hook exists for an item id, <paramref name="hookDispatcher"/>
    /// owns the bonus accumulation. Items without a hook (no SQL script
    /// to translate, or hand-overrides without an equip bonus) contribute
    /// only the static fields surfaced by <see cref="Aggregate"/>.
    /// </para>
    ///
    /// <para>
    /// Combo bonuses are layered separately by <see cref="IComboDispatcher"/>
    /// in <see cref="EquipService"/> — it runs after this and accumulates
    /// into the same bundle.
    /// </para>
    ///
    /// <para>
    /// The legacy <c>BonusScriptExtractor</c> regex pass + the runtime
    /// DSL bridge (<c>IScriptedBonusService</c>) were retired in CONV-5
    /// — both are now redundant with the TS-hook path. The extractor's
    /// internal apply-flat / apply-indexed helpers are still used by
    /// <c>ScriptedBonusHost</c> as the bundle-write implementation.
    /// </para>
    /// </summary>
    public static void BuildBundle(
        IEnumerable<InventoryItem>? inventory,
        IItemCatalog catalog,
        EquipBonusBundle bundle,
        PlayerEntity? pc = null,
        IItemHookDispatcher? hookDispatcher = null)
    {
        bundle.Reset();
        if (inventory == null) return;

        // Snapshot the equipped items once for the script host's
        // getrefine() / getequiprefinerycnt() lookups. Cheap walk
        // through the inventory — equipped slots are a small subset.
        List<InventoryItem>? equippedSnapshot = null;
        if (hookDispatcher != null && pc != null)
        {
            equippedSnapshot = new List<InventoryItem>();
            foreach (var item in inventory)
                if (item.Equip != 0) equippedSnapshot.Add(item);
        }

        foreach (var item in inventory)
        {
            if (item.Equip == 0) continue;
            var row = catalog.Get(item.NameId);
            if (row == null) continue;

            // CONV-4: per-item onEquip hook owns bonus accumulation
            // (silently no-ops when no TS registration exists).
            // UnequipScript intentionally never fires here — recalc
            // rebuilds the bundle from scratch each time, so the
            // unequip-time bonus reversal happens implicitly. The
            // onUnequip *state* mutations land via EquipService.Unequip.
            if (hookDispatcher != null && pc != null && equippedSnapshot != null)
                hookDispatcher.TryInvokeOnEquip(item, bundle, pc, equippedSnapshot);
        }
    }
}
