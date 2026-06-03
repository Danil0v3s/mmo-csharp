namespace Core.Server.Packets.In.CZ;

/// <summary>
/// RODEX: read one mail. rAthena <c>clif_parse_Mail_read</c> (clif.cpp:16422) +
/// <c>PACKET_CZ_REQ_READ_MAIL</c> (packets_struct.hpp). Wire:
/// <c>09ea &lt;opentype&gt;.B &lt;mailID&gt;.Q</c> — 11 bytes.
/// </summary>
public class CZ_REQ_READ_MAIL : IncomingPacket
{
    private const int SIZE = 11;

    public byte OpenType { get; private set; }
    public long MailId { get; private set; }

    public CZ_REQ_READ_MAIL() : base(PacketHeader.CZ_REQ_READ_MAIL, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        OpenType = reader.ReadByte();
        MailId = reader.ReadInt64();
    }

    public static CZ_REQ_READ_MAIL Create(BinaryReader reader)
    {
        var packet = new CZ_REQ_READ_MAIL();
        packet.Read(reader);
        return packet;
    }
}
