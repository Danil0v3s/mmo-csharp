namespace Core.Server.Packets.Out.HC;

public class HC_CHARLIST_NOTIFY : OutgoingPacket
{
    public int TotalPages { get; init; }
    public int CharSlots { get; init; }

    public HC_CHARLIST_NOTIFY(PacketHeader header) : base(header, sizeof(short) + sizeof(int)) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(TotalPages);
    }

    public override int GetSize()
    {
        return sizeof(short) + sizeof(int); // packetType + totalPages
    }
}
