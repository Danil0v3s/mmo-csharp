using Core.Server.Packets;

namespace Tools.PacketReplay.Decoders;

/// <summary>
/// Decoder for <c>ZC_ACCEPT_ENTER_ZONE</c> (0x02EB) — rAthena
/// <c>clif_authok</c> for PACKETVER ≥ 20160330. Fixed 13 bytes:
///
/// <code>
///   int16  packetType  2  ← header (skipped)
///   uint32 startTime   4  ← TOLERANT (gettick() — per-run)
///   uint8  posDir[3]   3  ← packed x/y/dir, decoded into named fields
///   uint8  xSize       1
///   uint8  ySize       1
///   int16  font        2
/// </code>
///
/// posDir is unpacked via the inverse of rAthena's WBUFPOS:
///   x   = (p[0] &lt;&lt; 2) | ((p[1] &gt;&gt; 6) &amp; 3)
///   y   = ((p[1] &amp; 0x3f) &lt;&lt; 4) | ((p[2] &gt;&gt; 4) &amp; 0xf)
///   dir = p[2] &amp; 0xf
/// </summary>
public sealed class ZcAcceptEnterZoneDecoder : IPacketDecoder
{
    public PacketHeader Header => PacketHeader.ZC_ACCEPT_ENTER_ZONE;

    public DecodedPacket Decode(byte[] packetBytes)
    {
        using var ms = new MemoryStream(packetBytes);
        using var r = new BinaryReader(ms);
        r.ReadInt16(); // packetType

        var startTime = r.ReadUInt32();
        var p0 = r.ReadByte();
        var p1 = r.ReadByte();
        var p2 = r.ReadByte();
        var xSize = r.ReadByte();
        var ySize = r.ReadByte();
        var font = r.ReadInt16();

        var x = (p0 << 2) | ((p1 >> 6) & 0x3);
        var y = ((p1 & 0x3f) << 4) | ((p2 >> 4) & 0xf);
        var dir = p2 & 0xf;

        var fields = new List<DecodedField>
        {
            new("StartTime", startTime, Tolerant: true),
            // X/Y are tolerant because rAthena's pc_setpos randomizes the
            // spawn cell when the saved (last_x, last_y) is OOB or
            // non-walkable on the loaded map — neither value is reproducible
            // across runs. Dir/XSize/YSize/Font remain strict.
            new("X", x, Tolerant: true),
            new("Y", y, Tolerant: true),
            new("Dir", dir),
            new("XSize", xSize),
            new("YSize", ySize),
            new("Font", font),
        };

        return new DecodedPacket(Header, fields);
    }
}
