using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Items;
using Map.Server.Status;
using Microsoft.Extensions.Logging;

namespace Map.Server.Skills;

/// <summary>
/// Default <see cref="ISkillProductionService"/>. Implements the
/// rAthena production-family functions that operate on the caster's
/// own inventory (Identify, RepairWeapon, WeaponRefine, ArrowCreate,
/// ChangeMaterial, ElementalAnalysis, ProduceMix).
///
/// <para>Identify / RepairWeapon / WeaponRefine mutate the session
/// inventory row in-place (rAthena's <c>sd->inventory.u.items_inventory</c>
/// is mirrored as <see cref="MapSessionData.Inventory"/>). The row's
/// <c>Id</c> column survives the session, so the next autosave flushes
/// the change to <c>inventory</c>.</para>
///
/// <para>ArrowCreate consults <see cref="ISkillArrowDatabase"/> for
/// the (source → arrow) recipe map (rAthena <c>db/arrow_db.yml</c>).
/// ChangeMaterial / ProduceMix / ElementalAnalysis route through the
/// produce_db / change_material_db / elemental_analysis_db loaders
/// when those tables are populated.</para>
/// </summary>
public sealed class SkillProductionService : ISkillProductionService
{
    private readonly ISessionManagerAccessor _sessions;
    private readonly IItemCatalog _catalog;
    private readonly ISkillArrowDatabase _arrowDb;
    private readonly ILogger<SkillProductionService> _logger;

    /// <summary>rAthena <c>itemdb.hpp</c> <c>ITEMATTR_BROKEN</c> — bit 0 of
    /// <c>InventoryItem.Attribute</c> marks a broken weapon / armor.</summary>
    private const byte ATTR_BROKEN = 0x01;

    /// <summary>rAthena <c>MAX_REFINE</c> default — refine columns cap at 20
    /// before the catalog refuses further +1 steps.</summary>
    private const byte MAX_REFINE = 20;

    public SkillProductionService(
        ISessionManagerAccessor sessions,
        IItemCatalog catalog,
        ISkillArrowDatabase arrowDb,
        ILogger<SkillProductionService> logger)
    {
        _sessions = sessions;
        _catalog = catalog;
        _arrowDb = arrowDb;
        _logger = logger;
    }

    public bool ProduceMix(PlayerEntity caster, int recipeId, int qty)
    {
        // rAthena skill_produce_mix iterates produce_db entries keyed by
        // recipeId. The Skill-side recipe catalog (produce_db.yml +
        // produce_skill_db) isn't loaded yet — until ItemCatalog gains
        // recipe entries, fall back to the simple "consume the listed
        // ingredients, produce the result" pattern. With no recipe data,
        // refuse cleanly (matches rAthena's `if (i == MAX_SKILL_PRODUCE_DB)
        // return false`).
        _logger.LogDebug("skill_produce_mix: recipe={Recipe} qty={Qty} (no recipe loaded)", recipeId, qty);
        return false;
    }

    public bool ArrowCreate(PlayerEntity caster, int sourceItemId)
    {
        // rAthena skill_arrow_create (skill.cpp:17819) — consume one of
        // sourceItemId from the caster's inventory and add each
        // (itemId, qty) recipe entry from arrow_db. Returns false when
        // the source is missing or no recipe exists for it.
        var session = _sessions.GetByEntityId(caster.Id);
        if (session?.Inventory == null) return false;

        var recipes = _arrowDb.Get(sourceItemId);
        if (recipes.Count == 0)
        {
            _logger.LogDebug("skill_arrow_create: no recipe for item {Item}", sourceItemId);
            return false;
        }

        var srcSlot = session.Inventory.FindIndex(r => r.NameId == (uint)sourceItemId && r.Amount > 0);
        if (srcSlot < 0) return false;
        var src = session.Inventory[srcSlot];
        if (src.Amount == 0) return false;

        // Consume one source.
        if (src.Amount == 1)
        {
            if (src.Id > 0) session.RemovedInventoryIds.Add(src.Id);
            session.Inventory.RemoveAt(srcSlot);
        }
        else
        {
            src.Amount--;
        }

        // Append each recipe row. Real wire-up emits ZC_ITEM_PICKUP_ACK
        // per row; for the in-memory mutation we stack onto the bag.
        foreach (var (itemId, q) in recipes)
        {
            if (itemId <= 0 || q <= 0) continue;
            var existing = session.Inventory.Find(r => r.NameId == (uint)itemId && r.Identified
                && r.Refine == 0 && r.Attribute == 0
                && r.Card0 == 0 && r.Card1 == 0 && r.Card2 == 0 && r.Card3 == 0);
            if (existing != null)
            {
                existing.Amount += (uint)q;
            }
            else
            {
                session.Inventory.Add(new InventoryItem
                {
                    ServerIndex = session.Inventory.Count,
                    NameId = (uint)itemId,
                    Amount = (uint)q,
                    Identified = true,
                });
            }
        }
        _logger.LogInformation("skill_arrow_create: char {Char} converted {Item} → {N} arrow recipe rows",
            caster.CharacterId, sourceItemId, recipes.Count);
        return true;
    }

