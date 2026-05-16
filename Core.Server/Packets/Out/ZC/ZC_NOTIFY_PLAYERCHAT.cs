namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// "System message — only the recipient sees it." rAthena
/// <c>clif_displaymessage</c> (packet name <c>ZC_NOTIFY_PLAYERCHAT</c>).
/// Variable-length: 0x008e packet_id (2) + packet_len (2) + message (?)
/// where the message is ASCII null-terminated. Used for GM-command
/// feedback, "you cannot use this here" gates, and other server-to-self
/// notices.
/// </summary>
public class ZC_NOTIFY_PLAYERCHAT : OutgoingPacket
{
    public string Message { get; init; } = string.Empty;

    public ZC_NOTIFY_PLAYERCHAT() : base(PacketHeader.ZC_NOTIFY_PLAYERCHAT, -1) { }

    public override bool HasPacketLength => true;

    public override int GetSize()
    {
        // packet_id (2) + packet_len (2) + body (msg + NUL)
        var bodyLen = System.Text.Encoding.ASCII.GetByteCount(Message ?? string.Empty) + 1;
        return 4 + bodyLen;
    }

    public override void Write(BinaryWriter writer)
    {
        // Base already emitted packet_id + packet_len. Body is the message
        // bytes followed by a single null terminator (rAthena clients
        // expect strtok-style null termination).
        var bytes = System.Text.Encoding.ASCII.GetBytes(Message ?? string.Empty);
        writer.Write(bytes);
        writer.Write((byte)0);
    }
}
