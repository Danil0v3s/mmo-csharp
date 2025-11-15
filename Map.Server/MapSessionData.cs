namespace Map.Server;

using System.Net.Sockets;
using Core.Server.Network;
using Core.Server.Packets;

public class MapSessionData(
    Socket socket,
    int heartbeatTimeout,
    IPacketFactory packetFactory,
    IPacketSizeRegistry sizeRegistry,
    ILogger logger
) : ClientSession(socket, heartbeatTimeout, packetFactory, sizeRegistry, logger)
{
    public int? CharacterId { get; set; }
    public int? MapId { get; set; }
}