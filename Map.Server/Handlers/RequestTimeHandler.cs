using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Core.Server.Packets.Out.ZC;

namespace Map.Server.Handlers;

/// <summary>
/// Client keep-alive / clock-skew probe. rAthena <c>clif_parse_TickSend</c>:
/// echoes back a fresh server tick. Sent every few seconds while the client
/// is connected.
/// </summary>
[PacketHandler(PacketHeader.CZ_REQUEST_TIME)]
public class RequestTimeHandler : IPacketHandler<MapSessionData, CZ_REQUEST_TIME>
{
    public Task HandleAsync(MapSessionData session, CZ_REQUEST_TIME packet)
    {
        session.UpdateHeartbeat();
        session.EnqueuePacket(new ZC_NOTIFY_TIME { ServerTick = (uint)Environment.TickCount });
        return Task.CompletedTask;
    }
}
