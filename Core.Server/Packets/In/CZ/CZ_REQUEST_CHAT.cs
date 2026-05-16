namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Global chat / GM command. rAthena <c>clif_parse_GlobalMessage</c>.
/// Variable-length: 0x008c packet_id (2) + packet_len (2) + text (?) where
/// the text is the format <c>"&lt;name&gt; : &lt;message&gt;\0"</c>.
///
/// GM commands are signaled by the first character of <c>message</c> being
/// either <c>@</c> or <c>#</c> (per rAthena <c>atcommand_symbol</c> /
/// <c>charcommand_symbol</c>); routing decision lives in
/// <c>ChatMessageHandler</c>.
/// </summary>
public class CZ_REQUEST_CHAT : IncomingPacket
{
    /// <summary>Raw chat line, including the "name : message" prefix.</summary>
    public string Text { get; private set; } = string.Empty;

    public CZ_REQUEST_CHAT() : base(PacketHeader.CZ_REQUEST_CHAT, -1) { }

    public override void Read(BinaryReader reader)
    {
        // PacketBuffer already consumed the 4-byte prefix (header + length).
        // What remains is the text payload, typically ASCII null-terminated.
        var bodyLength = (int)(reader.BaseStream.Length - reader.BaseStream.Position);
        if (bodyLength <= 0) return;
        var bytes = reader.ReadBytes(bodyLength);
        // Trim trailing nulls; rAthena pads / null-terminates.
        var nullAt = Array.IndexOf(bytes, (byte)0);
        var len = nullAt < 0 ? bytes.Length : nullAt;
        Text = System.Text.Encoding.ASCII.GetString(bytes, 0, len);
    }

    public static CZ_REQUEST_CHAT Create(BinaryReader reader)
    {
        var packet = new CZ_REQUEST_CHAT();
        packet.Read(reader);
        return packet;
    }
}
