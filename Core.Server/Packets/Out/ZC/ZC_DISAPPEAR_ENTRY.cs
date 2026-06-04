namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// Remove a vending stall sign. rAthena <c>clif_closevendingboard</c> (clif.cpp, 0x0132). Fixed 6
/// bytes: <c>0132 &lt;owner id&gt;.L</c>. Broadcast to the area when the shop closes.
/// </summary>
public class ZC_DISAPPEAR_ENTRY : OutgoingPacket
{
    private const int SIZE = 2 + 4; // 6

    public uint OwnerId { get; init; }

    public ZC_DISAPPEAR_ENTRY() : base(PacketHeader.ZC_DISAPPEAR_ENTRY, SIZE) { }

    public override void Write(BinaryWriter writer) => writer.Write(OwnerId);
}
