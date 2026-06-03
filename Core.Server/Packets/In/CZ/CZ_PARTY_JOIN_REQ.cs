namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Invite a player to your party by character name. rAthena <c>clif_parse_PartyInvite2</c>
/// (clif.cpp) + <c>PACKET_CZ_PARTY_JOIN_REQ</c>. Wire: <c>0802 &lt;character name&gt;.24B</c> — 26 bytes.
/// </summary>
public class CZ_PARTY_JOIN_REQ : IncomingPacket
{
    private const int SIZE = 26;

    public string TargetName { get; private set; } = string.Empty;

    public CZ_PARTY_JOIN_REQ() : base(PacketHeader.CZ_PARTY_JOIN_REQ, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        TargetName = MailWire.ReadFixedString(reader, 24);
    }

    public static CZ_PARTY_JOIN_REQ Create(BinaryReader reader)
    {
        var packet = new CZ_PARTY_JOIN_REQ();
        packet.Read(reader);
        return packet;
    }
}