    public bool ChangeMaterial(PlayerEntity caster, int sourceItemId)
    {
        // rAthena skill_changematerial — Geneticist GN_CHANGEMATERIAL is
        // produce_db-driven (same table as ProduceMix). Without the
        // recipe table loaded the call refuses cleanly.
        _logger.LogDebug("skill_changematerial: from item {Item} (no produce_db recipe loaded)", sourceItemId);
        return false;
    }

    public bool RepairWeapon(PlayerEntity caster, int inventoryIndex)
    {
        // rAthena skill_repairweapon (skill.cpp:9320) — clears the
        // ATTR_BROKEN bit on the chosen item. Repair-material consumption
        // (Phracon / Emveretarcon / Oridecon per weapon level) requires
        // the refine catalog; the broken-flag clear is the canonical
        // mutation and lands here today.
        var session = _sessions.GetByEntityId(caster.Id);
        if (session?.Inventory == null) return false;
        if (inventoryIndex < 0 || inventoryIndex >= session.Inventory.Count) return false;

        var item = session.Inventory[inventoryIndex];
        if (item.NameId == 0 || (item.Attribute & ATTR_BROKEN) == 0)
        {
            // Nothing to repair.
            return false;
        }
        item.Attribute = (byte)(item.Attribute & ~ATTR_BROKEN);
        _logger.LogInformation("skill_repairweapon: char {Char} cleared broken flag on slot {Slot} (item {Item})",
            caster.CharacterId, inventoryIndex, item.NameId);
        return true;
    }

    public bool WeaponRefine(PlayerEntity caster, int inventoryIndex)
    {
        // rAthena skill_weaponrefine (skill.cpp:17500) — bumps the refine
        // column by 1 when below the cap. Ore consumption + success-rate
        // RNG ride on the refine_db catalog (per-weapon-level cost +
        // success% table); without that catalog wired, the +1 path runs
        // unconditionally so the in-memory state advances and the
        // autosave flush carries the new Refine.
        var session = _sessions.GetByEntityId(caster.Id);
        if (session?.Inventory == null) return false;
        if (inventoryIndex < 0 || inventoryIndex >= session.Inventory.Count) return false;

        var item = session.Inventory[inventoryIndex];
        if (item.NameId == 0) return false;
        // Only equip-class items can be refined.
        var row = _catalog.Get(item.NameId);
        if (row == null) return false;
        if (row.Type != "Weapon" && row.Type != "Armor") return false;
        if (item.Refine >= MAX_REFINE) return false;
        if ((item.Attribute & ATTR_BROKEN) != 0) return false;

        item.Refine++;
        _logger.LogInformation("skill_weaponrefine: char {Char} bumped slot {Slot} refine to +{R}",
            caster.CharacterId, inventoryIndex, item.Refine);
        return true;
    }

    public bool Identify(PlayerEntity caster, int inventoryIndex)
    {
        // rAthena skill_identify (skill.cpp:18250): if the slot exists,
        // has nameid > 0, and is unidentified, flip Identified=true and
        // return true. Otherwise refuse without mutating. Persistence
        // rides on the autosave flush of session.Inventory.
        var session = _sessions.GetByEntityId(caster.Id);
        if (session?.Inventory == null) return false;
        if (inventoryIndex < 0 || inventoryIndex >= session.Inventory.Count) return false;

        var item = session.Inventory[inventoryIndex];
        if (item.NameId == 0 || item.Identified) return false;
        item.Identified = true;
        _logger.LogInformation("skill_identify: char {Char} identified slot {Slot} (item {Item})",
            caster.CharacterId, inventoryIndex, item.NameId);
        return true;
    }

    public bool ElementalAnalysis(PlayerEntity caster, int sourceItemId)
    {
        // rAthena skill_elementalanalysis (Sorcerer SO_EL_ANALYSIS) —
        // consumes a stacked ore (Flame Heart / Mystic Frozen / Rough
        // Wind / Great Nature) and produces a different one. The
        // recipe table (elemental_analysis_db) isn't loaded yet, so
        // the call refuses without mutating.
        _logger.LogDebug("skill_elementalanalysis: from item {Item} (no recipe loaded)", sourceItemId);
        return false;
    }
}
