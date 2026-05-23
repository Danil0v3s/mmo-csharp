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

    /// <summary>
    /// Populate <paramref name="bundle"/> from each equipped item's
    /// <c>script</c> column via <see cref="BonusScriptExtractor"/>.
    /// Resets the bundle first. Race / element / size / class bonus
    /// arrays accumulate across pieces; flat numeric fields
    /// (FlatAtk, FlatHit, ...) also accumulate.
    ///
    /// rAthena reference: <c>pc_bonus_script</c> + the
    /// <c>script_run</c> over <c>sd-&gt;inventory_data[i]-&gt;script</c>
    /// at <c>status_calc_pc</c> time. The regex extractor only
    /// catches the static patterns (~90% of cards + most armor /
    /// weapon attributes); dynamic patterns (<c>getrefine()</c>,
    /// conditional bonuses) flow through when the script engine
    /// ports, without touching this call site.
    ///
    /// <para>
    /// DBR-2a+: if <paramref name="activeCombos"/> is provided, every
    /// firing combo's rAthena bonus script is also applied through the
    /// same extractor. Combo bonuses accumulate into the same bundle
    /// so the BattleCalculator + cast/drain hooks see them
    /// indistinguishably from per-item bonuses. Conditional combos
    /// (the ~2,700 yml entries using <c>if (getrefine() &gt;= 7) {...}</c>)
    /// silently no-op for now — they need the V8 DSL→JS bridge.
    /// </para>
    /// </summary>
    public static void BuildBundle(
        IEnumerable<InventoryItem>? inventory,
        IItemCatalog catalog,
        EquipBonusBundle bundle,
        IReadOnlyList<ActiveCombo>? activeCombos = null,
        IScriptedBonusService? scriptedBonuses = null,
        PlayerEntity? pc = null,
        IItemHookDispatcher? hookDispatcher = null)
    {
        bundle.Reset();
        if (inventory == null) return;

        // Snapshot the equipped items once for the script host's
        // getrefine() / getequiprefinerycnt() lookups. Cheap walk
        // through the inventory — equipped slots are a small subset.
        List<InventoryItem>? equippedSnapshot = null;
        if ((scriptedBonuses != null && pc != null) || (hookDispatcher != null && pc != null))
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

            // CONV-4: when a TS onEquip hook exists, it owns the per-item
            // bonus accumulation and we skip the DSL Script / EquipScript
            // paths to avoid double-applying. Items without a TS hook
            // (rare — converter emits hooks for every script column we
            // could translate) continue to flow through the DSL bridge.
            var dispatched = hookDispatcher != null && pc != null && equippedSnapshot != null
                && hookDispatcher.TryInvokeOnEquip(item, bundle, pc, equippedSnapshot);
            if (dispatched) continue;

            ApplyOne(row.Script, bundle, scriptedBonuses, pc, equippedSnapshot);
            // EquipScript runs on equip (unfolded into the bundle);
            // UnequipScript would reverse on unequip but in this model
            // we re-run BuildBundle from scratch, so UnequipScript is
            // effectively never applied — that matches rAthena's
            // status_calc_pc cadence.
            ApplyOne(row.EquipScript, bundle, scriptedBonuses, pc, equippedSnapshot);
        }

        // DBR-2a+: layer combo bonuses on top of per-item bonuses.
        // Same DSL → same extractor. Conditional / autobonus combos
        // (~3,275 of 7,767) route through the V8 bridge when
        // scriptedBonuses + pc are provided.
        if (activeCombos != null)
        {
            for (var i = 0; i < activeCombos.Count; i++)
                ApplyOne(activeCombos[i].Script, bundle, scriptedBonuses, pc, equippedSnapshot);
        }
    }

    /// <summary>
    /// DSL-4: dispatch one script body. Always runs the regex extractor
    /// (it's a cheap pass that catches the ~30k static bonus/bonus2
    /// statements). For dynamic scripts (if/.@/autobonus markers), also
    /// runs the V8 bridge so conditional + autobonus + autospell paths
    /// land. The two passes layer onto the same bundle; double-applying
    /// a static `bonus bAtk,10;` would double the value, so we gate the
    /// V8 path on <see cref="IScriptedBonusService.NeedsDynamicEval"/>.
    /// </summary>
    private static void ApplyOne(
        string? script, EquipBonusBundle bundle,
        IScriptedBonusService? scriptedBonuses, PlayerEntity? pc,
        IReadOnlyList<InventoryItem>? equipped)
    {
        if (string.IsNullOrEmpty(script)) return;
        if (scriptedBonuses != null && pc != null
            && IScriptedBonusService.NeedsDynamicEval(script))
        {
            // V8 path handles BOTH the dynamic stuff AND the static
            // bonus/bonus2 calls inside the same script — host
            // delegates to the same keyword switch as the regex.
            scriptedBonuses.Apply(script, pc, bundle, equipped);
            return;
        }
        // Static-only fast path.
        BonusScriptExtractor.Apply(script, bundle);
    }
}
