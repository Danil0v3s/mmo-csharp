namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// Acknowledge a vending-shop open request. rAthena <c>clif_openvending_ack</c> (clif.cpp, 0x0a28).
/// Fixed 3 bytes: <c>0a28 &lt;result&gt;.B</c>. Result 0 = success (rAthena <c>e_ack_openstore2</c>).
/// </summary>
public class ZC_ACK_OPENSTORE2 : OutgoingPacket
{
    private const int SIZE = 3;

    public byte Result { get; init; }

    public ZC_ACK_OPENSTORE2() : base(PacketHeader.ZC_ACK_OPENSTORE2, SIZE) { }

    public override void Write(BinaryWriter writer) => writer.Write(Result);
}
