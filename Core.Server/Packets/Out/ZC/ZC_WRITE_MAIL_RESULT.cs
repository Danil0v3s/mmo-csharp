namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// RODEX send result. rAthena <c>clif_Mail_send</c> (clif.cpp:15940) +
/// <c>PACKET_ZC_WRITE_MAIL_RESULT</c> (rodexwriteresult). Wire: <c>09ed &lt;result&gt;.B</c> — 3 bytes.
/// <c>result</c>: 0 = success (rAthena <c>WRITE_MAIL_SUCCESS</c>), non-zero = failed.
/// </summary>
public class ZC_WRITE_MAIL_RESULT : OutgoingPacket
{
    public const byte Success = 0;
    public const byte Failed = 1;

    private const int SIZE = 3;

    public byte Result { get; init; }

    public ZC_WRITE_MAIL_RESULT() : base(PacketHeader.ZC_WRITE_MAIL_RESULT, SIZE) { }

    public override void Write(BinaryWriter writer) => writer.Write(Result);
}
