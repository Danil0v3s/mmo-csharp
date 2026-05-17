namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena coloured-broadcast packet (`intif_broadcast2`). Variable
/// length: header (2) + packetLength (2) + fontColor (4) + fontType (2)
/// + fontSize (2) + fontAlign (2) + fontY (2) + message[].
/// </summary>
public class ZC_BROADCAST2 : OutgoingPacket
{
    public uint FontColor { get; init; }
    public short FontType { get; init; }
    public short FontSize { get; init; }
    public short FontAlign { get; init; }
    public short FontY { get; init; }
    public string Message { get; init; } = string.Empty;

    public ZC_BROADCAST2() : base(PacketHeader.ZC_BROADCAST2, -1) { }

    public override int GetSize()
    {
        // packetType + packetLength + 14 bytes of font header + message + null
        return sizeof(short) + sizeof(short) + 14
             + System.Text.Encoding.ASCII.GetByteCount(Message ?? string.Empty) + 1;
    }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(FontColor);
        writer.Write(FontType);
        writer.Write(FontSize);
        writer.Write(FontAlign);
        writer.Write(FontY);
        var bytes = System.Text.Encoding.ASCII.GetBytes(Message ?? string.Empty);
        writer.Write(bytes);
        writer.Write((byte)0);
    }
}
