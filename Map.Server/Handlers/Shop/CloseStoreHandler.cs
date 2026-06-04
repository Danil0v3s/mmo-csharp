using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Session;
using Map.Server.Shop.Vending;

namespace Map.Server.Handlers.Shop;

/// <summary>
/// Close the player's vending shop. rAthena <c>clif_parse_CloseVending</c> (clif.cpp, 0x012e) →
/// <c>vending_closevending</c>. Tears down the stall (<see cref="IVendingService.CloseVending"/>),
/// which removes the stall sign from everyone in view.
/// </summary>
[PacketHandler(PacketHeader.CZ_REQ_CLOSESTORE)]
public class CloseStoreHandler(
    IEntityRegistry registry,
    IVendingService vending,
    ILogger<CloseStoreHandler> logger
) : IPacketHandler<MapSessionData, CZ_REQ_CLOSESTORE>
{
    public Task HandleAsync(MapSessionData session, CZ_REQ_CLOSESTORE packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity pc)
        {
            return Task.CompletedTask;
        }

        vending.CloseVending(pc);
        logger.LogInformation("CloseStore: char {Char} closed their shop", pc.CharacterId);
        return Task.CompletedTask;
    }
}
