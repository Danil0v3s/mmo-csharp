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
    public int PasswordEnc { get; set; } // 0=plain, 1=md5(md5key+pass), 2=md5(pass+md5key)
    public string Md5Key { get; set; } = string.Empty;

    public DateTime LastLogin { get; set; }
    public byte GroupId { get; set; }
    public byte ClientType { get; set; }
    public byte[] ClientHash { get; set; } = Array.Empty<byte>();
    public int HasClientHash { get; set; }

    public string WebAuthToken { get; set; } = string.Empty; // WEB_AUTH_TOKEN_LENGTH

    // Character server specific properties
    public bool IsCharServer { get; set; } = false;
    public string CharServerName { get; set; } = string.Empty;
    public uint CharServerIp { get; set; }
    public ushort CharServerPort { get; set; }
    public ushort CharServerType { get; set; }
    public ushort CharServerNew { get; set; }
    public ushort CharServerUsers { get; set; } = 0;
}
