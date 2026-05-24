using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// ALL_BUYING_STORE — Open Buying Store. Manual port of
/// <c>rathena-fork/src/map/skills/other/openbuyingstore.cpp</c>.
/// Calls buyingstore_setup with MAX_BUYINGSTORE_SLOTS (=5). Buying-store
/// subsystem isn't wired yet; we land the animation.
/// </summary>
public sealed class OpenBuyingStore : SkillImpl
{
    public OpenBuyingStore() : base(SkillIds.ALL_BUYING_STORE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        // Deferred: buyingstore_setup(sd, MAX_BUYINGSTORE_SLOTS) — buying-store subsystem not yet ported.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
