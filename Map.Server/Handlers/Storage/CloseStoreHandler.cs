using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Session;
using Map.Server.Storage;

namespace Map.Server.Handlers.Storage;

/// <summary>
/// Player closed the storage window. rAthena
/// <c>clif_parse_CloseKafra</c> (clif.cpp:13703). Drives the async
/// <c>AccountStorageSave</c> through <see cref="IStorageService.CloseAsync"/>;
/// emits <c>ZC_CLOSE_STORE</c> when the save settles (or immediately if
/// the window was never opened).
/// </summary>
[PacketHandler(PacketHeader.CZ_CLOSE_STORE)]
public class CloseStoreHandler(
    IEntityRegistry registry,
    IStorageService storage,
    ILogger<CloseStoreHandler> logger
) : IPacketHandler<MapSessionData, CZ_CLOSE_STORE>
{
    public async Task HandleAsync(MapSessionData session, CZ_CLOSE_STORE packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity)
        {
            return;
        }

        try
        {
            await storage.CloseAsync(session);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Storage close failed for char {Char}", session.CharacterId);
        }
        session.EnqueuePacket(new ZC_CLOSE_STORE());
    }
}
