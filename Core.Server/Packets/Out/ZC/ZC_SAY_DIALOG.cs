namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// One line of NPC dialog text. rAthena <c>clif_scriptmes</c>.
/// Variable-length, pre-20220504 shape:
/// 0x00b4 packet_id (2) + packet_len (2) + npcId (4) + message (?).
/// Message is ASCII, null-terminated.
///
/// dhxj has 0x00b4 in its packet length table (correctly parses the
/// length) but doesn't appear to render the dialog from it. Capturing
/// what the real server sends will tell us what the client actually
/// wants.
/// </summary>
public class ZC_SAY_DIALOG : OutgoingPacket
{
    public uint NpcId { get; init; }
    public string Message { get; init; } = string.Empty;

    public ZC_SAY_DIALOG() : base(PacketHeader.ZC_SAY_DIALOG, -1) { }

    public override bool HasPacketLength => true;

    public override int GetSize()
    {
        var bodyLen = System.Text.Encoding.ASCII.GetByteCount(Message ?? string.Empty) + 1;
        return 8 + bodyLen;
    }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(NpcId);
        var bytes = System.Text.Encoding.ASCII.GetBytes(Message ?? string.Empty);
        writer.Write(bytes);
        writer.Write((byte)0);
    }
}
