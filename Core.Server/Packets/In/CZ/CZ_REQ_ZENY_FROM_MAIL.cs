namespace Core.Server.Packets.In.CZ;

/// <summary>
/// RODEX: claim a mail's attached zeny. rAthena
/// <c>clif_parse_Mail_getattach</c> + <c>PACKET_CZ_REQ_ZENY_FROM_MAIL</c>
/// (packets_struct.hpp). Wire: <c>09f1 &lt;mailID&gt;.Q &lt;opentype&gt;.B</c> — 11 bytes.
/// </summary>
public class CZ_REQ_ZENY_FROM_MAIL : IncomingPacket
{
    private const int SIZE = 11;

    public long MailId { get; private set; }
    public byte OpenType { get; private set; }

    public CZ_REQ_ZENY_FROM_MAIL() : base(PacketHeader.CZ_REQ_ZENY_FROM_MAIL, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        MailId = reader.ReadInt64();
        OpenType = reader.ReadByte();
    }

    public static CZ_REQ_ZENY_FROM_MAIL Create(BinaryReader reader)
    {
        var packet = new CZ_REQ_ZENY_FROM_MAIL();
        packet.Read(reader);
        return packet;
    }
}
