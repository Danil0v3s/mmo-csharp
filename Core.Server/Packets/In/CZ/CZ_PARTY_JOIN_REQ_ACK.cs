namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Client reply to a party invite popup. rAthena
/// <c>clif_parse_ReplyPartyInvite</c> (clif.cpp:11860) →
/// <c>party_reply_invite</c> (party.cpp:559). Fixed 7 bytes:
/// <c>0x02c7 &lt;party id&gt;.L &lt;flag&gt;.B</c> where
/// <c>flag = 0</c> means refuse, <c>flag = 1</c> means accept. The
/// invited player's session is the one that sent this; the handler
/// looks up the inviter via the party-id and a server-side pending
/// invite map.
/// </summary>
public class CZ_PARTY_JOIN_REQ_ACK : IncomingPacket
{
    private const int SIZE = 7;

    public uint PartyId { get; private set; }
    /// <summary>0 = refuse, 1 = accept (rAthena party.cpp:566).</summary>
    public byte Flag { get; private set; }

    public CZ_PARTY_JOIN_REQ_ACK() : base(PacketHeader.CZ_PARTY_JOIN_REQ_ACK, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        PartyId = reader.ReadUInt32();
        Flag = reader.ReadByte();
    }

    public static CZ_PARTY_JOIN_REQ_ACK Create(BinaryReader reader)
    {
        var packet = new CZ_PARTY_JOIN_REQ_ACK();
        packet.Read(reader);
        return packet;
    }
}
