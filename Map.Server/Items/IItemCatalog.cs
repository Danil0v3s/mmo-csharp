using DbItem = Core.Database.Entities.ItemEntity;

namespace Map.Server.Items;

/// <summary>
/// In-memory snapshot of the seeded <c>item_db</c> table. Hydrated once at
/// map-server boot via <see cref="Core.Database.Repositories.Api.IItemRepository"/>
/// and used as the synchronous catalog the gameplay path (drops, pickup,
/// inventory, vending) consults to translate ids ↔ aegis names. Mirrors
/// rAthena's <c>itemdb_read_sqldb</c> path in <c>itemdb.cpp</c>.
///
/// MS3 first slice surfaces the raw <see cref="DbItem"/> row — the runtime
/// item type (weapon / armor / consumable / card) is encoded in
/// <c>Type</c> / <c>Subtype</c> strings, and the gameplay services can pull
/// whichever columns they need without us pre-shaping the data here.
/// </summary>
public interface IItemCatalog
{
    int Count { get; }

    /// <summary>Lookup by numeric id (the network/protocol item id).</summary>
    DbItem? Get(uint itemId);

    /// <summary>Lookup by aegis name (the string id, case-insensitive). Used to translate <c>mob_db.DropNItem</c> strings.</summary>
    DbItem? GetByAegisName(string aegisName);

    IEnumerable<DbItem> All();

    /// <summary>Rebuild the snapshot from the database (e.g. GM <c>/reloaditemdb</c>).</summary>
    void Reload();
}
