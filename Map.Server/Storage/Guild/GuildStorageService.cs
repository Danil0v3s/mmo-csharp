using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Status;
using Microsoft.Extensions.Logging;

namespace Map.Server.Storage.Guild;

/// <summary>
/// Default <see cref="IGuildStorageService"/>. Port of rAthena's
/// <c>storage.cpp</c> guild + premium half. Holds an in-memory cache of
/// per-guild storage state (<c>storage_db</c> shape) and per-(char,slot)
/// premium storage state. The char-side persistence flush already exists
/// via <c>IInterGuildStorageService.SaveAsync</c>; this service owns the
/// runtime projection consumed by the guild-storage packet handlers.
///
/// <para>The cache is filled on-demand: <see cref="Open"/> creates a
/// fresh <see cref="StorageState"/> for the player's guild_id when no
/// entry exists. Persistence load via <c>RequestGuildStorage</c> IPC is
/// the caller's responsibility (storage handler dispatches before
/// calling <c>Open</c>); this service just owns the in-memory map.</para>
/// </summary>
public sealed class GuildStorageService : IGuildStorageService
{
    private readonly ILogger<GuildStorageService> _logger;
    private readonly ISessionManagerAccessor? _sessions;

    /// <summary>Live per-guild storage state (rAthena <c>guild_storage_db</c>).</summary>
    private readonly Dictionary<int, StorageState> _byGuild = new();

    /// <summary>Live per-(charId, slot) premium storage state
    /// (rAthena <c>storage_premiumStorage_*</c>).</summary>
    private readonly Dictionary<(int charId, byte slot), StorageState> _premium = new();

    /// <summary>Tracks the character currently holding each guild storage
    /// open — rAthena enforces single-opener exclusion via the storage_db
    /// entry's <c>opened_by</c> field.</summary>
    private readonly Dictionary<int, int> _openHolder = new();

    public GuildStorageService(ILogger<GuildStorageService> logger, ISessionManagerAccessor? sessions = null)
    {
        _logger = logger;
        _sessions = sessions;
    }

    /// <summary>rAthena <c>storage_guild_storageopen</c>. Creates the
    /// cache entry on first open. Returns 0 on success matching the
    /// rAthena signature; nonzero error codes mirror the rAthena enum
    /// for kafra-side failure paths (we keep them best-effort here —
    /// the IPC fetch is the caller's concern).</summary>
    public int Open(PlayerEntity pc)
    {
        if (pc.GuildId <= 0) return 1; // not in a guild
        if (_openHolder.TryGetValue(pc.GuildId, out var holder) && holder != pc.Id.Value)
            return 2; // already opened by another guild member
        var stor = GetOrCreateGuildStorage(pc.GuildId);
        stor.IsOpen = true;
        _openHolder[pc.GuildId] = pc.Id.Value;
        return 0;
    }

    /// <summary>rAthena <c>storage_guild_storageclose</c>. Releases the
    /// opener latch; persistence flush happens via the IPC save path.</summary>
    public void Close(PlayerEntity pc)
    {
        if (pc.GuildId <= 0) return;
        if (!_byGuild.TryGetValue(pc.GuildId, out var stor)) return;
        stor.IsOpen = false;
        if (_openHolder.TryGetValue(pc.GuildId, out var holder) && holder == pc.Id.Value)
            _openHolder.Remove(pc.GuildId);
    }

    /// <summary>rAthena <c>storage_guild_storageadd</c>. Inventory →
    /// guild storage. Merges into an existing matching stack or appends
    /// a new row; decrements / removes the inventory source.</summary>
    public void Add(PlayerEntity pc, int inventoryIndex, int amount)
    {
        if (pc.GuildId <= 0 || amount < 1) return;
        if (!TryGetOpenStorage(pc, out var stor)) return;
        var session = _sessions?.GetByEntityId(pc.Id);
        var inv = session?.Inventory;
        if (inv == null || inventoryIndex < 0 || inventoryIndex >= inv.Count) return;
        var src = inv[inventoryIndex];
        if (src.Amount < 1 || amount > src.Amount) return;
        if (stor.Items.Count >= stor.MaxSlots && !CanStack(stor, src)) return;
        MergeOrAppend(stor, src, amount);
        src.Amount -= (uint)amount;
        if (src.Amount == 0)
        {
            inv.RemoveAt(inventoryIndex);
            for (int i = 0; i < inv.Count; i++) inv[i].ServerIndex = i;
        }
        stor.Dirty = true;
    }

    /// <summary>rAthena <c>storage_guild_storageaddfromcart</c>. Cart →
    /// guild storage. Mirrors <see cref="Add"/> with cart as source.
    /// Cart row decrements + compacts when emptied.</summary>
    public void AddFromCart(PlayerEntity pc, int cartIndex, int amount)
    {
        if (pc.GuildId <= 0 || amount < 1) return;
        if (!TryGetOpenStorage(pc, out var stor)) return;
        var session = _sessions?.GetByEntityId(pc.Id);
        var cart = session?.Cart;
        if (cart == null || cartIndex < 0 || cartIndex >= cart.Count) return;
        var src = cart[cartIndex];
        if (src.Amount < 1 || amount > src.Amount) return;
        if (stor.Items.Count >= stor.MaxSlots && !CanStack(stor, src)) return;
        MergeOrAppend(stor, src, amount);
        src.Amount -= (uint)amount;
        if (src.Amount == 0)
        {
            cart.RemoveAt(cartIndex);
            for (int i = 0; i < cart.Count; i++) cart[i].ServerIndex = i;
        }
        stor.Dirty = true;
    }

