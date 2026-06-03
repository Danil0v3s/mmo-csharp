using System.Text;

namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// RODEX begin-write acknowledgement. rAthena <c>clif_send_Mail_beginwrite_ack</c> +
/// <c>PACKET_ZC_ACK_OPEN_WRITE_MAIL</c> (rodexopenwrite). Wire:
/// <c>0a12 &lt;receiveName&gt;.24B &lt;result&gt;.B</c> — 27 bytes. <c>result</c>: 1 = ok to write, 0 = busy.
/// </summary>
public class ZC_ACK_OPEN_WRITE_MAIL : OutgoingPacket
{
    private const int SIZE = 27;
    private const int NameLength = 24;

    public string ReceiveName { get; init; } = string.Empty;
    public bool Ok { get; init; }

    public ZC_ACK_OPEN_WRITE_MAIL() : base(PacketHeader.ZC_ACK_OPEN_WRITE_MAIL, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        var bytes = Encoding.ASCII.GetBytes(ReceiveName ?? string.Empty);
        var buf = new byte[NameLength];
        Array.Copy(bytes, buf, Math.Min(bytes.Length, NameLength - 1));
        writer.Write(buf);
        writer.Write((byte)(Ok ? 1 : 0));
    }
}
