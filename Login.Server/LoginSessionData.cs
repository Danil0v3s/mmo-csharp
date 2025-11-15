namespace Login.Server;

using System.Net.Sockets;
using Core.Server.Network;
using Core.Server.Packets;

public class LoginSessionData(
    Socket socket,
    int heartbeatTimeout,
    IPacketFactory packetFactory,
    IPacketSizeRegistry sizeRegistry,
    ILogger logger
) : ClientSession(socket, heartbeatTimeout, packetFactory, sizeRegistry, logger)
{
    public int? AccountId { get; set; }
}