    /// <summary>rAthena <c>storage_guild_storageget</c>. Guild storage
    /// → inventory.</summary>
    public void Get(PlayerEntity pc, int storageIndex, int amount)
    {
        if (pc.GuildId <= 0 || amount < 1) return;
        if (!TryGetOpenStorage(pc, out var stor)) return;
        var session = _sessions?.GetByEntityId(pc.Id);
        if (session == null) return;
        session.Inventory ??= new List<InventoryItem>();
        var inv = session.Inventory;
        if (storageIndex < 0 || storageIndex >= stor.Items.Count) return;
        var src = stor.Items[storageIndex];
        if (src.Amount < 1 || amount > src.Amount) return;
        MergeOrAppendInventory(inv, src, amount);
        src.Amount -= (uint)amount;
        if (src.Amount == 0)
        {
            stor.Items.RemoveAt(storageIndex);
            for (int i = 0; i < stor.Items.Count; i++) stor.Items[i].ServerIndex = i;
        }
        stor.Dirty = true;
    }

    /// <summary>rAthena <c>storage_guild_storagegettocart</c>. Guild
    /// storage → cart. Reverse direction of
    /// <see cref="AddFromCart"/>.</summary>
    public void GetToCart(PlayerEntity pc, int storageIndex, int amount)
    {
        if (pc.GuildId <= 0 || amount < 1) return;
        if (!TryGetOpenStorage(pc, out var stor)) return;
        var session = _sessions?.GetByEntityId(pc.Id);
        if (session == null) return;
        session.Cart ??= new List<InventoryItem>();
        var cart = session.Cart;
        if (storageIndex < 0 || storageIndex >= stor.Items.Count) return;
        var src = stor.Items[storageIndex];
        if (src.Amount < 1 || amount > src.Amount) return;
        MergeOrAppendInventory(cart, src, amount);
        src.Amount -= (uint)amount;
        if (src.Amount == 0)
        {
            stor.Items.RemoveAt(storageIndex);
            for (int i = 0; i < stor.Items.Count; i++) stor.Items[i].ServerIndex = i;
        }
        stor.Dirty = true;
    }

    public bool AddItem(int guildId, int nameId, int amount)
    {
        if (guildId <= 0 || nameId <= 0 || amount < 1) return false;
        var stor = GetOrCreateGuildStorage(guildId);
        var existing = stor.Items.FirstOrDefault(s => s.NameId == nameId);
        if (existing != null)
        {
            existing.Amount += (uint)amount;
        }
        else
        {
            if (stor.Items.Count >= stor.MaxSlots) return false;
            stor.Items.Add(new StorageItem
            {
                ServerIndex = stor.Items.Count,
                NameId = (uint)nameId,
                Amount = (uint)amount,
                Identified = true,
            });
        }
        stor.Dirty = true;
        return true;
    }

    public bool AddItem2(int guildId, int nameId, int amount) => AddItem(guildId, nameId, amount);

    public bool DelItem(int guildId, int slot, int amount)
    {
        if (guildId <= 0 || amount < 1) return false;
        if (!_byGuild.TryGetValue(guildId, out var stor)) return false;
        if (slot < 0 || slot >= stor.Items.Count) return false;
        var src = stor.Items[slot];
        if (src.Amount < amount) return false;
        src.Amount -= (uint)amount;
        if (src.Amount == 0)
        {
            stor.Items.RemoveAt(slot);
            for (int i = 0; i < stor.Items.Count; i++) stor.Items[i].ServerIndex = i;
        }
        stor.Dirty = true;
        return true;
    }

    /// <summary>rAthena <c>storage_guild_storagesave</c>. Returns true
    /// when the live entry is dirty (caller-driven persistence flush is
    /// the IPC <c>SaveGuildStorage</c> path; this just signals dirty).</summary>
    public bool StorageSave(int guildId)
    {
        return _byGuild.TryGetValue(guildId, out var stor) && stor.Dirty;
    }

    public void StorageSaved(int guildId)
    {
        if (_byGuild.TryGetValue(guildId, out var stor))
            stor.Dirty = false;
    }

    public void Quit(PlayerEntity pc) => Close(pc);

    public void Delete(int guildId)
    {
        _byGuild.Remove(guildId);
        _openHolder.Remove(guildId);
    }

