namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// "Here's the player's name (and party / guild)." rAthena <c>clif_name</c>
/// for <c>BL_PC</c> (PACKETVER ≥ 20150225 variant). Fixed 106 bytes:
/// 0x0a30 packet_id (2) + gid (4) + char_name (24) + party_name (24) +
/// guild_name (24) + position_name (24) + titleId (4).
///
/// MS1 / Phase 3 emits just the character name; the rest are blank until
/// party / guild / title systems land. The packet still has to be the
/// full 106 bytes so the client doesn't desync.
/// </summary>
public class ZC_ACK_REQNAMEALL : OutgoingPacket
{
    private const int NameLength = 24;
    private const int SIZE = sizeof(short) + sizeof(uint)
                           + (NameLength * 4)
                           + sizeof(uint);

    public uint Gid { get; init; }
    public string CharName { get; init; } = string.Empty;
    public string PartyName { get; init; } = string.Empty;
    public string GuildName { get; init; } = string.Empty;
    public string PositionName { get; init; } = string.Empty;
    public uint TitleId { get; init; }

    public ZC_ACK_REQNAMEALL() : base(PacketHeader.ZC_ACK_REQNAMEALL, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(Gid);
        WriteFixedAsciz(writer, CharName, NameLength);
        WriteFixedAsciz(writer, PartyName, NameLength);
        WriteFixedAsciz(writer, GuildName, NameLength);
        WriteFixedAsciz(writer, PositionName, NameLength);
        writer.Write(TitleId);
    }

    private static void WriteFixedAsciz(BinaryWriter writer, string value, int length)
    {
        Span<byte> buf = stackalloc byte[length];
        var src = System.Text.Encoding.ASCII.GetBytes(value ?? string.Empty);
        var copy = Math.Min(src.Length, length - 1);
        src.AsSpan(0, copy).CopyTo(buf);
        writer.Write(buf);
    }
}
