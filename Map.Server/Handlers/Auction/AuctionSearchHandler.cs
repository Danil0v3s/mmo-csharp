using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Auction;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Handlers.Auction;

/// <summary>
/// Browse/search auctions. rAthena <c>clif_parse_Auction_search</c> (clif.cpp, 0x0251) →
/// <c>intif_Auction_requestlist</c>. The packet's auction-id field carries the price (type 5) or id;
/// the char side filters + pages. Renders <c>clif_Auction_results</c>.
/// </summary>
[PacketHandler(PacketHeader.CZ_AUCTION_ITEM_SEARCH)]
public class AuctionSearchHandler(
    IEntityRegistry registry,
    IAuctionService auction,
    IAuctionClientService client,
    ILogger<AuctionSearchHandler> logger
) : IPacketHandler<MapSessionData, CZ_AUCTION_ITEM_SEARCH>
{
    public async Task HandleAsync(MapSessionData session, CZ_AUCTION_ITEM_SEARCH packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity pc)
        {
            return;
        }

        // Searching cancels any in-progress register staging (rAthena clif_parse_Auction_cancelreg).
        session.AuctionStageIndex = -1;
        session.AuctionStageAmount = 0;

        var resp = await auction.RequestListAsync(pc, packet.Type, (int)packet.AuctionId, packet.Text, packet.Page);
        var entries = resp is { Success: true }
            ? AuctionResultMapper.ToEntries(resp.Auctions)
            : new List<Core.Server.Packets.Out.ZC.AuctionResultEntry>();
        client.SendResults(pc, resp?.Pages ?? 0, entries);
        logger.LogDebug("Auction search: char {Char} type {Type} page {Page} → {N} result(s)",
            pc.CharacterId, packet.Type, packet.Page, entries.Count);
    }
}
