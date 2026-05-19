namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Player whispers another character. rAthena
/// <c>clif_parse_WisMessage</c> (clif.cpp:11835). Variable-length:
/// <c>0096 &lt;len&gt;.W &lt;nick&gt;.24B &lt;message&gt;.?B</c> where the
/// nickname is null-padded to 24 bytes.
/// </summary>
public class CZ_WHISPER : IncomingPacket
{
    private const int NameLength = 24;

    public string TargetName { get; private set; } = string.Empty;
    public string Text { get; private set; } = string.Empty;

    public CZ_WHISPER() : base(PacketHeader.CZ_WHISPER, -1) { }

    public override void Read(BinaryReader reader)
    {
        var bodyLength = (int)(reader.BaseStream.Length - reader.BaseStream.Position);
        if (bodyLength < NameLength) return;
        var nameBytes = reader.ReadBytes(NameLength);
        TargetName = TrimToNull(nameBytes);

        var msgLen = bodyLength - NameLength;
        if (msgLen <= 0) return;
        var msgBytes = reader.ReadBytes(msgLen);
        Text = TrimToNull(msgBytes);
    }

    private static string TrimToNull(byte[] bytes)
    {
        var nullAt = Array.IndexOf(bytes, (byte)0);
        var len = nullAt < 0 ? bytes.Length : nullAt;
        return System.Text.Encoding.UTF8.GetString(bytes, 0, len);
    }

    public static CZ_WHISPER Create(BinaryReader reader)
    {
        var packet = new CZ_WHISPER();
        packet.Read(reader);
        return packet;
    }
}
