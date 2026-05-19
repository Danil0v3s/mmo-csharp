namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// Server delivers a whisper to the recipient. rAthena
/// <c>clif_wis_message</c>. Wire (PACKETVER ≥ 20131120 modern variant):
/// <c>09de &lt;len&gt;.W &lt;senderGID&gt;.L &lt;sender&gt;.24B
/// &lt;isAdmin&gt;.B &lt;message&gt;.?B</c>.
/// </summary>
public class ZC_WHISPER : OutgoingPacket
{
    private const int NameLength = 24;

    public int SenderAccountId { get; init; }
    public string SenderName { get; init; } = string.Empty;
    public byte IsAdmin { get; init; }
    public string Message { get; init; } = string.Empty;

    public ZC_WHISPER() : base(PacketHeader.ZC_WHISPER, -1) { }

    private byte[] MessageBytes() => System.Text.Encoding.UTF8.GetBytes(Message + "\0");

    public override int GetSize() => 2 + 2 + sizeof(int) + NameLength + 1 + MessageBytes().Length;

    public override void Write(BinaryWriter writer)
    {
        writer.Write(SenderAccountId);
        writer.Write(EncodeName(SenderName, NameLength));
        writer.Write(IsAdmin);
        writer.Write(MessageBytes());
    }

    private static byte[] EncodeName(string name, int length)
    {
        var bytes = new byte[length];
        var encoded = System.Text.Encoding.UTF8.GetBytes(name);
        Array.Copy(encoded, bytes, Math.Min(encoded.Length, length - 1));
        return bytes;
    }
}
