using System.Net.Sockets;
using Core.Server.Network;
using Core.Server.Packets;

namespace Char.Server;

public class CharSessionData(
    Socket socket,
    int heartbeatTimeout,
    IPacketFactory packetFactory,
    IPacketSizeRegistry sizeRegistry,
    ILogger logger
) : ClientSession(socket, heartbeatTimeout, packetFactory, sizeRegistry, logger)
{
    public int? CharacterId { get; set; }
}