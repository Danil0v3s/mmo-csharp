namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Walk request. rAthena <c>clif_parse_WalkToXY</c>. The on-wire payload is a
/// 3-byte packed position (x, y, dir) — mirrors WBUFPOS encoding:
///
/// <code>
///   byte 0 = (x &gt;&gt; 2) &amp; 0xff
///   byte 1 = ((x &amp; 0x3) &lt;&lt; 6) | ((y &gt;&gt; 4) &amp; 0x3f)
///   byte 2 = ((y &amp; 0xf) &lt;&lt; 4) | (dir &amp; 0xf)
/// </code>
///
/// Total: 0x0085 packet_id (2) + posBytes (3) = 5 bytes.
/// </summary>
public class CZ_REQUEST_MOVE : IncomingPacket
{
    private const int SIZE = 5;

    public short TargetX { get; private set; }
    public short TargetY { get; private set; }
    public byte Dir { get; private set; }

    public CZ_REQUEST_MOVE() : base(PacketHeader.CZ_REQUEST_MOVE, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        var b0 = reader.ReadByte();
        var b1 = reader.ReadByte();
        var b2 = reader.ReadByte();
        TargetX = (short)((b0 << 2) | ((b1 >> 6) & 0x3));
        TargetY = (short)(((b1 & 0x3f) << 4) | ((b2 >> 4) & 0xf));
        Dir = (byte)(b2 & 0xf);
    }

    public static CZ_REQUEST_MOVE Create(BinaryReader reader)
    {
        var packet = new CZ_REQUEST_MOVE();
        packet.Read(reader);
        return packet;
    }
}
