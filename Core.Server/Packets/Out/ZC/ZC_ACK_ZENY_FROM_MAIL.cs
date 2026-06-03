namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// RODEX: zeny-attachment claim result. rAthena <c>clif_mail_getattachment</c> +
/// <c>PACKET_ZC_ACK_ZENY_FROM_MAIL</c> (packets_struct.hpp). Wire:
/// <c>09f2 &lt;mailID&gt;.Q &lt;opentype&gt;.B &lt;result&gt;.B</c> — 12 bytes.
/// <c>result</c>: 0 = success, 1 = error.
/// </summary>
public class ZC_ACK_ZENY_FROM_MAIL : OutgoingPacket
{
    public const byte Success = 0;
    public const byte Error = 1;

    private const int SIZE = 12;

    public long MailId { get; init; }
    public byte OpenType { get; init; }
    public byte Result { get; init; }

    public ZC_ACK_ZENY_FROM_MAIL() : base(PacketHeader.ZC_ACK_ZENY_FROM_MAIL, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(MailId);
        writer.Write(OpenType);
        writer.Write(Result);
    }
}
