using Core.Server.Packets.Out.ZC;
using Map.Server.Inventory;
using Map.Server.Storage;

namespace Map.Server.Handlers.Storage;

/// <summary>
/// Wire-emission helpers that translate storage service outcomes
/// into rAthena packets. Mirrors <c>clif_storage_*</c> (clif.cpp:4868
/// .. 4935).
/// </summary>
public static class StorageNotifier
{
    /// <summary>
    /// Build the per-item add packet from a <see cref="StorageItem"/>
    /// (when an item enters storage) or an <see cref="InventoryItem"/>
    /// (when a duplicate inventory slot is sent to the client at open).
    /// </summary>
    public static ZC_ADD_ITEM_TO_STORE BuildAdd(int serverIndex, int amount, StorageItem src) => new()
    {
        ClientIndex = (ushort)(serverIndex + 1), // storage uses +1 offset
        Amount = amount,
        NameId = (ushort)src.NameId,
        Identified = src.Identified ? (byte)1 : (byte)0,
        Damaged = src.Attribute,
        Refine = src.Refine,
        Card0 = (ushort)src.Card0,
        Card1 = (ushort)src.Card1,
        Card2 = (ushort)src.Card2,
        Card3 = (ushort)src.Card3,
    };

    public static ZC_ADD_ITEM_TO_STORE BuildAdd(int serverIndex, int amount, InventoryItem src) => new()
    {
        ClientIndex = (ushort)(serverIndex + 1),
        Amount = amount,
        NameId = (ushort)src.NameId,
        Identified = src.Identified ? (byte)1 : (byte)0,
        Damaged = src.Attribute,
        Refine = src.Refine,
        Card0 = (ushort)src.Card0,
        Card1 = (ushort)src.Card1,
        Card2 = (ushort)src.Card2,
        Card3 = (ushort)src.Card3,
    };

    public static ZC_DELETE_ITEM_FROM_STORE BuildDelete(int serverIndex, int amount) => new()
    {
        ClientIndex = (ushort)(serverIndex + 1),
        Amount = amount,
    };

    public static ZC_NOTIFY_STOREITEM_COUNTINFO BuildCount(StorageState state) => new()
    {
        Current = (ushort)state.Items.Count,
        Max = (ushort)state.MaxSlots,
    };
}
