namespace Map.Server.Storage;

/// <summary>
/// Owns the account-storage gameplay loop. Port of rAthena
/// <c>storage.cpp</c> — <c>storage_storageopen</c> /
/// <c>storage_storageadd</c> / <c>storage_storageget</c> /
/// <c>storage_storageclose</c>.
///
/// First slice: account storage only (guild storage already has its
/// own load/save IPC but no live transfer surface yet — added when
/// guild gameplay ports).
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Open account storage for <paramref name="session"/>. Loads via
    /// <c>AccountStorageLoad</c> IPC and caches on the session. Idempotent
    /// — returns <see cref="StorageOpResult.Ok"/> if already open.
    /// </summary>
    Task<StorageOpResult> OpenAsync(MapSessionData session, CancellationToken ct = default);

    /// <summary>Move <paramref name="amount"/> from inventory slot <paramref name="invIndex"/> into storage.</summary>
    StorageOpResult AddFromInventory(MapSessionData session, int invIndex, int amount);

    /// <summary>Move <paramref name="amount"/> from storage slot <paramref name="storIndex"/> back to inventory.</summary>
    StorageOpResult TakeToInventory(MapSessionData session, int storIndex, int amount);

    /// <summary>
    /// rAthena <c>storage_storageaddfromcart</c> (storage.cpp). Transfer
    /// <paramref name="amount"/> from cart slot <paramref name="cartIndex"/>
    /// into account storage. Same validation as
    /// <see cref="AddFromInventory"/> but pulls source from the cart inventory.
    /// </summary>
    StorageOpResult AddFromCart(MapSessionData session, int cartIndex, int amount);

    /// <summary>
    /// rAthena <c>storage_storagegettocart</c>. Reverse direction: from
    /// storage slot <paramref name="storIndex"/> into the cart.
    /// </summary>
    StorageOpResult TakeToCart(MapSessionData session, int storIndex, int amount);

    /// <summary>
    /// rAthena <c>storage_sortitem</c>. Item-id ascending compare used by
    /// the in-memory storage sort callback (cards segregated by id range).
    /// </summary>
    int SortItem(int leftNameId, int rightNameId);

    /// <summary>Close storage. Saves via <c>AccountStorageSave</c> IPC if dirty.</summary>
    Task<StorageOpResult> CloseAsync(MapSessionData session, CancellationToken ct = default);
}
