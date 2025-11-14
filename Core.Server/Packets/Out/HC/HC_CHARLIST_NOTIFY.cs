namespace Core.Server.Packets.Out.HC;

public class HC_CHARLIST_NOTIFY : OutgoingPacket
{
    public int TotalPages { get; init; }
    public int CharSlots { get; init; }

    public HC_CHARLIST_NOTIFY(PacketHeader header) : base(header, true) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write((short)Header);
        writer.Write(TotalPages);

        // If PACKETVER >= 20151001 && PACKETVER < 20180103, also write char slots
        if (Header == PacketHeader.HC_CHARLIST_NOTIFY) // The header value may have conditional logic not captured here
        {
            writer.Write(CharSlots);
        }
    }

    public override int GetSize()
    {
        int size = sizeof(short) + sizeof(int); // packetType + totalPages
        if (Header == PacketHeader.HC_CHARLIST_NOTIFY)
        {
            size += sizeof(int); // + charSlots
        }
        return size;
    }
}