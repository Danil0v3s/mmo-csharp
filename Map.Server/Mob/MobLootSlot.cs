namespace Map.Server.Mob;

/// <summary>
/// One slot in a looter mob's <c>md->lootitems[]</c> array
/// (mob.hpp <c>struct s_mob_lootitem</c>).
///
/// <para>The mob holds onto picked-up floor items as a pre-baked
/// "drop bag" — at death they're re-spawned as floor items on the
/// mob's cell. We mirror the minimum surface needed to round-trip:
/// the original item id, amount, and the source mob class id
/// (for drop-table accounting / log_pick_mob).</para>
/// </summary>
public sealed record MobLootSlot(int ItemId, short Amount, int OriginalMobClassId)
{
    /// <summary>rAthena <c>LOOTITEM_SIZE</c> in mob.hpp.</summary>
    public const int LootBagSize = 10;
}
