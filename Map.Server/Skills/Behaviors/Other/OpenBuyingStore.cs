using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// ALL_BUYING_STORE — Open Buying Store (skill.cpp:ALL_BUYING_STORE
/// arm). Calls <c>buyingstore_open</c> with the configured
/// <c>MAX_BUYINGSTORE_SLOTS</c> (5). The buying-store service runs the
/// per-buyer stall lifecycle (offer registration, search dispatch,
/// trade execution); this plugin only opens the stall UI.
/// </summary>
public sealed class OpenBuyingStore : SkillImpl
{
    /// <summary>rAthena <c>MAX_BUYINGSTORE_SLOTS</c> — 5 offers per stall.</summary>
    private const byte MaxBuyingStoreSlots = 5;

    public OpenBuyingStore() : base(SkillIds.ALL_BUYING_STORE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.BuyingStore?.Open(pc, MaxBuyingStoreSlots);
    }
}
