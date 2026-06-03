namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// RODEX receiver-name acknowledgement. rAthena <c>clif_Mail_Receiver_Ack</c> (clif.cpp:16478) +
/// <c>PACKET_ZC_CHECKNAME</c> (0x0a14 variant). Wire:
/// <c>0a14 &lt;charId&gt;.L &lt;class&gt;.W &lt;baseLevel&gt;.W</c> — 10 bytes. <c>charId == 0</c> ⇒ no such recipient.
/// </summary>
public class ZC_CHECKNAME : OutgoingPacket
{
    private const int SIZE = 10;

    public int CharId { get; init; }
    public short Class { get; init; }
    public short BaseLevel { get; init; }

    public ZC_CHECKNAME() : base(PacketHeader.ZC_CHECKNAME, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(CharId);
        writer.Write(Class);
        writer.Write(BaseLevel);
    }
}
