using Map.Server.Entities;

namespace Map.Server.Storage.Guild;

/// <summary>
/// Guild + premium storage. Canonical entry points for the
/// storage.cpp half not covered by the account-storage port.
/// rAthena <c>storage.cpp</c> (1 206 lines total). Account storage
/// already exists in <c>Map.Server.Storage</c>.
/// </summary>
public interface IGuildStorageService
{
    /// <summary>rAthena <c>storage_guild_storageopen</c>.</summary>
    int Open(PlayerEntity pc);

    /// <summary>rAthena <c>storage_guild_storageclose</c>.</summary>
    void Close(PlayerEntity pc);

    /// <summary>rAthena <c>storage_guild_storageadd</c>.</summary>
    void Add(PlayerEntity pc, int inventoryIndex, int amount);

    /// <summary>rAthena <c>storage_guild_storageaddfromcart</c>.</summary>
    void AddFromCart(PlayerEntity pc, int cartIndex, int amount);

    /// <summary>rAthena <c>storage_guild_storageget</c>.</summary>
    void Get(PlayerEntity pc, int storageIndex, int amount);

    /// <summary>rAthena <c>storage_guild_storagegettocart</c>.</summary>
    void GetToCart(PlayerEntity pc, int storageIndex, int amount);

    /// <summary>rAthena <c>storage_guild_additem</c>.</summary>
    bool AddItem(int guildId, int nameId, int amount);

    /// <summary>rAthena <c>storage_guild_additem2</c>.</summary>
    bool AddItem2(int guildId, int nameId, int amount);

    /// <summary>rAthena <c>storage_guild_delitem</c>.</summary>
    bool DelItem(int guildId, int slot, int amount);

    /// <summary>rAthena <c>storage_guild_storagesave</c>.</summary>
    bool StorageSave(int guildId);

    /// <summary>rAthena <c>storage_guild_storagesaved</c>.</summary>
    void StorageSaved(int guildId);

    /// <summary>rAthena <c>storage_guild_storage_quit</c>.</summary>
    void Quit(PlayerEntity pc);

    /// <summary>rAthena <c>storage_guild_delete</c>.</summary>
    void Delete(int guildId);

    /// <summary>rAthena <c>storage_guild_log</c>.</summary>
    void Log(int guildId, int charId, int nameId, char operation, int amount);

    /// <summary>rAthena <c>storage_premiumStorage_load</c>.</summary>
    bool PremiumLoad(PlayerEntity pc, byte storageId, byte mode);

    /// <summary>rAthena <c>storage_premiumStorage_open</c>.</summary>
    void PremiumOpen(PlayerEntity pc);

    /// <summary>rAthena <c>storage_premiumStorage_save</c>.</summary>
    bool PremiumSave(PlayerEntity pc);

    /// <summary>rAthena <c>storage_premiumStorage_close</c>.</summary>
    void PremiumClose(PlayerEntity pc);

    /// <summary>rAthena <c>storage_premiumStorage_quit</c>.</summary>
    void PremiumQuit(PlayerEntity pc);

    /// <summary>rAthena <c>storage_sortitem</c>.</summary>
    int CompareItem(int leftNameId, int rightNameId);

    /// <summary>rAthena <c>storage_exists</c>.</summary>
    bool Exists(byte storageId);
}
