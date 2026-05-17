namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>ZC_ITEM_THROW_ACK</c> (0x00AF, [packets.hpp:815]).
/// Fixed 6 bytes: header (2) + index (2) + count (2). The captured
/// rAthena emits this on login for reasons not yet traced (possibly
/// a script trigger or item-related status init).
/// </summary>
public class ZC_ITEM_THROW_ACK : OutgoingPacket
{
    private const int SIZE = sizeof(short) + sizeof(ushort) + sizeof(ushort);

    public ushort Index { get; init; }
    public ushort Count { get; init; }

    public ZC_ITEM_THROW_ACK() : base(PacketHeader.ZC_ITEM_THROW_ACK, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(Index);
        writer.Write(Count);
    }
}
