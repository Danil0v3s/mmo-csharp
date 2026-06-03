namespace Core.Server.Packets.In.CZ;

/// <summary>
/// RODEX: check a recipient name. rAthena <c>clif_parse_Mail_Receiver_Check</c> (clif.cpp:16497) +
/// <c>PACKET_CZ_CHECKNAME1</c>. Wire: <c>0a13 &lt;name&gt;.24B</c> — 26 bytes.
/// </summary>
public class CZ_CHECKNAME : IncomingPacket
{
    private const int SIZE = 26;

    public string Name { get; private set; } = string.Empty;

    public CZ_CHECKNAME() : base(PacketHeader.CZ_CHECKNAME, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        Name = MailWire.ReadFixedString(reader, 24);
    }

    public static CZ_CHECKNAME Create(BinaryReader reader)
    {
        var packet = new CZ_CHECKNAME();
        packet.Read(reader);
        return packet;
    }
}
