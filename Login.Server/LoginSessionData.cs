using System.Net.Sockets;
using Core.Server.Network;
using Core.Server.Packets;

namespace Login.Server;

public class LoginSessionData(
    Socket socket,
    int heartbeatTimeout,
    IPacketFactory packetFactory,
    IPacketSizeRegistry sizeRegistry,
    ILogger logger
) : ClientSession(socket, heartbeatTimeout, packetFactory, sizeRegistry, logger)
{
    public int AccountId { get; set; } // Also GID
    public int LoginId1 { get; set; }
    public int LoginId2 { get; set; }
    public char Sex { get; set; }

    public string UserId { get; set; } = string.Empty; // NAME_LENGTH
    public string Password { get; set; } = string.Empty; // 23+1 for plaintext, 32+1 for md5-ed passwords
    public bool PasswordEnc { get; set; } // was the passwd transmited encrypted or clear ?

    public DateTime LastLogin { get; set; }
    public byte GroupId { get; set; }
    public byte ClientType { get; set; }
    public byte[] ClientHash { get; set; }
    public int HasClientHash { get; set; }
    
    public string WebAuthToken { get; set; } = string.Empty; // WEB_AUTH_TOKEN_LENGTH
}