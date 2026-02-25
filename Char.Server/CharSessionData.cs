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
    public int? AccountId { get; set; }
    public int? CharacterId { get; set; }
    public int LoginId1 { get; set; }
    public int LoginId2 { get; set; }
    public byte Sex { get; set; }
    public bool IsAuthenticated { get; set; }
    public bool AccountDataLoaded { get; set; }
    public bool PincodeVerified { get; set; }
    public int PincodeAttempts { get; set; }
    public uint ClientType { get; set; }
    public int GroupId { get; set; }
    public int CharacterSlots { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Birthdate { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
    public long PincodeChangeUnixTime { get; set; }
    public bool IsVip { get; set; }
    public int VipCharacterSlots { get; set; }
    public int BillingCharacterSlots { get; set; }
}
