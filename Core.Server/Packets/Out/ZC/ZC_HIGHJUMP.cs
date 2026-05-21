namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_slide</c> ([clif.cpp]) — visually slides an entity
/// to (x, y) without playing the walk animation. Used for knockback,
/// Backslide, Body Relocation, Pressure pull, Charge, and any other
/// instant displacement.
///
/// Wire shape (<c>0x01ff</c>, 10 bytes fixed):
/// <code>
///   0x01ff (2) + srcId (4) + x (2) + y (2)
/// </code>
///
/// Pairs with <see cref="ZC_STOPMOVE"/> (<c>clif_fixpos</c>) — knockback
/// emits both: the slide visualizes the motion, the stopmove locks
/// the new authoritative position.
/// </summary>
public class ZC_HIGHJUMP : OutgoingPacket
{
    private const int SIZE = 10;

    public int SrcId { get; init; }
    public short X { get; init; }
    public short Y { get; init; }

    public ZC_HIGHJUMP() : base(PacketHeader.ZC_HIGHJUMP, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(SrcId);
        writer.Write(X);
        writer.Write(Y);
    }
}
