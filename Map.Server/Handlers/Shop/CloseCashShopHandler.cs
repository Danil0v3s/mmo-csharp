using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Session;

namespace Map.Server.Handlers.Shop;

/// <summary>
/// Close the cash-shop UI. rAthena <c>clif_parse_cashshop_close</c> (clif.cpp, 0x084a) — clears
/// <c>sd-&gt;state.cashshop_open</c>. No packet is sent back.
/// </summary>
[PacketHandler(PacketHeader.CZ_REQ_CLOSE_CASHSHOP)]
public class CloseCashShopHandler(
    ILogger<CloseCashShopHandler> logger
) : IPacketHandler<MapSessionData, CZ_REQ_CLOSE_CASHSHOP>
{
    public Task HandleAsync(MapSessionData session, CZ_REQ_CLOSE_CASHSHOP packet)
    {
        session.CashShopOpen = false;
        logger.LogDebug("CloseCashShop: char {Char} closed cash shop", session.CharacterId);
        return Task.CompletedTask;
    }
}
