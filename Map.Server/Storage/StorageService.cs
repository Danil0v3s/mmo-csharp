using Map.Server.Inventory;
using Microsoft.Extensions.Logging;

namespace Map.Server.Storage;

/// <summary>
/// First-slice <see cref="IStorageService"/>. Routes load/save through
/// the existing char-server <c>AccountStorageLoad/Save</c> IPC; mediates
/// inventory ↔ storage transfers in-memory. The session holds the live
/// <see cref="StorageState"/>; <see cref="CloseAsync"/> flushes back via IPC.
///
/// rAthena reference: storage.cpp (storage_storageopen / storage_storageadd /
/// storage_storageget / storage_storageclose).
/// </summary>
public sealed class StorageService : IStorageService
{
    private readonly Services.ICharServerIpcServiceStorage _ipc;
    private readonly ILogger<StorageService> _logger;

    public StorageService(Services.ICharServerIpcServiceStorage ipc, ILogger<StorageService> logger)
    {
        _ipc = ipc;
        _logger = logger;
    }

    public async Task<StorageOpResult> OpenAsync(MapSessionData session, CancellationToken ct = default)
    {
        if (session.AccountId is not { } accountId || session.CharacterId is not { } charId)
            return StorageOpResult.Invalid;

        session.Storage ??= new StorageState();
        if (session.Storage.IsOpen) return StorageOpResult.Ok;

        var resp = await _ipc.AccountStorageLoadAsync(accountId, charId, ct);
        if (resp?.Success != true)
        {
            _logger.LogWarning("AccountStorageLoad failed for char {Char}", charId);
            return StorageOpResult.NoAccess;
        }
        session.Storage.Items.Clear();
        session.Storage.Items.AddRange(StorageCodec.Decode(resp.Data.ToByteArray()));
        session.Storage.IsOpen = true;
        session.Storage.Dirty = false;
        return StorageOpResult.Ok;
    }

    public StorageOpResult AddFromInventory(MapSessionData session, int invIndex, int amount)
    {
        if (session.Storage is not { IsOpen: true } stor) return StorageOpResult.NotOpen;
        if (session.Inventory is not { } inv) return StorageOpResult.Invalid;
        if (invIndex < 0 || invIndex >= inv.Count) return StorageOpResult.Invalid;
        var src = inv[invIndex];
        if (src.Amount <= 0 || amount < 1 || amount > src.Amount) return StorageOpResult.Invalid;
        if (stor.Items.Count >= stor.MaxSlots && !CanStack(stor, src)) return StorageOpResult.NoRoom;

        // Merge into an existing matching stack first (rAthena
        // storage_additem stacking rules, simplified to nameid+refine).
        var existing = FindMergeable(stor, src);
        if (existing != null)
        {
            existing.Amount += (uint)amount;
        }
        else
        {
            stor.Items.Add(new StorageItem
            {
                ServerIndex = stor.Items.Count,
                NameId = src.NameId,
                Amount = (uint)amount,
                Identified = src.Identified,
                Refine = src.Refine,
                Attribute = src.Attribute,
                Card0 = src.Card0, Card1 = src.Card1, Card2 = src.Card2, Card3 = src.Card3,
                ExpireTime = src.ExpireTime,
                Bound = src.Bound,
                UniqueId = src.UniqueId,
                EnchantGrade = src.EnchantGrade,
            });
        }

        // Decrement inventory slot — drop the row when empty.
        src.Amount -= (uint)amount;
        if (src.Amount == 0)
        {
            if (src.Id > 0) session.RemovedInventoryIds.Add(src.Id);
            inv.RemoveAt(invIndex);
        }
        stor.Dirty = true;
        return StorageOpResult.Ok;
    }

    /// <summary>
    /// rAthena <c>storage_storageaddfromcart</c> (storage.cpp). Source
    /// from the cart inventory (<see cref="MapSessionData.Cart"/>)
    /// instead of the main inventory. Validation mirrors
    /// <see cref="AddFromInventory"/>; the cart row is decremented in
    /// place (rows compact on empty).
    /// </summary>
    public StorageOpResult AddFromCart(MapSessionData session, int cartIndex, int amount)
    {
        if (session.Storage is not { IsOpen: true } stor) return StorageOpResult.NotOpen;
        var cart = session.Cart;
        if (cart == null) return StorageOpResult.Invalid;
        if (cartIndex < 0 || cartIndex >= cart.Count) return StorageOpResult.Invalid;
        var src = cart[cartIndex];
        if (src.Amount < 1 || amount < 1 || amount > src.Amount) return StorageOpResult.Invalid;
        if (stor.Items.Count >= stor.MaxSlots && !CanStack(stor, src)) return StorageOpResult.NoRoom;

        // Merge into existing storage stack or append a fresh one.
        var existing = FindMergeable(stor, src);
        if (existing != null)
        {
            existing.Amount += (uint)amount;
        }
        else
        {
            stor.Items.Add(new StorageItem
            {
                ServerIndex = stor.Items.Count,
                NameId = src.NameId, Amount = (uint)amount,
                Identified = src.Identified, Refine = src.Refine,
                Attribute = src.Attribute,
                Card0 = src.Card0, Card1 = src.Card1, Card2 = src.Card2, Card3 = src.Card3,
                ExpireTime = src.ExpireTime, Bound = src.Bound,
                UniqueId = src.UniqueId, EnchantGrade = src.EnchantGrade,
            });
        }
        src.Amount -= (uint)amount;
        if (src.Amount == 0)
        {
            cart.RemoveAt(cartIndex);
            for (int i = 0; i < cart.Count; i++) cart[i].ServerIndex = i;
        }
        stor.Dirty = true;
        return StorageOpResult.Ok;
    }

