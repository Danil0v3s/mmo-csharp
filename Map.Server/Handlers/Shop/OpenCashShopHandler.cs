using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Session;
using Map.Server.Shop.Cash;

namespace Map.Server.Handlers.Shop;

/// <summary>
/// Open the cash shop (the "cash shop" button). rAthena <c>clif_parse_cashshop_open_request</c>
/// (clif.cpp, 0x0b6d) → <c>clif_cashshop_open</c>. Flags the session open and sends the player's
/// current point balances + the focused tab.
/// </summary>
[PacketHandler(PacketHeader.CZ_SE_CASHSHOP_OPEN)]
public class OpenCashShopHandler(
    IEntityRegistry registry,
    ICashShopClientService client,
    ILogger<OpenCashShopHandler> logger
) : IPacketHandler<MapSessionData, CZ_SE_CASHSHOP_OPEN>
{
    public Task HandleAsync(MapSessionData session, CZ_SE_CASHSHOP_OPEN packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity pc)
        {
            return Task.CompletedTask;
        }

        session.CashShopOpen = true;
        client.SendOpen(pc, packet.Tab);
        logger.LogInformation("OpenCashShop: char {Char} opened cash shop (tab {Tab})", pc.CharacterId, packet.Tab);
        return Task.CompletedTask;
    }
}
