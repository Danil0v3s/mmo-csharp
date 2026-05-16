namespace Core.Server.Packets;

/// <summary>
/// Packs/unpacks the 3-byte "PosDir" / 6-byte "Move" coordinate blobs used
/// across the Ragnarok protocol. Mirrors rAthena's <c>WBUFPOS</c> /
/// <c>WBUFPOS2</c> / <c>RBUFPOS</c> macros (clif.cpp).
///
/// <para>3-byte single position (x, y, dir):
/// <code>
///   byte 0 = (x &gt;&gt; 2) &amp; 0xff
///   byte 1 = ((x &amp; 0x3) &lt;&lt; 6) | ((y &gt;&gt; 4) &amp; 0x3f)
///   byte 2 = ((y &amp; 0xf) &lt;&lt; 4) | (dir &amp; 0xf)
/// </code></para>
///
/// <para>6-byte move (from x0,y0 to x1,y1 + sx,sy of unit):
/// <code>
///   byte 0 = (x0 &gt;&gt; 2) &amp; 0xff
///   byte 1 = ((x0 &amp; 0x3) &lt;&lt; 6) | ((y0 &gt;&gt; 4) &amp; 0x3f)
///   byte 2 = ((y0 &amp; 0xf) &lt;&lt; 4) | ((x1 &gt;&gt; 6) &amp; 0xf)
///   byte 3 = ((x1 &amp; 0x3f) &lt;&lt; 2) | ((y1 &gt;&gt; 8) &amp; 0x3)
///   byte 4 = (y1 &amp; 0xff)
///   byte 5 = (sx &amp; 0xf) &lt;&lt; 4 | (sy &amp; 0xf)
/// </code></para>
/// </summary>
public static class PositionPacker
{
    /// <summary>Write a 3-byte packed (x, y, dir) into the stream.</summary>
    public static void WritePos(BinaryWriter writer, short x, short y, byte dir)
    {
        writer.Write((byte)((x >> 2) & 0xff));
        writer.Write((byte)(((x & 0x3) << 6) | ((y >> 4) & 0x3f)));
        writer.Write((byte)(((y & 0xf) << 4) | (dir & 0xf)));
    }

    /// <summary>Read a 3-byte packed (x, y, dir) from the stream.</summary>
    public static (short X, short Y, byte Dir) ReadPos(BinaryReader reader)
    {
        var b0 = reader.ReadByte();
        var b1 = reader.ReadByte();
        var b2 = reader.ReadByte();
        var x = (short)((b0 << 2) | ((b1 >> 6) & 0x3));
        var y = (short)(((b1 & 0x3f) << 4) | ((b2 >> 4) & 0xf));
        var dir = (byte)(b2 & 0xf);
        return (x, y, dir);
    }

    /// <summary>
    /// Write a 6-byte packed move (start + end cell + sub-cell offsets) into the stream.
    /// Sub-cell offsets are 0/0 for cardinal-aligned movement.
    /// </summary>
    public static void WriteMove(BinaryWriter writer, short x0, short y0, short x1, short y1, byte sx = 0, byte sy = 0)
    {
        writer.Write((byte)((x0 >> 2) & 0xff));
        writer.Write((byte)(((x0 & 0x3) << 6) | ((y0 >> 4) & 0x3f)));
        writer.Write((byte)(((y0 & 0xf) << 4) | ((x1 >> 6) & 0xf)));
        writer.Write((byte)(((x1 & 0x3f) << 2) | ((y1 >> 8) & 0x3)));
        writer.Write((byte)(y1 & 0xff));
        writer.Write((byte)(((sx & 0xf) << 4) | (sy & 0xf)));
    }

    /// <summary>Read a 6-byte packed move from the stream.</summary>
    public static (short X0, short Y0, short X1, short Y1, byte Sx, byte Sy) ReadMove(BinaryReader reader)
    {
        var b0 = reader.ReadByte();
        var b1 = reader.ReadByte();
        var b2 = reader.ReadByte();
        var b3 = reader.ReadByte();
        var b4 = reader.ReadByte();
        var b5 = reader.ReadByte();
        var x0 = (short)((b0 << 2) | ((b1 >> 6) & 0x3));
        var y0 = (short)(((b1 & 0x3f) << 4) | ((b2 >> 4) & 0xf));
        var x1 = (short)(((b2 & 0xf) << 6) | ((b3 >> 2) & 0x3f));
        var y1 = (short)(((b3 & 0x3) << 8) | b4);
        var sx = (byte)((b5 >> 4) & 0xf);
        var sy = (byte)(b5 & 0xf);
        return (x0, y0, x1, y1, sx, sy);
    }
}
