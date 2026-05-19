namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// Public-area chat broadcast. rAthena <c>clif_message</c> / WT pubic chat.
/// Wire shape (variable-length): <c>008d &lt;packet_len&gt;.W &lt;src_id&gt;.L &lt;message&gt;.?B</c>
/// where the message is the speaker's name + ": " + utterance, framed by the
/// server. Total: 2 (header) + 2 (length) + 4 (src id) + N (message) = 8+N bytes.
/// </summary>
public class ZC_NOTIFY_CHAT : OutgoingPacket
{
    public int SourceId { get; init; }
    /// <summary>Pre-formatted message body (typically "Name: text\0").</summary>
    public string Message { get; init; } = string.Empty;

    public ZC_NOTIFY_CHAT() : base(PacketHeader.ZC_NOTIFY_CHAT, -1) { }

    private byte[] MessageBytes()
        => System.Text.Encoding.UTF8.GetBytes(Message + "\0");

    public override int GetSize() => 2 + 2 + sizeof(int) + MessageBytes().Length;

    public override void Write(BinaryWriter writer)
    {
        // WritePacket already emitted the header + packet_len. Body only.
        writer.Write(SourceId);
        writer.Write(MessageBytes());
    }
}
