using Map.Server.Status;

namespace Map.Server.Inventory.ItemEffects;

/// <summary>
/// Aegis-name → <see cref="IItemEffectHandler"/> dispatch table. Same
/// strategy-pattern shape as <c>SkillUnitService.SpecFor</c> / SkillDb.
/// New consumable items <c>Register(handler)</c> here; no switch case
/// to touch in <see cref="ItemUseService"/>.
/// </summary>
public sealed class ItemEffectRegistry
{
    private readonly Dictionary<string, IItemEffectHandler> _byAegis = new(StringComparer.Ordinal);

    public ItemEffectRegistry(IStatusChangeService scService)
    {
        // Standard healing potions — itemheal X,0 equivalents.
        Register(new HealHpHandler("Red_Potion", 50));
        Register(new HealHpHandler("Orange_Potion", 105));
        Register(new HealHpHandler("Yellow_Potion", 175));
        Register(new HealHpHandler("White_Potion", 425));

        // SP potions.
        Register(new HealSpHandler("Blue_Potion", 50));

        // Buff scrolls — sc_start equivalents.
        Register(new ApplyStatusHandler("Blessing_10_Scroll", StatusType.Blessing,
            val1: 10, durationMs: 240_000, scService));
        Register(new ApplyStatusHandler("Increase_Agility_10_Scroll", StatusType.IncreaseAgi,
            val1: 10, durationMs: 140_000, scService));
    }

    public void Register(IItemEffectHandler handler) => _byAegis[handler.AegisName] = handler;

    public IItemEffectHandler? Get(string aegisName) => _byAegis.GetValueOrDefault(aegisName);

    public int Count => _byAegis.Count;
}
