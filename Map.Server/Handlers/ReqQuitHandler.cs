using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;

namespace Map.Server.Handlers;

/// <summary>
/// Client-initiated quit (ALT+E). rAthena <c>clif_parse_QuitGame</c>: enters
/// the quit state, then tears the connection down. The periodic lifecycle
/// sweep on <see cref="MapServerImpl"/> picks up the dead session and runs
/// the full cleanup (entity removal, broadcast vanish, char-server LeaveMap).
/// </summary>
[PacketHandler(PacketHeader.CZ_REQ_QUIT)]
public class ReqQuitHandler : IPacketHandler<MapSessionData, CZ_REQ_QUIT>
{
    public Task HandleAsync(MapSessionData session, CZ_REQ_QUIT packet)
    {
        session.Disconnect(DisconnectReason.ClientDisconnect);
        return Task.CompletedTask;
    }
}