    /// <summary>
    /// rAthena <c>storage_storagegettocart</c>. Reverse direction —
    /// storage → cart. Storage row decrements + cart row appends/merges.
    /// </summary>
    public StorageOpResult TakeToCart(MapSessionData session, int storIndex, int amount)
    {
        if (session.Storage is not { IsOpen: true } stor) return StorageOpResult.NotOpen;
        if (storIndex < 0 || storIndex >= stor.Items.Count) return StorageOpResult.Invalid;
        var src = stor.Items[storIndex];
        if (src.Amount < 1 || amount < 1 || amount > src.Amount) return StorageOpResult.Invalid;
        session.Cart ??= new List<InventoryItem>();
        var cart = session.Cart;

        var mergeTarget = cart.FirstOrDefault(c =>
            c.NameId == src.NameId && c.Refine == src.Refine &&
            c.Card0 == src.Card0 && c.Card1 == src.Card1 && c.Card2 == src.Card2 && c.Card3 == src.Card3 &&
            c.EnchantGrade == src.EnchantGrade);
        if (mergeTarget != null)
        {
            mergeTarget.Amount += (uint)amount;
        }
        else
        {
            cart.Add(new InventoryItem
            {
                ServerIndex = cart.Count,
                NameId = src.NameId, Amount = (uint)amount,
                Identified = src.Identified, Refine = src.Refine,
                Attribute = src.Attribute,
                Card0 = src.Card0, Card1 = src.Card1, Card2 = src.Card2, Card3 = src.Card3,
                ExpireTime = src.ExpireTime, Bound = src.Bound,
                UniqueId = src.UniqueId, EnchantGrade = src.EnchantGrade,
            });
        }
        src.Amount -= (uint)amount;
        if (src.Amount == 0)
        {
            stor.Items.RemoveAt(storIndex);
            for (int i = 0; i < stor.Items.Count; i++) stor.Items[i].ServerIndex = i;
        }
        stor.Dirty = true;
        return StorageOpResult.Ok;
    }

    /// <summary>
    /// rAthena <c>storage_sortitem</c>. Item-id ascending compare —
    /// matches the rAthena sort callback applied on storage open.
    /// </summary>
    public int SortItem(int leftNameId, int rightNameId) => leftNameId.CompareTo(rightNameId);

    public StorageOpResult TakeToInventory(MapSessionData session, int storIndex, int amount)
    {
        if (session.Storage is not { IsOpen: true } stor) return StorageOpResult.NotOpen;
        if (storIndex < 0 || storIndex >= stor.Items.Count) return StorageOpResult.Invalid;
        var src = stor.Items[storIndex];
        if (src.Amount < 1 || amount < 1 || amount > src.Amount) return StorageOpResult.Invalid;
        session.Inventory ??= new List<InventoryItem>();
        var inv = session.Inventory;

        var mergeTarget = FindMergeable(inv, src);
        if (mergeTarget != null)
        {
            mergeTarget.Amount += (uint)amount;
        }
        else
        {
            inv.Add(new InventoryItem
            {
                ServerIndex = inv.Count,
                NameId = src.NameId,
                Amount = (uint)amount,
                Identified = src.Identified,
                Refine = src.Refine,
                Attribute = src.Attribute,
                Card0 = src.Card0, Card1 = src.Card1, Card2 = src.Card2, Card3 = src.Card3,
                ExpireTime = src.ExpireTime,
                Bound = src.Bound,
                UniqueId = src.UniqueId,
                EnchantGrade = src.EnchantGrade,
            });
        }

        src.Amount -= (uint)amount;
        if (src.Amount == 0)
        {
            stor.Items.RemoveAt(storIndex);
            // Reindex — rAthena keeps holes; we compact for simplicity.
            for (int i = 0; i < stor.Items.Count; i++) stor.Items[i].ServerIndex = i;
        }
        stor.Dirty = true;
        return StorageOpResult.Ok;
    }

    public async Task<StorageOpResult> CloseAsync(MapSessionData session, CancellationToken ct = default)
    {
        if (session.Storage is not { IsOpen: true } stor) return StorageOpResult.NotOpen;
        if (session.AccountId is not { } accountId || session.CharacterId is not { } charId)
            return StorageOpResult.Invalid;

        if (stor.Dirty)
        {
            var bytes = StorageCodec.Encode(stor.Items);
            var resp = await _ipc.AccountStorageSaveAsync(accountId, charId, bytes, ct);
            if (resp?.Success != true)
            {
                _logger.LogWarning("AccountStorageSave failed for char {Char}", charId);
                return StorageOpResult.NoAccess;
            }
            stor.Dirty = false;
        }
        stor.IsOpen = false;
        return StorageOpResult.Ok;
    }

    // --- merge helpers ---

    private static bool CanStack(StorageState stor, InventoryItem candidate)
        => FindMergeable(stor, candidate) != null;

    private static StorageItem? FindMergeable(StorageState stor, InventoryItem src)
    {
        foreach (var s in stor.Items)
        {
            if (s.NameId != src.NameId) continue;
            if (s.Refine != src.Refine || s.Identified != src.Identified) continue;
            if (s.Card0 != src.Card0 || s.Card1 != src.Card1 || s.Card2 != src.Card2 || s.Card3 != src.Card3) continue;
            return s;
        }
        return null;
    }

    private static InventoryItem? FindMergeable(List<InventoryItem> inv, StorageItem src)
    {
        foreach (var i in inv)
        {
            if (i.NameId != src.NameId) continue;
            if (i.Refine != src.Refine || i.Identified != src.Identified) continue;
            if (i.Card0 != src.Card0 || i.Card1 != src.Card1 || i.Card2 != src.Card2 || i.Card3 != src.Card3) continue;
            return i;
        }
        return null;
    }
}
