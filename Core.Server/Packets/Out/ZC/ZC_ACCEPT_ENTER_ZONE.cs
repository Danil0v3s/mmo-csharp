namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// Map auth accepted. rAthena <c>clif_authok</c>. PACKETVER >= 20160330 uses
/// the 13-byte variant with a font field:
///
/// <code>
///   0x02eb packet_id (2) + startTime (4) + posDir (3) + xSize (1) + ySize (1) + font (2) = 13 bytes
/// </code>
///
/// The packed posDir uses the same encoding as <c>CZ_REQUEST_MOVE</c>
/// (10 bits x, 10 bits y, 4 bits dir).
/// </summary>
public class ZC_ACCEPT_ENTER_ZONE : OutgoingPacket
{
    private const int SIZE = 13;

    public uint StartTime { get; init; }
    public short X { get; init; }
    public short Y { get; init; }
    public byte Dir { get; init; }
    public short Font { get; init; }

    public ZC_ACCEPT_ENTER_ZONE() : base(PacketHeader.ZC_ACCEPT_ENTER_ZONE, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(StartTime);
        PositionPacker.WritePos(writer, X, Y, Dir);
        writer.Write((byte)5); // xSize — ignored by rAthena clients
        writer.Write((byte)5); // ySize — ignored
        writer.Write(Font);
    }
}
