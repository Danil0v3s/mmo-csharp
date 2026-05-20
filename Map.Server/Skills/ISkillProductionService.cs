using Map.Server.Entities;

namespace Map.Server.Skills;

/// <summary>
/// Production / crafting / forge / refine paths. Canonical entry
/// points for the rAthena skill.cpp production family:
/// <c>skill_produce_mix</c>, <c>skill_arrow_create</c>,
/// <c>skill_changematerial</c>, <c>skill_repairweapon</c>,
/// <c>skill_weaponrefine</c>, <c>skill_identify</c>,
/// <c>skill_elementalanalysis</c>.
///
/// Every production path consumes inventory items + produces a new
/// inventory item (or upgrades an existing one). The C# port has
/// the inventory primitives — what's missing is the production-
/// recipe catalog (rAthena <c>db/produce_db.yml</c> +
/// <c>db/arrow_db.yml</c>). Until those YAML loaders land, each
/// method returns a "production refused" outcome and logs the call
/// site so the entry point is visible.
/// </summary>
public interface ISkillProductionService
{
    /// <summary>rAthena <c>skill_produce_mix</c> — generic recipe path (Pharmacy / Cooking / Forge).</summary>
    bool ProduceMix(PlayerEntity caster, int recipeId, int qty);

    /// <summary>rAthena <c>skill_arrow_create</c> — Arrow Crafting on an inventory item.</summary>
    bool ArrowCreate(PlayerEntity caster, int sourceItemId);

    /// <summary>rAthena <c>skill_changematerial</c> — Geneticist Change Material.</summary>
    bool ChangeMaterial(PlayerEntity caster, int sourceItemId);

    /// <summary>rAthena <c>skill_repairweapon</c> — Blacksmith Weapon Repair (BS_REPAIRWEAPON).</summary>
    bool RepairWeapon(PlayerEntity caster, int inventoryIndex);

    /// <summary>rAthena <c>skill_weaponrefine</c> — Blacksmith Weapon Refining (WS_WEAPONREFINE).</summary>
    bool WeaponRefine(PlayerEntity caster, int inventoryIndex);

    /// <summary>rAthena <c>skill_identify</c> — Merchant Identify (MC_IDENTIFY).</summary>
    bool Identify(PlayerEntity caster, int inventoryIndex);

    /// <summary>rAthena <c>skill_elementalanalysis</c> — Alchemist Elemental Analysis.</summary>
    bool ElementalAnalysis(PlayerEntity caster, int sourceItemId);
}
