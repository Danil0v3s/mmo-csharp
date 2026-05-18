namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// "Here's the NPC's name." rAthena <c>clif_name</c> for <c>BL_NPC</c>
/// (PACKETVER ≥ 20180207 variant). Fixed 58 bytes:
/// 0x0adf packet_id (2) + gid (4) + groupId (4) + name (24) + title (24).
///
/// <c>groupId</c> mirrors rAthena's <c>nd->ud.group_id</c> — used for
/// title-coloring in the client. 0 is the safe default.
/// <c>name</c> and <c>title</c> are null-padded ASCII, exactly 24 bytes each.
/// </summary>
public class ZC_ACK_REQNAMEALL_NPC : OutgoingPacket
{
    private const int NameLength = 24;
    private const int SIZE = sizeof(short) + sizeof(uint) + sizeof(uint) + NameLength + NameLength;

    public uint Gid { get; init; }
    public uint GroupId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;

    public ZC_ACK_REQNAMEALL_NPC() : base(PacketHeader.ZC_ACK_REQNAMEALL_NPC, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(Gid);
        writer.Write(GroupId);
        WriteFixedAsciz(writer, Name, NameLength);
        WriteFixedAsciz(writer, Title, NameLength);
    }

    private static void WriteFixedAsciz(BinaryWriter writer, string value, int length)
    {
        Span<byte> buf = stackalloc byte[length];
        var src = System.Text.Encoding.ASCII.GetBytes(value ?? string.Empty);
        var copy = Math.Min(src.Length, length - 1);  // reserve one for null
        src.AsSpan(0, copy).CopyTo(buf);
        // remaining bytes are already zero
        writer.Write(buf);
    }
}
