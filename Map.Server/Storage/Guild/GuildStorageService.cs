using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Storage.Guild;

/// <summary>
/// Default <see cref="IGuildStorageService"/>. Persistence + guild
/// storage table deferred per PARITY-REMAINING.md §P2.2.d — the
/// char-side <c>IInterGuildStorageService.SaveAsync</c> is already
/// implemented; the map-side IPC wire is the residual hook.
/// </summary>
public sealed class GuildStorageService : IGuildStorageService
{
    private readonly ILogger<GuildStorageService> _logger;
    public GuildStorageService(ILogger<GuildStorageService> logger) => _logger = logger;

    public int Open(PlayerEntity pc) => 0;
    public void Close(PlayerEntity pc) { }
    public void Add(PlayerEntity pc, int inventoryIndex, int amount) { }
    public void AddFromCart(PlayerEntity pc, int cartIndex, int amount) { }
    public void Get(PlayerEntity pc, int storageIndex, int amount) { }
    public void GetToCart(PlayerEntity pc, int storageIndex, int amount) { }
    public bool AddItem(int guildId, int nameId, int amount) => false;
    public bool AddItem2(int guildId, int nameId, int amount) => false;
    public bool DelItem(int guildId, int slot, int amount) => false;
    public bool StorageSave(int guildId) => false;
    public void StorageSaved(int guildId) { }
    public void Quit(PlayerEntity pc) { }
    public void Delete(int guildId) { }
    public void Log(int guildId, int charId, int nameId, char operation, int amount) { }
    public bool PremiumLoad(PlayerEntity pc, byte storageId, byte mode) => false;
    public void PremiumOpen(PlayerEntity pc) { }
    public bool PremiumSave(PlayerEntity pc) => false;
    public void PremiumClose(PlayerEntity pc) { }
    public void PremiumQuit(PlayerEntity pc) { }
    public int CompareItem(int leftNameId, int rightNameId) => leftNameId.CompareTo(rightNameId);
    public bool Exists(byte storageId) => false;
}
