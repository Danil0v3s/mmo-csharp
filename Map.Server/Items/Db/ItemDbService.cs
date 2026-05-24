using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Items.Db;

/// <summary>
/// Default <see cref="IItemDbService"/>. Gate predicates default to
/// `true` (permissive) — restrictive logic kicks in when the
/// per-item RestrictedFlag column ports onto the item catalog.
/// </summary>
public sealed class ItemDbService : IItemDbService
{
    private readonly ILogger<ItemDbService> _logger;
    public ItemDbService(ILogger<ItemDbService> logger) => _logger = logger;

    public bool CanAuction(int itemId, PlayerEntity pc) => true;
    public bool CanCartStore(int itemId, PlayerEntity pc) => true;
    public bool CanGuildStore(int itemId, PlayerEntity pc) => true;
    public bool CanMail(int itemId, PlayerEntity pc) => true;
    public bool CanPartnerTrade(int itemId, PlayerEntity pc) => true;
    public bool CanSell(int itemId, PlayerEntity pc) => true;
    public bool CanStore(int itemId, PlayerEntity pc) => true;
    public bool CanTrade(int itemId, PlayerEntity pc) => true;
    public bool IsDroppable(int itemId, PlayerEntity pc) => true;
    public bool IsRestricted(int itemId, PlayerEntity pc) => false;
    public bool IsNoEquip(int itemId, uint mapId) => false;
    public bool IsEquip2(int itemId) => false;
    public bool IsStackable2(int itemId) => false;
    public bool IsHatchedEgg(int itemId) => false;
    public bool IsIdentified(int itemId) => true;
    public int SearchNameArray(string namePattern, IList<int> output, int max) => 0;
    public void Reload() { /* item_db.yml + auxiliary YAML loaders deferred per PARITY-REMAINING.md §P2.2 */ }
    public void GenItemMoveInfo() { }
    public bool ParseRouletteDb() => false;
    public byte GetItemGroup(int groupId, PlayerEntity pc) => 0;
    public ushort FindComboId(IReadOnlyList<int> equippedItems) => 0;
    public void ApplyRandomOptionGroup(int groupId, IList<(int id, int value, int param)> output) { }
    public bool RandomOptionExists(int optionId) => false;
    public int RandomOptionGetId(string optionName) => 0;
}
