using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Skills;

/// <summary>
/// Default <see cref="ISkillProductionService"/>. All paths log a
/// debug line + return false until the production-recipe YAML loaders
/// land. The point of having concrete methods here is to give the
/// skill-resolver code a single named entry to call.
/// </summary>
public sealed class SkillProductionService : ISkillProductionService
{
    private readonly ILogger<SkillProductionService> _logger;

    public SkillProductionService(ILogger<SkillProductionService> logger) => _logger = logger;

    public bool ProduceMix(PlayerEntity caster, int recipeId, int qty)
    {
        _logger.LogDebug("skill_produce_mix: recipe={Recipe} qty={Qty} (produce_db.yml data-pending)", recipeId, qty);
        return false;
    }

    public bool ArrowCreate(PlayerEntity caster, int sourceItemId)
    {
        _logger.LogDebug("skill_arrow_create: from item {Item} (arrow_db.yml data-pending)", sourceItemId);
        return false;
    }

    public bool ChangeMaterial(PlayerEntity caster, int sourceItemId)
    {
        _logger.LogDebug("skill_changematerial: from item {Item} (data-pending)", sourceItemId);
        return false;
    }

    public bool RepairWeapon(PlayerEntity caster, int inventoryIndex)
    {
        // Real path: scan caster.Inventory for broken weapon at slot
        // <inventoryIndex>, consume repair material, clear broken flag.
        // Broken-flag column not on InventoryEntity yet — gate the call.
        _logger.LogDebug("skill_repairweapon: slot {Slot} (broken-flag data-pending)", inventoryIndex);
        return false;
    }

    public bool WeaponRefine(PlayerEntity caster, int inventoryIndex)
    {
        _logger.LogDebug("skill_weaponrefine: slot {Slot} (refine catalog data-pending)", inventoryIndex);
        return false;
    }

    public bool Identify(PlayerEntity caster, int inventoryIndex)
    {
        // Identify simply flips the inventory row's Identified column
        // from false to true. The IdentifyAll equivalent already exists
        // in pc.cpp port (PC-15); MC_IDENTIFY can route to the same
        // helper. Until the helper accepts a single index, keep the
        // entry point flagged.
        _logger.LogDebug("skill_identify: slot {Slot} (single-index helper data-pending)", inventoryIndex);
        return false;
    }

    public bool ElementalAnalysis(PlayerEntity caster, int sourceItemId)
    {
        _logger.LogDebug("skill_elementalanalysis: from item {Item} (data-pending)", sourceItemId);
        return false;
    }
}
