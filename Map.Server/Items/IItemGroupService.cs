using Core.Database.Entities;

namespace Map.Server.Items;

/// <summary>
/// rAthena <c>itemgroup()</c> / <c>itemdb_group::pc_get_itemgroup</c>
/// — weighted random pick from a named bag (e.g. "Bloody_Branch",
/// "2013_RWC_SCROLL", "Old_Card_Album"). Each rAthena <c>SubGroup</c>
/// is an independent roll bucket; the per-row <c>Rate</c> is the
/// weight inside that subgroup (sums to whatever yaml authored).
///
/// DBR-2b: sources from <see cref="Core.Database.Repositories.Api.IItemGroupCatalogDbRepository"/>
/// (2722 groups / 30809 entries seeded by DB-8b). Cached at boot;
/// rolls happen on the game-loop thread, so a per-service
/// <see cref="System.Random"/> is fine (no contention).
/// </summary>
public interface IItemGroupService
{
    /// <summary>Number of catalog groups loaded (zero pre-boot, post-reload).</summary>
    int GroupCount { get; }

    /// <summary>True if the named group exists in the catalog (any subgroup).</summary>
    bool HasGroup(string groupName);

    /// <summary>SubGroup ids defined for a group, sorted ascending. Empty if unknown.</summary>
    IReadOnlyList<int> SubGroupsOf(string groupName);

    /// <summary>
    /// rAthena <c>itemdb_group::get_random_item</c> — weighted random
    /// entry from <paramref name="subGroup"/> inside
    /// <paramref name="groupName"/>. Returns null if the group/subgroup
    /// is unknown or empty.
    /// </summary>
    ItemGroupCatalogEntryDbEntity? RollFromGroup(string groupName, int subGroup);

    /// <summary>
    /// Convenience: roll once per defined SubGroup and return the
    /// resulting entries (rAthena <c>pc_get_itemgroup</c> grants one
    /// roll per subgroup). Empty list if the group is unknown.
    /// </summary>
    IReadOnlyList<ItemGroupCatalogEntryDbEntity> RollAllSubGroups(string groupName);

    /// <summary>Rebuild the in-memory cache from the SQL catalog (GM reload hook).</summary>
    void Reload();

    /// <summary>
    /// rAthena <c>itemdb_group::item_exists(group_id, nameid)</c>
    /// (itemdb.cpp:1126) — does any sub-group of <paramref name="groupName"/>
    /// contain <paramref name="itemAegis"/>? Used by skills that gate
    /// on a kind-of-item (GN_SLINGITEM — BOMB vs THROWABLE; pet-feed
    /// skills against the feed-item group, etc.).
    /// </summary>
    bool ContainsItem(string groupName, string itemAegis);
}
