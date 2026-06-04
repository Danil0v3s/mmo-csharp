using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Auction;
using Map.Server.Entities;
using Map.Server.Items;
using Map.Server.Session;

namespace Map.Server.Handlers.Auction;

/// <summary>
/// Stage an inventory item for auction. rAthena <c>clif_parse_Auction_setitem</c> (clif.cpp, 0x024c).
/// Validates the item (auctionable type, identified, not equipped, not expired, amount 1) and stores
/// the staged slot on the session; replies <c>clif_Auction_setitem</c> (success/fail).
/// </summary>
[PacketHandler(PacketHeader.CZ_AUCTION_ADD_ITEM)]
public class AuctionSetItemHandler(
    IEntityRegistry registry,
    IAuctionClientService client,
    IItemCatalog items,
    ILogger<AuctionSetItemHandler> logger
) : IPacketHandler<MapSessionData, CZ_AUCTION_ADD_ITEM>
{
    // rAthena: IT_ARMOR / IT_PETARMOR / IT_WEAPON / IT_CARD / IT_ETC / IT_SHADOWGEAR are auctionable.
    private static readonly HashSet<string> Auctionable = new(StringComparer.OrdinalIgnoreCase)
    { "Armor", "PetArmor", "Weapon", "Card", "Etc", "Shadowgear", "ShadowGear" };

    public Task HandleAsync(MapSessionData session, CZ_AUCTION_ADD_ITEM packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity pc)
        {
            return Task.CompletedTask;
        }

        // rAthena setting an item resets any prior stage first.
        session.AuctionStageIndex = -1;
        session.AuctionStageAmount = 0;

        var serverIndex = packet.Index - 2; // client index → server index
        var item = session.Inventory?.FirstOrDefault(i => i.ServerIndex == serverIndex);

        var ok = item != null
            && packet.Amount == 1 && item.Amount >= 1
            && item.Equip == 0
            && item.Identified
            && item.ExpireTime == 0
            && Auctionable.Contains(items.Get(item.NameId)?.Type ?? string.Empty);

        if (!ok)
        {
            client.SetItemResult(pc, (short)Math.Max(0, serverIndex), fail: true);
            return Task.CompletedTask;
        }

        session.AuctionStageIndex = serverIndex;
        session.AuctionStageAmount = 1;
        client.SetItemResult(pc, (short)(serverIndex + 2), fail: false);
        logger.LogDebug("Auction stage: char {Char} staged inv slot {Slot}", pc.CharacterId, serverIndex);
        return Task.CompletedTask;
    }
}
