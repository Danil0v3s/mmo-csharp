namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Create a party. rAthena <c>clif_parse_CreateParty</c> (clif.cpp) + <c>PACKET_CZ_MAKE_GROUP</c>.
/// Wire: <c>00f9 &lt;party name&gt;.24B</c> — 26 bytes.
/// </summary>
public class CZ_MAKE_GROUP : IncomingPacket
{
    private const int SIZE = 26;

    public string PartyName { get; private set; } = string.Empty;

    public CZ_MAKE_GROUP() : base(PacketHeader.CZ_MAKE_GROUP, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        PartyName = MailWire.ReadFixedString(reader, 24);
    }

    public static CZ_MAKE_GROUP Create(BinaryReader reader)
    {
        var packet = new CZ_MAKE_GROUP();
        packet.Read(reader);
        return packet;
    }
}