    public void Log(int guildId, int charId, int nameId, char operation, int amount)
    {
        // Persistent guild-storage audit log lives in
        // guild_storage_log via Core.Database; the read surface
        // (IGuildStorageLogRepository) is the residual storage-parity
        // gap (storage_guild_log_read). The write path inserts via
        // the char-side intif loop; map-side just signals here.
        _logger.LogDebug("guild storage log: guild={Guild} char={Char} item={Item} op={Op} amount={Amount}",
            guildId, charId, nameId, operation, amount);
    }

    /// <summary>rAthena <c>storage_premiumStorage_load</c>. Initializes
    /// the cache entry; the IPC fetch lives in the caller.</summary>
    public bool PremiumLoad(PlayerEntity pc, byte storageId, byte mode)
    {
        if (storageId == 0) return false;
        var key = (pc.Id.Value, storageId);
        if (!_premium.ContainsKey(key))
            _premium[key] = new StorageState();
        return true;
    }

    /// <summary>rAthena <c>storage_premiumStorage_open</c>. Marks the
    /// premium-storage cache entry as open so subsequent add/get can
    /// gate on it. Caller is responsible for sending the display
    /// packet; this just flips the IsOpen latch matching the rAthena
    /// arm at storage.cpp:storage_premiumStorage_open.</summary>
    public void PremiumOpen(PlayerEntity pc)
    {
        // Scan all loaded premium slots for this char; pick the most
        // recently loaded as the active one. rAthena's pc->premiumStorage
        // is a single live block per char (the storageId selects which
        // saved row got loaded into it), so opening simply flags the
        // currently-loaded entry IsOpen.
        foreach (var ((charId, _), stor) in _premium)
        {
            if (charId == pc.Id.Value)
            {
                stor.IsOpen = true;
                return;
            }
        }
        _logger.LogDebug("premium storage open requested for char {Char} but no slot loaded; call PremiumLoad first.",
            pc.Id.Value);
    }

    public bool PremiumSave(PlayerEntity pc)
    {
        foreach (var ((charId, _), stor) in _premium)
        {
            if (charId == pc.Id.Value && stor.Dirty)
                return true;
        }
        return false;
    }

    public void PremiumClose(PlayerEntity pc)
    {
        foreach (var ((charId, _), stor) in _premium)
        {
            if (charId == pc.Id.Value)
                stor.IsOpen = false;
        }
    }

    public void PremiumQuit(PlayerEntity pc) => PremiumClose(pc);

    public int CompareItem(int leftNameId, int rightNameId) => leftNameId.CompareTo(rightNameId);

    public bool Exists(byte storageId) => storageId > 0;

    // --- internals -------------------------------------------------

    private StorageState GetOrCreateGuildStorage(int guildId)
    {
        if (!_byGuild.TryGetValue(guildId, out var stor))
        {
            stor = new StorageState();
            _byGuild[guildId] = stor;
        }
        return stor;
    }

    private bool TryGetOpenStorage(PlayerEntity pc, out StorageState stor)
    {
        stor = null!;
        if (!_byGuild.TryGetValue(pc.GuildId, out var s)) return false;
        if (!s.IsOpen) return false;
        if (!_openHolder.TryGetValue(pc.GuildId, out var holder) || holder != pc.Id.Value) return false;
        stor = s;
        return true;
    }

    private static bool CanStack(StorageState stor, InventoryItem candidate)
        => stor.Items.Any(s =>
            s.NameId == candidate.NameId && s.Refine == candidate.Refine &&
            s.Card0 == candidate.Card0 && s.Card1 == candidate.Card1 &&
            s.Card2 == candidate.Card2 && s.Card3 == candidate.Card3 &&
            s.EnchantGrade == candidate.EnchantGrade);

    private static void MergeOrAppend(StorageState stor, InventoryItem src, int amount)
    {
        var existing = stor.Items.FirstOrDefault(s =>
            s.NameId == src.NameId && s.Refine == src.Refine &&
            s.Card0 == src.Card0 && s.Card1 == src.Card1 &&
            s.Card2 == src.Card2 && s.Card3 == src.Card3 &&
            s.EnchantGrade == src.EnchantGrade);
        if (existing != null)
        {
            existing.Amount += (uint)amount;
            return;
        }
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

    private static void MergeOrAppendInventory(List<InventoryItem> inv, StorageItem src, int amount)
    {
        var existing = inv.FirstOrDefault(c =>
            c.NameId == src.NameId && c.Refine == src.Refine &&
            c.Card0 == src.Card0 && c.Card1 == src.Card1 &&
            c.Card2 == src.Card2 && c.Card3 == src.Card3 &&
            c.EnchantGrade == src.EnchantGrade);
        if (existing != null)
        {
            existing.Amount += (uint)amount;
            return;
        }
        inv.Add(new InventoryItem
        {
            ServerIndex = inv.Count,
            NameId = src.NameId, Amount = (uint)amount,
            Identified = src.Identified, Refine = src.Refine,
            Attribute = src.Attribute,
            Card0 = src.Card0, Card1 = src.Card1, Card2 = src.Card2, Card3 = src.Card3,
            ExpireTime = src.ExpireTime, Bound = src.Bound,
            UniqueId = src.UniqueId, EnchantGrade = src.EnchantGrade,
        });
    }
}
