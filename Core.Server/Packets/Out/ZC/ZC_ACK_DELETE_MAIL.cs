namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// RODEX: delete-mail acknowledgement. rAthena <c>clif_mail_delete</c> +
/// <c>PACKET_ZC_ACK_DELETE_MAIL</c> (packets_struct.hpp). Wire:
/// <c>09f6 &lt;opentype&gt;.B &lt;mailID&gt;.Q</c> — 11 bytes. rAthena only emits this
/// on success (the client removes the row).
/// </summary>
public class ZC_ACK_DELETE_MAIL : OutgoingPacket
{
    private const int SIZE = 11;

    public byte OpenType { get; init; }
    public long MailId { get; init; }

    public ZC_ACK_DELETE_MAIL() : base(PacketHeader.ZC_ACK_DELETE_MAIL, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(OpenType);
        writer.Write(MailId);
    }
}
