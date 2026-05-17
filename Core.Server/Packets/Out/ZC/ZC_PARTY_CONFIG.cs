namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_partyinvitationstate</c> [clif.cpp:7909]. Sent on
/// login so the client UI reflects the saved "deny party invites"
/// setting. Fixed 3 bytes:
/// <c>0x02c9 packet_id (2) + denyPartyInvites (1)</c>.
/// </summary>
public class ZC_PARTY_CONFIG : OutgoingPacket
{
    private const int SIZE = sizeof(short) + sizeof(byte);

    public byte DenyPartyInvites { get; init; }

    public ZC_PARTY_CONFIG() : base(PacketHeader.ZC_PARTY_CONFIG, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(DenyPartyInvites);
    }
}
